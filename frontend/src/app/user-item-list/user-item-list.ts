import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Item } from '../models/item';
import { CartService } from '../services/cart.service';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-user-item-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './user-item-list.html',
  styleUrls: ['./user-item-list.css']
})
export class UserItemListComponent implements OnInit {
  allItems: Item[] = [];
  categories: string[] = [];
  selectedCategory = '';
  searchText = '';
  loading = true;
  errorMsg = '';

  /** Toast feedback for "Added to cart" */
  readonly toast = signal<{ visible: boolean; message: string }>({ visible: false, message: '' });

  constructor(
    private cart: CartService,
    private http: HttpClient
  ) {}

  ngOnInit() {
    this.fetchItems();
  }

  fetchItems() {
    this.loading = true;
    this.errorMsg = '';
    this.http.get<any[]>(`${environment.apiUrl}/inventory`)
      .subscribe({
        next: (items) => {
          this.allItems = items.map(i => ({
            id: i.id,
            itemCode: i.itemCode,
            name: i.name,
            category: i.category || 'Uncategorized',
            description: i.description
          }));

          const uniqueCats = new Set<string>();
          this.allItems.forEach(i => uniqueCats.add(i.category));
          this.categories = ['All', ...Array.from(uniqueCats).sort()];
          this.selectedCategory = 'All';
          this.loading = false;
        },
        error: () => {
          this.errorMsg = 'Failed to load items. Please try again.';
          this.loading = false;
        }
      });
  }

  /** Items filtered by both category and search text */
  get items(): Item[] {
    let result = this.allItems;

    if (this.selectedCategory && this.selectedCategory !== 'All') {
      result = result.filter(i => i.category === this.selectedCategory);
    }

    const normalizedSearchText = (this.searchText ?? '').trim().toLowerCase();
    if (normalizedSearchText) {
      result = result.filter(i =>
        (i.name ?? '').toLowerCase().includes(normalizedSearchText) ||
        (i.description ?? '').toLowerCase().includes(normalizedSearchText) ||
        (i.category ?? '').toLowerCase().includes(normalizedSearchText)
      );
    }

    return result;
  }

  selectCategory(cat: string) {
    this.selectedCategory = cat;
  }

  addToCart(item: Item) {
    this.cart.addItem(item, 1);
    this.showToast(`${item.name} added to cart`);
  }

  /**
   * Track by function for ngFor performance optimization
   */
  trackById(index: number, item: Item): string | number {
    return item.id;
  }

  private showToast(message: string) {
    this.toast.set({ visible: true, message });
    setTimeout(() => this.toast.set({ visible: false, message: '' }), 2200);
  }
}
