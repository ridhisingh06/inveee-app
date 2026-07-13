import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-reorder-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="modal-backdrop" *ngIf="isOpen" (click)="close.emit()">
      <div class="modal-content" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <h2>Reorder Suggestions</h2>
          <button class="close-btn" (click)="close.emit()">&times;</button>
        </div>
        
        <div class="modal-body">
          <p class="subtitle">The following items from your request were rejected due to stock limitations. You can reorder them in a new request.</p>
          
          <div *ngIf="loading" class="loading-state">
            Loading suggestions...
          </div>
          
          <div *ngIf="!loading && suggestions.length === 0" class="empty-state">
            No reorder suggestions available.
          </div>
          
          <ul class="suggestion-list" *ngIf="!loading && suggestions.length > 0">
            <li *ngFor="let item of suggestions" class="suggestion-item">
              <span class="item-name">{{ item.itemName }}</span>
              <span class="qty-chip">Rejected: <strong>{{ item.suggestedQuantity }}</strong></span>
            </li>
          </ul>
        </div>
        
        <div class="modal-footer">
          <button class="btn-cancel" (click)="close.emit()">Close</button>
          <button class="btn-primary" (click)="reorder.emit(suggestions)" [disabled]="loading || suggestions.length === 0">Add to Cart</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .modal-backdrop {
      position: fixed;
      top: 0; left: 0; right: 0; bottom: 0;
      background: rgba(0,0,0,0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
    }
    .modal-content {
      background: white;
      border-radius: 8px;
      width: 90%;
      max-width: 500px;
      box-shadow: 0 4px 20px rgba(0,0,0,0.15);
      animation: slideUp 0.3s ease;
    }
    .modal-header {
      padding: 16px 20px;
      border-bottom: 1px solid #e2e8f0;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .modal-header h2 {
      margin: 0;
      font-size: 1.25rem;
      color: #1e293b;
    }
    .close-btn {
      background: none;
      border: none;
      font-size: 1.5rem;
      cursor: pointer;
      color: #64748b;
    }
    .modal-body {
      padding: 20px;
    }
    .subtitle {
      color: #64748b;
      font-size: 0.9rem;
      margin-bottom: 16px;
    }
    .suggestion-list {
      list-style: none;
      padding: 0;
      margin: 0;
      border: 1px solid #e2e8f0;
      border-radius: 6px;
    }
    .suggestion-item {
      display: flex;
      justify-content: space-between;
      padding: 12px 16px;
      border-bottom: 1px solid #e2e8f0;
    }
    .suggestion-item:last-child {
      border-bottom: none;
    }
    .qty-chip {
      background: #fee2e2;
      color: #b91c1c;
      padding: 2px 8px;
      border-radius: 4px;
      font-size: 0.85rem;
    }
    .modal-footer {
      padding: 16px 20px;
      border-top: 1px solid #e2e8f0;
      display: flex;
      justify-content: flex-end;
      gap: 12px;
    }
    .btn-cancel {
      padding: 8px 16px;
      border: 1px solid #cbd5e1;
      background: white;
      border-radius: 6px;
      cursor: pointer;
    }
    .btn-primary {
      padding: 8px 16px;
      background: #2563eb;
      color: white;
      border: none;
      border-radius: 6px;
      cursor: pointer;
    }
    .btn-primary:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }
    @keyframes slideUp {
      from { transform: translateY(20px); opacity: 0; }
      to { transform: translateY(0); opacity: 1; }
    }
  `]
})
export class ReorderModalComponent {
  @Input() isOpen = false;
  @Input() suggestions: any[] = [];
  @Input() loading = false;
  @Output() close = new EventEmitter<void>();
  @Output() reorder = new EventEmitter<any[]>();
}
