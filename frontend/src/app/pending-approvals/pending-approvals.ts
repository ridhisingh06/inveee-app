import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormArray } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { WorkflowService } from '../services/workflow.service';
import { AdminPendingItem, AdminPendingList } from '../models/request.model';
import { normalizeStatus, getStatusLabel, getStatusClass } from '../utils/status.util';
import {
  TotalIssuedPipe,
  TotalApprovePipe,
  TotalRejectPipe
} from '../pipes/workflow-totals.pipe';

/**
 * PendingApprovalsComponent — Admin Partial-Approval Table
 *
 * Replaces the old binary approve/reject-per-item UI.
 *
 * New behaviour:
 *  - Fetches items from GET /api/admin/pending (grouped by requestId)
 *  - Each item shows: Requested Qty, Issuer-Issued Qty, Approve Qty (editable), Reject Qty (auto)
 *  - Validation: approveQty + rejectQty = issuerIssuedQty, no negatives, approve ≤ issued
 *  - Submit per request group via PUT /api/admin/approve-partially
 *  - Rejected quantities are restored to inventory on the backend (inside a DB transaction)
 */
@Component({
  standalone: true,
  selector: 'app-pending-approvals',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TotalIssuedPipe,
    TotalApprovePipe,
    TotalRejectPipe
  ],
  templateUrl: './pending-approvals.html',
  styleUrls: ['./pending-approvals.css']
})
export class PendingApprovalsComponent implements OnInit, OnDestroy {
  // ── Data ─────────────────────────────────────────────────────────────────
  requestGroups: {
    requestId: number;
    requesterName: string;
    issuedByName: string;
    issuedDate: string;
    items: AdminPendingItem[];
  }[] = [];

  /** requestId → FormGroup */
  formMap: { [requestId: number]: FormGroup } = {};

  // ── UI state ─────────────────────────────────────────────────────────────
  loading       = true;
  errorMsg      = '';
  successMsg    = '';
  /** requestId → true while submitting */
  submittingMap: { [requestId: number]: boolean } = {};
  /** requestId → error string */
  submitErrorMap: { [requestId: number]: string } = {};

  // ── Pagination / search ───────────────────────────────────────────────────
  currentPage = 1;
  pageSize    = 20;
  totalCount  = 0;
  searchText  = '';

