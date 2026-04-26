using FAFS.Application.Contracts.Destinations;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Volo.Abp;
using Microsoft.Extensions.Logging;

namespace FAFS.Destinations
{
    // Service that connects to the external GeoDB Cities API
    public class GeoDbCitySearchService : ICitySearchService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ExternalApisOptions _options;
        private readonly ILogger<GeoDbCitySearchService> _logger;

        public GeoDbCitySearchService(
            IHttpClientFactory httpClientFactory,
            IOptions<ExternalApisOptions> options,
            ILogger<GeoDbCitySearchService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<CitySearchResultDto> SearchCitiesAsync(CitySearchRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.PartialName) || request.PartialName.Trim().Length < 2)
            {
                throw new BusinessException("CitySearch:InvalidPartialName")
                    .WithData("Message", "El texto de búsqueda debe tener al menos 2 caracteres.");
            }

            var client = _httpClientFactory.CreateClient("GeoDbClient");
            var urlRelative = $"cities?namePrefix={Uri.EscapeDataString(request.PartialName)}&limit={request.Limit}";

            if (!string.IsNullOrWhiteSpace(request.CountryCode))
                urlRelative += $"&countryIds={Uri.EscapeDataString(request.CountryCode)}";

            if (!string.IsNullOrWhiteSpace(request.RegionCode))
                urlRelative += $"&regionIds={Uri.EscapeDataString(request.RegionCode)}";

            if (request.MinPopulation.HasValue)
                urlRelative += $"&minPopulation={request.MinPopulation.Value}";

            var fullUrl = new Uri(new Uri(_options.GeoDb.BaseUrl), urlRelative);

            // Implementación de reintentos para manejar el límite de 429 (Too Many Requests) del plan BASIC de RapidAPI
            int maxRetries = 2;
            int delayMs = 1200; // Un poco más de 1 segundo para estar seguros

            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    using var httpRequest = new HttpRequestMessage(HttpMethod.Get, fullUrl);
                    httpRequest.Headers.Add("X-RapidAPI-Key", _options.GeoDb.ApiKey);
                    httpRequest.Headers.Add("X-RapidAPI-Host", _options.GeoDb.ApiHost);

                    _logger.LogInformation($"[GeoDB] Intento {i + 1} para: {fullUrl}");

                    var response = await client.SendAsync(httpRequest);

                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests && i < maxRetries)
                    {
                        _logger.LogWarning($"[GeoDB] Límite de 429 alcanzado. Reintentando en {delayMs}ms...");
                        await Task.Delay(delayMs);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"[GeoDB ERROR] Status: {(int)response.StatusCode}. Body: {errorContent}");
                        
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                            throw new UserFriendlyException("Error de autenticación con la API de ciudades.");

                        throw new UserFriendlyException("El servicio de ciudades está temporalmente saturado. Intente de nuevo en unos segundos.");
                    }

                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var doc = await JsonDocument.ParseAsync(stream);

                    var result = new CitySearchResultDto();
                    if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in data.EnumerateArray())
                        {
                            result.Cities.Add(new CityDto
                            {
                                Id = item.GetProperty("id").ToString(),
                                Name = item.GetProperty("name").GetString() ?? string.Empty,
                                Country = item.GetProperty("country").GetString() ?? string.Empty,
                                CountryCode = item.GetProperty("countryCode").GetString() ?? string.Empty,
                                Region = item.TryGetProperty("region", out var r) ? r.GetString() : null,
                                RegionCode = item.TryGetProperty("regionCode", out var rc) ? rc.GetString() : null,
                                Latitude = item.TryGetProperty("latitude", out var lat) ? lat.ToString() : null,
                                Longitude = item.TryGetProperty("longitude", out var lon) ? lon.ToString() : null,
                                Population = item.TryGetProperty("population", out var pop) ? (pop.ValueKind == JsonValueKind.Number ? pop.GetInt32() : 0) : 0
                            });
                        }
                    }

                    return result;
                }
                catch (Exception ex) when (!(ex is UserFriendlyException || ex is BusinessException))
                {
                    _logger.LogError(ex, "Error consultando GeoDB");
                    if (i == maxRetries) throw new UserFriendlyException("Error al conectar con el servicio de ciudades.");
                    await Task.Delay(delayMs);
                }
            }

            throw new UserFriendlyException("No se pudo completar la búsqueda después de varios intentos.");
        }

        public async Task<CityDto?> GetCityDetailsAsync(string cityId)
        {
            var client = _httpClientFactory.CreateClient("GeoDbClient");
            var fullUrl = new Uri(new Uri(_options.GeoDb.BaseUrl), $"cities/{cityId}");

            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Get, fullUrl);
                httpRequest.Headers.Add("X-RapidAPI-Key", _options.GeoDb.ApiKey);
                httpRequest.Headers.Add("X-RapidAPI-Host", _options.GeoDb.ApiHost);

                var response = await client.SendAsync(httpRequest);
                if (!response.IsSuccessStatusCode) return null;

                using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);

                if (doc.RootElement.TryGetProperty("data", out var data))
                {
                    return new CityDto
                    {
                        Id = data.GetProperty("id").ToString(),
                        Name = data.GetProperty("name").GetString() ?? string.Empty,
                        Country = data.GetProperty("country").GetString() ?? string.Empty,
                        CountryCode = data.GetProperty("countryCode").GetString() ?? string.Empty,
                        Region = data.TryGetProperty("region", out var r) ? r.GetString() : null,
                        RegionCode = data.TryGetProperty("regionCode", out var rc) ? rc.GetString() : null,
                        Latitude = data.TryGetProperty("latitude", out var lat) ? lat.ToString() : null,
                        Longitude = data.TryGetProperty("longitude", out var lon) ? lon.ToString() : null,
                        Population = data.TryGetProperty("population", out var pop) ? (pop.ValueKind == JsonValueKind.Number ? pop.GetInt32() : 0) : 0
                    };
                }
                return null;
            }
            catch { return null; }
        }
    }

    public class ExternalApisOptions
    {
        public GeoDbOptions GeoDb { get; set; } = new();
        public TicketmasterOptions Ticketmaster { get; set; } = new();

        public class GeoDbOptions
        {
            public string BaseUrl { get; set; } = "https://wft-geo-db.p.rapidapi.com/v1/geo/";
            public string ApiHost { get; set; } = "wft-geo-db.p.rapidapi.com";
            public string ApiKey { get; set; } = string.Empty;
        }

        public class TicketmasterOptions
        {
            public string BaseUrl { get; set; } = "https://app.ticketmaster.com/discovery/v2/";
            public string ApiKey { get; set; } = string.Empty;
        }
    }
}