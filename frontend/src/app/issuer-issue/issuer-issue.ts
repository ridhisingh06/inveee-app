import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { WorkflowService } from '../services/workflow.service';
import { RefreshService } from '../services/refresh.service';
import { IssuerPendingItem, IssuerPendingList } from '../models/request.model';
import { normalizeStatus, getStatusLabel, getStatusClass } from '../utils/status.util';
import { TotalRequestedPipe, TotalIssuePipe, TotalRejectPipe } from '../pipes/workflow-totals.pipe';

/**
 * IssuerIssueComponent — Partial Issuing Table
 *
 * After a successful submit:
 *  1. The submitted group is removed from the local view (instant feedback).
 *  2. RefreshService.notifyIssuer() is called so IssuerDashboardComponent
 *     reloads its stats cards without a manual browser refresh.
 */
@Component({
  standalone: true,
  selector: 'app-issuer-issue',
  imports: [CommonModule, ReactiveFormsModule, TotalRequestedPipe, TotalIssuePipe, TotalRejectPipe],
  templateUrl: './issuer-issue.html',
  styleUrls: ['./issuer-issue.css']
})
export class IssuerIssueComponent implements OnInit, OnDestroy {
  // ── Data ─────────────────────────────────────────────────────────────────
  pendingList: IssuerPendingList | null = null;
  requestGroups: { requestId: number; requesterName: string; requestedDate: string; items: IssuerPendingItem[] }[] = [];
  formMap: { [requestId: number]: FormGroup } = {};

  // ── UI state ─────────────────────────────────────────────────────────────
  loading       = true;
  errorMsg      = '';
  successMsg    = '';
  submittingMap: { [requestId: number]: boolean } = {};
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
    private readonly fb: FormBuilder,
    private readonly refresh: RefreshService
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
    this.loading  = true;
    this.errorMsg = '';
    this.currentPage = page;

    this.workflow.getIssuerPendingItems(page, this.pageSize)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (list) => {
          this.pendingList = list;
          this.totalCount  = list.totalCount;
          this.buildGroups(list.items);
          this.loading = false;
        },
        error: (err) => {
          this.errorMsg = err.message || 'Failed to load pending items.';
          this.loading  = false;
        }
      });
  }

  refresh_page(): void { this.loadPage(this.currentPage); }

  onSearchChange(value: string): void {
    this.searchText = value;
    this.search$.next(value);
  }

  // ── Group + Form building ─────────────────────────────────────────────────

  private buildGroups(items: IssuerPendingItem[]): void {
    const normalizedSearch = this.searchText.trim().toLowerCase();
    const filtered = normalizedSearch
      ? items.filter(i =>
          (i.itemName ?? '').toLowerCase().includes(normalizedSearch) ||
          (i.requestedByUserName ?? '').toLowerCase().includes(normalizedSearch)
        )
      : items;

    const map = new Map<number, IssuerPendingItem[]>();
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
        requestedDate: groupItems[0].requestedDate,
        items: groupItems
      });

      const rowControls = groupItems.map(item =>
        this.fb.group({
          requestItemId: [item.requestItemId],
          requestedQty:  [item.requestedQuantity],
          availableQty:  [item.availableQuantity],
          issueQty: [
            Math.min(item.requestedQuantity, item.availableQuantity),
            [Validators.required, Validators.min(0), Validators.max(item.requestedQuantity)]
          ],
          rejectQty: [
            item.requestedQuantity - Math.min(item.requestedQuantity, item.availableQuantity)
          ]
        })
      );

      const fg = this.fb.group({ rows: this.fb.array(rowControls) });
      this.formMap[requestId] = fg;

      rowControls.forEach((rowFg, idx) => {
        rowFg.get('issueQty')!.valueChanges
          .pipe(takeUntil(this.destroy$))
          .subscribe(val => this.onIssueQtyChange(requestId, idx, val));
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

  onIssueQtyChange(requestId: number, rowIndex: number, rawValue: any): void {
    const row       = this.getRow(requestId, rowIndex);
    const requested = row.get('requestedQty')!.value as number;
    const available = row.get('availableQty')!.value as number;
    const issue     = Math.max(0, parseInt(rawValue, 10) || 0);
    const clamped   = Math.min(issue, requested, available);
    const reject    = requested - clamped;

    row.get('rejectQty')!.setValue(reject, { emitEvent: false });

    if (issue !== clamped) {
      row.get('issueQty')!.setValue(clamped, { emitEvent: false });
    }
  }

  // ── Per-row validation helpers ────────────────────────────────────────────

  rowValid(requestId: number, idx: number): boolean {
    const row  = this.getRow(requestId, idx);
    const req  = row.get('requestedQty')!.value as number;
    const avl  = row.get('availableQty')!.value as number;
    const iss  = row.get('issueQty')!.value as number;
    const rej  = row.get('rejectQty')!.value as number;
    return iss >= 0 && rej >= 0 && (iss + rej) === req && iss <= avl;
  }

  allRowsValid(requestId: number): boolean {
    const rows = this.getRows(requestId);
    for (let i = 0; i < rows.length; i++) {
      if (!this.rowValid(requestId, i)) return false;
    }
    return true;
  }

  isLowStock(requestId: number, idx: number): boolean {
    const row = this.getRow(requestId, idx);
    return (row.get('availableQty')!.value as number) <
           (row.get('requestedQty')!.value as number);
  }

  validationMessage(requestId: number, idx: number): string {
    const row  = this.getRow(requestId, idx);
    const req  = row.get('requestedQty')!.value as number;
    const avl  = row.get('availableQty')!.value as number;
    const iss  = row.get('issueQty')!.value as number;
    const rej  = row.get('rejectQty')!.value as number;

    if (iss < 0 || rej < 0)      return 'Quantities cannot be negative.';
    if (iss > avl)                return `Issue Qty exceeds available stock (${avl}).`;
    if (iss + rej !== req)        return `Issue + Reject must equal ${req}.`;
    return '';
  }

  // ── Submit ────────────────────────────────────────────────────────────────

  submit(requestId: number): void {
    if (!this.allRowsValid(requestId)) return;

    this.submittingMap[requestId] = true;
    this.submitErrorMap[requestId] = '';
    this.successMsg = '';

    const rows = this.getRows(requestId);
    const payload = {
      requestId,
      items: rows.controls.map(row => ({
        requestItemId: row.get('requestItemId')!.value,
        issueQuantity:  row.get('issueQty')!.value,
        rejectQuantity: row.get('rejectQty')!.value
      }))
    };

    this.workflow.issuePartially(payload)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          delete this.submittingMap[requestId];
          this.successMsg = res.message || `Request #${requestId} issued successfully.`;

          // Remove the submitted group from the view immediately.
          this.requestGroups = this.requestGroups.filter(g => g.requestId !== requestId);
          delete this.formMap[requestId];
          this.totalCount = Math.max(0, this.totalCount - payload.items.length);

          // ✅ Notify IssuerDashboardComponent to refresh stats cards so the
          //    "Pending" and "Issued" counts update without a page reload.
          this.refresh.notifyIssuer();

          setTimeout(() => { this.successMsg = ''; }, 5000);
        },
        error: (err) => {
          delete this.submittingMap[requestId];
          this.submitErrorMap[requestId] = err.message || 'Failed to submit issue.';
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
