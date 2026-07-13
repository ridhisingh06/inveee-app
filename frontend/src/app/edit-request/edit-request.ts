import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { RequestService } from '../services/request.service';
import { RefreshService } from '../services/refresh.service';
import { RequestDetailEnhanced } from '../models/request.model';
import { Item } from '../models/item';

interface EditLine {
  item: Item;
  qty: number;
}

@Component({
  selector: 'app-edit-request',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './edit-request.html',
  styleUrls: ['./edit-request.css']
})
export class EditRequestComponent implements OnInit, OnDestroy {
  requestId: number | null = null;
  loading = true;
  submitting = false;
  errorMsg = '';
  successMsg = '';

  lines: EditLine[] = [];

  // Inventory state for adding new items
  allItems: Item[] = [];
  filteredItems: Item[] = [];
  searchText = '';
  showItemSearch = false;
  inventoryLoading = false;

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient,
    private requestService: RequestService,
    private refresh: RefreshService
  ) {}

  ngOnInit() {
    this.route.paramMap.pipe(takeUntil(this.destroy$)).subscribe(params => {
      const idStr = params.get('id');
      if (idStr) {
        this.requestId = Number(idStr);
        this.loadRequest();
        this.loadInventory();
      }
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadRequest() {
    if (!this.requestId) return;
    this.loading = true;
    this.errorMsg = '';

    // First check if editable
    this.requestService.isRequestEditable(this.requestId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (editableRes) => {
          if (!editableRes.editable) {
            this.errorMsg = editableRes.reason || 'This request can no longer be edited.';
            this.loading = false;
            return;
          }

          // Fetch full details
          this.requestService.getRequestById(this.requestId!)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
              next: (req) => {
                const r = req as any;
                this.lines = (r.items || []).map((ri: any) => ({
                  item: { id: ri.itemId, name: ri.itemName, category: 'Unknown' },
                  qty: ri.quantityRequested
                }));
                this.loading = false;
              },
              error: (err) => {
                this.errorMsg = err.message || 'Failed to load request.';
                this.loading = false;
              }
            });
        },
        error: (err) => {
          this.errorMsg = err.message || 'Failed to check if request is editable.';
          this.loading = false;
        }
      });
  }

  loadInventory() {
    this.inventoryLoading = true;
    this.http.get<any[]>(`${environment.apiUrl}/inventory`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (items) => {
          this.allItems = items.map(i => ({
            id: i.id,
            name: i.name,
            category: i.category || 'Uncategorized'
          }));
          this.inventoryLoading = false;
        },
        error: (err) => {
          console.error('[EditRequest] Failed to load inventory', err);
          this.inventoryLoading = false;
        }
      });
  }

  changeQty(itemId: string | number, currentQty: number, delta: 1 | -1) {
    const next = currentQty + delta;
    if (next < 1) return;
    const line = this.lines.find(l => l.item.id === itemId);
    if (line) {
      line.qty = next;
    }
  }

  removeLine(itemId: string | number) {
    this.lines = this.lines.filter(l => l.item.id !== itemId);
  }

  get totalUnits(): number {
    return this.lines.reduce((acc, line) => acc + line.qty, 0);
  }

  // ── Add Item Flow ─────────────────────────────────────────────────────────

  toggleItemSearch() {
    this.showItemSearch = !this.showItemSearch;
    if (this.showItemSearch) {
      this.searchText = '';
      this.filterItems();
    }
  }

  filterItems() {
    const term = (this.searchText || '').toLowerCase().trim();
    if (!term) {
      this.filteredItems = this.allItems.slice(0, 50);
      return;
    }
    this.filteredItems = this.allItems
      .filter(i => (i.name || '').toLowerCase().includes(term))
      .slice(0, 50);
  }

  onSearchChange() {
    this.filterItems();
  }

  addItem(item: Item) {
    const existing = this.lines.find(l => l.item.id === item.id);
    if (existing) {
      existing.qty += 1;
    } else {
      this.lines.push({ item, qty: 1 });
    }
    this.successMsg = `${item.name} added.`;
    setTimeout(() => this.successMsg = '', 2000);
  }

  // ── Submit ────────────────────────────────────────────────────────────────

  saveChanges() {
    if (!this.requestId) return;
    if (this.lines.length === 0) {
      this.errorMsg = 'Request must contain at least one item.';
      return;
    }

    this.submitting = true;
    this.errorMsg = '';
    this.successMsg = '';

    const payload = {
      items: this.lines.map(line => ({
        itemId: Number(line.item.id),
        quantity: line.qty
      }))
    };

    this.requestService.updateRequest(this.requestId, payload)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.successMsg = 'Request updated successfully!';
          this.submitting = false;

          // ✅ Notify all subscribers that requests have changed so every
          //    component (UserCheckStatusComponent, OrderHistoryComponent,
          //    user dashboard counters) reloads immediately without a manual
          //    browser refresh.
          this.refresh.notifyRequests();

          // Navigate back after a short visual confirmation delay.
          setTimeout(() => {
            this.router.navigate(['/user-dashboard/my-requests']);
          }, 1000);
        },
        error: (err) => {
          this.errorMsg = err.message || 'Failed to update request.';
          this.submitting = false;
        }
      });
  }

  cancel() {
    this.router.navigate(['/user-dashboard/my-requests']);
  }
}
