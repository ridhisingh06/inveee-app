import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Item, ItemCategory } from '../models/item';
import { CartService } from '../services/cart.service';

const CATEGORIES: ItemCategory[] = ['Stationary', 'IT Items', 'Housekeeping'];

const ALL_ITEMS: Item[] = [
  { id: 'st-pen', name: 'Ball Pen', category: 'Stationary', description: 'Blue/Black ink' },
  { id: 'st-notebook', name: 'Notebook', category: 'Stationary', description: 'A4 ruled' },
  { id: 'st-marker', name: 'Marker', category: 'Stationary', description: 'Permanent marker' },

  { id: 'it-mouse', name: 'Mouse', category: 'IT Items', description: 'Wireless mouse' },
  { id: 'it-keyboard', name: 'Keyboard', category: 'IT Items', description: 'USB keyboard' },
  { id: 'it-headset', name: 'Headset', category: 'IT Items', description: 'With mic' },

  { id: 'hk-cleaner', name: 'Floor Cleaner', category: 'Housekeeping', description: '1L bottle' },
  { id: 'hk-gloves', name: 'Disposable Gloves', category: 'Housekeeping', description: 'Pack of 100' },
  { id: 'hk-wipes', name: 'Cleaning Wipes', category: 'Housekeeping', description: 'Multi-surface' }
];

@Component({
  selector: 'app-user-item-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './user-item-list.html',
  styleUrls: ['./user-item-list.css']
})
export class UserItemListComponent {
  categories = CATEGORIES;
  selectedCategory: ItemCategory = 'Stationary';

  constructor(private cart: CartService) {}

  get items(): Item[] {
    return ALL_ITEMS.filter((i) => i.category === this.selectedCategory);
  }

  addToCart(item: Item) {
    this.cart.addItem(item, 1);
  }
}
