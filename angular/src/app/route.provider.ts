import { RoutesService, eLayoutType } from '@abp/ng.core';
import { inject, provideAppInitializer } from '@angular/core';

/// Proveedor de rutas para configurar el menú de navegación principal de la aplicación.
/// ABP utiliza este proveedor para inyectar dinámicamente los elementos en la barra lateral/menú superior.
export const APP_ROUTE_PROVIDER = [
  provideAppInitializer(() => {
    configureRoutes();
  }),
];

function configureRoutes() {
  const routes = inject(RoutesService);
  routes.add([
    // Ruta de Inicio (Home)
    {
      path: '/',
      name: '::Menu:Home',
      iconClass: 'fas fa-home',
      order: 1,
      layout: eLayoutType.application,
    },
    // Ruta de Destinos (Destinations)
    {
      path: '/destinations',
      name: '::Menu:Destinations',
      iconClass: 'fas fa-map-marked-alt',
      order: 2,
      layout: eLayoutType.application,
    },
    // Ruta de Mi Perfil (MyProfile)
    {
      path: '/my-profile',
      name: '::Menu:MyProfile',
      iconClass: 'fas fa-user',
      order: 3,
      layout: eLayoutType.application,
    },
  ]);
}
