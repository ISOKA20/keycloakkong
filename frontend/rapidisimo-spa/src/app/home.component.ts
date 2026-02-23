import { Component, inject, signal, resource } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common'; // Para el pipe json
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  selector: 'app-home',
  imports: [CommonModule],
  template: `
    <h1>hola carola</h1>

    @if (apiResource.isLoading()) {
      <p>Cargando desde Kong...</p>
    } @else if (apiResource.error()) {
      <p style="color: red;">Error: No se pudo conectar con el API</p>
    } @else {
      <div class="card">
        <h3>Datos recibidos:</h3>
        <pre>{{ apiResource.value() | json }}</pre>
      </div>
    }

    <button (click)="apiResource.reload()">Refrescar datos</button>
  `
})
export class HomeComponent {
  private http = inject(HttpClient);

  // Usamos 'resource' (Angular 19/20) para manejar la petición HTTP
  apiResource = resource({
    loader: () => {
      // Usamos firstValueFrom para convertir el Observable a Promesa
      return firstValueFrom(
        this.http.get('http://localhost:8000/api/get', { 
          withCredentials: true 
        })
      );
    }
  });
}