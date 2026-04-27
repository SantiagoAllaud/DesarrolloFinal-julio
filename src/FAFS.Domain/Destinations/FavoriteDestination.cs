using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace FAFS.Destinations
{
    public class FavoriteDestination : CreationAuditedEntity<Guid>
    {
        public Guid UserId { get; private set; }
        public Guid DestinationId { get; private set; }

        protected FavoriteDestination()
        {
        }

        public FavoriteDestination(Guid id, Guid userId, Guid destinationId)
        {
            Id = id;
            UserId = userId;
            DestinationId = destinationId;
        }
    }
}
// sada
