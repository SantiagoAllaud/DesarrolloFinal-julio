using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using FAFS.EntityFrameworkCore;


namespace FAFS.DbMigrator;

// Módulo ABP para la herramienta de migración de base de datos.
[DependsOn(
    typeof(AbpAutofacModule), // Contenedor de dependencias Autofac
    typeof(FAFSEntityFrameworkCoreModule), // Acceso a la base de datos (DbContext y repositorios)
    typeof(FAFSApplicationContractsModule) // Contratos de aplicación
    )]
public class FAFSDbMigratorModule : AbpModule
{
}
