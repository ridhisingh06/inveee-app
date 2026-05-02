import { Component } from '@angular/core';
import { NavbarComponent } from '../navbar/navbar';

@Component({
  standalone: true,
  selector: 'app-issuer-issue',
  imports: [NavbarComponent],
  template: `
    <app-navbar></app-navbar>
    <div style="padding:20px;">
      <h2>Issue Items</h2>
      <p>This page is ready for issuing approved requests.</p>
    </div>
  `
})
export class IssuerIssueComponent {}
