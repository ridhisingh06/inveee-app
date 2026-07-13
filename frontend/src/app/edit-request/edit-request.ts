import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Subject, forkJoin } from 'rxjs';
import { switchMap, takeUntil } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { RequestService } from '../services/request.service';
import { RefreshService } from '../services/refresh.service';
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
    this.route.paramMap
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        const idStr = params.get('id');
        if (idStr) {
          this.requestId = Number(idStr);
          this.loadAll();
        } else {
          this.errorMsg = 'No request ID provided.';
          this.loading = false;
        }
      });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load the editable check AND the full request details in a single
   * sequential chain (switchMap), plus the inventory list in parallel via
   * forkJoin so both arrive at the same time.
   *
   * This replaces the old two-subscription nested pattern which was fragile
   * and doubled the perceived latency.
   */
  loadAll() {
    if (!this.requestId) return;
    this.loading = true;
    this.inventoryLoading = true;
    this.errorMsg = '';

    // ── Parallel: editable-check + inventory ──────────────────────────────
    forkJoin({
      editable: this.requestService.isRequestEditable(this.requestId),
      inventory: this.http.get<any[]>(`${environment.apiUrl}/inventory`)
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: ({ editable, inventory }) => {
        // ── Populate inventory list (used by Add New Item panel) ──────────
        this.allItems = (inventory || []).map((i: any) => ({
          id: i.id,
          name: i.name,
          category: i.category || 'Uncategorized'
        }));
        this.inventoryLoading = false;

        // ── Guard: property may arrive as "editable" OR "Editable" ────────
        // The backend now emits camelCase via PropertyNamingPolicy, but this
        // defensive check keeps things working even against older cached builds.
        const isEditable: boolean =
          (editable as any)['editable'] ?? (editable as any)['Editable'] ?? false;
        const reason: string =
          (editable as any)['reason'] ?? (editable as any)['Reason'] ?? '';

        if (!isEditable) {
          this.errorMsg = reason || 'This request can no longer be edited.';
          this.loading = false;
          return;
        }

        // ── Load request details only if editable ─────────────────────────
        this.requestService.getRequestById(this.requestId!)
          .pipe(takeUntil(this.destroy$))
          .subscribe({
            next: (req) => {
              const r = req as any;
              this.lines = (r.items || []).map((ri: any) => ({
                item: {
                  id: ri.itemId ?? ri.ItemId,
                  name: ri.itemName ?? ri.ItemName ?? 'Unknown',
                  category: 'Unknown'
                },
                qty: ri.quantityRequested ?? ri.QuantityRequested ?? 1
              }));
              this.loading = false;
            },
            error: (err) => {
              this.errorMsg = err.message || 'Failed to load request details.';
              this.loading = false;
            }
          });
      },
      error: (err) => {
        // If either parallel call fails, surface the error immediately.
        this.errorMsg = err?.message || 'Failed to load request. Please try again.';
        this.loading = false;
        this.inventoryLoading = false;
      }
    });
  }

  // ── Kept for explicit manual refresh (not currently called by template) ──
  loadRequest() { this.loadAll(); }

  changeQty(itemId: string | number, currentQty: number, delta: 1 | -1) {
    const next = currentQty + delta;
    if (next < 1) return;
    const line = this.lines.find(l => l.item.id === itemId);
    if (line) line.qty = next;
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
          this.refresh.notifyRequests();
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
