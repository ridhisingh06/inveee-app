import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';     
import { environment } from '../../../environments/environment';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterModule],
  templateUrl: './login.html'
})
export class LoginComponent {

  email = '';
  password = '';

  errorMsg = '';

  constructor(private http: HttpClient, private router: Router) { }

  login() {

    const payload = {
      email: this.email,
      password: this.password
    };

    this.http.post(`${environment.apiUrl}/auth/login`, payload)
      .subscribe({
        next: (res: any) => {

          // 🔐 token save
          localStorage.setItem('token', res.token);

          // 🔥 role extract
          const payloadDecoded = JSON.parse(atob(res.token.split('.')[1]));

          const role =
            payloadDecoded['role'] ||
            payloadDecoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

          localStorage.setItem('role', role);

          // 🚀 redirect
          this.router.navigate(['/dashboard']);
        },
        error: () => {
          this.errorMsg = "Invalid credentials";
        }
      });
  }
}
