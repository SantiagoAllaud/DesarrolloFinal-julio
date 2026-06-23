using FAFS.Destinations;
using FAFS.Experiences;
using FAFS.Notifications;
using FAFS.Administration;

using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.Users;

namespace FAFS.EntityFrameworkCore;

/// <summary>
/// Contexto principal de Base de Datos para el proyecto FAFS.
/// Hereda de AbpDbContext y configura Entity Framework Core para conectarse a la base de datos.
/// Aquí es donde declaramos nuestras "tablas" (DbSet) y cómo se mapean.
/// </summary>
[ReplaceDbContext(typeof(IIdentityDbContext))]
[ConnectionStringName("Default")]
public class FAFSDbContext : AbpDbContext<FAFSDbContext>, IIdentityDbContext
{
    private const string Schema = "Abp";

    // Tablas de la aplicación
    public DbSet<Destination> Destinations { get; set; }
    public DbSet<DestinationRating> DestinationRatings { get; set; }
    public DbSet<Experience> Experiences { get; set; }
    public DbSet<AppNotification> AppNotifications { get; set; }
    public DbSet<FavoriteDestination> FavoriteDestinations { get; set; }
    public DbSet<ApiUsageMetric> ApiUsageMetrics { get; set; }

    #region Identity
    // Tablas propias de ABP Identity (Usuarios, Roles, etc.)
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }
    #endregion

    public FAFSDbContext(DbContextOptions<FAFSDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Configuración de los modelos al crear la base de datos.
    /// Aquí definimos las restricciones, índices y relaciones entre tablas usando Fluent API.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configuración de los módulos de ABP
        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureBlobStoring();

        // Configuración para la tabla Destination (Destinos)
        builder.Entity<Destination>(b =>
        {
            b.ToTable("Destination", Schema);
            b.ConfigureByConvention(); // Configura las propiedades base de ABP
            b.Property(d => d.Name).IsRequired();
            b.Property(d => d.Country).IsRequired();
            b.Property(d => d.City).IsRequired();
            
            // Configura Coordinates como un Value Object propio
            b.OwnsOne(d => d.Coordinates, c =>
            {
                c.Property(p => p.Latitude).HasColumnName("Latitude").IsRequired();
                c.Property(p => p.Longitude).HasColumnName("Longitude").IsRequired();
            });
        });

        // Configuración para las calificaciones de los destinos
        builder.Entity<DestinationRating>(b =>
        {
            b.ToTable("DestinationRatings", Schema);
            b.ConfigureByConvention();
            b.Property(x => x.Score).IsRequired();
            b.Property(x => x.Comment).HasMaxLength(1000);
            b.HasIndex(x => new { x.UserId, x.DestinationId }).IsUnique(false);
        });

        // Configuración para las experiencias (actividades, tours, etc.)
        builder.Entity<Experience>(b =>
        {
            b.ToTable("Experiences", Schema);
            b.ConfigureByConvention();
            b.Property(x => x.Title).IsRequired().HasMaxLength(ExperienceConsts.MaxTitleLength);
            b.Property(x => x.Description).IsRequired().HasMaxLength(ExperienceConsts.MaxDescriptionLength);
            b.Property(x => x.Rating).IsRequired();
            // Una experiencia pertenece a un destino
            b.HasOne<Destination>().WithMany().HasForeignKey(x => x.DestinationId).IsRequired();
            b.HasIndex(x => x.DestinationId);
        });

        // Configuración de las notificaciones
        builder.Entity<AppNotification>(b =>
        {
            b.ToTable("AppNotifications", Schema);
            b.ConfigureByConvention();
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Message).IsRequired().HasMaxLength(1024);
            b.Property(x => x.Type).IsRequired().HasMaxLength(64);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.IsRead);
        });

        // Configuración de destinos favoritos por usuario
        builder.Entity<FavoriteDestination>(b =>
        {
            b.ToTable("FavoriteDestinations", Schema);
            b.ConfigureByConvention();
            // Un usuario solo puede guardar un mismo destino como favorito una vez
            b.HasIndex(x => new { x.UserId, x.DestinationId }).IsUnique(); 
            // Relación con el Destino, y borrado en cascada (si se borra el destino, se borra el favorito)
            b.HasOne<Destination>().WithMany().HasForeignKey(x => x.DestinationId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración de métricas de uso de API
        builder.Entity<ApiUsageMetric>(b =>
        {
            b.ToTable("ApiUsageMetrics", Schema);
            b.ConfigureByConvention();
            b.Property(x => x.Endpoint).IsRequired().HasMaxLength(256);
            b.Property(x => x.Method).IsRequired().HasMaxLength(16);
        });
    }
}
