import { CommonModule } from '@angular/common';
import { Component, ChangeDetectorRef, OnDestroy, OnInit } from '@angular/core';
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

  lines: EditLine[]     = [];
  allItems: Item[]      = [];
  filteredItems: Item[] = [];
  searchText     = '';
  showItemSearch = false;

  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly route:          ActivatedRoute,
    private readonly router:         Router,
    private readonly http:           HttpClient,
    private readonly requestService: RequestService,
    private readonly refresh:        RefreshService,
    private readonly cdr:            ChangeDetectorRef
  ) {
    console.log('[EditRequestComponent] constructor called');
  }

  ngOnInit(): void {
    console.log('[EditRequestComponent] ngOnInit called');
    this.route.paramMap
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        console.log('[EditRequestComponent] paramMap subscription fired', params);
        const idStr = params.get('id');
        if (!idStr || isNaN(+idStr) || +idStr <= 0) {
          this.errorMsg = 'No valid request ID provided.';
          this.loading  = false;
          console.log('[EditRequestComponent] Invalid request ID, loading set to false');
          return;
        }
        this.requestId = +idStr;
        console.log('[EditRequestComponent] Valid request ID:', this.requestId, 'calling loadRequest()');
        this.loadRequest();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Load request from GET /api/requests/{id} ────────────────────────────

  loadRequest(): void {
    console.log('[EditRequestComponent] loadRequest() called, requestId:', this.requestId);
    this.loading  = true;
    this.errorMsg = '';
    this.lines    = [];

    this.requestService.getRequestById(this.requestId!)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (req) => {
          console.log('[EditRequestComponent] API response received:', req);
          // Cast to any so we can safely read camelCase properties regardless
          // of whether the TypeScript interface is fully typed.
          const r = req as any;

          // ── 1. Normalise request-level status ──────────────────────────────
          // Backend serialises RequestStatus enum as PascalCase string e.g.
          // "PendingWithIssuer" (via JsonStringEnumConverter).  Lower-casing it
          // lets us do a simple equality check that also handles legacy aliases
          // ("Requested" → "requested" is caught by the second condition).
          const reqStatus: string = (r.status ?? '').toLowerCase().trim();
          const isPending = reqStatus === 'pendingwithissuer' || reqStatus === 'requested';
          console.log('[EditRequestComponent] Request status:', r.status, 'isPending:', isPending);

          if (!isPending) {
            this.errorMsg = `This request is in "${r.status}" status and can no longer be edited.`;
            this.loading  = false;
            console.log('[EditRequestComponent] Status not pending, loading set to false');
            return;
          }

          // ── 2. Normalise item list ────────────────────────────────────────
          // Backend uses camelCase JSON ("items"), but guard against both
          // casings just in case a proxy or older backend build uses PascalCase.
          const items: any[] = r.items ?? r.Items ?? [];
          console.log('[EditRequestComponent] Items count:', items.length);

          if (items.length === 0) {
            this.errorMsg = 'This request has no items and cannot be edited.';
            this.loading  = false;
            console.log('[EditRequestComponent] No items, loading set to false');
            return;
          }

          // ── 3. Check that the issuer has NOT started processing ────────────
          // If ANY item has moved out of PendingWithIssuer the issuer has
          // touched it; editing must be blocked.
          const allItemsPending = items.every((i: any) => {
            const s = (i.status ?? i.Status ?? '').toLowerCase().trim();
            return s === 'pendingwithissuer' || s === 'requested';
          });
          console.log('[EditRequestComponent] allItemsPending:', allItemsPending);

          if (!allItemsPending) {
            this.errorMsg = 'The issuer has started processing this request. Editing is no longer allowed.';
            this.loading  = false;
            console.log('[EditRequestComponent] Items not all pending, loading set to false');
            return;
          }

          // ── 4. Populate the editable lines ────────────────────────────────
          // The detail endpoint (RequestItemDetailDto) uses camelCase field
          // names: itemId, itemName, quantityRequested.
          this.lines = items.map((ri: any) => ({
            item: {
              id:       ri.itemId   ?? ri.ItemId   ?? 0,
              itemCode: ri.itemCode ?? ri.ItemCode ?? String(ri.itemId ?? ''),
              name:     ri.itemName ?? ri.ItemName ?? 'Unknown',
              category: 'Unknown'
            } as Item,
            qty: ri.quantityRequested ?? ri.QuantityRequested ?? 1
          }));
          console.log('[EditRequestComponent] Lines populated:', this.lines.length, 'loading set to false');

          this.loading = false;
          this.cdr.detectChanges();

          // ── 5. Load inventory in background for "Add New Item" ────────────
          this.loadInventory();
        },
        error: (err) => {
          console.log('[EditRequestComponent] API error:', err);
          this.errorMsg = err?.message || 'Failed to load request. Please try again.';
          this.loading  = false;
        }
      });
  }

  // ── Load inventory lazily — non-blocking ───────────────────────────────

  loadInventory(): void {
    this.inventoryLoading = true;

    this.http.get<any>(`${environment.apiUrl}/inventory`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          // GET /api/inventory returns a flat array directly (not paginated).
          // Guard against unexpected shapes just in case.
          const raw: any[] = Array.isArray(res) ? res : (res?.data ?? res?.items ?? []);

          this.allItems = raw.map((i: any) => ({
            id:       i.id   ?? i.Id   ?? 0,
            itemCode: i.itemCode ?? i.ItemCode ?? String(i.id ?? ''),
            name:     i.name ?? i.Name ?? '',
            category: i.category ?? i.Category ?? 'Uncategorized'
          } as Item)).filter(i => i.id && i.name);

          this.inventoryLoading = false;
        },
        error: () => {
          // Non-fatal: user can still edit quantities on existing items even
          // if the inventory endpoint is unavailable.
          this.inventoryLoading = false;
        }
      });
  }

  // ── Quantity controls ──────────────────────────────────────────────────

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

  // ── Add-item panel ─────────────────────────────────────────────────────

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

  // ── Save changes via PUT /api/requests/{id} ────────────────────────────

  saveChanges(): void {
    if (!this.requestId) return;

    // Guard: at least one item required.
    if (this.lines.length === 0) {
      this.errorMsg = 'A request must contain at least one item.';
      return;
    }

    // Guard: all quantities must be ≥ 1.
    const invalidLine = this.lines.find(l => !l.qty || l.qty < 1);
    if (invalidLine) {
      this.errorMsg = `Quantity for "${invalidLine.item.name}" must be at least 1.`;
      return;
    }

    // Guard: no duplicate items (defensive — the UI prevents duplicates, but
    // validate anyway before hitting the network).
    const seen = new Set<number>();
    for (const line of this.lines) {
      const id = Number(line.item.id);
      if (seen.has(id)) {
        this.errorMsg = `Duplicate item: "${line.item.name}". Please remove the duplicate before saving.`;
        return;
      }
      seen.add(id);
    }

    this.submitting = true;
    this.errorMsg   = '';
    this.successMsg = '';

    // Payload shape must match backend UpdateRequestDto:
    //   { items: [{ itemId: number, quantity: number }] }
    const payload = {
      items: this.lines.map(l => ({
        itemCode: String(l.item.itemCode || l.item.id),
        quantity: l.qty
      }))
    };

    this.requestService.updateRequest(this.requestId, payload)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res: any) => {
          this.submitting = false;
          this.successMsg = res?.message || 'Request updated successfully!';

          // Signal the My-Requests list to reload so it shows the updated data.
          this.refresh.notifyRequests();

          // Navigate back after a brief success flash.
          setTimeout(() => {
            this.router.navigate(['/user-dashboard/my-requests']);
          }, 1200);
        },
        error: (err) => {
          this.submitting = false;
          this.errorMsg   = err?.message || 'Failed to save changes. Please try again.';
        }
      });
  }

  trackByItemCode(_idx: number, line: EditLine): string | number {
    return line.item.itemCode || line.item.id;
  }

  cancel(): void {
    this.router.navigate(['/user-dashboard/my-requests']);
  }
}
