using FAFS.Destinations;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;


namespace FAFS;

[DependsOn(
    typeof(FAFSDomainModule),
    typeof(FAFSApplicationContractsModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
public class FAFSApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<FAFSApplicationModule>();
        });

        // Configuración del servicio externo GeoDB Cities y Ticketmaster
        // 1 Registrar el HttpClient para las llamadas HTTP
        context.Services.AddHttpClient("GeoDbClient");
        context.Services.AddHttpClient("TicketmasterClient");

        // 2️ Vincular la configuración del archivo appsettings.json (sección ExternalApis)
        context.Services.AddOptions<ExternalApisOptions>()
            .BindConfiguration("ExternalApis");

        // 3️⃣ Registrar el servicio real que implementa la interfaz ICitySearchService
        context.Services.AddTransient<ICitySearchService, GeoDbCitySearchService>();

    }
}
