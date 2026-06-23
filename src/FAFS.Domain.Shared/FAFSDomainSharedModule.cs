using FAFS.Localization;
using Volo.Abp.AuditLogging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Localization.ExceptionHandling;
using Volo.Abp.Validation.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.OpenIddict;
using Volo.Abp.BlobStoring.Database;

namespace FAFS;

// Define los recursos compartidos entre todas las capas (Constantes, Enums, Idiomas/Traducciones).
[DependsOn(
    typeof(AbpAuditLoggingDomainSharedModule),
    typeof(AbpBackgroundJobsDomainSharedModule),
    typeof(AbpFeatureManagementDomainSharedModule),
    typeof(AbpPermissionManagementDomainSharedModule),
    typeof(AbpSettingManagementDomainSharedModule),
    typeof(AbpIdentityDomainSharedModule),
    typeof(AbpOpenIddictDomainSharedModule),
    typeof(BlobStoringDatabaseDomainSharedModule)
    )]
public class FAFSDomainSharedModule : AbpModule
{
    // Se ejecuta antes de configurar los servicios principales
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        FAFSGlobalFeatureConfigurator.Configure(); // Configura características globales de ABP
        FAFSModuleExtensionConfigurator.Configure(); // Extiende entidades nativas (agrega FotoUrl y Preferencias al IdentityUser)
    }

    // Configura los servicios de internacionalización y archivos virtuales
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Configura el sistema de archivos virtuales para poder embeber archivos JSON de traducción
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<FAFSDomainSharedModule>();
        });

        // Configura los idiomas soportados por la aplicación y el recurso de traducción por defecto
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Add<FAFSResource>("en") // Agrega el recurso en inglés por defecto
                .AddBaseTypes(typeof(AbpValidationResource)) // Extiende las validaciones base
                .AddVirtualJson("/Localization/FAFS"); // Indica la ruta de los archivos JSON de traducciones

            options.DefaultResourceType = typeof(FAFSResource);
            
            // Agrega los idiomas disponibles para seleccionar en la interfaz
            options.Languages.Add(new LanguageInfo("en", "en", "English")); 
            options.Languages.Add(new LanguageInfo("ar", "ar", "Arabic")); 
            options.Languages.Add(new LanguageInfo("zh-Hans", "zh-Hans", "Chinese (Simplified)")); 
            options.Languages.Add(new LanguageInfo("zh-Hant", "zh-Hant", "Chinese (Traditional)")); 
            options.Languages.Add(new LanguageInfo("cs", "cs", "Czech")); 
            options.Languages.Add(new LanguageInfo("en-GB", "en-GB", "English (United Kingdom)")); 
            options.Languages.Add(new LanguageInfo("fi", "fi", "Finnish")); 
            options.Languages.Add(new LanguageInfo("fr", "fr", "French")); 
            options.Languages.Add(new LanguageInfo("de-DE", "de-DE", "German (Germany)")); 
            options.Languages.Add(new LanguageInfo("hi", "hi", "Hindi ")); 
            options.Languages.Add(new LanguageInfo("hu", "hu", "Hungarian")); 
            options.Languages.Add(new LanguageInfo("is", "is", "Icelandic")); 
            options.Languages.Add(new LanguageInfo("it", "it", "Italian")); 
            options.Languages.Add(new LanguageInfo("pt-BR", "pt-BR", "Portuguese (Brazil)")); 
            options.Languages.Add(new LanguageInfo("ro-RO", "ro-RO", "Romanian (Romania)")); 
            options.Languages.Add(new LanguageInfo("ru", "ru", "Russian")); 
            options.Languages.Add(new LanguageInfo("sk", "sk", "Slovak")); 
            options.Languages.Add(new LanguageInfo("es", "es", "Spanish")); 
            options.Languages.Add(new LanguageInfo("sv", "sv", "Swedish")); 
            options.Languages.Add(new LanguageInfo("tr", "tr", "Turkish")); 
        });
        
        // Mapea los códigos de excepción locales para que traduzca los errores de negocio automáticamente
        Configure<AbpExceptionLocalizationOptions>(options =>
        {
            options.MapCodeNamespace("FAFS", typeof(FAFSResource));
        });
    }
}
