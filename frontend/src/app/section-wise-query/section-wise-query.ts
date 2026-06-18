import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SectionWiseQueryService } from '../services/section-wise-query.service';
import { SectionWiseQueryOfficer, SectionWiseQueryItem, SectionWiseQueryRow } from '../models/section-wise-query.model';

@Component({
  selector: 'app-section-wise-query',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './section-wise-query.html',
  styleUrls: ['./section-wise-query.css']
})
export class SectionWiseQueryComponent implements OnInit {
  officers: SectionWiseQueryOfficer[] = [];
  bhawans: string[] = [];
  itemSuggestions: SectionWiseQueryItem[] = [];
  private suggestionCache: Record<string, SectionWiseQueryItem[]> = {};
  private suggestionTimer: any = null;
  suggestionLoading = signal(false);

  selectedOfficerId: number | null = null;
  fromDate = '';
  toDate = '';
  selectedBhawan = '';
  itemQuery = '';
  selectedItem: SectionWiseQueryItem | null = null;

  rows = signal<SectionWiseQueryRow[]>([]);
  loading = signal(false);
  errorMsg = signal('');
  emptyState = signal(false);
  page = signal(1);
  pageSize = signal(20);
  totalPages = signal(0);
  totalCount = signal(0);
  showItemDropdown = signal(false);

  constructor(private queryService: SectionWiseQueryService) {}

  ngOnInit() {
    this.loadDropdownData();
  }

  loadDropdownData() {
    this.queryService.getOfficers().subscribe({
      next: (response) => this.officers = response.officers,
      error: () => this.errorMsg.set('Unable to load officers. Please refresh.')
    });

    this.queryService.getBhawans().subscribe({
      next: (response) => this.bhawans = response.bhawans,
      error: () => this.errorMsg.set('Unable to load bhawans. Please refresh.')
    });
  }

  onItemInput(value: string) {
    this.itemQuery = value;
    this.selectedItem = null;
    this.showItemDropdown.set(true);

    const q = (value || '').trim();
    if (q.length < 2) {
      this.itemSuggestions = [];
      this.suggestionLoading.set(false);
      return;
    }

    // Check cache first
    if (this.suggestionCache[q]) {
      this.itemSuggestions = this.suggestionCache[q];
      this.showItemDropdown.set(true);
      return;
    }

    // Debounce outbound requests
    if (this.suggestionTimer) clearTimeout(this.suggestionTimer);
    this.suggestionLoading.set(true);
    this.suggestionTimer = setTimeout(() => {
      this.queryService.searchItems(q).subscribe({
        next: (result) => {
          this.itemSuggestions = result.items || [];
          // Cache by the exact query fragment
          this.suggestionCache[q] = this.itemSuggestions;
          this.suggestionLoading.set(false);
          this.showItemDropdown.set(true);
        },
        error: () => {
          this.itemSuggestions = [];
          this.suggestionLoading.set(false);
        }
      });
    }, 300);
  }

  selectItem(item: SectionWiseQueryItem) {
    this.selectedItem = item;
    this.itemQuery = item.name;
    this.itemSuggestions = [];
    this.showItemDropdown.set(false);
  }

  runSearch() {
    this.errorMsg.set('');
    if (this.fromDate && this.toDate && this.fromDate > this.toDate) {
      this.errorMsg.set('From Date cannot be later than To Date.');
      return;
    }

    const filter: any = {
      officerId: this.selectedOfficerId || undefined,
      fromDate: this.fromDate || undefined,
      toDate: this.toDate || undefined,
      bhawan: this.selectedBhawan || undefined,
      itemId: this.selectedItem?.id || undefined,
      itemName: this.selectedItem ? undefined : this.itemQuery || undefined,
      pageNumber: this.page(),
      pageSize: this.pageSize()
    };

    this.loading.set(true);
    this.rows.set([]);
    this.emptyState.set(false);

    this.queryService.getSectionWiseQuery(filter).subscribe({
      next: (result) => {
        this.rows.set(result.data || []);
        this.totalCount.set(result.totalCount);
        this.totalPages.set(result.totalPages);
        this.emptyState.set((result.data || []).length === 0);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMsg.set(err.error?.message || 'Failed to load section query results.');
      }
    });
  }

  downloadCsv() {
    this.errorMsg.set('');
    const filter: any = {
      officerId: this.selectedOfficerId || undefined,
      fromDate: this.fromDate || undefined,
      toDate: this.toDate || undefined,
      bhawan: this.selectedBhawan || undefined,
      itemId: this.selectedItem?.id || undefined,
      itemName: this.selectedItem ? undefined : this.itemQuery || undefined,
      pageNumber: 1,
      pageSize: 10000
    };

    this.loading.set(true);
    this.queryService.exportCsv(filter).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `section-wise-query-${new Date().toISOString().slice(0,19).replace(/[:T]/g,'')}.csv`;
        document.body.appendChild(a);
        a.click();
        a.remove();
        window.URL.revokeObjectURL(url);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMsg.set(err.error?.message || 'Failed to download CSV.');
      }
    });
  }

  resetForm() {
    this.selectedOfficerId = null;
    this.fromDate = '';
    this.toDate = '';
    this.selectedBhawan = '';
    this.selectedItem = null;
    this.itemQuery = '';
    this.itemSuggestions = [];
    this.errorMsg.set('');
    this.rows.set([]);
    this.emptyState.set(false);
    this.page.set(1);
    this.totalPages.set(0);
    this.totalCount.set(0);
  }

  onPageChange(nextPage: number) {
    if (nextPage < 1 || nextPage > this.totalPages()) {
      return;
    }
    this.page.set(nextPage);
    this.runSearch();
  }

  clearSelectedItem() {
    this.selectedItem = null;
    this.itemQuery = '';
    this.itemSuggestions = [];
  }

  trackById(_index: number, row: SectionWiseQueryRow): number {
    return row.requestItemId;
  }
}
