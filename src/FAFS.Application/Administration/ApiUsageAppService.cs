using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace FAFS.Administration;

// Servicio de administración encargado de registrar y calcular las métricas de uso de la API.
public class ApiUsageAppService : ApplicationService, IApiUsageAppService
{
    private readonly IRepository<ApiUsageMetric, Guid> _repository; // Base de datos donde se guardan las métricas

    public ApiUsageAppService(IRepository<ApiUsageMetric, Guid> repository)
    {
        _repository = repository;
    }

    // Calcula y obtiene las estadísticas generales del uso de la API en el sistema
    public async Task<ApiUsageStatisticsDto> GetStatisticsAsync()
    {
        var metrics = await _repository.GetListAsync(); // Trae todas las métricas de la base de datos

        // Si no hay métricas guardadas aún, devuelve un reporte vacío para no romper la app
        if (!metrics.Any())
        {
            return new ApiUsageStatisticsDto
            {
                MostUsedEndpoints = new List<EndpointExecutionDto>()
            };
        }

        // Construye el reporte consolidado de estadísticas
        var stats = new ApiUsageStatisticsDto
        {
            TotalCalls = metrics.Count, // Cantidad total de llamadas a la API
            AverageExecutionTime = metrics.Average(x => x.ExecutionTime), // Tiempo de respuesta promedio global
            SuccessCount = metrics.Count(x => x.StatusCode >= 200 && x.StatusCode < 300), // Cantidad de peticiones exitosas (códigos 2xx)
            ErrorCount = metrics.Count(x => x.StatusCode >= 400), // Cantidad de peticiones fallidas (códigos >= 400)
            
            // Agrupa y calcula el Top 10 de los endpoints (rutas) más consultados
            MostUsedEndpoints = metrics
                .GroupBy(x => x.Endpoint)
                .Select(g => new EndpointExecutionDto
                {
                    Endpoint = g.Key,
                    CallCount = g.Count(), // Cuántas veces se llamó
                    AverageExecutionTime = g.Average(x => x.ExecutionTime) // Tiempo promedio de ejecución de esta ruta
                })
                .OrderByDescending(x => x.CallCount) // Ordena de mayor a menor cantidad de llamadas
                .Take(10) // Toma solo los 10 primeros
                .ToList()
        };

        return stats;
    }

    // Registra una nueva métrica cada vez que se llama a un endpoint de la API
    public async Task RecordMetricAsync(string endpoint, string method, int statusCode, double executionTime, string? clientIp = null)
    {
        var metric = new ApiUsageMetric(
            Guid.NewGuid(), // Usamos Guid.NewGuid() para simplificar tests unitarios manuales
            endpoint,
            method,
            statusCode,
            executionTime,
            clientIp
        );

        // Inserta la métrica en la base de datos
        await _repository.InsertAsync(metric);
    }
}
