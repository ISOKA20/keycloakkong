import { inject, Injectable } from '@angular/core';
import { ApiService } from '../core/api.service';

@Injectable({ providedIn: 'root' })
export class AuthService {

  private api = inject(ApiService);

  login(email: string, password: string) {
    return this.api.post('/auth/login', { email, password });
  }

  logout() {
    return this.api.post('/auth/logout', {});
  }
}