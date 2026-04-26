using FAFS.Application.Contracts.Destinations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;


namespace FAFS.Destinations
{
    public interface IDestinationAppService :
        ICrudAppService<
            DestinationDto,              // DTO de salida (para mostrar)
            Guid,                         // Tipo de la PK de la entidad
            GetDestinationsInput, // Para paginación, ordenamiento y filtros
            CreateUpdateDestinationDto   // DTO para crear/actualizar
        >
    {
        // Custom method for searching cities (external service)
        Task<CitySearchResultDto> SearchCitiesAsync(CitySearchRequestDto input);

        // Custom method for getting city details (external service)
        Task<CityDto?> GetCityDetailsAsync(string cityId);

        // Custom method for getting events (Ticketmaster API)
        Task<List<DestinationEventDto>> GetEventsAsync(string city);
    }
}