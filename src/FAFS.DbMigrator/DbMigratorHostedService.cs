using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FAFS.Data;
using Serilog;
using Volo.Abp;
using Volo.Abp.Data;

namespace FAFS.DbMigrator;

// Servicio en segundo plano (Hosted Service) encargado de disparar el proceso de migración de la base de datos.
public class DbMigratorHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime; // Para apagar la consola cuando termine la migración
    private readonly IConfiguration _configuration; // Para leer la conexión a la base de datos

    public DbMigratorHostedService(IHostApplicationLifetime hostApplicationLifetime, IConfiguration configuration)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _configuration = configuration;
    }

    // Se ejecuta automáticamente al arrancar la aplicación
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Crea la aplicación ABP específica para migraciones
        using (var application = await AbpApplicationFactory.CreateAsync<FAFSDbMigratorModule>(options =>
        {
           options.Services.ReplaceConfiguration(_configuration);
           options.UseAutofac();
           options.Services.AddLogging(c => c.AddSerilog());
           options.AddDataMigrationEnvironment(); // Configura el entorno como entorno de migración
        }))
        {
            await application.InitializeAsync(); // Inicializa el contenedor y servicios de ABP

            // Resuelve el servicio de migración y ejecuta la migración y seeders (datos de prueba/iniciales)
            await application
                .ServiceProvider
                .GetRequiredService<FAFSDbMigrationService>()
                .MigrateAsync();

            await application.ShutdownAsync(); // Cierre limpio de ABP

            // Apaga la aplicación de consola automáticamente al finalizar
            _hostApplicationLifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
