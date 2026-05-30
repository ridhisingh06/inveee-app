import { TestBed } from '@angular/core/testing';
import { CartService, CartLine } from './cart.service';
import { Item } from '../models/item';
import { vi } from 'vitest';
import { LoggerService } from './logger.service';

describe('CartService', () => {
  let service: CartService;
  let mockLoggerService: any;
  let mockLocalStorage: any;

  beforeEach(() => {
    mockLoggerService = {
      log: vi.fn(),
      warn: vi.fn(),
      error: vi.fn(),
      debug: vi.fn(),
    };

    mockLocalStorage = {
      getItem: vi.fn(),
      setItem: vi.fn(),
      removeItem: vi.fn(),
      clear: vi.fn(),
    };

    vi.stubGlobal('localStorage', mockLocalStorage);

    TestBed.configureTestingModule({
        providers: [
            { provide: LoggerService, useValue: mockLoggerService }
        ]
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  describe('with empty initial storage', () => {
    beforeEach(() => {
        mockLocalStorage.getItem.mockReturnValue(null);
        service = TestBed.inject(CartService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should initialize with empty cart', () => {
      expect(service.getLinesSnapshot()).toEqual([]);
      expect(service.getItemCountSnapshot()).toBe(0);
    });

    it('should add item', () => {
      const item: Item = { id: 1, name: 'Item 1', category: 'Cat' };
      service.addItem(item, 2);

      const snapshot = service.getLinesSnapshot();
      expect(snapshot.length).toBe(1);
      expect(snapshot[0].item).toEqual(item);
      expect(snapshot[0].qty).toBe(2);
      expect(mockLocalStorage.setItem).toHaveBeenCalled();
      expect(mockLoggerService.log).toHaveBeenCalled();
    });

    it('should merge quantities for same item', () => {
      const item: Item = { id: 1, name: 'Item 1', category: 'Cat' };
      service.addItem(item, 2);
      service.addItem(item, 3);

      const snapshot = service.getLinesSnapshot();
      expect(snapshot.length).toBe(1);
      expect(snapshot[0].qty).toBe(5);
    });

    it('should clamp qty to 1 if <= 0 on add', () => {
      const item: Item = { id: 1, name: 'Item 1', category: 'Cat' };
      service.addItem(item, 0);
      expect(service.getLinesSnapshot()[0].qty).toBe(1);
      
      service.addItem(item, -5);
      expect(service.getLinesSnapshot()[0].qty).toBe(2);
    });

    it('should update quantity', () => {
      const item: Item = { id: 1, name: 'Item 1', category: 'Cat' };
      service.addItem(item, 2);
      service.updateQuantity(1, 5);

      expect(service.getLinesSnapshot()[0].qty).toBe(5);
    });

    it('should remove item when quantity updated to <= 0', () => {
      const item: Item = { id: 1, name: 'Item 1', category: 'Cat' };
      service.addItem(item, 2);
      service.updateQuantity(1, 0);

      expect(service.getLinesSnapshot().length).toBe(0);
    });

    it('should remove item', () => {
      const item: Item = { id: 1, name: 'Item 1', category: 'Cat' };
      service.addItem(item, 2);
      service.removeItem(1);

      expect(service.getLinesSnapshot().length).toBe(0);
    });

    it('should clear cart', () => {
      const item: Item = { id: 1, name: 'Item 1', category: 'Cat' };
      service.addItem(item, 2);
      service.clear();

      expect(service.getLinesSnapshot().length).toBe(0);
      expect(mockLocalStorage.setItem).toHaveBeenCalledWith('cart_v1', '[]');
    });
  });

  describe('with existing data in storage', () => {
    it('should load cart from storage', () => {
        const mockData: CartLine[] = [
            { item: { id: 1, name: 'Stored Item', category: 'Cat' }, qty: 3 }
        ];
        mockLocalStorage.getItem.mockReturnValue(JSON.stringify(mockData));

        service = TestBed.inject(CartService);

        expect(service.getLinesSnapshot().length).toBe(1);
        expect(service.getLinesSnapshot()[0].qty).toBe(3);
    });
    
    it('should handle invalid storage data', () => {
        mockLocalStorage.getItem.mockReturnValue('invalid json');
        service = TestBed.inject(CartService);
        expect(service.getLinesSnapshot().length).toBe(0);
    });
  });
});
