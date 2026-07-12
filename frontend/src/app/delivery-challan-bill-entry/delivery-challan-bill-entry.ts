import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';

interface Item {
  id: number;
  name: string;
  category?: string;
  availableQuantity?: number;
  lastPrice?: number | null;
  price?: number | null;
}

interface BillItem {
  slNo: number;
  itemId: number;
  itemName: string;
  quantity: number;
  unitPrice: number;
  amount: number;
}

@Component({
  selector: 'app-delivery-challan-bill-entry',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './delivery-challan-bill-entry.html',
  styleUrls: ['./delivery-challan-bill-entry.css']
})
export class DeliveryChallanBillEntryComponent implements OnInit {
  form: FormGroup;
  items: Item[] = [];
  billItems: BillItem[] = [];
  suppliers: string[] = [];
  filteredItems: Item[] = [];
  selectedItem: Item | null = null;
  
  loading = signal(false);
  successMsg = signal('');
  errorMsg = signal('');
  isSubmitting = signal(false);
  
  showItemDropdown = signal(false);
  itemSearchQuery = signal('');
  
  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private router: Router
  ) {
    this.form = this.fb.group({
      challanDate: ['', Validators.required],
      challanNo: ['', [Validators.required, Validators.minLength(1)]],
      purchasedFrom: ['', Validators.required],
      quantity: ['', [Validators.required, Validators.min(1)]],
      unitPrice: ['', [Validators.required, Validators.min(0.01)]]
    });
  }

  ngOnInit() {
    this.loadItemsAndSuppliers();
  }

  loadItemsAndSuppliers() {
    this.loading.set(true);
    this.http.get<{ items: Item[]; vendors: string[] }>(`${environment.apiUrl}/bills/init`)
      .subscribe({
        next: (data) => {
          this.items = data.items || [];
          this.suppliers = data.vendors || [];
          this.filteredItems = this.items;
          this.loading.set(false);
        },
        error: (err) => {
          console.error('Error loading data:', err);
          this.errorMsg.set('Failed to load items and suppliers');
          this.loading.set(false);
        }
      });
  }

  onItemSearch(query: string) {
    this.itemSearchQuery.set(query);
    const trimmed = query.trim();
    if (trimmed === '') {
      this.filteredItems = this.items;
      return;
    }

    this.http.get<{ items: Item[] }>(`${environment.apiUrl}/bills/items/search`, {
      params: { query: trimmed }
    }).subscribe({
      next: (response) => {
        this.filteredItems = response.items ?? [];
      },
      error: () => {
        const q = trimmed.toLowerCase();
        this.filteredItems = this.items.filter(item => (item.name ?? '').toLowerCase().includes(q));
      }
    });
  }

  selectItem(item: Item) {
    this.selectedItem = item;
    this.itemSearchQuery.set(item.name);
    this.form.patchValue({ unitPrice: item.lastPrice ?? item.price ?? '' });
    this.showItemDropdown.set(false);
  }

  addItem() {
    if (!this.selectedItem) {
      this.errorMsg.set('Please select an item');
      setTimeout(() => this.errorMsg.set(''), 3000);
      return;
    }

    const quantity = this.form.get('quantity')?.value;
    const unitPrice = Number(this.form.get('unitPrice')?.value);

    if (!quantity || quantity < 1) {
      this.errorMsg.set('Please enter a valid quantity');
      setTimeout(() => this.errorMsg.set(''), 3000);
      return;
    }

    if (!unitPrice || unitPrice <= 0) {
      this.errorMsg.set('Please enter a valid unit price');
      setTimeout(() => this.errorMsg.set(''), 3000);
      return;
    }

    const newSlNo = this.billItems.length + 1;
    const amount = quantity * unitPrice;

    const billItem: BillItem = {
      slNo: newSlNo,
      itemId: this.selectedItem.id,
      itemName: this.selectedItem.name,
      quantity,
      unitPrice,
      amount
    };

    this.billItems.push(billItem);
    
    // Reset form
    this.selectedItem = null;
    this.itemSearchQuery.set('');
    this.form.get('quantity')?.reset('');
    this.form.get('unitPrice')?.reset('');
    this.showItemDropdown.set(false);
    
    this.successMsg.set('Item added successfully');
    setTimeout(() => this.successMsg.set(''), 2000);
  }

  removeItem(slNo: number) {
    this.billItems = this.billItems.filter(item => item.slNo !== slNo);
    this.reorderSlNo();
  }

  reorderSlNo() {
    this.billItems.forEach((item, index) => {
      item.slNo = index + 1;
    });
  }

  get grandTotal(): number {
    return this.billItems.reduce((sum, item) => sum + item.amount, 0);
  }

  get numberOfItems(): number {
    return this.billItems.length;
  }

  onSubmit() {
    if (!this.form.valid) {
      this.errorMsg.set('Please fill all required fields');
      setTimeout(() => this.errorMsg.set(''), 3000);
      return;
    }

    if (this.billItems.length === 0) {
      this.errorMsg.set('Please add at least one item to the bill');
      setTimeout(() => this.errorMsg.set(''), 3000);
      return;
    }

    this.isSubmitting.set(true);

    const payload = {
      billDate: this.form.get('challanDate')?.value,
      billNo: this.form.get('challanNo')?.value,
      vendorName: this.form.get('purchasedFrom')?.value,
      items: this.billItems.map(item => ({
        itemId: item.itemId,
        quantity: item.quantity,
        unitPrice: item.unitPrice
      }))
    };

    this.http.post(`${environment.apiUrl}/bills`, payload)
      .subscribe({
        next: () => {
          this.successMsg.set('Delivery challan/bill entry submitted successfully');
          setTimeout(() => {
            this.router.navigate(['/admin-dashboard']);
          }, 1500);
        },
        error: (err) => {
          console.error('Error submitting:', err);
          this.errorMsg.set(err.error?.message || 'Failed to submit challan/bill entry');
          this.isSubmitting.set(false);
          setTimeout(() => this.errorMsg.set(''), 3000);
        }
      });
  }

  onReset() {
    this.form.reset();
    this.form.patchValue({ unitPrice: '' });
    this.billItems = [];
    this.selectedItem = null;
    this.itemSearchQuery.set('');
    this.successMsg.set('');
    this.errorMsg.set('');
  }

  printChallan() {
    window.print();
  }

  goBack() {
    this.router.navigate(['/admin-dashboard']);
  }

  trackBySlNo(_: number, item: BillItem) {
    return item.slNo;
  }
}
