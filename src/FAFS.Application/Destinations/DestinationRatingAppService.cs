using FAFS.Application.Contracts.Destinations;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace FAFS.Destinations
{
    // Esta clase maneja las calificaciones y comentarios (estrellas) que los usuarios le dan a los destinos.
    // El [Authorize] obliga a que el usuario esté logueado para poder usar estos métodos (salvo los marcados como AllowAnonymous).
    [Authorize]
    public class DestinationRatingAppService : ApplicationService, IDestinationRatingAppService
    {
        private readonly IRepository<DestinationRating, Guid> _ratingRepository; // Base de datos de calificaciones
        private readonly ICurrentUser _currentUser; // Para saber qué usuario está logueado en este momento
        private readonly Volo.Abp.Guids.IGuidGenerator _guidGenerator; // Para inventar IDs únicos

        public DestinationRatingAppService(
            IRepository<DestinationRating, Guid> ratingRepository,
            ICurrentUser currentUser,
            Volo.Abp.Guids.IGuidGenerator guidGenerator)
        {
            _ratingRepository = ratingRepository;
            _currentUser = currentUser;
            _guidGenerator = guidGenerator;
        }

        // Permite calificar un destino (ej: de 1 a 5 estrellas y un comentario)
        public async Task RateDestinationAsync(Guid destinationId, int score, string? comment)
        {
            // Valida que el puntaje sea entre 1 y 5
            if (score < 1 || score > 5)
                throw new UserFriendlyException("La puntuación debe estar entre 1 y 5.");

            // Busca si este usuario ya votó por este destino anteriormente
            var ratings = await _ratingRepository.GetListAsync(r => 
                r.DestinationId == destinationId && r.UserId == _currentUser.GetId());
            
            var existingRating = ratings.FirstOrDefault();

            // Si ya votó, no lo deja volver a votar
            if (existingRating != null)
            {
                throw new UserFriendlyException("Ya has calificado este destino.");
            }

            // Crea el nuevo voto con sus datos
            var rating = new DestinationRating(
                _guidGenerator.Create(),
                _currentUser.GetId(),
                destinationId,
                score,
                comment
            );

            // Guarda la calificación en la base de datos
            await _ratingRepository.InsertAsync(rating, autoSave: true);
        }

        // Modifica una calificación existente
        public async Task UpdateRatingAsync(Guid id, int score, string? comment)
        {
            var rating = await _ratingRepository.GetAsync(id); // Busca el voto en la BD

            // Valida que el voto le pertenezca al usuario que quiere editarlo
            if (rating.UserId != _currentUser.GetId())
            {
                throw new AbpAuthorizationException("No tienes permiso.");
            }

            // Actualiza el puntaje y el comentario
            rating.Score = score;
            rating.Comment = comment;

            await _ratingRepository.UpdateAsync(rating, autoSave: true); // Guarda los cambios
        }

        // Borra una calificación existente
        public async Task DeleteRatingAsync(Guid id)
        {
            var rating = await _ratingRepository.GetAsync(id); // Busca la calificación

            // Valida que sea el dueño de la calificación antes de borrarla
            if (rating.UserId != _currentUser.GetId())
            {
                throw new AbpAuthorizationException("No tienes permiso.");
            }

            await _ratingRepository.DeleteAsync(rating, autoSave: true); // Borra de la BD
        }

        // Trae todas las calificaciones de un destino (Cualquiera lo puede ver, sin estar logueado)
        [AllowAnonymous]
        public async Task<List<DestinationRatingDto>> GetRatingsAsync(Guid destinationId)
        {
            var ratings = await _ratingRepository.GetListAsync(r => r.DestinationId == destinationId);

            // Transforma los datos de la base de datos a un formato DTO para enviar al Frontend
            return ratings.Select(r => new DestinationRatingDto
            {
                Id = r.Id,
                UserId = r.UserId,
                DestinationId = r.DestinationId,
                Score = r.Score,
                Comment = r.Comment,
                CreationTime = r.CreationTime
            }).ToList();
        }

        // Obtiene el promedio matemático de estrellas de un destino (Cualquiera lo puede ver)
        [AllowAnonymous]
        public async Task<double> GetAverageRatingAsync(Guid destinationId)
        {
            var ratings = await _ratingRepository.GetListAsync(r => r.DestinationId == destinationId);
            return ratings.Any() ? ratings.Average(r => r.Score) : 0; // Si hay votos calcula promedio, sino da 0
        }

        // Trae todas las calificaciones hechas únicamente por el usuario logueado actualmente
        public async Task<List<DestinationRatingDto>> GetMyRatingsAsync()
        {
            var userId = _currentUser.Id;
            if (!userId.HasValue) return new List<DestinationRatingDto>();

            var ratings = await _ratingRepository.GetListAsync(r => r.UserId == userId.Value);

            return ratings.Select(r => new DestinationRatingDto
            {
                Id = r.Id,
                UserId = r.UserId,
                DestinationId = r.DestinationId,
                Score = r.Score,
                Comment = r.Comment,
                CreationTime = r.CreationTime
            }).ToList();
        }
    }
}
