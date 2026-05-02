import { Component } from '@angular/core';
import { NavbarComponent } from '../navbar/navbar';

@Component({
  standalone: true,
  selector: 'app-issuer-approved',
  imports: [NavbarComponent],
  template: `
    <app-navbar></app-navbar>
    <div style="padding:20px;">
      <h2>Approved Requests</h2>
      <p>This page is ready for your approved-requests list.</p>
    </div>
  `
})
export class IssuerApprovedComponent {}
