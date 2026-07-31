import { Component, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { PersonnelService } from '../services/personnel.service';
import { PersonnelResponse } from '../models/personnel.model';

@Component({
  selector: 'app-personnel-details-modify-delete',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './personnel-details-modify-delete.html',
  styleUrls: ['./personnel-details-modify-delete.css']
})
export class PersonnelDetailsModifyDeleteComponent {
  // State signals
  readonly personnelRecords = signal<PersonnelResponse[]>([]);
  readonly personnelPage = signal(1);
  readonly personnelTotalCount = signal(0);
  readonly personnelTotalPages = signal(1);
  readonly personnelLoading = signal(false);
  readonly personnelError = signal('');

  // Pagination size (use same default as backend)
  private readonly pageSize = 20;

  constructor(private readonly personnelService: PersonnelService, private readonly router: Router) {
    this.fetchPersonnel();
  }

  private fetchPersonnel(page = this.personnelPage()): void {
    this.personnelLoading.set(true);
    this.personnelError.set('');
    this.personnelService.getPersonnel(page, this.pageSize).subscribe({
      next: result => {
        this.personnelRecords.set(result.data);
        this.personnelPage.set(result.page);
        this.personnelTotalCount.set(result.totalCount);
        this.personnelTotalPages.set(result.totalPages);
        this.personnelLoading.set(false);
      },
      error: err => {
        console.error('Unable to load personnel records', err);
        this.personnelError.set('Failed to load personnel records.');
        this.personnelLoading.set(false);
      }
    });
  }

  deletePersonnel(id: number): void {
    const confirmed = window.confirm('Delete this personnel record? This cannot be undone.');
    if (!confirmed) return;

    this.personnelLoading.set(true);
    this.personnelService.deletePersonnel(id).subscribe({
      next: () => {
        // Refresh the list to update totalCount and totalPages
        this.fetchPersonnel(this.personnelPage());
        this.personnelLoading.set(false);
      },
      error: err => {
        console.error('Delete failed', err);
        this.personnelError.set('Unable to delete personnel record.');
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

  // Navigation to edit form
  editPersonnel(id: number): void {
    this.router.navigate(['/personnel-management', 'personnel-details-new-entry', id]);
  }
}
