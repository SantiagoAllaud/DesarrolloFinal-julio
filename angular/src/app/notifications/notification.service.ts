import { Injectable } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable, Subject } from 'rxjs';

export interface AppNotificationDto {
    id: string;
    userId: string;
    title: string;
    message: string;
    isRead: boolean;
    type: string;
    creationTime: string;
}

export interface PagedResultDto<T> {
    totalCount: number;
    items: T[];
}

/// Servicio para interactuar con la API de notificaciones en el backend.
/// Realiza llamadas HTTP para consultar las notificaciones, marcar como leídas, etc.
@Injectable({
    providedIn: 'root'
})
export class NotificationService {
    // Subject (Event Emitter) usado para avisarle a la campanita que debe refrescar la lista de notificaciones
    public notificationUpdated$ = new Subject<void>();

    constructor(private restService: RestService) { }

    /// Obtiene las notificaciones paginadas, opcionalmente filtrando por estado de lectura (Leído/No leído)
    getNotifications(isRead?: boolean): Observable<PagedResultDto<AppNotificationDto>> {
        const query = isRead !== undefined ? `?IsRead=${isRead}` : '';
        return this.restService.request<void, PagedResultDto<AppNotificationDto>>({
            method: 'GET',
            url: `/api/app/notification${query}`
        },
            { apiName: 'Default' });
    }

    /// Obtiene la cantidad de notificaciones sin leer del usuario
    getUnreadCount(): Observable<number> {
        return this.restService.request<void, number>({
            method: 'GET',
            url: `/api/app/notification/unread-count`
        },
            { apiName: 'Default' });
    }

    /// Marca una notificación en específico como leída
    markAsRead(id: string): Observable<void> {
        return this.restService.request<void, void>({
            method: 'POST',
            url: `/api/app/notification/${id}/mark-as-read`
        },
            { apiName: 'Default' });
    }

    /// Marca todas las notificaciones pendientes como leídas de una sola vez
    markAllAsRead(): Observable<void> {
        return this.restService.request<void, void>({
            method: 'POST',
            url: `/api/app/notification/mark-all-as-read`
        },
            { apiName: 'Default' });
    }
}
