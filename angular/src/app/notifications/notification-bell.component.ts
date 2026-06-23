import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService, AppNotificationDto } from './notification.service';
import { CoreModule } from '@abp/ng.core';
import { ThemeSharedModule } from '@abp/ng.theme.shared';
import { Subscription } from 'rxjs';

/// Componente de la campanita de notificaciones en la barra de navegación.
/// Escucha en tiempo real (mediante HTTP interceptor) y muestra una burbuja emergente (popup)
/// temporal y un modal detallado de notificaciones clasificadas por Leídas/No leídas.
@Component({
    selector: 'app-notification-bell',
    standalone: true,
    imports: [CommonModule, CoreModule, ThemeSharedModule],
    template: `
    <!-- Icono de Campanita -->
    <a
      class="nav-link"
      role="button"
      (click)="openModal()"
    >
      <i class="fa fa-bell"></i>
      <span class="badge bg-danger position-absolute top-0 start-100 translate-middle badge-pill" *ngIf="unreadCount > 0">
        {{ unreadCount }}
      </span>
    </a>

    <!-- Ventana Emergente Temporal (se muestra 5 segundos al recibir nueva notificación) -->
    <div 
      *ngIf="showPopup && latestNotification" 
      class="notification-popup shadow-lg rounded border bg-white p-3 position-absolute"
      style="top: 100%; right: 0; width: 300px; z-index: 1050; animation: fadeIn 0.3s;"
    >
      <div class="d-flex justify-content-between align-items-center mb-2">
         <h6 class="mb-0 text-primary"><i class="fa fa-info-circle me-1"></i> Nueva Notificación</h6>
         <button type="button" class="btn-close" style="font-size: 0.7em;" (click)="closePopup()"></button>
      </div>
      <div class="fw-bold">{{ latestNotification.title }}</div>
      <p class="mb-1 small text-muted text-wrap">{{ latestNotification.message }}</p>
    </div>

    <!-- Fondo Oscuro del Modal (Backdrop) -->
    <div *ngIf="isModalOpen" class="modal-backdrop fade show" (click)="closeModal()"></div>

    <!-- Contenido del Modal de Notificaciones -->
    <div 
        *ngIf="isModalOpen" 
        class="modal fade show custom-modal" 
        tabindex="-1" 
        role="dialog" 
        style="display: block;"
        (click)="onModalClick($event)"
    >
      <div class="modal-dialog modal-dialog-centered modal-lg modal-dialog-scrollable" role="document">
        <div class="modal-content shadow-lg border-0 rounded-4">
          <div class="modal-header bg-light border-bottom-0 pb-0">
            <h5 class="modal-title fw-bold">Notificaciones</h5>
            <button type="button" class="btn-close" aria-label="Close" (click)="closeModal()"></button>
          </div>
          <div class="modal-header bg-light pt-2 pb-0 border-bottom">
            <!-- Pestañas (Tabs) de Navegación -->
            <ul class="nav nav-tabs border-0 w-100">
              <li class="nav-item">
                <a class="nav-link" [class.active]="activeTab === 'general'" (click)="activeTab = 'general'" role="button">
                    General
                </a>
              </li>
              <li class="nav-item">
                <a class="nav-link" [class.active]="activeTab === 'unread'" (click)="activeTab = 'unread'" role="button">
                    No leídas <span class="badge bg-danger ms-1" *ngIf="unreadCount > 0">{{ unreadCount }}</span>
                </a>
              </li>
              <li class="nav-item">
                <a class="nav-link" [class.active]="activeTab === 'read'" (click)="activeTab = 'read'" role="button">
                    Leídas
                </a>
              </li>
            </ul>
          </div>
          
          <div class="modal-body p-0" style="min-height: 300px; background-color: #f8f9fa;">
            <div class="list-group list-group-flush">
                <ng-container *ngIf="filteredNotifications.length > 0; else noNotifs">
                    <button 
                        class="list-group-item list-group-item-action d-flex flex-column align-items-start p-3 border-bottom" 
                        *ngFor="let n of filteredNotifications" 
                        (click)="markAsRead(n)" 
                        [class.bg-white]="n.isRead"
                        [class.bg-light]="!n.isRead"
                    >
                        <div class="d-flex w-100 justify-content-between align-items-center mb-1">
                            <h6 class="mb-0" [class.fw-bold]="!n.isRead" [class.text-dark]="!n.isRead" [class.text-secondary]="n.isRead">
                                <span *ngIf="!n.isRead" class="badge bg-primary rounded-circle p-1 me-2" style="width: 10px; height: 10px; display: inline-block;"></span>
                                {{ n.title }}
                            </h6>
                            <small class="text-muted">{{ n.creationTime | date:'short' }}</small>
                        </div>
                        <p class="mb-1 text-wrap ms-3" style="font-size: 0.9em;" [class.text-muted]="n.isRead">{{ n.message }}</p>
                    </button>
                </ng-container>
                <ng-template #noNotifs>
                    <div class="p-5 text-center text-muted d-flex flex-column align-items-center justify-content-center h-100">
                        <i class="fa fa-bell-slash fa-3x mb-3 text-light"></i>
                        <h5>No hay notificaciones</h5>
                        <p>No tienes notificaciones en esta sección en este momento.</p>
                    </div>
                </ng-template>
            </div>
          </div>
          
          <div class="modal-footer bg-white border-top">
             <div class="w-100 d-flex justify-content-between">
                <button type="button" class="btn btn-outline-secondary btn-sm" (click)="closeModal()">
                    Cerrar
                </button>
                <button 
                    *ngIf="unreadCount > 0"
                    type="button" 
                    class="btn btn-link text-primary btn-sm text-decoration-none" 
                    (click)="markAllAsRead()"
                >
                    <i class="fa fa-check-double me-1"></i> Marcar todas como leídas
                </button>
             </div>
          </div>
        </div>
      </div>
    </div>
  `,
    styles: [`
    .nav-link { position: relative; cursor: pointer; }
    .text-wrap { white-space: normal; }
    .custom-modal { z-index: 1055; }
    .modal-backdrop { z-index: 1050; }
    
    .list-group-item { transition: background-color 0.2s; }
    .list-group-item:hover { background-color: #f1f3f5 !important; }
    
    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(-10px); }
      to { opacity: 1; transform: translateY(0); }
    }
  `]
})
export class NotificationBellComponent implements OnInit, OnDestroy {
    notifications: AppNotificationDto[] = [];
    unreadCount = 0;
    
