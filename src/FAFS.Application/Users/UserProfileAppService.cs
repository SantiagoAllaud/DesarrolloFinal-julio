using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace FAFS.Users;

// Servicio para gestionar el perfil del usuario activo (información personal, foto de perfil y eliminación de cuenta).
public class UserProfileAppService : FAFSAppService, IUserProfileAppService
{
    protected IdentityUserManager UserManager { get; } // Gestor de usuarios de ASP.NET Core / ABP
    protected IIdentityUserRepository UserRepository { get; } // Repositorio para guardar cambios en la base de datos de usuarios

    public UserProfileAppService(
        IdentityUserManager userManager,
        IIdentityUserRepository userRepository)
    {
        UserManager = userManager;
        UserRepository = userRepository;
    }

    // Obtiene la información pública de cualquier usuario usando su ID (UserName, Nombre, Apellido, Email y Foto)
    public virtual async Task<PublicUserProfileDto> GetPublicProfileAsync(Guid id)
    {
        var user = await UserManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            throw new UserFriendlyException("Usuario no encontrado");
        }

        return new PublicUserProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Name = user.Name,
            Surname = user.Surname,
            Email = user.Email,
            FotoUrl = user.GetProperty<string>("FotoUrl") // Propiedad extra/personalizada agregada a la tabla de ABP
        };
    }

    // Obtiene el perfil del usuario logueado en este momento (Requiere autorización)
    [Authorize]
    public virtual async Task<PublicUserProfileDto> GetMyProfileAsync()
    {
        var userId = CurrentUser.GetId();
        return await GetPublicProfileAsync(userId);
    }

    // Permite al usuario borrar su propia cuenta del sistema de forma permanente
    [Authorize]
    public virtual async Task DeleteMyAccountAsync()
    {
        var userId = CurrentUser.GetId();
        var user = await UserManager.FindByIdAsync(userId.ToString());
        
        if (user == null)
        {
            throw new UserFriendlyException("Usuario no encontrado");
        }

        // Borra al usuario del sistema de identidad
        var result = await UserManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            throw new UserFriendlyException("Error al eliminar la cuenta: " + string.Join(", ", result.Errors));
        }
    }

    // Permite al usuario actualizar la URL de su foto de perfil
    [Authorize]
    public virtual async Task UpdateProfilePictureAsync(UpdateProfilePictureDto input)
    {
        try
        {
            var userId = CurrentUser.GetId();
            var user = await UserManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                throw new UserFriendlyException("Usuario no encontrado");
            }

            if (string.IsNullOrEmpty(input.FotoUrl))
            {
                throw new UserFriendlyException("La imagen está vacía");
            }

            // Asigna la nueva URL de la foto en la propiedad extendida "FotoUrl" de la entidad User
            user.SetProperty("FotoUrl", input.FotoUrl);
            
            // Guarda los cambios en la base de datos
            await UserRepository.UpdateAsync(user, autoSave: true);
        }
        catch (Exception ex) when (!(ex is UserFriendlyException))
        {
            throw new UserFriendlyException("Error interno al procesar la imagen: " + ex.Message);
        }
    }
}