  private readonly search$  = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly workflow: WorkflowService,
    private readonly fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.search$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => this.loadPage(1));

    this.loadPage(1);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Load ──────────────────────────────────────────────────────────────────

  loadPage(page: number): void {
    this.loading     = true;
    this.errorMsg    = '';
    this.currentPage = page;

    this.workflow.getAdminPendingItems(page, this.pageSize)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (list: AdminPendingList) => {
          this.totalCount = list.totalCount;
          this.buildGroups(list.items);
          this.loading = false;
        },
        error: (err) => {
          this.errorMsg = err.message || 'Failed to load pending items.';
          this.loading  = false;
        }
      });
  }

  refresh(): void { this.loadPage(this.currentPage); }

  onSearchChange(value: string): void {
    this.searchText = value;
    this.search$.next(value);
  }

  // ── Group + Form building ─────────────────────────────────────────────────

  private buildGroups(items: AdminPendingItem[]): void {
    const normalizedSearch = this.searchText.trim().toLowerCase();
    const filtered = normalizedSearch
      ? items.filter(i =>
          (i.itemName ?? '').toLowerCase().includes(normalizedSearch) ||
          (i.requestedByUserName ?? '').toLowerCase().includes(normalizedSearch) ||
          (i.issuedByUserName ?? '').toLowerCase().includes(normalizedSearch)
        )
      : items;

    const map = new Map<number, AdminPendingItem[]>();
    for (const item of filtered) {
      const arr = map.get(item.requestId) ?? [];
      arr.push(item);
      map.set(item.requestId, arr);
    }

    this.requestGroups = [];
    this.formMap = {};

    map.forEach((groupItems, requestId) => {
      this.requestGroups.push({
        requestId,
        requesterName: groupItems[0].requestedByUserName,
        issuedByName:  groupItems[0].issuedByUserName,
        issuedDate:    groupItems[0].issuedDate,
        items: groupItems
      });

      const rowControls = groupItems.map(item =>
        this.fb.group({
          requestItemId: [item.requestItemId],
          requestedQty:  [item.requestedQuantity],
          issuedQty:     [item.issuerIssuedQuantity],
          issuerRejected:[item.issuerRejectedQuantity],
          // Default: approve all that issuer issued
          approveQty: [item.issuerIssuedQuantity],
          rejectQty:  [0]
        })
      );

      const fg = this.fb.group({ rows: this.fb.array(rowControls) });
      this.formMap[requestId] = fg;

      // Wire up live recalc for each row
      rowControls.forEach((rowFg, idx) => {
        rowFg.get('approveQty')!.valueChanges
          .pipe(takeUntil(this.destroy$))
          .subscribe(val => this.onApproveQtyChange(requestId, idx, val));
      });
    });
  }

  getRows(requestId: number): FormArray {
    return this.formMap[requestId].get('rows') as FormArray;
  }

  getRow(requestId: number, idx: number): FormGroup {
    return this.getRows(requestId).at(idx) as FormGroup;
  }

  // ── Live qty recalc ───────────────────────────────────────────────────────

  onApproveQtyChange(requestId: number, rowIndex: number, rawValue: any): void {
    const row    = this.getRow(requestId, rowIndex);
    const issued = row.get('issuedQty')!.value as number;
    const approve = Math.max(0, Math.min(parseInt(rawValue, 10) || 0, issued));
    const reject  = issued - approve;

    row.get('rejectQty')!.setValue(reject, { emitEvent: false });

    if ((parseInt(rawValue, 10) || 0) !== approve) {
      row.get('approveQty')!.setValue(approve, { emitEvent: false });
    }
  }

  // ── Per-row validation ────────────────────────────────────────────────────

  rowValid(requestId: number, idx: number): boolean {
    const row    = this.getRow(requestId, idx);
    const issued = row.get('issuedQty')!.value as number;
    const approve= row.get('approveQty')!.value as number;
    const reject = row.get('rejectQty')!.value as number;
    return approve >= 0 && reject >= 0 && (approve + reject) === issued && approve <= issued;
  }

  allRowsValid(requestId: number): boolean {
    const rows = this.getRows(requestId);
    for (let i = 0; i < rows.length; i++) {
      if (!this.rowValid(requestId, i)) return false;
    }
    return true;
  }

  validationMessage(requestId: number, idx: number): string {
    const row    = this.getRow(requestId, idx);
    const issued = row.get('issuedQty')!.value as number;
    const approve= row.get('approveQty')!.value as number;
    const reject = row.get('rejectQty')!.value as number;

    if (approve < 0 || reject < 0)        return 'Quantities cannot be negative.';
    if (approve > issued)                  return `Cannot approve more than issued (${issued}).`;
    if (approve + reject !== issued)       return `Approve + Reject must equal ${issued}.`;
    return '';
  }

  // ── Submit ────────────────────────────────────────────────────────────────

  submit(requestId: number): void {
    if (!this.allRowsValid(requestId)) return;

    this.submittingMap[requestId]  = true;
    this.submitErrorMap[requestId] = '';
    this.successMsg = '';

    const rows    = this.getRows(requestId);
    const payload = {
      requestId,
      items: rows.controls.map(row => ({
        requestItemId: row.get('requestItemId')!.value,
        approveQuantity: row.get('approveQty')!.value,
        rejectQuantity:  row.get('rejectQty')!.value
      }))
    };

    this.workflow.approvePartially(payload)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          delete this.submittingMap[requestId];
          this.successMsg = res.message || `Request #${requestId} approved successfully.`;
          this.requestGroups = this.requestGroups.filter(g => g.requestId !== requestId);
          delete this.formMap[requestId];
          this.totalCount = Math.max(0, this.totalCount - payload.items.length);
          setTimeout(() => { this.successMsg = ''; }, 5000);
        },
        error: (err) => {
          delete this.submittingMap[requestId];
          this.submitErrorMap[requestId] = err.message || 'Failed to submit approval.';
        }
      });
  }

  // ── Pagination ─────────────────────────────────────────────────────────────

  get totalPages(): number { return Math.max(1, Math.ceil(this.totalCount / this.pageSize)); }
  prevPage(): void { if (this.currentPage > 1) this.loadPage(this.currentPage - 1); }
  nextPage(): void { if (this.currentPage < this.totalPages) this.loadPage(this.currentPage + 1); }

  // ── Template helpers ───────────────────────────────────────────────────────

  normalizeStatus = normalizeStatus;
  getStatusLabel  = getStatusLabel;
  getStatusClass  = getStatusClass;

  trackByRequestId(_: number, g: any): number { return g.requestId; }
  trackByIdx(idx: number): number { return idx; }
}
