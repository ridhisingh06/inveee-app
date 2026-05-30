import { CommonModule } from '@angular/common';
import { HttpClient, HttpClientModule, HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-personnel-details-new-entry',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, HttpClientModule],
  templateUrl: './personnel-details-new-entry.html',
  styleUrls: ['./personnel-details-new-entry.css']
})
export class PersonnelDetailsNewEntryComponent implements OnInit {
  form!: FormGroup;

  // Toast state
  toast = signal<{ visible: boolean; message: string; type: 'success' | 'error' }>({
    visible: false,
    message: '',
    type: 'success'
  });

  // Photo preview
  photoPreviewUrl = signal<string | null>(null);

  // Submission state
  isSubmitting = signal(false);

  // Edit state
  isEditMode = signal(false);
  personnelId: number | null = null;

  readonly departments = [
    'Administration',
    'Finance',
    'Human Resources',
    'Information Technology',
    'Logistics',
    'Operations',
    'Procurement',
    'Quality Assurance',
    'Stores – Section A',
    'Stores – Section B',
    'Stores – Section C',
    'Stores – Section D',
    'Stores – Incharge Unit',
    'Warehouse Management'
  ];

  today = new Date().toISOString().split('T')[0];

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      // ── Personal Details ──────────────────────────────────────────────
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      icNumber: ['', [Validators.required, Validators.pattern(/^\d{6}-\d{2}-\d{4}$/)]],
      birthDate: ['', [Validators.required, this.pastDateValidator]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(150)]],
      residentialAddress: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(300)]],
      residentialPhone: ['', [Validators.required, Validators.pattern(/^[0-9+\-\s()]{7,20}$/)]],
      officePhone: ['', [Validators.pattern(/^[0-9+\-\s()]{7,20}$/)]],

      // ── Employment Details ────────────────────────────────────────────
      designation: ['', [Validators.required, Validators.maxLength(100)]],
      jobDescription: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(500)]],
      department: ['', Validators.required],
      isStoresIncharge: ['no', Validators.required],
      building: ['', [Validators.required, Validators.maxLength(100)]],
      reportingOfficer: ['', [Validators.required, Validators.maxLength(100)]],

      // ── ID Card Details ───────────────────────────────────────────────
      idCardNumber: ['', [Validators.required, Validators.pattern(/^[A-Z0-9\-]{4,20}$/i)]],
      idCardExpiryDate: ['', [Validators.required, this.futureDateValidator]],
      photo: [null, Validators.required]
    });

    this.route.paramMap.subscribe((params) => {
      const idParam = params.get('id');
      if (idParam) {
        const id = Number(idParam);
        if (!Number.isNaN(id) && id > 0) {
          this.isEditMode.set(true);
          this.personnelId = id;
          this.loadPersonnelRecord(id);
          this.form.get('photo')?.clearValidators();
          this.form.get('photo')?.updateValueAndValidity();
          return;
        }
      }

      this.isEditMode.set(false);
      this.personnelId = null;
      this.photoPreviewUrl.set(null);
      const photoControl = this.form.get('photo');
      photoControl?.setValidators(Validators.required);
      photoControl?.updateValueAndValidity();
    });
  }

  // ─── Custom Validators ─────────────────────────────────────────────────────

  pastDateValidator(control: AbstractControl) {
    if (!control.value) return null;
    const val = new Date(control.value);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return val < today ? null : { notPastDate: true };
  }

  futureDateValidator(control: AbstractControl) {
    if (!control.value) return null;
    const val = new Date(control.value);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return val > today ? null : { notFutureDate: true };
  }

  // ─── Convenience getter ────────────────────────────────────────────────────

  c(name: string): AbstractControl {
    return this.form.get(name)!;
  }

  hasError(name: string): boolean {
    const ctrl = this.c(name);
    return ctrl.invalid && (ctrl.dirty || ctrl.touched);
  }

  // ─── Photo Upload ──────────────────────────────────────────────────────────

  onPhotoChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
      this.form.get('photo')!.setValue(null);
      this.photoPreviewUrl.set(null);
      return;
    }

    // Validate file type
    if (!['image/jpeg', 'image/jpg'].includes(file.type)) {
      this.form.get('photo')!.setErrors({ invalidType: true });
      this.form.get('photo')!.markAsTouched();
      this.photoPreviewUrl.set(null);
      return;
    }

    // Validate file size (max 2MB)
    if (file.size > 2 * 1024 * 1024) {
      this.form.get('photo')!.setErrors({ fileTooLarge: true });
      this.form.get('photo')!.markAsTouched();
      this.photoPreviewUrl.set(null);
      return;
    }

    this.form.get('photo')!.setValue(file);
    this.form.get('photo')!.updateValueAndValidity();

    const reader = new FileReader();
    reader.onload = (e) => this.photoPreviewUrl.set(e.target?.result as string);
    reader.readAsDataURL(file);
  }

  removePhoto(): void {
    this.form.get('photo')!.setValue(null);
    this.photoPreviewUrl.set(null);
  }

  // ─── Form Actions ──────────────────────────────────────────────────────────

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.showToast('Please fix the errors before submitting.', 'error');
      return;
    }

    this.isSubmitting.set(true);

    const formData = this.buildFormData();
    const request$ = this.isEditMode() && this.personnelId
      ? this.http.put<{ message: string; data?: unknown }>(
          `${environment.apiUrl}/personnel/${this.personnelId}`,
          formData
        )
      : this.http.post<{ message: string; data?: unknown }>(
          `${environment.apiUrl}/personnel`,
          formData
        );

    request$.subscribe({
      next: (response) => {
        const successMessage = this.isEditMode() ? 'Personnel record updated successfully!' : 'Personnel record saved successfully!';
        this.showToast(response.message || successMessage, 'success');
        this.isSubmitting.set(false);
        if (!this.isEditMode()) {
          this.form.reset({ isStoresIncharge: 'no' });
          this.photoPreviewUrl.set(null);
        }
        setTimeout(() => this.router.navigate(['/personnel-management', 'personnel-details-modify-delete']), 1200);
      },
      error: (err: HttpErrorResponse) => {
        console.error('Personnel submit error:', err);
        const message = err.error?.message || 'Unable to save personnel record. Please try again.';
        this.isSubmitting.set(false);
        this.showToast(message, 'error');
      }
    });
  }

  private buildFormData(): FormData {
    const formData = new FormData();
    formData.append('Name', this.c('name').value.trim());
    formData.append('ICNumber', this.c('icNumber').value.trim());
    formData.append('BirthDate', this.c('birthDate').value);
    formData.append('Email', this.c('email').value.trim());
    formData.append('ResidentialAddress', this.c('residentialAddress').value.trim());
    formData.append('ResidentialPhone', this.c('residentialPhone').value.trim());
    formData.append('OfficePhone', this.c('officePhone').value?.trim() ?? '');
    formData.append('Designation', this.c('designation').value.trim());
    formData.append('JobDescription', this.c('jobDescription').value.trim());
    formData.append('Department', this.c('department').value);
    formData.append('IsStoresIncharge', this.c('isStoresIncharge').value === 'yes' ? 'true' : 'false');
    formData.append('Building', this.c('building').value.trim());
    formData.append('ReportingOfficer', this.c('reportingOfficer').value.trim());
    formData.append('IdCardNumber', this.c('idCardNumber').value.trim());
    formData.append('IdCardExpiryDate', this.c('idCardExpiryDate').value);

    const photo = this.c('photo').value;
    if (photo) {
      formData.append('photo', photo);
    }

    return formData;
  }

  private loadPersonnelRecord(id: number): void {
    this.isSubmitting.set(true);
    this.http.get<any>(`${environment.apiUrl}/personnel/${id}`)
      .subscribe({
        next: (person) => {
          this.form.patchValue({
            name: person.name ?? '',
            icNumber: person.icNumber ?? '',
            birthDate: person.birthDate ?? '',
            email: person.email ?? '',
            residentialAddress: person.residentialAddress ?? '',
            residentialPhone: person.residentialPhone ?? '',
            officePhone: person.officePhone ?? '',
            designation: person.designation ?? '',
            jobDescription: person.jobDescription ?? '',
            department: person.department ?? '',
            isStoresIncharge: person.isStoresIncharge ? 'yes' : 'no',
            building: person.building ?? '',
            reportingOfficer: person.reportingOfficer ?? '',
            idCardNumber: person.idCardNumber ?? '',
            idCardExpiryDate: person.idCardExpiryDate ?? ''
          });
          this.photoPreviewUrl.set(person.photoUrl ?? null);
          this.isSubmitting.set(false);
        },
        error: (err: HttpErrorResponse) => {
          console.error('Failed to load personnel record:', err);
          this.showToast('Unable to load personnel record for edit.', 'error');
          this.isSubmitting.set(false);
        }
      });
  }

  onReset(): void {
    this.form.reset({ isStoresIncharge: 'no' });
    this.photoPreviewUrl.set(null);
    this.form.markAsUntouched();
    this.form.markAsPristine();
  }

  goBack(): void {
    this.router.navigate(['/personnel-management']);
  }

  // ─── Toast ─────────────────────────────────────────────────────────────────

  private showToast(message: string, type: 'success' | 'error'): void {
    this.toast.set({ visible: true, message, type });
    setTimeout(() => this.toast.set({ visible: false, message: '', type: 'success' }), 4000);
  }
}
