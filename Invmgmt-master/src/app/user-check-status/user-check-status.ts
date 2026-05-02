import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-user-check-status',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './user-check-status.html',
  styleUrls: ['./user-check-status.css']
})
export class UserCheckStatusComponent {}

