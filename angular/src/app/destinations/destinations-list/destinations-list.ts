import { Component, OnInit } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { ToasterService } from '@abp/ng.theme.shared';
import { ConfigStateService } from '@abp/ng.core';

import { DestinationService, FavoriteDestinationService, DestinationRatingService } from '@proxy/destinations';
import { CityDto, CitySearchRequestDto, DestinationDto, DestinationRatingDto, DestinationEventDto } from '@proxy/application/contracts/destinations/models';

/// Componente principal para listar, buscar y calificar destinos turísticos.
/// Permite buscar ciudades, agregarlas a favoritos, ver eventos de Ticketmaster y calificarlas.
@Component({
  selector: 'app-destinations-list',
  standalone: false,
  templateUrl: './destinations-list.component.html',
  styleUrls: ['./destinations-list.component.scss']
})
export class DestinationsListComponent implements OnInit {

  // Variables de estado
  cities: any[] = [];
  savedDestinations: DestinationDto[] = [];
  isLoading = false;
  showFilters = false;
  selectedCity: any = null;
  showModal = false;
  
  // Mapeo de eventos (Ticketmaster) por ID de destino
  eventsMap: { [key: string]: DestinationEventDto[] } = {};
  expandedEvents: { [key: string]: boolean } = {};

  popularDestinations: DestinationDto[] = [];
  selectedDestinationRatings: DestinationRatingDto[] = [];
  averageRating = 0;
  userRating: DestinationRatingDto | null = null;
  newRating = { score: 5, comment: '' };
  isRatingDraft = false;
  currentUserId: string | null = null;

  // Filtros de búsqueda
  searchParams = {
    query: '',
    country: '',
    region: '',
    minPopulation: null as number | null
  };

  private searchSubject = new Subject<string>();

  constructor(
    private destinationService: DestinationService, // Proxies auto-generados por ABP
    private favoriteService: FavoriteDestinationService,
    private ratingService: DestinationRatingService,
    private toasterService: ToasterService,
    private configState: ConfigStateService
  ) { 
    // Obtiene el ID del usuario actualmente autenticado desde el estado global de ABP
    this.currentUserId = this.configState.getDeep('currentUser.id');
  }

  ngOnInit(): void {
    this.loadSavedDestinations();
    this.loadPopularDestinations();

    // Configura búsqueda reactiva con delay (debounce) para no saturar el servidor al escribir
    this.searchSubject.pipe(
      debounceTime(1200),
      distinctUntilChanged()
    ).subscribe((searchTerm) => {
      this.executeSearch(searchTerm);
    });
  }

  onSearchChange(): void {
    this.searchSubject.next(this.searchParams.query);
  }

  onSearch(): void {
    this.executeSearch(this.searchParams.query);
  }

  /// Ejecuta la búsqueda de ciudades utilizando la API de GeoDB a través del backend
  private executeSearch(query: string): void {
    if (this.isLoading) return;
    if (!query && !this.searchParams.country && !this.searchParams.region) return;

    this.isLoading = true;

    const request: CitySearchRequestDto = {
      partialName: query,
      limit: 10,
      countryCode: this.searchParams.country || undefined,
      regionCode: this.searchParams.region || undefined,
      minPopulation: this.searchParams.minPopulation || undefined
    };

    this.destinationService.searchCities(request)
      .pipe(finalize(() => this.isLoading = false))
      .subscribe({
        next: (result: any) => {
          this.cities = result.cities || result.items || [];
          this.fetchUnsplashImages();
          // Carga los recitales/eventos de Ticketmaster de inmediato para cada ciudad encontrada
          this.cities.forEach(city => this.loadEventsForDestination(city));
        },
        error: (err) => {
          console.error('Error:', err);
          this.toasterService.error('Error al buscar ciudades', 'Error');
          this.cities = [];
        }
      });
  }

  /// Asigna imágenes de Unsplash aleatorias pero coherentes para cada ciudad
  private fetchUnsplashImages() {
    const curatedPhotoIds = [
      '1502602898657-3e91760cbb34', '1449156001566-35957096fb91', '1477959858617-67f85cf4f1df',
      '1513635269975-59663e0ac1ad', '1501594907352-0dfc58eb36fe', '1552832230-019623e618aa',
      '1523482580672-f109ba8cb9be', '1520117147647-0349b22ef104', '1493333858332-df7ed0ec419e',
      '1512100353987-0b19280d4607', '1533105079780-92b9be482077', '1464822759023-fed622ff2c3b',
      '1503389158882-9366144bea98', '1480714378408-67cf0d13bc1b', '1518684079-3c830dcef090',
      '1529156069912-ab0023a73e1c', '1507525428034-b723cf961d3e', '1519501025264-65ba15a82390',
      '1534447677768-be436bb09401', '1517154421773-0529f29ea451'
    ];

    for (let i = 0; i < this.cities.length; i++) {
      const city = this.cities[i];
      const charSum = city.name.split('').reduce((acc, char) => acc + char.charCodeAt(0), 0);
      const photoId = curatedPhotoIds[charSum % curatedPhotoIds.length];
      city.wikiImage = `https://images.unsplash.com/photo-${photoId}?auto=format&fit=crop&w=800&q=80`;
    }
  }

