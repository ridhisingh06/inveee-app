import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthApiService } from '../services/auth-api.service';

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
  loading = false; //  new

  constructor(private authApi: AuthApiService) { }

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
      password: this.password,
      designation: this.designation,
      departmentId: this.departmentId,
      roleId: this.roleId
    };

    this.loading = true;

    this.authApi.register({
      username: payload.username,
      email: payload.email,
      password: payload.password,
      designation: payload.designation,
      departmentId: payload.departmentId,
      roleId: payload.roleId
    })
      .subscribe({
        next: (res) => {
          this.successMsg = res.message || 'Your request is pending. Please wait for admin approval.';
          this.errorMsg = '';
          this.resetForm();
          this.loading = false;
        },
        error: (err) => {
          let msg = ' Registration failed';
          if (err?.error?.errors) {
            // ASP.NET Core Model Validation errors (e.g. invalid DepartmentId or null fields)
            const firstErrorKey = Object.keys(err.error.errors)[0];
            msg = err.error.errors[firstErrorKey][0];
          } else if (err?.error?.message) {
            // Our custom AuthController errors
            msg = err.error.message;
          } else if (typeof err?.error === 'string') {
            msg = err.error;
          }
          
          this.errorMsg = msg;
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
