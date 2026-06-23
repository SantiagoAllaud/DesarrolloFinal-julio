using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace FAFS;

/// <summary>
/// Punto de entrada principal (Entry Point) de la aplicación web.
/// Aquí es donde arranca todo el servidor, se configuran los logs y se inicializa el contenedor de dependencias (Autofac).
/// </summary>
public class Program
{
    public async static Task<int> Main(string[] args)
    {
        // 1. Configuración temprana de los logs (Serilog) antes de que arranque la app
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .WriteTo.Async(c => c.Console())
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Arrancando el servidor web FAFS.HttpApi.Host...");
            
            // 2. Crea el constructor de la aplicación web (estándar de ASP.NET Core)
            var builder = WebApplication.CreateBuilder(args);
            
            // 3. Configuración del Host (Motor)
            builder.Host
                .AddAppSettingsSecretsJson()
                .UseAutofac() // Usa Autofac como inyector de dependencias (muy usado en ABP)
                .UseSerilog((context, services, loggerConfiguration) =>
                {
                    loggerConfiguration
                    #if DEBUG
                        .MinimumLevel.Debug()
                    #else
                        .MinimumLevel.Information()
                    #endif
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                        .Enrich.FromLogContext()
                        .WriteTo.Async(c => c.File("Logs/logs.txt"))
                        .WriteTo.Async(c => c.Console())
                        .WriteTo.Async(c => c.AbpStudio(services));
                });
                
            // 4. Registra nuestro módulo principal de ABP en la aplicación
            await builder.AddApplicationAsync<FAFSHttpApiHostModule>();
            
            // 5. Construye la app (compila la configuración)
            var app = builder.Build();
            
            // 6. Inicializa la tubería (Pipeline) HTTP y los middlewares de ABP
            await app.InitializeApplicationAsync();
            
            // 7. Pone a correr el servidor esperando peticiones
            await app.RunAsync();
            
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }

            Log.Fatal(ex, "¡El servidor se cerró inesperadamente!");
            return 1;
        }
        finally
        {
            // Asegura que los logs pendientes se escriban en disco al cerrar
            Log.CloseAndFlush();
        }
    }
}
// trigger rebuild
