using FAFS.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace FAFS.Controllers;

/// <summary>
/// Controlador base del cual deben heredar todos los demás controladores de la API.
/// En ABP Framework, muchas veces no necesitas crear controladores manualmente porque
/// los AppServices (en FAFS.Application) se exponen como API automáticamente (Auto API Controllers).
/// </summary>
public abstract class FAFSController : AbpControllerBase
{
    protected FAFSController()
    {
        // Define el recurso de localización por defecto (para usar textos traducidos)
        LocalizationResource = typeof(FAFSResource);
    }
}
