import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Subject, forkJoin } from 'rxjs';
import { takeUntil, timeout } from 'rxjs/operators';
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
   * Load the request details and inventory list in parallel.
   *
   * The editability check is derived from the request data itself
   * (status + item statuses) rather than making a separate API call to
   * /editable — this removes one network round-trip and eliminates the
   * risk of the page hanging when that endpoint is slow.
   *
   * A 15-second timeout guard prevents the spinner from running forever
   * if the backend is unreachable.
   */
  loadAll() {
    if (!this.requestId) return;
    this.loading = true;
    this.inventoryLoading = true;
    this.errorMsg = '';

    forkJoin({
      request: this.requestService.getRequestById(this.requestId),
      inventory: this.http.get<any[]>(`${environment.apiUrl}/inventory`)
    })
    .pipe(
      timeout(15000),          // 15 s hard cap — prevents infinite spinner
      takeUntil(this.destroy$)
    )
    .subscribe({
      next: ({ request, inventory }) => {
        // ── Populate inventory list for the Add New Item panel ────────────
        this.allItems = (inventory || []).map((i: any) => ({
          id: i.id,
          name: i.name,
          category: i.category || 'Uncategorized'
        }));
        this.inventoryLoading = false;

        const r = request as any;

        // ── Derive editability from the response directly ─────────────────
        // A request is editable when:
        //   1. Request-level status is PendingWithIssuer
        //   2. Every item is still PendingWithIssuer (issuer hasn't touched it)
        const reqStatus: string = (r.status ?? r.Status ?? '').toLowerCase().trim();
        const items: any[] = r.items ?? r.Items ?? [];

        const isPendingWithIssuer = reqStatus === 'pendingwithissuer' || reqStatus === 'requested';

        const allItemsPending = items.length > 0 && items.every((i: any) => {
          const s = (i.status ?? i.Status ?? '').toLowerCase().trim();
          return s === 'pendingwithissuer' || s === 'requested';
        });

        if (!isPendingWithIssuer || !allItemsPending) {
          // Not editable — show a clear message and offer to go back
          this.errorMsg = isPendingWithIssuer
            ? 'The issuer has started processing this request. Editing is no longer allowed.'
            : `This request is in "${r.status ?? 'unknown'}" status and can no longer be edited.`;
          this.loading = false;
          return;
        }

        // ── Populate edit lines ───────────────────────────────────────────
        this.lines = items.map((ri: any) => ({
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
        if (err?.name === 'TimeoutError') {
          this.errorMsg = 'Request timed out. Please check your connection and try again.';
        } else {
          this.errorMsg = err?.message || 'Failed to load request. Please try again.';
        }
        this.loading = false;
        this.inventoryLoading = false;
      }
    });
  }

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

  // ── Add Item Panel ────────────────────────────────────────────────────────

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
