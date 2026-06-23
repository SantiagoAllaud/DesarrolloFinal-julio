using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Volo.Abp.Account;
using Volo.Abp.Account.Settings;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Settings;
using FAFS.Notifications;

namespace FAFS.Account
{
    // Reemplaza el servicio de perfil nativo de ABP (ProfileAppService) por nuestra propia versión personalizada
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IProfileAppService), typeof(ProfileAppService), typeof(MyProfileAppService))]
    public class MyProfileAppService : ProfileAppService
    {
        private readonly IRepository<AppNotification, Guid> _notificationRepository; // Repositorio de notificaciones

        public MyProfileAppService(
            IdentityUserManager userManager,
            IOptions<IdentityOptions> identityOptions,
            IRepository<AppNotification, Guid> notificationRepository) 
            : base(userManager, identityOptions) // Pasa los gestores de usuarios de identidad a la clase base de ABP
        {
            _notificationRepository = notificationRepository;
        }

        // Sobrescribe el método de actualización del perfil de usuario
        public override async Task<ProfileDto> UpdateAsync(UpdateProfileDto input)
        {
            var result = await base.UpdateAsync(input); // Llama al comportamiento base de ABP para guardar el perfil

            // Si el usuario actual tiene sesión activa, le mandamos una notificación avisándole del cambio
            if (CurrentUser.Id.HasValue)
            {
                await _notificationRepository.InsertAsync(new AppNotification(
                    GuidGenerator.Create(),
                    CurrentUser.Id.Value,
                    "Datos de cuenta actualizados",
                    "Se han actualizado los datos personales o de seguridad de tu cuenta.",
                    "AccountUpdate"
                ));
            }

            return result; // Retorna el perfil ya modificado
        }

        // Sobrescribe el método de cambio de contraseña
        public override async Task ChangePasswordAsync(ChangePasswordInput input)
        {
            await base.ChangePasswordAsync(input); // Llama al comportamiento base de ABP para cambiar la contraseña en Identity

            // Si el usuario tiene sesión activa, le enviamos una alerta en la base de datos de que modificó su clave
            if (CurrentUser.Id.HasValue)
            {
                await _notificationRepository.InsertAsync(new AppNotification(
                    GuidGenerator.Create(),
                    CurrentUser.Id.Value,
                    "Datos de cuenta actualizados",
                    "Se han actualizado los datos personales o de seguridad de tu cuenta.",
                    "AccountUpdate"
                ));
            }
        }
    }
}
