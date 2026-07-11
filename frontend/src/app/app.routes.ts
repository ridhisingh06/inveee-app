import { Routes } from '@angular/router';
import { LoginComponent } from './auth/login/login';
import { RegisterComponent } from './auth/register/register';
import { authGuard } from './auth/Guard/guard';
import { InventoryComponent } from './inventory/inventory';
import { AdminPendingComponent } from './admin-pending/admin-pending';
import { AdminDashboardComponent } from './admin-dashboard/admin-dashboard';
import { UserDashboardComponent } from './user-dashboard/user-dashboard';
import { IssuerDashboardComponent } from './issuer-dashboard/issuer-dashboard';
import { IssuerApprovedComponent } from './issuer-approved/issuer-approved';
import { IssuerIssueComponent } from './issuer-issue/issuer-issue';
import { PersonnelManagementComponent } from './personnel-management/personnel-management';
import { PersonnelDetailsNewEntryComponent } from './personnel-details-new-entry/personnel-details-new-entry';
import { StoresSectionAllocationComponent } from './stores-section-allocation/stores-section-allocation';
import { UserItemListComponent } from './user-item-list/user-item-list';
import { UserCartComponent } from './user-cart/user-cart';
import { UserCheckStatusComponent } from './user-check-status/user-check-status';
import { CategoryManagementComponent } from './category-management/category-management';
import { PendingApprovalsComponent } from './pending-approvals/pending-approvals';
import { AdminLayoutComponent } from './admin-layout/admin-layout';
import { DeliveryChallanBillEntryComponent } from './delivery-challan-bill-entry/delivery-challan-bill-entry';
import { MonthlyRegisterComponent } from './monthly-register/monthly-register';
import { SectionWiseQueryComponent } from './section-wise-query/section-wise-query';
import { OrderHistoryComponent } from './order-history/order-history';
import { OrderSummaryComponent } from './order-summary/order-summary';

export const routes: Routes = [
  // register page
  { path: 'register', component: RegisterComponent },

  // default route
  { path: '', redirectTo: 'login', pathMatch: 'full' },

  // login page
  { path: 'login', component: LoginComponent },

  // default dashboard catch-all (redirect to login or specific dashboard)
  { path: 'dashboard', redirectTo: 'login', pathMatch: 'full' },

  // ADMIN SECTION (With Sidebar Layout)
  {
    path: '',
    component: AdminLayoutComponent,
    canActivate: [authGuard],
    data: { roles: ['ADMIN'] },
    children: [
      {
        path: 'admin-dashboard',
        component: AdminDashboardComponent
      },
      {
        path: 'pending-requests',
        component: AdminPendingComponent
      },
      {
        path: 'personnel-management',
        component: PersonnelManagementComponent
      },
      {
        path: 'personnel-management/personnel-details-new-entry',
        component: PersonnelDetailsNewEntryComponent
      },
      {
        path: 'personnel-management/personnel-details-new-entry/:id',
        component: PersonnelDetailsNewEntryComponent
      },
      {
        path: 'personnel-management/:slug',
        component: PersonnelManagementComponent
      },
      {
        path: 'stores-section-allocation',
        component: StoresSectionAllocationComponent
      },
      {
        path: 'incharge-allocation',
        component: StoresSectionAllocationComponent
      },
      {
        path: 'category-management',
        component: CategoryManagementComponent
      },
      {
        path: 'pending-approvals',
        component: PendingApprovalsComponent
      },
      {
        path: 'delivery-challan-bill-entry',
        component: DeliveryChallanBillEntryComponent
      },
      {
        path: 'monthly-register',
        component: MonthlyRegisterComponent
      },
      {
        path: 'section-wise-query',
        component: SectionWiseQueryComponent
      }
    ]
  },

  // USER SECTION
  {
    path: 'user-dashboard',
    component: UserDashboardComponent,
    canActivate: [authGuard],
    data: { roles: ['USER'] },
    children: [
      { path: '', redirectTo: 'item-list', pathMatch: 'full' },
      { path: 'item-list', component: UserItemListComponent },
      { path: 'my-requests', component: UserCheckStatusComponent },
      { path: 'cart', component: UserCartComponent },
      { path: 'order-history', component: OrderHistoryComponent },
      { path: 'order-summary/:id', component: OrderSummaryComponent },
      // Legacy redirects for old links
      { path: 'request-items', redirectTo: 'item-list', pathMatch: 'full' },
      { path: 'check-status', redirectTo: 'my-requests', pathMatch: 'full' }
    ]
  },
  {
    path: 'issuer-dashboard',
    component: IssuerDashboardComponent,
    canActivate: [authGuard],
    data: { roles: ['ISSUER'] }
  },
  {
    path: 'inventory',
    component: InventoryComponent,
    canActivate: [authGuard],
    data: { roles: ['ADMIN', 'USER', 'ISSUER'] }
  },
  {
    path: 'approved',
    component: IssuerApprovedComponent,
    canActivate: [authGuard],
    data: { roles: ['ISSUER'] }
  },
  {
    path: 'issue',
    component: IssuerIssueComponent,
    canActivate: [authGuard],
    data: { roles: ['ISSUER'] }
  }
];
