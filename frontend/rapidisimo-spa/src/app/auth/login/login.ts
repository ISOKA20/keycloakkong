import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../auth.service';
import { Router } from '@angular/router';

@Component({
  standalone: true,
  selector: 'app-login',
  imports: [FormsModule],
  template: `
    <h2>Login</h2>

    <input [(ngModel)]="email" placeholder="usuario">
    <input [(ngModel)]="password" type="password" placeholder="password">

    <button (click)="login()">Ingresar</button>

    <div style="color:red">{{error()}}</div>
  `
})
export class LoginComponent {

  email = '';
  password = '';
  error = signal('');

  constructor(private auth: AuthService, private router: Router) {}

  login() {
    this.auth.login(this.email, this.password)
      .subscribe({
        next: () => this.router.navigateByUrl('/'),
        error: () => this.error.set('Credenciales inválidas')
      });
  }
}