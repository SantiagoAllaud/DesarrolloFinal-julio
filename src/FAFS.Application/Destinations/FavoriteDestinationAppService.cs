using FAFS.Notifications;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using FAFS.Application.Contracts.Destinations;
using Volo.Abp.Users;

namespace FAFS.Destinations
{
    // Esta clase maneja la sección de "Mis Favoritos" de los usuarios
    // El [Authorize] obliga a que el usuario esté logueado para poder interactuar con sus favoritos
    [Authorize]
    public class FavoriteDestinationAppService : ApplicationService, IFavoriteDestinationAppService
    {
        private readonly IRepository<FavoriteDestination, Guid> _favoriteRepository; // Base de datos de favoritos
        private readonly IRepository<Destination, Guid> _destinationRepository; // Base de datos de destinos turísticos
        private readonly IRepository<AppNotification, Guid> _notificationRepository; // Base de datos de notificaciones
        private readonly IGuidGenerator _guidGenerator; // Generador de IDs únicos seguros
        private readonly ICurrentUser _currentUser; // Para saber cuál es el usuario logueado actualmente

        public FavoriteDestinationAppService(
            IRepository<FavoriteDestination, Guid> favoriteRepository,
            IRepository<Destination, Guid> destinationRepository,
            IRepository<AppNotification, Guid> notificationRepository,
            IGuidGenerator guidGenerator,
            ICurrentUser currentUser)
        {
            _favoriteRepository = favoriteRepository;
            _destinationRepository = destinationRepository;
            _notificationRepository = notificationRepository;
            _guidGenerator = guidGenerator;
            _currentUser = currentUser;
        }

        // Sobrescribimos el acceso al usuario actual para usar nuestra variable inyectada
        protected new ICurrentUser CurrentUser => _currentUser;

        // Agrega o quita un destino de favoritos (si ya existe lo borra, si no existe lo agrega)
        public async Task ToggleFavoriteAsync(ToggleFavoriteDto input)
        {
            // Valida que el usuario esté logueado
            if (CurrentUser.Id == null)
            {
                throw new UnauthorizedAccessException();
            }

            var userId = CurrentUser.Id.Value;
            var destinationId = input.DestinationId;

            // Valida que el destino que se quiere guardar exista en la base de datos
            var destination = await _destinationRepository.FindAsync(destinationId);
            if (destination == null)
            {
                throw new UserFriendlyException("El destino no existe.");
            }

            // Busca si este destino ya estaba guardado en favoritos de este usuario
            var existing = await _favoriteRepository.FindAsync(f => f.UserId == userId && f.DestinationId == destinationId);

            if (existing != null)
            {
                // Si ya existía, lo quitamos de favoritos (Delete)
                await _favoriteRepository.DeleteAsync(existing, autoSave: true);
            }
            else
            {
                // Si no existía, lo agregamos a favoritos (Insert)
                var newFavorite = new FavoriteDestination(
                    _guidGenerator.Create(),
                    userId,
                    destinationId
                );

                await _favoriteRepository.InsertAsync(newFavorite, autoSave: true);

                // Y le enviamos una notificación en la base de datos avisándole que se guardó con éxito
                await _notificationRepository.InsertAsync(new AppNotification(
                    _guidGenerator.Create(),
                    userId,
                    "Destino guardado",
                    $"El destino '{destination.Name}' ha sido guardado en tus favoritos.",
                    "FavoriteAdded"
                ), autoSave: true);
            }
        }

        // Obtiene la lista completa de destinos que el usuario logueado tiene en favoritos
        public async Task<List<DestinationDto>> GetMyFavoritesAsync()
        {
            // Si no está logueado, devuelve una lista vacía
            if (CurrentUser.Id == null)
            {
                return new List<DestinationDto>();
            }

            var userId = CurrentUser.Id.Value;

            // Consulta que une (JOIN) la tabla de Favoritos con la de Destinos
            // para traer los datos completos de cada destino marcado como favorito por el usuario
            var query = from fav in await _favoriteRepository.GetQueryableAsync()
                        join dest in await _destinationRepository.GetQueryableAsync()
                        on fav.DestinationId equals dest.Id
                        where fav.UserId == userId
                        select dest;

            var destinations = await AsyncExecuter.ToListAsync(query);

            // Mapea la entidad de dominio a DTOs de salida para enviar al frontend
            return ObjectMapper.Map<List<Destination>, List<DestinationDto>>(destinations);
        }
    }
}
