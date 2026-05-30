import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

interface CategoryItem {
  id: number;
  name: string;
  itemCount: number;
  totalStock: number;
}

@Component({
  selector: 'app-category-management',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './category-management.html',
  styleUrls: ['./category-management.css']
})
export class CategoryManagementComponent implements OnInit {
  categories: CategoryItem[] = [];
  loading = true;
  errorMsg = '';

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.fetchCategories();
  }

  fetchCategories() {
    this.loading = true;
    this.errorMsg = '';
    this.http.get<CategoryItem[]>(`${environment.apiUrl}/admin/categories`)
      .subscribe({
        next: (data) => {
          this.categories = data;
          this.loading = false;
        },
        error: (err) => {
          console.error(err);
          this.errorMsg = 'Failed to load categories.';
          this.loading = false;
        }
      });
  }

  get totalCategories(): number {
    return this.categories.length;
  }

  get totalLinkedItems(): number {
    return this.categories.reduce((acc, c) => acc + c.itemCount, 0);
  }

  get totalStockUnits(): number {
    return this.categories.reduce((acc, c) => acc + c.totalStock, 0);
  }
}