  /// Carga los destinos mejor calificados por los usuarios (populares)
  loadPopularDestinations(): void {
    this.destinationService.getList({ maxResultCount: 20 })
      .subscribe(result => {
        const items = (result as any).items || result || [];
        this.popularDestinations = items
          .sort((a, b) => (b.averageRating || 0) - (a.averageRating || 0))
          .slice(0, 4);

        if (this.popularDestinations.length < 4) {
          const others = items.filter(d => !this.popularDestinations.find(p => p.id === d.id)).slice(0, 4 - this.popularDestinations.length);
          this.popularDestinations = [...this.popularDestinations, ...others];
        }

        this.popularDestinations.forEach(d => this.loadEventsForDestination(d));
      });
  }

  /// Abre el modal de detalle de la ciudad.
  /// Intenta buscar una descripción en tiempo real directamente desde Wikipedia en español.
  async openCityDetails(city: any) {
    this.selectedCity = city;
    this.showModal = true;
    this.selectedDestinationRatings = [];
    this.averageRating = 0;
    this.userRating = null;
    this.isRatingDraft = false;

    // Verifica si la ciudad ya está guardada en favoritos
    const existingDest = this.savedDestinations.find(d => 
      (d.city === city.name || d.city === city.city) && d.country === city.country);

    if (existingDest) {
      this.loadRatings(existingDest.id);
    }

    if (city.description) return;

    try {
      const searchTerms = [
        `${city.name}, ${city.country} (localidad)`,
        `${city.name}, ${city.country} (municipio)`,
        `${city.name} ${city.country}`
      ];

      // Filtros para evitar traer desambiguaciones, enfermedades u otros textos raros de Wikipedia
      const BANNED = /enfermedad|político|partido|médico|virus|biografía|nacido en|elecciones|mosquito|protesta/i;
      const REQUIRED = /ciudad|municipio|localidad|población|situado|clima|turismo|historia/i;
      let foundExtract = '';

      for (const term of searchTerms) {
        const url = `https://es.wikipedia.org/w/api.php?action=query&format=json&generator=search&gsrsearch=${encodeURIComponent(term)}&gsrlimit=3&prop=extracts&exintro&explaintext&exchars=600&origin=*`;
        const response = await fetch(url);
        const data = await response.json();

        if (data.query?.pages) {
          for (const id in data.query.pages) {
            const extract = data.query.pages[id].extract || '';
            if (REQUIRED.test(extract) && !BANNED.test(extract)) {
              foundExtract = extract;
              break;
            }
          }
        }
        if (foundExtract) break;
      }
      city.description = foundExtract || `Descubre ${city.name}, un destino fascinante en ${city.country} conocido por su vibrante cultura y hospitalidad.`;
    } catch (e) {
      city.description = "La información sobre este destino se está actualizando.";
    }
  }

  closeDetails() {
    this.showModal = false;
  }

  /// Carga la lista de favoritos del usuario conectado
  loadSavedDestinations(): void {
    this.favoriteService.getMyFavorites().subscribe(result => {
      this.savedDestinations = result;
      this.savedDestinations.forEach(d => this.loadEventsForDestination(d));
    });
  }

  /// Consulta al backend los eventos locales de Ticketmaster para una ciudad dada
  loadEventsForDestination(destination: DestinationDto): void {
    const city = destination.city || destination.name;
    if (!city || this.eventsMap[destination.id]) return;

    this.destinationService.getEvents(city).subscribe({
      next: (events) => {
        this.eventsMap[destination.id] = events;
      },
      error: () => {
        console.error('No se pudieron cargar eventos para', city);
      }
    });
  }

  isFavorited(city: CityDto | any): boolean {
    const cityName = city.name || city.city;
    return this.savedDestinations.some(d => d.city === cityName && d.country === city.country);
  }

  getFavoriteId(city: CityDto | any): string | undefined {
    const cityName = city.name || city.city;
    return this.savedDestinations.find(d => d.city === cityName && d.country === city.country)?.id;
  }

  /// Agrega o quita un destino de favoritos.
  /// Si el destino no existe en la BD del backend, primero lo crea y luego lo marca como favorito.
  toggleFavorite(city: CityDto | any): void {
    const existingId = this.getFavoriteId(city);
    if (existingId) {
      this.favoriteService.toggleFavorite(existingId).subscribe({
        next: () => this.loadSavedDestinations(),
        error: () => this.toasterService.error('Error al cambiar favorito')
      });
    } else {
      const input = {
        name: city.name || city.city,
        country: city.country,
        city: city.name || city.city,
        latitude: city.latitude,
        longitude: city.longitude,
        photoUrl: city.wikiImage || ''
      };
      this.destinationService.create(input).subscribe({
        next: (newDest) => {
          this.favoriteService.toggleFavorite(newDest.id).subscribe(() => this.loadSavedDestinations());
        },
        error: () => this.toasterService.error('Error al guardar destino')
      });
    }
  }

