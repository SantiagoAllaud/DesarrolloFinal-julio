using System.ComponentModel.DataAnnotations;
using Volo.Abp.Identity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.Threading;

namespace FAFS;

// Configuración para extender los módulos existentes de ABP (por ejemplo, agregar campos extras a los usuarios).
public static class FAFSModuleExtensionConfigurator
{
    private static readonly OneTimeRunner OneTimeRunner = new OneTimeRunner();

    public static void Configure()
    {
        OneTimeRunner.Run(() =>
        {
            ConfigureExistingProperties();
            ConfigureExtraProperties();
        });
    }

    private static void ConfigureExistingProperties()
    {
        /* Aquí se pueden modificar longitudes máximas de campos existentes de ABP */
    }

    // Método para agregar propiedades personalizadas (campos extra) a las entidades nativas de ABP
    private static void ConfigureExtraProperties()
    {
        ObjectExtensionManager.Instance.Modules()
            .ConfigureIdentity(identity =>
            {
                // Configura la entidad nativa de Usuario (IdentityUser)
                identity.ConfigureUser(user =>
                {
                    // 1. Agrega el campo extra "FotoUrl" (de tipo string) para guardar la foto de perfil del usuario
                    user.AddOrUpdateProperty<string>(
                        "FotoUrl",
                        property =>
                        {
                            // Configuración básica
                        }
                    );
                    
                    // 2. Agrega el campo extra "Preferencias" (de tipo string con límite de 2048 letras) para almacenar sus gustos
                    user.AddOrUpdateProperty<string>(
                        "Preferencias",
                        property =>
                        {
                            property.Attributes.Add(new StringLengthAttribute(2048));
                        }
                    );
                });
            });
    }
}