    // Configuración del Popup emergente
    showPopup = false;
    latestNotification: AppNotificationDto | null = null;
    private popupTimer: any;
    private updateSub: Subscription | undefined;

    // Configuración del Modal
    isModalOpen = false;
    activeTab: 'general' | 'unread' | 'read' = 'general';

    constructor(private notificationService: NotificationService) { }

    ngOnInit() {
        this.loadNotifications();
        this.loadUnreadCount();

        // Escucha novedades gatilladas por el interceptor HTTP de notificaciones
        this.updateSub = this.notificationService.notificationUpdated$.subscribe(() => {
            this.handleNotificationUpdate();
        });
    }

    ngOnDestroy() {
        if (this.updateSub) {
            this.updateSub.unsubscribe();
        }
        if (this.popupTimer) {
            clearTimeout(this.popupTimer);
        }
    }

    /// Filtra la lista de notificaciones localmente según la pestaña activa
    get filteredNotifications(): AppNotificationDto[] {
        if (this.activeTab === 'unread') {
            return this.notifications.filter(n => !n.isRead);
        }
        if (this.activeTab === 'read') {
            return this.notifications.filter(n => n.isRead);
        }
        return this.notifications;
    }

    openModal() {
        this.showPopup = false;
        this.activeTab = 'general';
        this.loadNotifications();
        this.isModalOpen = true;
        document.body.classList.add('modal-open');
    }

    closeModal() {
        this.isModalOpen = false;
        document.body.classList.remove('modal-open');
    }

    onModalClick(event: MouseEvent) {
        // Cierra el modal si se hace clic por fuera de su caja
        if ((event.target as HTMLElement).classList.contains('modal')) {
            this.closeModal();
        }
    }

    /// Método que responde a una nueva notificación detectada
    handleNotificationUpdate() {
        this.loadUnreadCount();
        this.notificationService.getNotifications().subscribe((res) => {
            const newItems = res.items;
            // Si hay notificaciones nuevas y es distinta a la última conocida, muestra el popup temporal
            if (newItems.length > 0 && 
                (!this.notifications.length || newItems[0].id !== this.notifications[0].id)) {
                
                this.notifications = newItems;
                this.latestNotification = newItems[0];
                this.displayPopup();
            } else {
                this.notifications = newItems;
            }
        });
    }

    /// Muestra la notificación en la cajita flotante durante 5 segundos
    displayPopup() {
        this.showPopup = true;
        if (this.popupTimer) {
            clearTimeout(this.popupTimer);
        }
        this.popupTimer = setTimeout(() => {
            this.showPopup = false;
        }, 5000);
    }

    closePopup() {
        this.showPopup = false;
        if (this.popupTimer) {
            clearTimeout(this.popupTimer);
        }
    }

    loadNotifications() {
        this.notificationService.getNotifications().subscribe((res) => {
            this.notifications = res.items;
        });
    }

    loadUnreadCount() {
        this.notificationService.getUnreadCount().subscribe((count) => {
            this.unreadCount = count;
        });
    }

    /// Marca una notificación individual como leída
    markAsRead(notification: AppNotificationDto) {
        if (!notification.isRead) {
            this.notificationService.markAsRead(notification.id).subscribe(() => {
                notification.isRead = true;
                this.unreadCount = Math.max(0, this.unreadCount - 1);
            });
        }
    }

    /// Marca la totalidad de notificaciones del usuario como leídas
    markAllAsRead() {
        this.notificationService.markAllAsRead().subscribe(() => {
            this.notifications.forEach(n => n.isRead = true);
            this.unreadCount = 0;
        });
    }
}
