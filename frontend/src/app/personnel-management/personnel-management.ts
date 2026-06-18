import { CommonModule } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { PersonnelService } from '../services/personnel.service';
import { PersonnelResponse } from '../models/personnel.model';

type NavItem = {
  label: string;
  route: any[] | string;
  slug?: string;
  description?: string;
};

@Component({
  selector: 'app-personnel-management',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './personnel-management.html',
  styleUrls: ['./personnel-management.css']
})
export class PersonnelManagementComponent {
  readonly codeFileItems: NavItem[] = [
    {
      label: 'Personnel Details – New Entry',
      slug: 'personnel-details-new-entry',
      route: ['/personnel-management', 'personnel-details-new-entry'],
      description: 'Create new personnel records'
    },
    {
      label: 'Division names, codes New Entry',
      slug: 'division-new-entry',
      route: ['/personnel-management', 'division-new-entry'],
      description: 'Add divisions and codes'
    },
    {
      label: 'New Stores Sections: Entry',
      slug: 'stores-sections-new-entry',
      route: ['/personnel-management', 'stores-sections-new-entry'],
      description: 'Create stores sections'
    },
    {
      label: 'New Sections: Entry',
      slug: 'sections-new-entry',
      route: ['/personnel-management', 'sections-new-entry'],
      description: 'Create sections'
    },
    {
      label: 'Personnel Details – Modification/Deletion',
      slug: 'personnel-details-modify-delete',
      route: ['/personnel-management', 'personnel-details-modify-delete'],
      description: 'Update or remove personnel'
    },
    {
      label: 'Division names, codes Modification/Deletion',
      slug: 'division-modify-delete',
      route: ['/personnel-management', 'division-modify-delete'],
      description: 'Update or remove divisions'
    },
    {
      label: 'Stores Section Allocation',
      route: '/stores-section-allocation',
      description: 'Allocate/modify stores section incharge'
    },
    {
      label: 'Incharge Allocation',
      route: '/incharge-allocation',
      description: 'Open incharge allocation form'
    }
  ];

  readonly reportItems: NavItem[] = [
    {
      label: 'SO List',
      slug: 'report-so-list',
      route: ['/personnel-management', 'report-so-list'],
      description: 'Section Officers list'
    },
    {
      label: 'List All',
      slug: 'report-list-all',
      route: ['/personnel-management', 'report-list-all'],
      description: 'Complete personnel list'
    },
    {
      label: 'Stores Sections List',
      slug: 'report-stores-sections-list',
      route: ['/personnel-management', 'report-stores-sections-list'],
      description: 'Stores sections directory'
    },
    {
      label: 'US List',
      slug: 'report-us-list',
      route: ['/personnel-management', 'report-us-list'],
      description: 'Unit Supervisors list'
    },
    {
      label: 'Sections List',
      slug: 'report-sections-list',
      route: ['/personnel-management', 'report-sections-list'],
      description: 'All sections list'
    }
  ];

  private readonly selectedSlug = signal<string | null>(null);
  readonly selectedLabel = computed(() => {
    const slug = this.selectedSlug();
    if (!slug) return null;
    const all = [...this.codeFileItems, ...this.reportItems];
    return all.find(i => i.slug === slug)?.label ?? slug;
  });

  readonly personnelRecords = signal<PersonnelResponse[]>([]);
  readonly personnelPage = signal(1);
  readonly personnelTotalCount = signal(0);
  readonly personnelTotalPages = signal(1);
  readonly personnelLoading = signal(false);
  readonly personnelError = signal('');

  private readonly personnelListSlugs = new Set([
    'report-list-all',
    'personnel-details-modify-delete'
  ]);

  constructor(
    route: ActivatedRoute,
    private readonly personnelService: PersonnelService
  ) {
    route.paramMap.subscribe(map => {
      const slug = map.get('slug');
      this.selectedSlug.set(slug);
      if (slug && this.personnelListSlugs.has(slug)) {
        this.fetchPersonnel();
      } else {
        this.personnelRecords.set([]);
        this.personnelError.set('');
      }
    });
  }

  get showPersonnelListView() {
    return this.personnelListSlugs.has(this.selectedSlug() ?? '');
  }

  fetchPersonnel(page = this.personnelPage()): void {
    this.personnelLoading.set(true);
    this.personnelError.set('');
    this.personnelService.getPersonnel(page)
      .subscribe({
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

  deletePersonnel(id: number): void {
    if (!confirm('Delete this personnel record? This cannot be undone.')) {
      return;
    }

    this.personnelLoading.set(true);
    this.personnelService.deletePersonnel(id)
      .subscribe({
        next: () => {
          this.personnelRecords.update(current => current.filter(p => p.id !== id));
          this.personnelLoading.set(false);
          if (this.personnelRecords().length === 0 && this.personnelPage() > 1) {
            this.changePage(this.personnelPage() - 1);
          }
        },
        error: (err) => {
          console.error('Delete failed', err);
          this.personnelError.set('Unable to delete personnel record.');
          this.personnelLoading.set(false);
        }
      });
  }

  changePage(page: number): void {
    if (page < 1 || page > this.personnelTotalPages()) {
      return;
    }

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
}

