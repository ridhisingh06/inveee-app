import { Component, OnInit, DoCheck } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { NavbarComponent } from '../navbar/navbar';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule,NavbarComponent],
  templateUrl: './inventory.html',
  styleUrls: ['./inventory.css']
})
export class InventoryComponent implements OnInit, DoCheck {

  items: any[] = [];
  filteredItems: any[] = [];
  categories: any[] = [];

  itemName = '';
  selectedCategoryId: number | null = null;
  quantity = 0;

  searchText = '';
  editingItemId: number | null = null;

  role: string = '';

  constructor(private http: HttpClient) { }

  ngOnInit() {
    this.getItems();
    this.getCategories();

    // 🔐 Role fetch from token
    const token = localStorage.getItem('token');
    if (token) {
      const payload = JSON.parse(atob(token.split('.')[1]));
      this.role =
        payload['role'] ||
        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    }
  }

  // 🔍 Auto filter
  ngDoCheck() {
    this.filteredItems = this.items.filter(item =>
      item.name.toLowerCase().includes(this.searchText.toLowerCase())
    );
  }

  // =========================
  // GET ITEMS
  // =========================
  getItems() {
    this.http.get<any[]>(`${environment.apiUrl}/inventory`)
      .subscribe({
        next: (res) => {
          this.items = res;
          this.filteredItems = res;
        },
        error: (err) => {
          console.error('Error fetching items', err);
        }
      });
  }

  // =========================
  // GET CATEGORIES
  // =========================
  getCategories() {
    this.http.get<any[]>(`${environment.apiUrl}/ItemCategory`)
      .subscribe({
        next: (res) => {
          this.categories = res;
        },
        error: (err) => {
          console.error('Error fetching categories', err);
        }
      });
  }

  // =========================
  // ADD ITEM
  // =========================
  addItem() {

    if (!this.itemName || !this.selectedCategoryId || this.quantity <= 0) {
      alert("Fill all fields properly");
      return;
    }

    const payload = {
      name: this.itemName,
      categoryId: this.selectedCategoryId,
      totalQuantity: this.quantity,
      description: "New Item"
    };

    this.http.post(`${environment.apiUrl}/inventory`, payload)
      .subscribe({
        next: () => {
          this.getItems();
          this.resetForm();
        },
        error: (err) => {
          console.error('Error adding item', err);
        }
      });
  }

  // =========================
  // EDIT ITEM
  // =========================
  editItem(item: any) {
    this.editingItemId = item.id;
    this.itemName = item.name;
    this.selectedCategoryId = item.categoryId;
    this.quantity = item.availableQuantity;
  }

  // =========================
  // UPDATE ITEM
  // =========================
  updateItem() {

    const payload = {
      name: this.itemName,
      categoryId: this.selectedCategoryId,
      totalQuantity: this.quantity,
      description: "Updated Item"
    };

    this.http.put(`${environment.apiUrl}/inventory/${this.editingItemId}`, payload)
      .subscribe({
        next: () => {
          this.getItems();
          this.resetForm();
        },
        error: (err) => {
          console.error('Error updating item', err);
        }
      });
  }

  // =========================
  // DELETE ITEM
  // =========================
  deleteItem(id: number) {
    this.http.delete(`${environment.apiUrl}/inventory/${id}`)
      .subscribe({
        next: () => {
          this.getItems();
        },
        error: (err) => {
          console.error('Error deleting item', err);
        }
      });
  }

  // =========================
  // RESET FORM
  // =========================
  resetForm() {
    this.itemName = '';
    this.selectedCategoryId = null;
    this.quantity = 0;
    this.editingItemId = null;
  }
}
