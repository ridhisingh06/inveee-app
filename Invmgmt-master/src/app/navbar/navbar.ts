import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../auth/services/service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.html',
  styleUrls: ['./navbar.css']
})
export class NavbarComponent implements OnInit {

  role: string | null = null;

  constructor(private auth: AuthService, private router: Router) { }

  ngOnInit() {
    this.role = this.auth.getRole(); // 🔥 role from token
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/']); // ✅ Angular way
  }
}
