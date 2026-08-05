import { Component, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { PersonnelService } from '../../services/personnel.service';
import { PersonnelResponse } from '../../models/personnel.model';

@Component({
  selector: 'app-personnel-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './personnel-list.html',
  styleUrls: ['./personnel-list.css']
})
export class PersonnelListComponent {
  readonly personnelRecords = signal<PersonnelResponse[]>([]);
  readonly personnelPage = signal(1);
  readonly personnelTotalCount = signal(0);
  readonly personnelTotalPages = signal(1);
  readonly personnelLoading = signal(false);
  readonly personnelError = signal('');

  constructor(private readonly personnelService: PersonnelService, private readonly destroyRef: DestroyRef) {
    this.fetchPersonnel();
  }

  fetchPersonnel(page = this.personnelPage()): void {
    this.personnelLoading.set(true);
    this.personnelError.set('');
    this.personnelService.getPersonnel(page).subscribe({
      next: (result) => {
        this.personnelRecords.set(result.data);
        this.personnelPage.set(result.page);
        this.personnelTotalCount.set(result.totalCount);
        this.personnelTotalPages.set(result.totalPages);
        this.personnelLoading.set(false);
      },
      error: (err) => {
        console.error('Unable to load personnel records', err);
        this.personnelError.set('Failed to load personnel records.');
        this.personnelLoading.set(false);
      }
    });
  }

  changePage(page: number): void {
    if (page < 1 || page > this.personnelTotalPages()) return;
    this.personnelPage.set(page);
    this.fetchPersonnel(page);
  }

  formatDate(dateString?: string): string {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleDateString();
  }

  trackByPersonnelId(_: number, item: PersonnelResponse): number {
    return item.id;
  }

  // Returns a label for the current view (placeholder for future enhancements)
  selectedLabel(): string {
    return '';
  }

  // Deletes a personnel record and refreshes the list
  deletePersonnel(id: number): void {
    this.personnelService.deletePersonnel(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          // Refresh current page after deletion
          this.fetchPersonnel(this.personnelPage());
        },
        error: err => {
          console.error('Failed to delete personnel:', err);
        }
      });
  }
}

