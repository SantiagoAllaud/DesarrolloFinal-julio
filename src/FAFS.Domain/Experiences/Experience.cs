using System;
using Volo.Abp.Domain.Entities.Auditing;
using FAFS.Experiences;

namespace FAFS.Experiences
{
    // Representa una Experiencia turística (un post o testimonio de un usuario) en la base de datos.
    public class Experience : AuditedAggregateRoot<Guid>
    {
        public Guid DestinationId { get; private set; } // El destino al cual se refiere esta experiencia
        public string Title { get; private set; } // Título del post
        public string Description { get; private set; } // Contenido/reseña del post
        public ExperienceRating Rating { get; private set; } // Calificación (un enum: excelente, buena, mala, etc)

        protected Experience()
        {
        }

        public Experience(
            Guid id,
            Guid destinationId,
            string title,
            string description,
            ExperienceRating rating
        ) : base(id)
        {
            DestinationId = destinationId;
            SetTitle(title);
            SetDescription(description);
            Rating = rating;
        }

        // Valida y asigna el título, controlando que no esté vacío y no supere el límite de caracteres
        public void SetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title cannot be empty.");
            }
            if (title.Length > ExperienceConsts.MaxTitleLength)
            {
                throw new ArgumentException($"Title cannot exceed {ExperienceConsts.MaxTitleLength} characters.");
            }
            Title = title;
        }

        // Valida y asigna la descripción/cuerpo del testimonio
        public void SetDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Description cannot be empty.");
            }
            if (description.Length > ExperienceConsts.MaxDescriptionLength)
            {
                throw new ArgumentException($"Description cannot exceed {ExperienceConsts.MaxDescriptionLength} characters.");
            }
            Description = description;
        }

        public void SetRating(ExperienceRating rating)
        {
            Rating = rating;
        }
    }
}
