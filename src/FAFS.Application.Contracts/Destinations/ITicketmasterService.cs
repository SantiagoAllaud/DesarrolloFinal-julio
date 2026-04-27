using System.Collections.Generic;
using System.Threading.Tasks;

namespace FAFS.Application.Contracts.Destinations
{
    public interface ITicketmasterService
    {
        Task<List<DestinationEventDto>> GetEventsForCityAsync(string city);
    }
}
