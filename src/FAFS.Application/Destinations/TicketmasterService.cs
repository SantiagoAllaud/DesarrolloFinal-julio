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
    // Servicio para consultar eventos y recitales locales usando la API oficial de Ticketmaster.
    // La interfaz ITransientDependency hace que ABP lo registre automáticamente en la inyección de dependencias.
    public class TicketmasterService : ITicketmasterService, ITransientDependency
    {
        private readonly IHttpClientFactory _httpClientFactory; // Para crear y administrar llamadas HTTP
        private readonly ExternalApisOptions _options; // Opciones que contienen la API Key y la URL Base
        private readonly ILogger<TicketmasterService> _logger; // Para loguear warnings o errores

        public TicketmasterService(
            IHttpClientFactory httpClientFactory,
            IOptions<ExternalApisOptions> options,
            ILogger<TicketmasterService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        // Obtiene una lista de los próximos 5 eventos programados para una ciudad en los siguientes 3 meses.
        public async Task<List<DestinationEventDto>> GetEventsForCityAsync(string city)
        {
            var events = new List<DestinationEventDto>();
            // Si no nos pasan ninguna ciudad, devolvemos una lista vacía directamente
            if (string.IsNullOrWhiteSpace(city)) return events;

            try
            {
                var client = _httpClientFactory.CreateClient("TicketmasterClient");
                var baseUrl = _options.Ticketmaster.BaseUrl.TrimEnd('/');
                
                // Formateamos la fecha actual y la fecha de acá a 3 meses en formato ISO 8601 (requerido por Ticketmaster)
                var startDateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                var endDateTime = DateTime.UtcNow.AddMonths(3).ToString("yyyy-MM-ddTHH:mm:ssZ");
                
                // Construimos la URL agregando la API Key, la ciudad, las fechas y configuramos para traer máximo 5 eventos ordenados por fecha
                var url = $"{baseUrl}/events.json?apikey={_options.Ticketmaster.ApiKey}&city={Uri.EscapeDataString(city)}&startDateTime={startDateTime}&endDateTime={endDateTime}&sort=date,asc&size=5";

                var response = await client.GetAsync(url);

                // Si la API falla, registramos el código de error y retornamos una lista vacía para no romper la app
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"[Ticketmaster] Error consultando eventos. Status: {response.StatusCode}");
                    return events;
                }

                // Si salió bien, leemos la respuesta web como JSON
                using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);

                // Buscamos los eventos dentro de la estructura JSON ("_embedded" -> "events")
                if (doc.RootElement.TryGetProperty("_embedded", out var embedded) && 
                    embedded.TryGetProperty("events", out var eventsArray))
                {
                    foreach (var item in eventsArray.EnumerateArray())
                    {
                        var eventDto = new DestinationEventDto
                        {
                            Name = item.GetProperty("name").GetString() ?? string.Empty, // Nombre del show/evento
                            Url = item.TryGetProperty("url", out var u) ? u.GetString() : null // Link para comprar entradas
                        };

                        // Extraemos la fecha y hora del evento
                        if (item.TryGetProperty("dates", out var dates) && dates.TryGetProperty("start", out var start))
                        {
                            eventDto.Date = start.TryGetProperty("localDate", out var d) ? d.GetString() : null;
                            eventDto.Time = start.TryGetProperty("localTime", out var t) ? t.GetString() : null;
                        }

                        // Extraemos la primera imagen publicitaria del evento
                        if (item.TryGetProperty("images", out var images) && images.GetArrayLength() > 0)
                        {
                            eventDto.ImageUrl = images[0].TryGetProperty("url", out var iu) ? iu.GetString() : null;
                        }

                        // Extraemos el nombre del lugar físico del show (estadio, teatro, etc.)
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
                // En caso de que se caiga la conexión o falle el parseo del JSON, se loguea la excepción
                _logger.LogError(ex, $"[Ticketmaster] Excepción al obtener eventos para la ciudad: {city}");
            }

            return events;
        }
    }
}
