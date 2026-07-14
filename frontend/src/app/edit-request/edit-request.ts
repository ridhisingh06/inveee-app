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
  loading           = true;
  inventoryLoading  = false;
  submitting        = false;
  errorMsg          = '';
  successMsg        = '';

  lines: EditLine[]  = [];
  allItems: Item[]   = [];
  filteredItems: Item[] = [];
  searchText     = '';
  showItemSearch = false;

  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly route:    ActivatedRoute,
    private readonly router:   Router,
    private readonly http:     HttpClient,
    private readonly requestService: RequestService,
    private readonly refresh:  RefreshService
  ) {}

  ngOnInit(): void {
    this.route.paramMap
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        const idStr = params.get('id');
        if (!idStr || isNaN(+idStr)) {
          this.errorMsg = 'No valid request ID provided.';
          this.loading  = false;
          return;
        }
        this.requestId = +idStr;
        this.loadRequest();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Step 1: load the request (fast — no timeout race) ──────────────────

  loadRequest(): void {
    this.loading  = true;
    this.errorMsg = '';

    this.requestService.getRequestById(this.requestId!)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (req) => {
          const r = req as any;

          // Normalise status — backend always returns camelCase string e.g. "PendingWithIssuer"
          const reqStatus: string = (r.status ?? r.Status ?? '').toLowerCase().trim();
          const items: any[]      = r.items ?? r.Items ?? [];

          const isPending = reqStatus === 'pendingwithissuer' || reqStatus === 'requested';

          const allItemsPending = items.length > 0 && items.every((i: any) => {
            const s = (i.status ?? i.Status ?? '').toLowerCase().trim();
            return s === 'pendingwithissuer' || s === 'requested';
          });

          if (!isPending) {
            this.errorMsg = `This request is in "${r.status}" status and can no longer be edited.`;
            this.loading  = false;
            return;
          }

          if (!allItemsPending) {
            this.errorMsg = 'The issuer has started processing this request. Editing is no longer allowed.';
            this.loading  = false;
            return;
          }

          // Populate editable lines from the existing request items
          this.lines = items.map((ri: any) => ({
            item: {
              id:       ri.itemId   ?? ri.ItemId   ?? 0,
              name:     ri.itemName ?? ri.ItemName  ?? 'Unknown',
              category: 'Unknown'
            } as Item,
            qty: ri.quantityRequested ?? ri.QuantityRequested ?? 1
          }));

          this.loading = false;

          // Step 2: load inventory in the background so "Add New Item" works
          this.loadInventory();
        },
        error: (err) => {
          this.errorMsg = err?.message || 'Failed to load request. Please try again.';
          this.loading  = false;
        }
      });
  }

  // ── Step 2: load inventory lazily (non-blocking) ───────────────────────

  loadInventory(): void {
    this.inventoryLoading = true;

    this.http.get<any[]>(`${environment.apiUrl}/inventory`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (items) => {
          this.allItems = (items ?? []).map((i: any) => ({
            id:       i.id,
            name:     i.name,
            category: i.category || 'Uncategorized'
          } as Item));
          this.inventoryLoading = false;
        },
        error: () => {
          // Non-fatal — user can still edit existing items even if inventory fails
          this.inventoryLoading = false;
        }
      });
  }

  // ── Quantity controls ─────────────────────────────────────────────────

  changeQty(itemId: string | number, currentQty: number, delta: 1 | -1): void {
    const next = currentQty + delta;
    if (next < 1) return;
    const line = this.lines.find(l => l.item.id === itemId);
    if (line) line.qty = next;
  }

  setQty(itemId: string | number, value: string): void {
    const parsed = parseInt(value, 10);
    if (isNaN(parsed) || parsed < 1) return;
    const line = this.lines.find(l => l.item.id === itemId);
    if (line) line.qty = parsed;
  }

  removeLine(itemId: string | number): void {
    this.lines = this.lines.filter(l => l.item.id !== itemId);
  }

  get totalUnits(): number {
    return this.lines.reduce((acc, l) => acc + l.qty, 0);
  }

  // ── Add new item panel ────────────────────────────────────────────────

  toggleItemSearch(): void {
    this.showItemSearch = !this.showItemSearch;
    if (this.showItemSearch) {
      this.searchText = '';
      this.filterItems();
    }
  }

  filterItems(): void {
    const term = (this.searchText || '').toLowerCase().trim();
    if (!term) {
      this.filteredItems = this.allItems.slice(0, 50);
      return;
    }
    this.filteredItems = this.allItems
      .filter(i => (i.name || '').toLowerCase().includes(term))
      .slice(0, 50);
  }

  onSearchChange(): void {
    this.filterItems();
  }

  addItem(item: Item): void {
    const existing = this.lines.find(l => l.item.id === item.id);
    if (existing) {
      existing.qty += 1;
    } else {
      this.lines.push({ item, qty: 1 });
    }
    this.successMsg = `"${item.name}" added to request.`;
    setTimeout(() => { this.successMsg = ''; }, 2500);
  }

  // ── Save ──────────────────────────────────────────────────────────────

  saveChanges(): void {
    if (!this.requestId) return;

    if (this.lines.length === 0) {
      this.errorMsg = 'A request must contain at least one item.';
      return;
    }

    // Client-side duplicate check (same itemId appearing twice — shouldn't happen
    // from this UI, but guard it anyway)
    const seen = new Set<number>();
    for (const line of this.lines) {
      const id = Number(line.item.id);
      if (seen.has(id)) {
        this.errorMsg = `Duplicate item detected: "${line.item.name}". Please remove duplicates before saving.`;
        return;
      }
      seen.add(id);
    }

    this.submitting = true;
    this.errorMsg   = '';
    this.successMsg = '';

    const payload = {
      items: this.lines.map(l => ({
        itemId:   Number(l.item.id),
        quantity: l.qty
      }))
    };

    this.requestService.updateRequest(this.requestId, payload)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res: any) => {
          this.submitting = false;
          this.successMsg = res?.message || 'Request updated successfully!';

          // Notify sibling components (MyRequests list) to refresh
          this.refresh.notifyRequests();

          // Navigate back after brief success feedback
          setTimeout(() => {
            this.router.navigate(['/user-dashboard/my-requests']);
          }, 1200);
        },
        error: (err) => {
          this.errorMsg   = err?.message || 'Failed to save changes. Please try again.';
          this.submitting = false;
        }
      });
  }

  trackByItemId(_idx: number, line: EditLine): string | number {
    return line.item.id;
  }

  cancel(): void {
    this.router.navigate(['/user-dashboard/my-requests']);
  }
}
