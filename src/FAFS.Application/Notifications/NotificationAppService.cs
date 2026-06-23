using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace FAFS.Notifications
{
    // Servicio para gestionar el buzón de notificaciones de cada usuario.
    // Se requiere estar logueado para consultar o modificar las notificaciones.
    [Authorize]
    public class NotificationAppService : FAFSAppService, INotificationAppService
    {
        private readonly IRepository<AppNotification, Guid> _notificationRepository; // Base de datos de notificaciones

        public NotificationAppService(IRepository<AppNotification, Guid> notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        // Obtiene el listado de notificaciones del usuario logueado con paginado y filtro (leídas/no leídas)
        public async Task<PagedResultDto<AppNotificationDto>> GetListAsync(GetAppNotificationsInput input)
        {
            var query = await _notificationRepository.GetQueryableAsync();
            var currentUserId = CurrentUser.GetId(); // ID del usuario logueado

            // Trae solo las notificaciones que le pertenecen a este usuario
            query = query.Where(n => n.UserId == currentUserId);

            // Filtra por leídas o no leídas si se especifica el parámetro
            if (input.IsRead.HasValue)
            {
                query = query.Where(n => n.IsRead == input.IsRead.Value);
            }

            var totalCount = await AsyncExecuter.CountAsync(query); // Cuenta el total
            
            // Trae los registros ordenados desde el más nuevo al más viejo
            var items = await AsyncExecuter.ToListAsync(
                query.OrderByDescending(n => n.CreationTime)
                     .Skip(input.SkipCount)
                     .Take(input.MaxResultCount)
            );

            // Devuelve el total y los DTOs de salida mapeados
            return new PagedResultDto<AppNotificationDto>(
                totalCount,
                ObjectMapper.Map<List<AppNotification>, List<AppNotificationDto>>(items)
            );
        }

        // Cuenta cuántas notificaciones sin leer (nuevas) tiene el usuario logueado
        public async Task<int> GetUnreadCountAsync()
        {
            var currentUserId = CurrentUser.GetId();
            return await _notificationRepository.CountAsync(n => n.UserId == currentUserId && !n.IsRead);
        }

        // Marca una notificación específica como leída usando su ID
        public async Task MarkAsReadAsync(Guid id)
        {
            var notification = await _notificationRepository.GetAsync(id);
            // Valida que la notificación realmente le pertenezca al usuario antes de modificarla
            if (notification.UserId == CurrentUser.GetId())
            {
                notification.SetAsRead(); // Cambia el estado a leída
                await _notificationRepository.UpdateAsync(notification); // Guarda los cambios
            }
        }

        // Marca todas las notificaciones del usuario actual como leídas a la vez
        public async Task MarkAllAsReadAsync()
        {
            var currentUserId = CurrentUser.GetId();
            
            // Busca todas las notificaciones sin leer del usuario
            var unreadNotifications = await _notificationRepository.GetListAsync(n => n.UserId == currentUserId && !n.IsRead);

            foreach (var notification in unreadNotifications)
            {
                notification.SetAsRead(); // Las marca como leídas
            }

            // Si encontró notificaciones, guarda las actualizaciones de todas juntas
            if (unreadNotifications.Any())
            {
                await _notificationRepository.UpdateManyAsync(unreadNotifications);
            }
        }
    }
}
