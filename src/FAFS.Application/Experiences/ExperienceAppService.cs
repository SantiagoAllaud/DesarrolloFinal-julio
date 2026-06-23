using System;
using System.Linq;
using System.Threading.Tasks;
using FAFS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace FAFS.Experiences
{
    // Clase que administra las Experiencias turísticas o testimonios creados por los usuarios.
    // Con [Authorize], aseguramos que se requiera inicio de sesión para realizar modificaciones de base.
    [Authorize]
    public class ExperienceAppService : 
        CrudAppService<
            Experience, // Entidad de la base de datos
            ExperienceDto, // DTO para mandar datos al exterior
            Guid, // Clave primaria
            GetExperiencesInput, // Parámetros para listar y buscar
            CreateUpdateExperienceDto>, // Datos para crear o actualizar
        IExperienceAppService
    {
        public ExperienceAppService(IRepository<Experience, Guid> repository) 
            : base(repository)
        {
        }

        // Modifica una experiencia existente, validando permisos de autoría
        public override async Task<ExperienceDto> UpdateAsync(Guid id, CreateUpdateExperienceDto input)
        {
            var experience = await Repository.GetAsync(id); // Obtiene la experiencia
            
            // Valida que el usuario que intenta editarla sea el creador original
            if (experience.CreatorId != CurrentUser.Id)
            {
                throw new UnauthorizedAccessException("You can only edit your own experiences.");
            }

            return await base.UpdateAsync(id, input); // Ejecuta la actualización base
        }

        // Elimina una experiencia existente, validando permisos de autoría
        public override async Task DeleteAsync(Guid id)
        {
            var experience = await Repository.GetAsync(id); // Busca el registro en la BD

            // Valida que el usuario logueado sea el creador de la experiencia
            if (experience.CreatorId != CurrentUser.Id)
            {
                throw new UnauthorizedAccessException("You can only delete your own experiences.");
            }

            await base.DeleteAsync(id); // Ejecuta el borrado base
        }

        // Permite ver una experiencia específica sin necesidad de estar logueado (público)
        [AllowAnonymous]
        public override Task<ExperienceDto> GetAsync(Guid id)
        {
            return base.GetAsync(id);
        }

        // Permite ver la lista completa de experiencias sin necesidad de estar logueado (público)
        [AllowAnonymous]
        public override Task<PagedResultDto<ExperienceDto>> GetListAsync(GetExperiencesInput input)
        {
            return base.GetListAsync(input);
        }

        // Construye y personaliza la consulta (Query SQL/LINQ) para filtrar experiencias
        protected override async Task<IQueryable<Experience>> CreateFilteredQueryAsync(GetExperiencesInput input)
        {
            var query = await base.CreateFilteredQueryAsync(input);

            return query
                // Si viene un ID de destino, filtra solo las experiencias de ese destino
                .WhereIf(input.DestinationId.HasValue, x => x.DestinationId == input.DestinationId)
                // Si viene un rating/puntuación específica, filtra por esa calificación
                .WhereIf(input.Rating.HasValue, x => x.Rating == input.Rating)
                // Si viene una palabra clave, busca que esté contenida en el título o descripción (ignora mayúsculas/minúsculas)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Keyword), x => 
                    x.Title.ToLower().Contains(input.Keyword!.ToLower()) || 
                    x.Description.ToLower().Contains(input.Keyword!.ToLower()));
        }
    }
}
