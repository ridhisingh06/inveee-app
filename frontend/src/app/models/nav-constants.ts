export interface NavItem {
  label: string;
  route: string;
  icon: string;
  description?: string;
}

export interface NavGroup {
  title: string;
  subtitle?: string;
  items: NavItem[];
}

export const ADMIN_NAV_GROUPS: NavGroup[] = [
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
        label: 'Delivery Challan/Bill Entry',
        route: '/delivery-challan-bill-entry',
        icon: 'i-doc',
        description: 'Capture challan and bill details'
      },
      {
        label: 'Monthly Register',
        route: '/monthly-register',
        icon: 'i-calendar',
        description: 'View monthly issued/requested register'
      },
      {
        label: 'Section Wise Query',
        route: '/section-wise-query',
        icon: 'i-bar-chart',
        description: 'Run section wise item and officer queries'
      },
      {
        label: 'Pending Requests',
        route: '/pending-requests',
        icon: 'i-inbox',
        description: 'Review pending administrative requests'
      },
      {
        label: 'Pending Approvals',
        route: '/pending-approvals',
        icon: 'i-approve',
        description: 'Approve or reject pending item requests'
      }
    ]
  },
  {
    title: 'Operations',
    subtitle: 'Daily activity screens',
    items: [
      {
        label: 'Category Management',
        route: '/category-management',
        icon: 'i-list',
        description: 'View and manage item categories'
      },
      {
        label: 'Stores Section Allocation',
        route: '/stores-section-allocation',
        icon: 'i-settings',
        description: 'Allocate section incharges to stores'
      },
      {
        label: 'Incharge Allocation',
        route: '/incharge-allocation',
        icon: 'i-settings',
        description: 'Open incharge allocation form'
      }
    ]
  },
  {
    title: 'Personnel Actions',
    subtitle: 'Create and review staff entries',
    items: [
      {
        label: 'New Personnel Entry',
        route: '/personnel-management/personnel-details-new-entry',
        icon: 'i-plus',
        description: 'Add a new personnel record'
      },
      {
        label: 'View Personnel Reports',
        route: '/personnel-management',
        icon: 'i-people',
        description: 'Review personnel reports and details'
      }
    ]
  }
];