  toggleFilters(): void {
    this.showFilters = !this.showFilters;
  }

  toggleEvents(id: string, event: Event): void {
    event.stopPropagation();
    this.expandedEvents[id] = !this.expandedEvents[id];
  }

  clearSearch(): void {
    this.searchParams.query = '';
    this.searchParams.country = '';
    this.searchParams.region = '';
    this.searchParams.minPopulation = null;
    this.cities = [];
  }

  /// Obtiene una imagen de repuesto dinámica usando LoremFlickr basada en etiquetas
  getCityImageUrl(cityOrName: any, country?: string): string {
    const name = typeof cityOrName === 'string' ? cityOrName : (cityOrName?.name || cityOrName?.city);
    const countryName = country || cityOrName?.country || '';
    if (!name) return 'https://images.unsplash.com/photo-1519501025264-65ba15a82390?auto=format&fit=crop&w=800&q=80';
    const seed = name.length + (countryName.length * 2);
    const tags = `${name.toLowerCase().replace(/\s+/g, '-')},landmark,city`;
    return `https://loremflickr.com/800/600/${tags}/all?lock=${seed}`;
  }

  formatCoordinates(lat?: string, long?: string): string {
    if (!lat || !long) return 'N/A';
    const latNum = parseFloat(lat);
    const longNum = parseFloat(long);
    if (isNaN(latNum) || isNaN(longNum)) return `${lat}, ${long}`;
    return `${latNum.toFixed(4)}, ${longNum.toFixed(4)}`;
  }

  openInMaps(lat?: string, long?: string): void {
    if (lat && long) {
      window.open(`https://www.google.com/maps/search/?api=1&query=${lat},${long}`, '_blank');
    }
  }

  /// Carga los puntajes y comentarios creados para un destino
  loadRatings(destinationId: string): void {
    this.ratingService.getRatings(destinationId).subscribe(ratings => {
      this.selectedDestinationRatings = ratings;
      this.userRating = ratings.find(r => r.userId === this.currentUserId) || null;
      if (this.userRating) {
        this.newRating = { score: this.userRating.score, comment: this.userRating.comment || '' };
      } else {
        this.newRating = { score: 5, comment: '' };
      }
    });
    this.ratingService.getAverageRating(destinationId).subscribe(avg => {
      this.averageRating = avg;
    });
  }

  /// Guarda una calificación (puntuación + reseña) para el destino.
  /// Si el destino no está guardado previamente en favoritos/BD, lo crea primero.
  submitRating(): void {
    const existingDest = this.savedDestinations.find(d => 
      (d.city === this.selectedCity.name || d.city === this.selectedCity.city) && d.country === this.selectedCity.country);

    if (!existingDest) {
      this.createAndRate(this.selectedCity);
      return;
    }

    if (this.userRating) {
      this.ratingService.updateRating(this.userRating.id, this.newRating.score, this.newRating.comment)
        .subscribe(() => {
          this.toasterService.success('Calificación actualizada');
          this.loadRatings(existingDest.id);
          this.isRatingDraft = false;
        });
    } else {
      this.ratingService.rateDestination(existingDest.id, this.newRating.score, this.newRating.comment)
        .subscribe(() => {
          this.toasterService.success('Calificación guardada');
          this.loadRatings(existingDest.id);
          this.isRatingDraft = false;
        });
    }
  }

  private createAndRate(city: any): void {
    const input = {
      name: city.name || city.city,
      country: city.country,
      city: city.name || city.city,
      latitude: city.latitude,
      longitude: city.longitude,
      photoUrl: city.wikiImage || ''
    };
    this.destinationService.create(input).subscribe(newDest => {
      this.loadSavedDestinations();
      this.ratingService.rateDestination(newDest.id, this.newRating.score, this.newRating.comment)
        .subscribe(() => {
          this.toasterService.success('Destino guardado y calificado');
          this.loadRatings(newDest.id);
          this.isRatingDraft = false;
        });
    });
  }

  /// Elimina la calificación del usuario actual
  deleteRating(): void {
    if (!this.userRating) return;
    this.ratingService.deleteRating(this.userRating.id).subscribe(() => {
      this.toasterService.success('Calificación eliminada');
      const existingDest = this.savedDestinations.find(d => 
        (d.city === this.selectedCity.name || d.city === this.selectedCity.city) && d.country === this.selectedCity.country);
      if (existingDest) this.loadRatings(existingDest.id);
      this.userRating = null;
      this.newRating = { score: 5, comment: '' };
    });
  }

  editRating(): void {
    this.isRatingDraft = true;
  }

  cancelEdit(): void {
    this.isRatingDraft = false;
    if (this.userRating) {
      this.newRating = { score: this.userRating.score, comment: this.userRating.comment || '' };
    }
  }
}
