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
    // Servicio que se conecta con la API externa de GeoDB Cities para buscar ciudades y sus detalles.
    public class GeoDbCitySearchService : ICitySearchService
    {
        private readonly IHttpClientFactory _httpClientFactory; // Creador de clientes HTTP para hacer las peticiones
        private readonly ExternalApisOptions _options; // Opciones con la API Key y la URL Base de las APIs externas
        private readonly ILogger<GeoDbCitySearchService> _logger; // Para loguear info o errores en la consola

        public GeoDbCitySearchService(
            IHttpClientFactory httpClientFactory,
            IOptions<ExternalApisOptions> options,
            ILogger<GeoDbCitySearchService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        // Realiza una búsqueda de ciudades filtrando por texto, país, región, población mínima, etc.
        public async Task<CitySearchResultDto> SearchCitiesAsync(CitySearchRequestDto request)
        {
            // Valida que el texto que escribiste para buscar no esté vacío y tenga al menos 2 letras
            if (string.IsNullOrWhiteSpace(request.PartialName) || request.PartialName.Trim().Length < 2)
            {
                throw new BusinessException("CitySearch:InvalidPartialName")
                    .WithData("Message", "El texto de búsqueda debe tener al menos 2 caracteres.");
            }

            var client = _httpClientFactory.CreateClient("GeoDbClient");
            
            // Construimos la URL de consulta dinámica basada en los filtros
            var urlRelative = $"cities?namePrefix={Uri.EscapeDataString(request.PartialName)}&limit={request.Limit}";

            if (!string.IsNullOrWhiteSpace(request.CountryCode))
                urlRelative += $"&countryIds={Uri.EscapeDataString(request.CountryCode)}";

            if (!string.IsNullOrWhiteSpace(request.RegionCode))
                urlRelative += $"&regionIds={Uri.EscapeDataString(request.RegionCode)}";

            if (request.MinPopulation.HasValue)
                urlRelative += $"&minPopulation={request.MinPopulation.Value}";

            var fullUrl = new Uri(new Uri(_options.GeoDb.BaseUrl), urlRelative);

            // Reintentos para manejar el error 429 (Too Many Requests / Demasiadas peticiones)
            // de la versión gratis (Basic) de la API en RapidAPI.
            int maxRetries = 2;
            int delayMs = 1200; // Esperamos 1.2 segundos para reintentar y no saturar

            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    // Armamos la petición web de tipo GET
                    using var httpRequest = new HttpRequestMessage(HttpMethod.Get, fullUrl);
                    // Agregamos las llaves de seguridad requeridas por RapidAPI
                    httpRequest.Headers.Add("X-RapidAPI-Key", _options.GeoDb.ApiKey);
                    httpRequest.Headers.Add("X-RapidAPI-Host", _options.GeoDb.ApiHost);

                    _logger.LogInformation($"[GeoDB] Intento {i + 1} para: {fullUrl}");

                    var response = await client.SendAsync(httpRequest);

                    // Si la API nos dice que hicimos muchas peticiones seguidas (429) y nos quedan reintentos...
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests && i < maxRetries)
                    {
                        _logger.LogWarning($"[GeoDB] Límite de 429 alcanzado. Reintentando en {delayMs}ms...");
                        await Task.Delay(delayMs); // Esperamos un segundo largo y reintentamos en el próximo bucle
                        continue;
                    }

                    // Si no fue exitosa (código 200), manejamos el error
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"[GeoDB ERROR] Status: {(int)response.StatusCode}. Body: {errorContent}");
                        
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                            throw new UserFriendlyException("Error de autenticación con la API de ciudades.");

                        throw new UserFriendlyException("El servicio de ciudades está temporalmente saturado. Intente de nuevo en unos segundos.");
                    }

                    // Si salió bien, leemos la respuesta JSON
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var doc = await JsonDocument.ParseAsync(stream);

                    var result = new CitySearchResultDto();
                    // Extraemos la lista de ciudades que viene dentro de la propiedad "data"
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
                    // Si falló y ya no nos quedan intentos de reintento, tiramos error final
                    if (i == maxRetries) throw new UserFriendlyException("Error al conectar con el servicio de ciudades.");
                    await Task.Delay(delayMs);
                }
            }

            throw new UserFriendlyException("No se pudo completar la búsqueda después de varios intentos.");
        }

        // Obtiene la información súper detallada de una ciudad específica usando su ID
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

                // Mapea la información detallada de la ciudad de JSON a nuestro objeto de C#
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

    // Estructuras de configuración para las APIs externas de GeoDB y Ticketmaster
    public class ExternalApisOptions
    {
        public GeoDbOptions GeoDb { get; set; } = new();
        public TicketmasterOptions Ticketmaster { get; set; } = new();

        // Opciones de configuración específicas para GeoDB (URL, host y api key)
        public class GeoDbOptions
        {
            public string BaseUrl { get; set; } = "https://wft-geo-db.p.rapidapi.com/v1/geo/";
            public string ApiHost { get; set; } = "wft-geo-db.p.rapidapi.com";
            public string ApiKey { get; set; } = string.Empty;
        }

        // Opciones de configuración específicas para la API de Ticketmaster (URL y api key)
        public class TicketmasterOptions
        {
            public string BaseUrl { get; set; } = "https://app.ticketmaster.com/discovery/v2/";
            public string ApiKey { get; set; } = string.Empty;
        }
    }
}