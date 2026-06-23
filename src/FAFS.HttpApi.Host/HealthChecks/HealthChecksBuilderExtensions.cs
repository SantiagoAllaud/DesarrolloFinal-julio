using System;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FAFS.HealthChecks;

/// <summary>
/// Extensiones para configurar los "Health Checks" (Chequeos de Salud).
/// Los Health Checks sirven para que herramientas externas (como Docker o Kubernetes)
/// puedan saber si esta API está "viva" y funcionando correctamente, o si se cayó la base de datos.
/// </summary>
public static class HealthChecksBuilderExtensions
{
    public static void AddFAFSHealthChecks(this IServiceCollection services)
    {
        // 1. Agrega los chequeos de salud básicos
        var healthChecksBuilder = services.AddHealthChecks();
        
        // 2. Agrega un chequeo específico para la base de datos de FAFS
        // (Verifica que la app pueda conectarse a SQL Server)
        healthChecksBuilder.AddCheck<FAFSDatabaseCheck>("FAFS DbContext Check", tags: new string[] { "database" });

        // 3. Configura en qué URL responde la API si está viva (ej: "/health-status")
        services.ConfigureHealthCheckEndpoint("/health-status");

        var configuration = services.GetConfiguration();
        var healthCheckUrl = configuration["App:HealthCheckUrl"];

        if (string.IsNullOrEmpty(healthCheckUrl))
        {
            healthCheckUrl = "/health-status";
        }

        // 4. Agrega una Interfaz Gráfica (UI) para ver los Health Checks bonito en el navegador
        var healthChecksUiBuilder = services.AddHealthChecksUI(settings =>
        {
            settings.AddHealthCheckEndpoint("FAFS Health Status", configuration["App:HealthUiCheckUrl"] ?? healthCheckUrl);
        });

        // Configura que el historial de la interfaz gráfica se guarde en memoria temporal
        healthChecksUiBuilder.AddInMemoryStorage();

        // 5. Configura las URLs de la Interfaz Gráfica
        services.MapHealthChecksUiEndpoints(options =>
        {
            options.UIPath = "/health-ui";
            options.ApiPath = "/health-api";
        });
    }

    private static IServiceCollection ConfigureHealthCheckEndpoint(this IServiceCollection services, string path)
    {
        services.Configure<AbpEndpointRouterOptions>(options =>
        {
            options.EndpointConfigureActions.Add(endpointContext =>
            {
                endpointContext.Endpoints.MapHealthChecks(
                    new PathString(path.EnsureStartsWith('/')),
                    new HealthCheckOptions
                    {
                        Predicate = _ => true,
                        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
                        AllowCachingResponses = false,
                    });
            });
        });

        return services;
    }

    private static IServiceCollection MapHealthChecksUiEndpoints(this IServiceCollection services, Action<global::HealthChecks.UI.Configuration.Options>? setupOption = null)
    {
        services.Configure<AbpEndpointRouterOptions>(routerOptions =>
        {
            routerOptions.EndpointConfigureActions.Add(endpointContext =>
            {
                endpointContext.Endpoints.MapHealthChecksUI(setupOption);
            });
        });

        return services;
    }
}
