import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { Item } from '../models/item';
import { LoggerService } from './logger.service';

export interface CartLine {
  item: Item;
  qty: number;
}

const STORAGE_KEY = 'cart_v1';
const CTX = 'CartService';

function readCartFromStorage(): CartLine[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw) as CartLine[];
    if (!Array.isArray(parsed)) return [];
    return parsed
      .filter((x) => x && x.item && typeof x.qty === 'number')
      .map((x) => ({ item: x.item, qty: Math.max(1, Math.floor(x.qty)) }));
  } catch {
    return [];
  }
}

function writeCartToStorage(lines: CartLine[], logger: LoggerService) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(lines));
  } catch (err) {
    logger.warn(CTX, 'Failed to persist cart to localStorage', err);
  }
}

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly linesSubject = new BehaviorSubject<CartLine[]>(readCartFromStorage());

  readonly lines$ = this.linesSubject.asObservable();

  constructor(private logger: LoggerService) {}

  getLinesSnapshot(): CartLine[] {
    return this.linesSubject.value;
  }

  getItemCountSnapshot(): number {
    return this.linesSubject.value.reduce((sum, line) => sum + line.qty, 0);
  }

  addItem(item: Item, qty: number = 1) {
    const safeQty = Math.max(1, Math.floor(qty));
    const lines = [...this.linesSubject.value];
    const idx = lines.findIndex((l) => l.item.id === item.id);
    if (idx >= 0) {
      lines[idx] = { ...lines[idx], qty: lines[idx].qty + safeQty };
      this.logger.log(CTX, `Updated qty for "${item.name}" → ${lines[idx].qty}`);
    } else {
      lines.push({ item, qty: safeQty });
      this.logger.log(CTX, `Added new item "${item.name}" (qty: ${safeQty})`);
    }
    this.setLines(lines);
  }

  updateQuantity(itemId: string | number, qty: number) {
    const lines = [...this.linesSubject.value];
    const idx = lines.findIndex((l) => l.item.id === itemId);
    if (idx >= 0) {
      if (qty <= 0) {
        this.logger.log(CTX, `Quantity <= 0 for item id="${itemId}" — removing`);
        this.removeItem(itemId);
      } else {
        lines[idx] = { ...lines[idx], qty };
        this.logger.log(CTX, `Updated qty for item id="${itemId}" → ${qty}`);
        this.setLines(lines);
      }
    }
  }

  removeItem(itemId: string | number) {
    const current = this.linesSubject.value;
    const updated = current.filter((l) => l.item.id !== itemId);
    this.logger.log(CTX, `Removed item id="${itemId}" from cart`);
    this.linesSubject.next(updated);
    writeCartToStorage(updated, this.logger);
  }

  clear() {
    this.logger.log(CTX, 'Cart cleared');
    this.setLines([]);
  }

  private setLines(lines: CartLine[]) {
    this.linesSubject.next(lines);
    writeCartToStorage(lines, this.logger);
  }
}
