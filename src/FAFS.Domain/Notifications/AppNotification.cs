using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace FAFS.Notifications
{
    // Representa una Notificación del sistema que se muestra a un usuario específico.
    public class AppNotification : AuditedAggregateRoot<Guid>
    {
        public Guid UserId { get; private set; } // El usuario destinatario de la notificación
        public string Title { get; private set; } // Título de la notificación (ej: "Destino guardado")
        public string Message { get; private set; } // Mensaje detallado (ej: "Se guardó Roma en tus favoritos")
        public bool IsRead { get; internal set; } // Indica si el usuario ya leyó esta notificación
        public string Type { get; private set; } // Tipo/categoría de notificación (ej: FavoriteAdded, DestinationUpdated)

        protected AppNotification()
        {
        }

        public AppNotification(
            Guid id,
            Guid userId,
            string title,
            string message,
            string type,
            bool isRead = false) : base(id)
        {
            UserId = userId;
            Title = title;
            Message = message;
            Type = type;
            IsRead = isRead;
        }

        // Marca la notificación como leída
        public void SetAsRead()
        {
            IsRead = true;
        }
    }
}
