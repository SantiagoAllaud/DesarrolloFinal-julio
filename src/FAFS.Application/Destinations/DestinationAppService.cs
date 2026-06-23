using FAFS.Application.Contracts.Destinations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using FAFS.Notifications;
using Volo.Abp.Guids;

namespace FAFS.Destinations
{
    // Esta clase maneja los destinos de viaje (crear, ver, editar y borrar)
    // El "CrudAppService" ya hace casi todo el trabajo básico por nosotros de forma automática
    public class DestinationAppService :
     CrudAppService<
         Destination, // La base de datos guarda este objeto (Entidad)
         DestinationDto, // Lo que enviamos hacia afuera (DTO de salida)
         Guid, // El tipo de ID único que usa cada destino (clave primaria)
     GetDestinationsInput, // Para buscar y paginar la lista
     CreateUpdateDestinationDto>, // Los datos necesarios para crear o editar
     IDestinationAppService // El contrato/interfaz del servicio
    {
        // Estas son las herramientas (servicios y bases de datos) que necesitamos usar:
        private readonly ICitySearchService _citySearchService; // Para buscar ciudades
        private readonly IRepository<DestinationRating, Guid> _ratingRepository; // Para calificaciones de la gente
        private readonly IRepository<FavoriteDestination, Guid> _favoriteRepository; // Para ver quién lo tiene en favoritos
        private readonly IRepository<AppNotification, Guid> _notificationRepository; // Para guardar notificaciones
        private readonly IGuidGenerator _guidGenerator; // Para inventar IDs únicos seguros
        private readonly ITicketmasterService _ticketmasterService; // Para buscar eventos/recitales en Ticketmaster

        // Constructor: Recibe todas las herramientas y las guarda en las variables de arriba
        public DestinationAppService(
            IRepository<Destination, Guid> repository, 
            ICitySearchService citySearchService,
            IRepository<DestinationRating, Guid> ratingRepository,
            IRepository<FavoriteDestination, Guid> favoriteRepository,
            IRepository<AppNotification, Guid> notificationRepository,
            IGuidGenerator guidGenerator,
            ITicketmasterService ticketmasterService)
            : base(repository) // Le pasa el repositorio principal a la clase base de ABP
        {
            _citySearchService = citySearchService;
            _ratingRepository = ratingRepository;
            _favoriteRepository = favoriteRepository;
            _notificationRepository = notificationRepository;
            _guidGenerator = guidGenerator;
            _ticketmasterService = ticketmasterService;
        }

        // Muestra un solo destino usando su ID único
        public override async Task<DestinationDto> GetAsync(Guid id)
        {
            var dto = await base.GetAsync(id); // Busca el destino en la base de datos
            await FillRatingInfoAsync(dto); // Le agrega el promedio de estrellitas y cantidad de votos
            return dto; // Devuelve el destino completo
        }

        // Modifica un destino existente
        public override async Task<DestinationDto> UpdateAsync(Guid id, CreateUpdateDestinationDto input)
        {
            // Buscamos cómo estaba antes de cambiarlo para comparar
            var original = await Repository.GetAsync(id);
            var nameChanged = original.Name != input.Name;            
            var locationChanged = original.Coordinates?.Latitude != input.Latitude || 
                                   original.Coordinates?.Longitude != input.Longitude; 
            var result = await base.UpdateAsync(id, input); // Guarda los cambios nuevos en la base de datos
            // Si cambió el nombre o la ubicación, hay que avisarle a la gente
            if (nameChanged || locationChanged)
            {
                // Busca a todas las personas que guardaron este destino en favoritos
                var favoriters = await _favoriteRepository.GetListAsync(f => f.DestinationId == id);
                var notifications = new List<AppNotification>();
                
                foreach (var fav in favoriters)
                {
                    // Crea una notificación para avisarle a cada uno
                    notifications.Add(new AppNotification(
                        _guidGenerator.Create(),
                        fav.UserId,
                        "Destino favorito actualizado",
                        $"El destino '{result.Name}' que tienes en favoritos ha sido modificado.",
                        "DestinationUpdated"
                    ));
                }

                // Si hay notificaciones armadas, las guarda todas juntas en la base de datos
                if (notifications.Any())
                {
                    await _notificationRepository.InsertManyAsync(notifications);
                }
            }

            return result; // Devuelve el destino ya actualizado
        }

        // Trae la lista completa de destinos (con filtros y paginado)
        public override async Task<PagedResultDto<DestinationDto>> GetListAsync(GetDestinationsInput input)
        {
            var result = await base.GetListAsync(input); // Trae los destinos
            foreach (var dto in result.Items)
            {
                await FillRatingInfoAsync(dto); // A cada destino de la lista le calcula las estrellitas
            }
            return result; // Devuelve la lista con las estrellitas incluidas
        }

        // Método interno para calcular el promedio de estrellas y cantidad de votos
        private async Task FillRatingInfoAsync(DestinationDto dto)
        {
            // Busca todas las valoraciones que la gente le dio a este destino
            var ratings = await _ratingRepository.GetListAsync(r => r.DestinationId == dto.Id);
            if (ratings.Any())
            {
                dto.AverageRating = ratings.Average(r => r.Score); // Saca el promedio de puntuación
                dto.RatingCount = ratings.Count; // Cuenta cuánta gente votó
            }
        }

        // Busca ciudades usando el buscador externo (por ejemplo, autocompletar mientras escribís)
        public async Task<CitySearchResultDto> SearchCitiesAsync(CitySearchRequestDto request)
        {
            return await _citySearchService.SearchCitiesAsync(request);
        }

        // Obtiene los detalles de una ciudad específica por su ID
        public async Task<CityDto?> GetCityDetailsAsync(string cityId)
        {
            return await _citySearchService.GetCityDetailsAsync(cityId);
        }

        // Busca recitales y shows de Ticketmaster para una ciudad
        public async Task<List<DestinationEventDto>> GetEventsAsync(string city)
        {
            return await _ticketmasterService.GetEventsForCityAsync(city);
        }

        // Aplica el filtro de texto para buscar cuando listamos destinos
        protected override async Task<IQueryable<Destination>> CreateFilteredQueryAsync(GetDestinationsInput input)
        {
            var query = await base.CreateFilteredQueryAsync(input);

            // Si el usuario escribió un texto para buscar...
            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                // Filtra destinos que tengan ese texto en el nombre, país o ciudad
                query = query.Where(d => 
                    d.Name.Contains(input.Filter) || 
                    d.Country.Contains(input.Filter) ||
                    d.City.Contains(input.Filter));
            }

            return query;
        }
    }
}
