import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { Item } from '../models/item';

export interface CartLine {
  item: Item;
  qty: number;
}

const STORAGE_KEY = 'cart_v1';

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

function writeCartToStorage(lines: CartLine[]) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(lines));
  } catch {
    // ignore storage errors
  }
}

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly linesSubject = new BehaviorSubject<CartLine[]>(readCartFromStorage());

  readonly lines$ = this.linesSubject.asObservable();

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
    } else {
      lines.push({ item, qty: safeQty });
    }
    this.setLines(lines);
  }

  removeItem(itemId: string) {
    this.setLines(this.linesSubject.value.filter((l) => l.item.id !== itemId));
  }

  clear() {
    this.setLines([]);
  }

  private setLines(lines: CartLine[]) {
    this.linesSubject.next(lines);
    writeCartToStorage(lines);
  }
}

