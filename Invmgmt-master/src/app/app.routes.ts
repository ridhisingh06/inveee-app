import { Routes } from '@angular/router';
import { LoginComponent } from './auth/login/login';
import { RegisterComponent } from './auth/register/register';
import { authGuard } from './auth/Guard/guard';
import { DashboardComponent} from './dashboard/dashboard';

export const routes: Routes = [
  // register page
  { path: 'register', component: RegisterComponent },

  // default route
  { path: '', redirectTo: 'login', pathMatch: 'full' },

  // login page
  { path: 'login', component: LoginComponent },

  // dashboard (protected)
  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [authGuard],
    data: { roles: ['Admin', 'User'] }
  }
];
