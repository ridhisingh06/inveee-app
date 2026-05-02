import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { NavbarComponent } from '../navbar/navbar';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, NavbarComponent],
  templateUrl: './admin-dashboard.html',
  styleUrls: ['./admin-dashboard.css']
})
export class AdminDashboardComponent {
  isNavOpen = false;
  search = '';

  readonly navGroups: Array<{
    title: string;
    subtitle?: string;
    items: Array<{
      label: string;
      route: string;
      icon: string;
      description?: string;
    }>;
  }> = [
    {
      title: 'Quick Links',
      subtitle: 'Common screens',
      items: [
        {
          label: 'Inventory',
          route: '/inventory',
          icon: 'i-box',
          description: 'View and manage stock items'
        },
        {
          label: 'Personnel Management',
          route: '/personnel-management',
          icon: 'i-people',
          description: 'Manage personnel and sections'
        },
        {
          label: 'Request Item',
          route: '/request-item',
          icon: 'i-doc',
          description: 'Create a stationery request'
        },
        {
          label: 'Pending Requests',
          route: '/pending-requests',
          icon: 'i-inbox',
          description: 'Review admin pending requests'
        }
      ]
    },
    {
      title: 'Issue & Entry',
      subtitle: 'Daily transactions and registers',
      items: [
        {
          label: 'Physical Issue',
          route: '/physical-issue',
          icon: 'i-issue',
          description: 'Issue stationery against requests'
        },
        {
          label: 'Physical Receipt',
          route: '/physical-receipt',
          icon: 'i-receipt',
          description: 'Record receipt from supplier/stock'
        },
        {
          label: 'Delivery Challan/Bill Entry',
          route: '/delivery-challan-bill-entry',
          icon: 'i-doc',
          description: 'Capture challan and bill details'
        },
        {
          label: 'Proxy Entry',
          route: '/proxy-entry',
          icon: 'i-proxy',
          description: 'Enter items on behalf of section'
        },
        {
          label: 'Proxy Receipt',
          route: '/proxy-receipt',
          icon: 'i-proxy',
          description: 'Receive items on behalf of section'
        },
        {
          label: 'Monthly Register',
          route: '/monthly-register',
          icon: 'i-calendar',
          description: 'Monthly issue/receipt register'
        },
        {
          label: 'Process for Monthly Register',
          route: '/process-monthly-register',
          icon: 'i-settings',
          description: 'Generate and finalize monthly register'
        }
      ]
    },
    {
      title: 'Management',
      subtitle: 'People, sections, and suggestions',
      items: [
        {
          label: 'Personnel Management',
          route: '/personnel-management',
          icon: 'i-people',
          description: 'Manage users and roles'
        },
        {
          label: 'Stores Section Allocation',
          route: '/stores-section-allocation',
          icon: 'i-settings',
          description: 'Allocate incharge to stores sections'
        },
        {
          label: 'Incharge Allocation',
          route: '/incharge-allocation',
          icon: 'i-settings',
          description: 'Open allocation form'
        },
        {
          label: 'Sections List',
          route: '/sections-list',
          icon: 'i-list',
          description: 'View and maintain sections'
        },
        {
          label: 'View Suggestions',
          route: '/view-suggestions',
          icon: 'i-suggest',
          description: 'Review improvement suggestions'
        }
      ]
    },
    {
      title: 'Queries & Reports',
      subtitle: 'Search, stock, and analytics',
      items: [
        {
          label: 'Item wise Query',
          route: '/item-wise-query',
          icon: 'i-search',
          description: 'Search transactions by item'
        },
        {
          label: 'Receipt Query',
          route: '/receipt-query',
          icon: 'i-search',
          description: 'Search receipts and invoices'
        },
        {
          label: 'Section wise Query',
          route: '/section-wise-query',
          icon: 'i-search',
          description: 'Search usage by section'
        },
        {
          label: 'Present Stock Position',
          route: '/present-stock-position',
          icon: 'i-chart',
          description: 'Current stock and balances'
        },
        {
          label: 'List of Exhausted Items',
          route: '/exhausted-items',
          icon: 'i-alert',
          description: 'Items at or near zero stock'
        }
      ]
    },
    {
      title: 'Approvals & Monitoring',
      subtitle: 'Pending items and compliance checks',
      items: [
        {
          label: 'Pending Approvals',
          route: '/pending-approvals',
          icon: 'i-approve',
          description: 'Approve requests and entries'
        },
        {
          label: 'Pending Receipts',
          route: '/pending-receipts',
          icon: 'i-inbox',
          description: 'Receipts awaiting verification'
        },
        {
          label: 'Sections Not Taking Items',
          route: '/sections-not-taking-items',
          icon: 'i-eye',
          description: 'Monitor non-collection patterns'
        }
      ]
    }
  ];

  toggleNav() {
    this.isNavOpen = !this.isNavOpen;
  }

  closeNav() {
    this.isNavOpen = false;
  }

  clearSearch() {
    this.search = '';
  }

  get totalModules(): number {
    return this.navGroups.reduce((sum, g) => sum + g.items.length, 0);
  }

  get filteredGroups() {
    const q = this.search.trim().toLowerCase();
    if (!q) return this.navGroups;

    return this.navGroups
      .map((g) => ({
        ...g,
        items: g.items.filter((i) => {
          const hay = `${i.label} ${i.description ?? ''} ${g.title}`.toLowerCase();
          return hay.includes(q);
        })
      }))
      .filter((g) => g.items.length > 0);
  }

  trackByLabel(_: number, item: { label: string }) {
    return item.label;
  }
}
