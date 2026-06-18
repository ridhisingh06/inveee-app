import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MonthlyRegisterService } from '../services/monthly-register.service';
import { MonthlyRegisterRow } from '../models/monthly-register.model';

@Component({
  selector: 'app-monthly-register',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './monthly-register.html',
  styleUrls: ['./monthly-register.css']
})
export class MonthlyRegisterComponent implements OnInit {
  rows = signal<MonthlyRegisterRow[]>([]);
  loading = signal(false);
  errorMsg = signal('');

  month = signal(new Date().getMonth() + 1);
  year = signal(new Date().getFullYear());
  searchQuery = signal('');
  page = signal(1);
  pageSize = signal(20);
  totalPages = signal(0);
  totalCount = signal(0);
  totalQuantityRequested = signal(0);
  totalQuantityApproved = signal(0);
  totalQuantityIssued = signal(0);

  readonly monthOptions = [
    { value: 1, label: 'January' },
    { value: 2, label: 'February' },
    { value: 3, label: 'March' },
    { value: 4, label: 'April' },
    { value: 5, label: 'May' },
    { value: 6, label: 'June' },
    { value: 7, label: 'July' },
    { value: 8, label: 'August' },
    { value: 9, label: 'September' },
    { value: 10, label: 'October' },
    { value: 11, label: 'November' },
    { value: 12, label: 'December' }
  ];

  constructor(private service: MonthlyRegisterService) {}

  ngOnInit() {
    this.loadRegister();
  }

  loadRegister() {
    this.loading.set(true);
    this.errorMsg.set('');

    this.service
      .getMonthlyRegister(
        this.month(),
        this.year(),
        this.page(),
        this.pageSize(),
        this.searchQuery()
      )
      .subscribe({
        next: (result) => {
          this.rows.set(result.data || []);
          this.totalCount.set(result.totalCount);
          this.totalPages.set(result.totalPages);
          this.totalQuantityRequested.set(result.totalQuantityRequested);
          this.totalQuantityApproved.set(result.totalQuantityApproved);
          this.totalQuantityIssued.set(result.totalQuantityIssued);
          this.loading.set(false);
        },
        error: (err) => {
          console.error('Failed to load monthly register', err);
          this.errorMsg.set(err.error?.message || 'Unable to load monthly register data');
          this.loading.set(false);
        }
      });
  }

  onSearch() {
    this.page.set(1);
    this.loadRegister();
  }

  onPageChange(newPage: number) {
    if (newPage < 1 || newPage > this.totalPages()) {
      return;
    }
    this.page.set(newPage);
    this.loadRegister();
  }

  onReset() {
    this.searchQuery.set('');
    this.month.set(new Date().getMonth() + 1);
    this.year.set(new Date().getFullYear());
    this.page.set(1);
    this.loadRegister();
  }
}
