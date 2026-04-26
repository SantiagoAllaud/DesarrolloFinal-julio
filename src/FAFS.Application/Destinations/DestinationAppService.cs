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
    public class DestinationAppService :
     CrudAppService<
         Destination, //Entidad
         DestinationDto, //dto de salida
         Guid, //Primary key destination entity
     GetDestinationsInput, //Used for paging/sorting
     CreateUpdateDestinationDto>, //Used to create/update a destination
     IDestinationAppService //implement the IDestinationAppService
    {
        private readonly ICitySearchService _citySearchService;
        private readonly IRepository<DestinationRating, Guid> _ratingRepository;
        private readonly IRepository<FavoriteDestination, Guid> _favoriteRepository;
        private readonly IRepository<AppNotification, Guid> _notificationRepository;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ITicketmasterService _ticketmasterService;

        public DestinationAppService(
            IRepository<Destination, Guid> repository, 
            ICitySearchService citySearchService,
            IRepository<DestinationRating, Guid> ratingRepository,
            IRepository<FavoriteDestination, Guid> favoriteRepository,
            IRepository<AppNotification, Guid> notificationRepository,
            IGuidGenerator guidGenerator,
            ITicketmasterService ticketmasterService)
            : base(repository)
        {
            _citySearchService = citySearchService;
            _ratingRepository = ratingRepository;
            _favoriteRepository = favoriteRepository;
            _notificationRepository = notificationRepository;
            _guidGenerator = guidGenerator;
            _ticketmasterService = ticketmasterService;
        }

        public override async Task<DestinationDto> GetAsync(Guid id)
        {
            var dto = await base.GetAsync(id);
            await FillRatingInfoAsync(dto);
            return dto;
        }

        public override async Task<DestinationDto> UpdateAsync(Guid id, CreateUpdateDestinationDto input)
        {
            // Get original to check for changes
            var original = await Repository.GetAsync(id);
            var nameChanged = original.Name != input.Name;
            var locationChanged = original.Coordinates?.Latitude != input.Latitude || 
                                  original.Coordinates?.Longitude != input.Longitude;

            var result = await base.UpdateAsync(id, input);

            if (nameChanged || locationChanged)
            {
                // Send notification to ALL users who favorited this destination
                var favoriters = await _favoriteRepository.GetListAsync(f => f.DestinationId == id);
                var notifications = new List<AppNotification>();
                
                foreach (var fav in favoriters)
                {
                    notifications.Add(new AppNotification(
                        _guidGenerator.Create(),
                        fav.UserId,
                        "Destino favorito actualizado",
                        $"El destino '{result.Name}' que tienes en favoritos ha sido modificado.",
                        "DestinationUpdated"
                    ));
                }

                if (notifications.Any())
                {
                    await _notificationRepository.InsertManyAsync(notifications);
                }
            }

            return result;
        }

        public override async Task<PagedResultDto<DestinationDto>> GetListAsync(GetDestinationsInput input)
        {
            var result = await base.GetListAsync(input);
            foreach (var dto in result.Items)
            {
                await FillRatingInfoAsync(dto);
            }
            return result;
        }

        private async Task FillRatingInfoAsync(DestinationDto dto)
        {
            var ratings = await _ratingRepository.GetListAsync(r => r.DestinationId == dto.Id);
            if (ratings.Any())
            {
                dto.AverageRating = ratings.Average(r => r.Score);
                dto.RatingCount = ratings.Count;
            }
        }

        public async Task<CitySearchResultDto> SearchCitiesAsync(CitySearchRequestDto request)
        {
            return await _citySearchService.SearchCitiesAsync(request);
        }

        public async Task<CityDto?> GetCityDetailsAsync(string cityId)
        {
            return await _citySearchService.GetCityDetailsAsync(cityId);
        }

        public async Task<List<DestinationEventDto>> GetEventsAsync(string city)
        {
            return await _ticketmasterService.GetEventsForCityAsync(city);
        }

        protected override async Task<IQueryable<Destination>> CreateFilteredQueryAsync(GetDestinationsInput input)
        {
            var query = await base.CreateFilteredQueryAsync(input);

            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                query = query.Where(d => 
                    d.Name.Contains(input.Filter) || 
                    d.Country.Contains(input.Filter) ||
                    d.City.Contains(input.Filter));
            }

            return query;
        }
    }
}
