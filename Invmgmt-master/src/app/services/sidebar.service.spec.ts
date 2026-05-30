import { TestBed } from '@angular/core/testing';
import { SidebarService } from './sidebar.service';

describe('SidebarService', () => {
  let service: SidebarService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SidebarService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should start with sidebarCollapsed = false', () => {
    expect(service.sidebarCollapsed()).toBe(false);
  });

  it('should toggle sidebar', () => {
    service.toggleSidebar();
    expect(service.sidebarCollapsed()).toBe(true);
    service.toggleSidebar();
    expect(service.sidebarCollapsed()).toBe(false);
  });

  it('should close sidebar', () => {
    service.closeSidebar();
    expect(service.sidebarCollapsed()).toBe(true);
    
    // Test idempotency
    service.closeSidebar();
    expect(service.sidebarCollapsed()).toBe(true);
  });

  it('should open sidebar', () => {
    service.closeSidebar(); // start closed
    service.openSidebar();
    expect(service.sidebarCollapsed()).toBe(false);
    
    // Test idempotency
    service.openSidebar();
    expect(service.sidebarCollapsed()).toBe(false);
  });
});
