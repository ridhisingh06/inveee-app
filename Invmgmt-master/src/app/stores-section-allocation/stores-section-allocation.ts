import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, firstValueFrom, takeUntil } from 'rxjs';
import { environment } from '../../environments/environment';
import { NavbarComponent } from '../navbar/navbar';

type Mode = 'modify' | 'delete';

type SectionSummary = {
  code: string;
  name: string;
  totalStaff: number;
  inchargeEmployeeCode?: string | null;
  inchargeName?: string | null;
};

type EmployeeLookup = { code: string; name: string };

@Component({
  selector: 'app-stores-section-allocation',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, NavbarComponent],
  templateUrl: './stores-section-allocation.html',
  styleUrls: ['./stores-section-allocation.css']
})
export class StoresSectionAllocationComponent implements OnInit, OnDestroy {
  mode: Mode = 'modify';

  isLoading = false;
  isSaving = false;

  message: { type: 'success' | 'error' | 'info'; text: string } | null = null;

  sections: SectionSummary[] = [];
  selectedSectionCode = '';

  sectionName = '';
  inchargeEmployeeCode = '';
  inchargeName = '';
  totalStaff: number | null = null;

  private readonly destroy$ = new Subject<void>();
  private readonly inchargeCode$ = new Subject<string>();

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.inchargeCode$
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(code => {
        const trimmed = (code ?? '').trim();
        if (!trimmed) {
          this.inchargeName = '';
          return;
        }
        this.lookupInchargeName(trimmed);
      });

    this.loadSections();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get isReadOnly() {
    return this.mode === 'delete';
  }

  setMode(mode: Mode) {
    this.mode = mode;
    this.message = null;
  }

  async loadSections() {
    this.isLoading = true;
    this.message = null;

    try {
      // Expected API shape (adjust as per backend):
      // GET /sections -> [{ code, name, totalStaff, inchargeEmployeeCode, inchargeName }]
      const res = await firstValueFrom(
        this.http.get<SectionSummary[]>(`${environment.apiUrl}/sections`)
      );

      this.sections = (res ?? []).map(s => ({
        code: String((s as any).code ?? ''),
        name: String((s as any).name ?? ''),
        totalStaff: Number((s as any).totalStaff ?? 0),
        inchargeEmployeeCode: (s as any).inchargeEmployeeCode ?? '',
        inchargeName: (s as any).inchargeName ?? ''
      }));

      if (!this.selectedSectionCode && this.sections.length) {
        this.selectedSectionCode = this.sections[0].code;
      }
      this.applySectionToForm(this.selectedSectionCode);
      this.message = { type: 'info', text: 'Section list loaded.' };
    } catch (e) {
      this.message = {
        type: 'error',
        text: 'Failed to load sections. Please check API endpoint `/sections`.'
      };
    } finally {
      this.isLoading = false;
    }
  }

  onSectionChange() {
    this.message = null;
    this.applySectionToForm(this.selectedSectionCode);
  }

  applySectionToForm(sectionCode: string) {
    const section = this.sections.find(s => s.code === sectionCode);
    if (!section) {
      this.sectionName = '';
      this.inchargeEmployeeCode = '';
      this.inchargeName = '';
      this.totalStaff = null;
      return;
    }

    this.sectionName = section.name ?? '';
    this.inchargeEmployeeCode = section.inchargeEmployeeCode ?? '';
    this.inchargeName = section.inchargeName ?? '';
    this.totalStaff = typeof section.totalStaff === 'number' ? section.totalStaff : Number(section.totalStaff ?? 0);

    if (this.inchargeEmployeeCode && !this.inchargeName) {
      this.inchargeCode$.next(this.inchargeEmployeeCode);
    }
  }

  onInchargeCodeInput(value: string) {
    this.inchargeEmployeeCode = value;
    this.inchargeCode$.next(value);
  }

  async lookupInchargeName(employeeCode: string) {
    try {
      // Expected API shape (adjust as per backend):
      // GET /employees/{code} -> { code, name }
      const res = await firstValueFrom(
        this.http.get<EmployeeLookup>(
          `${environment.apiUrl}/employees/${encodeURIComponent(employeeCode)}`
        )
      );

      this.inchargeName = String((res as any)?.name ?? '');
      if (!this.inchargeName) {
        this.message = { type: 'info', text: 'Employee code found but name is empty.' };
      }
    } catch {
      this.inchargeName = '';
      this.message = {
        type: 'error',
        text: 'Failed to fetch Incharge Name. Please check API endpoint `/employees/{code}`.'
      };
    }
  }

  clearForm() {
    this.message = null;
    this.applySectionToForm(this.selectedSectionCode);
  }

  async submit() {
    if (!this.selectedSectionCode) {
      this.message = { type: 'error', text: 'Please select a Section Code.' };
      return;
    }

    if (this.mode === 'delete') {
      const ok = confirm(`Delete section allocation for code "${this.selectedSectionCode}"?`);
      if (!ok) return;
      await this.deleteAllocation();
      return;
    }

    await this.modifyAllocation();
  }

  private async modifyAllocation() {
    if (!this.sectionName.trim()) {
      this.message = { type: 'error', text: 'Section Name is required.' };
      return;
    }
    if (!this.inchargeEmployeeCode.trim()) {
      this.message = { type: 'error', text: 'Incharge Employee Code is required.' };
      return;
    }
    if (this.totalStaff == null || Number.isNaN(this.totalStaff) || this.totalStaff < 0) {
      this.message = { type: 'error', text: 'Total Staff must be a valid number.' };
      return;
    }

    this.isSaving = true;
    this.message = null;
    try {
      // Expected API shape (adjust as per backend):
      // PUT /sections/{code}/allocation -> { sectionName, inchargeEmployeeCode, totalStaff }
      const payload = {
        sectionCode: this.selectedSectionCode,
        sectionName: this.sectionName.trim(),
        inchargeEmployeeCode: this.inchargeEmployeeCode.trim(),
        totalStaff: Number(this.totalStaff)
      };

      await firstValueFrom(
        this.http.put(
          `${environment.apiUrl}/sections/${encodeURIComponent(this.selectedSectionCode)}/allocation`,
          payload
        )
      );

      this.message = { type: 'success', text: 'Allocation updated successfully.' };
      await this.loadSections();
    } catch {
      this.message = {
        type: 'error',
        text: 'Failed to update allocation. Please check API endpoint `/sections/{code}/allocation`.'
      };
    } finally {
      this.isSaving = false;
    }
  }

  private async deleteAllocation() {
    this.isSaving = true;
    this.message = null;
    try {
      // Expected API shape (adjust as per backend):
      // DELETE /sections/{code}/allocation
      await firstValueFrom(
        this.http.delete(
          `${environment.apiUrl}/sections/${encodeURIComponent(this.selectedSectionCode)}/allocation`
        )
      );

      this.message = { type: 'success', text: 'Allocation deleted successfully.' };
      await this.loadSections();
    } catch {
      this.message = {
        type: 'error',
        text: 'Failed to delete allocation. Please check API endpoint `/sections/{code}/allocation`.'
      };
    } finally {
      this.isSaving = false;
    }
  }

  showHelp() {
    this.message = {
      type: 'info',
      text: 'Tip: Choose Modify to update incharge/staff. Choose Delete to remove allocation (confirmation required).'
    };
  }
}
