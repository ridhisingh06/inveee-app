import { Routes } from '@angular/router';
import { LoginComponent } from './auth/login/login';
import { RegisterComponent } from './auth/register/register';
import { authGuard } from './auth/Guard/guard';
import { InventoryComponent } from './inventory/inventory';
import { AdminPendingComponent } from './admin-pending/admin-pending';
import { RequestItemComponent } from './request-item/request-item';
import { MyRequestsComponent } from './my-requests/my-requests';
import { DashboardRedirectComponent } from './dashboard-redirect/dashboard-redirect';
import { AdminDashboardComponent } from './admin-dashboard/admin-dashboard';
import { UserDashboardComponent } from './user-dashboard/user-dashboard';
import { IssuerDashboardComponent } from './issuer-dashboard/issuer-dashboard';
import { IssuerApprovedComponent } from './issuer-approved/issuer-approved';
import { IssuerIssueComponent } from './issuer-issue/issuer-issue';
import { PersonnelManagementComponent } from './personnel-management/personnel-management';
import { StoresSectionAllocationComponent } from './stores-section-allocation/stores-section-allocation';
import { UserItemListComponent } from './user-item-list/user-item-list';
import { UserCartComponent } from './user-cart/user-cart';
import { UserCheckStatusComponent } from './user-check-status/user-check-status';

export const routes: Routes = [
  // register page
  { path: 'register', component: RegisterComponent },

  // default route
  { path: '', redirectTo: 'login', pathMatch: 'full' },

  // login page
  { path: 'login', component: LoginComponent },

  // dashboard redirect (protected)
  {
    path: 'dashboard',
    component: DashboardRedirectComponent,
    canActivate: [authGuard],
    data: { roles: ['Admin', 'User', 'Issuer'] }
  },

  // role dashboards (protected)
  {
    path: 'admin-dashboard',
    component: AdminDashboardComponent,
    canActivate: [authGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'user-dashboard',
    component: UserDashboardComponent,
    canActivate: [authGuard],
    data: { roles: ['User'] },
    children: [
      { path: '', redirectTo: 'item-list', pathMatch: 'full' },
      { path: 'request-items', component: UserItemListComponent },
      { path: 'item-list', component: UserItemListComponent },
      { path: 'check-status', component: UserCheckStatusComponent },
      { path: 'cart', component: UserCartComponent }
    ]
  },
  {
    path: 'issuer-dashboard',
    component: IssuerDashboardComponent,
    canActivate: [authGuard],
    data: { roles: ['Issuer'] }
  },
  {
    path: 'inventory',
    component: InventoryComponent,
    canActivate: [authGuard],
    data: { roles: ['Admin', 'User', 'Issuer'] }
  },
  {
    path: 'pending-requests',
    component: AdminPendingComponent,
    canActivate: [authGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'request-item',
    component: RequestItemComponent,
    canActivate: [authGuard],
    data: { roles: ['User'] }
  },
  {
    path: 'my-requests',
    component: MyRequestsComponent,
    canActivate: [authGuard],
    data: { roles: ['User'] }
  },
  {
    path: 'approved',
    component: IssuerApprovedComponent,
    canActivate: [authGuard],
    data: { roles: ['Issuer'] }
  },
  {
    path: 'issue',
    component: IssuerIssueComponent,
    canActivate: [authGuard],
    data: { roles: ['Issuer'] }
  },
  {
    path: 'personnel-management',
    component: PersonnelManagementComponent,
    canActivate: [authGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'personnel-management/:slug',
    component: PersonnelManagementComponent,
    canActivate: [authGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'stores-section-allocation',
    component: StoresSectionAllocationComponent,
    canActivate: [authGuard],
    data: { roles: ['Admin'] }
  },
  {
    path: 'incharge-allocation',
    component: StoresSectionAllocationComponent,
    canActivate: [authGuard],
    data: { roles: ['Admin'] }
  }
];
