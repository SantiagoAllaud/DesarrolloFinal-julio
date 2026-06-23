import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { UserProfileService } from '../proxy/users/user-profile.service';
import { PublicUserProfileDto } from '../proxy/users/models';
import { finalize } from 'rxjs/operators';

/// Componente para mostrar la información del perfil público de otros usuarios.
/// Permite que un usuario vea la información básica de otro usuario registrado.
@Component({
    selector: 'app-public-profile',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './public-profile.component.html',
    styleUrls: ['./public-profile.component.scss']
})
export class PublicProfileComponent implements OnInit {
    // Inyección moderna de dependencias mediante la función 'inject' de Angular 16+
    private route = inject(ActivatedRoute); // Permite leer parámetros de la URL (ej: el id del usuario)
    private userProfileService = inject(UserProfileService); // Proxy de ABP para consumir el servicio de perfiles

    user: PublicUserProfileDto | null = null;
    loading = true;
    error: string | null = null;

    ngOnInit(): void {
        // Al inicializar el componente, obtenemos el ID de usuario de los parámetros de la ruta de navegación
        const userId = this.route.snapshot.paramMap.get('id');
        if (userId) {
            this.loadPublicProfile(userId);
        } else {
            this.error = 'No se proporcionó un ID de usuario.';
            this.loading = false;
        }
    }

    /// Llama al servicio del backend para cargar los datos del perfil público del usuario
    loadPublicProfile(id: string) {
        this.userProfileService.getPublicProfile(id)
            .pipe(finalize(() => this.loading = false)) // Asegura desactivar el spinner de carga al terminar
            .subscribe({
                next: (data) => this.user = data,
                error: (err) => {
                    this.error = 'No se pudo cargar el perfil del usuario.';
                    console.error(err);
                }
            });
    }
}
