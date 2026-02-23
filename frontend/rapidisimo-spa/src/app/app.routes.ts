import { Routes } from '@angular/router';
import { LoginComponent } from './auth/login/login';
import { HomeComponent } from './home.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'login', component: LoginComponent }
];