using FAFS.Application.Contracts.Destinations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace FAFS.Destinations
{
    public class TicketmasterService : ITicketmasterService, ITransientDependency
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ExternalApisOptions _options;
        private readonly ILogger<TicketmasterService> _logger;

        public TicketmasterService(
            IHttpClientFactory httpClientFactory,
            IOptions<ExternalApisOptions> options,
            ILogger<TicketmasterService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<List<DestinationEventDto>> GetEventsForCityAsync(string city)
        {
            var events = new List<DestinationEventDto>();
            if (string.IsNullOrWhiteSpace(city)) return events;

            try
            {
                var client = _httpClientFactory.CreateClient("TicketmasterClient");
                var baseUrl = _options.Ticketmaster.BaseUrl.TrimEnd('/');
                var startDateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                var endDateTime = DateTime.UtcNow.AddMonths(3).ToString("yyyy-MM-ddTHH:mm:ssZ");
                
                var url = $"{baseUrl}/events.json?apikey={_options.Ticketmaster.ApiKey}&city={Uri.EscapeDataString(city)}&startDateTime={startDateTime}&endDateTime={endDateTime}&sort=date,asc&size=5";

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"[Ticketmaster] Error consultando eventos. Status: {response.StatusCode}");
                    return events;
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);

                if (doc.RootElement.TryGetProperty("_embedded", out var embedded) && 
                    embedded.TryGetProperty("events", out var eventsArray))
                {
                    foreach (var item in eventsArray.EnumerateArray())
                    {
                        var eventDto = new DestinationEventDto
                        {
                            Name = item.GetProperty("name").GetString() ?? string.Empty,
                            Url = item.TryGetProperty("url", out var u) ? u.GetString() : null
                        };

                        if (item.TryGetProperty("dates", out var dates) && dates.TryGetProperty("start", out var start))
                        {
                            eventDto.Date = start.TryGetProperty("localDate", out var d) ? d.GetString() : null;
                            eventDto.Time = start.TryGetProperty("localTime", out var t) ? t.GetString() : null;
                        }

                        if (item.TryGetProperty("images", out var images) && images.GetArrayLength() > 0)
                        {
                            // we get the first image
                            eventDto.ImageUrl = images[0].TryGetProperty("url", out var iu) ? iu.GetString() : null;
                        }

                        if (item.TryGetProperty("_embedded", out var emb) && emb.TryGetProperty("venues", out var venues) && venues.GetArrayLength() > 0)
                        {
                            eventDto.VenueName = venues[0].TryGetProperty("name", out var vn) ? vn.GetString() : null;
                        }

                        events.Add(eventDto);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Ticketmaster] Excepción al obtener eventos para la ciudad: {city}");
            }

            return events;
        }
    }
}
