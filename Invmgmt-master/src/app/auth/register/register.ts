import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, CommonModule,RouterModule],
  templateUrl: './register.html',
  styleUrls: ['./register.css']
})
export class RegisterComponent {

  username = '';
  email = '';
  password = '';
  designation = '';

  departmentId: number | null = null;
  roleId: number | null = null;

  successMsg = '';
  errorMsg = '';
  loading = false; // 🔥 new

  constructor(private http: HttpClient) { }

  register() {

    if (!this.username || !this.email || !this.password ||
      !this.departmentId || !this.roleId) {
      this.errorMsg = "Please fill all fields";
      this.successMsg = '';
      return;
    }

    const payload = {
      username: this.username,
      email: this.email,
      passwordHash: this.password,
      designation: this.designation,
      departmentId: this.departmentId,
      roleId: this.roleId
    };

    this.loading = true;

    this.http.post(`${environment.apiUrl}/registration/register`, payload)
      .subscribe({
        next: () => {
          this.successMsg = '✅ Registered! Wait for admin approval.';
          this.errorMsg = '';
          this.resetForm();
          this.loading = false;
        },
        error: (err) => {
          this.errorMsg = err.error || '❌ Registration failed';
          this.successMsg = '';
          this.loading = false;
        }
      });
  }

  resetForm() {
    this.username = '';
    this.email = '';
    this.password = '';
    this.designation = '';
    this.departmentId = null;
    this.roleId = null;
  }
}
