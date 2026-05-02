import { CommonModule } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { NavbarComponent } from '../navbar/navbar';

type NavItem = {
  label: string;
  route: any[] | string;
  slug?: string;
  description?: string;
};

@Component({
  selector: 'app-personnel-management',
  standalone: true,
  imports: [CommonModule, RouterModule, NavbarComponent],
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

  constructor(route: ActivatedRoute) {
    route.paramMap.subscribe(map => {
      this.selectedSlug.set(map.get('slug'));
    });
  }
}

