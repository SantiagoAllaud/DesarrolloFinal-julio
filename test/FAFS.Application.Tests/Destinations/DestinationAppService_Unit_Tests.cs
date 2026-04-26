using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using FAFS.Application.Contracts.Destinations;
using FAFS.Destinations;
using Moq;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Xunit;
using FAFS.Notifications;

namespace FAFS.Application.Tests.Destinations
{
    public class DestinationAppService_Unit_Tests
    {
        private readonly Mock<IRepository<Destination, Guid>> _mockDestinationRepository;
        private readonly Mock<IRepository<DestinationRating, Guid>> _mockRatingRepository;
        private readonly Mock<IRepository<FavoriteDestination, Guid>> _mockFavoriteRepository;
        private readonly Mock<IRepository<AppNotification, Guid>> _mockNotificationRepository;
        private readonly Mock<ICitySearchService> _mockCitySearchService;
        private readonly Mock<Volo.Abp.Guids.IGuidGenerator> _mockGuidGenerator;
        private readonly Mock<ITicketmasterService> _mockTicketmasterService;
        private readonly DestinationAppService _appService;

        public DestinationAppService_Unit_Tests()
        {
            _mockDestinationRepository = new Mock<IRepository<Destination, Guid>>();
            _mockRatingRepository = new Mock<IRepository<DestinationRating, Guid>>();
            _mockFavoriteRepository = new Mock<IRepository<FavoriteDestination, Guid>>();
            _mockNotificationRepository = new Mock<IRepository<AppNotification, Guid>>();
            _mockCitySearchService = new Mock<ICitySearchService>();
            _mockGuidGenerator = new Mock<Volo.Abp.Guids.IGuidGenerator>();
            _mockTicketmasterService = new Mock<ITicketmasterService>();
            
            _appService = new DestinationAppService(
                _mockDestinationRepository.Object, 
                _mockCitySearchService.Object,
                _mockRatingRepository.Object,
                _mockFavoriteRepository.Object,
                _mockNotificationRepository.Object,
                _mockGuidGenerator.Object,
                _mockTicketmasterService.Object);
        }

        [Fact]
        public async Task FillRatingInfoAsync_Should_Populate_Stats()
        {
            // Arrange
            var destinationId = Guid.NewGuid();
            var dto = new DestinationDto { Id = destinationId };
            
            var ratings = new List<DestinationRating>
            {
                new DestinationRating(Guid.NewGuid(), Guid.NewGuid(), destinationId, 5),
                new DestinationRating(Guid.NewGuid(), Guid.NewGuid(), destinationId, 1)
            };

            _mockRatingRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<DestinationRating, bool>>>(), It.IsAny<bool>(), default))
                .ReturnsAsync(ratings);

            // Access private method via reflection
            var method = typeof(DestinationAppService).GetMethod("FillRatingInfoAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act
            if (method != null)
            {
                await (Task)method.Invoke(_appService, new object[] { dto })!;
            }

            // Assert
            dto.AverageRating.ShouldBe(3.0);
            dto.RatingCount.ShouldBe(2);
        }

        [Fact]
        public async Task FillRatingInfoAsync_Should_Handle_No_Ratings()
        {
            // Arrange
            var destinationId = Guid.NewGuid();
            var dto = new DestinationDto { Id = destinationId };
            
            _mockRatingRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<DestinationRating, bool>>>(), It.IsAny<bool>(), default))
                .ReturnsAsync(new List<DestinationRating>());

            // Access private method via reflection
            var method = typeof(DestinationAppService).GetMethod("FillRatingInfoAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act
            if (method != null)
            {
                await (Task)method.Invoke(_appService, new object[] { dto })!;
            }

            // Assert
            dto.AverageRating.ShouldBe(0);
            dto.RatingCount.ShouldBe(0);
        }
    }
}
