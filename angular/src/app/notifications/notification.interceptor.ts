import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { NotificationService } from './notification.service';

/// Interceptor HTTP para refrescar automáticamente la campanita de notificaciones.
/// Intercepta las respuestas exitosas de las peticiones para detectar si el usuario
/// guardó un destino, actualizó su perfil, cambió su contraseña o quitó favoritos.
/// En caso afirmativo, le avisa al NotificationService para refrescar las notificaciones del header.
@Injectable()
export class NotificationInterceptor implements HttpInterceptor {

  constructor(private notificationService: NotificationService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      tap((event: HttpEvent<any>) => {
        if (event instanceof HttpResponse) {
          // Detectamos si la petición fue exitosa y de interés para notificaciones
          const isProfileUpdate = (request.url.includes('/api/account/my-profile') && request.method === 'PUT') ||
                                  (request.url.includes('/api/account/my-profile/change-password') && request.method === 'POST');
          const isDestinationCreate = request.url.includes('/api/app/destination') && request.method === 'POST';
          const isDestinationUpdate = request.url.includes('/api/app/destination') && request.method === 'PUT';
          const isFavoriteToggle = request.url.includes('/api/app/favorite-destination/toggle-favorite') && request.method === 'POST';

          if (isProfileUpdate || isDestinationCreate || isDestinationUpdate || isFavoriteToggle) {
            // Damos una pequeña demora de 500ms para asegurar que la transacción del backend 
            // se complete del todo antes de gatillar la recarga de notificaciones
            setTimeout(() => {
                this.notificationService.notificationUpdated$.next();
            }, 500);
          }
        }
      })
    );
  }
}
