import { TestBed } from '@angular/core/testing';
import { AuthService } from './service';
import { LoggerService } from '../../services/logger.service';
import { vi } from 'vitest';

describe('AuthService', () => {
  let service: AuthService;
  let mockLogger: any;
  let mockLocalStorage: any;

  // Helper to create a fake JWT token
  const createToken = (payload: any) => {
    const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
    const body = btoa(JSON.stringify(payload));
    const sig = 'signature';
    return `${header}.${body}.${sig}`;
  };

  beforeEach(() => {
    mockLogger = {
      log: vi.fn(),
      warn: vi.fn(),
      error: vi.fn(),
      debug: vi.fn()
    };

    mockLocalStorage = {
      getItem: vi.fn(),
      setItem: vi.fn(),
      removeItem: vi.fn()
    };
    vi.stubGlobal('localStorage', mockLocalStorage);

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        { provide: LoggerService, useValue: mockLogger }
      ]
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  describe('with empty initial storage', () => {
    beforeEach(() => {
      mockLocalStorage.getItem.mockReturnValue(null);
      service = TestBed.inject(AuthService);
    });

    it('should be created', () => {
      expect(service).toBeTruthy();
    });

    it('should set token and extract role', () => {
      const token = createToken({ role: 'ADMIN' });
      service.setToken(token);

      expect(mockLocalStorage.setItem).toHaveBeenCalledWith('token', token);
      expect(service.getToken()).toBe(token);
      expect(service.getRole()).toBe('ADMIN');
      expect(service.isAdmin()).toBe(true);
      expect(service.isUser()).toBe(false);
      expect(service.isIssuer()).toBe(false);
      expect(service.isLoggedIn()).toBe(true);
      expect(mockLogger.log).toHaveBeenCalled();
    });

    it('should extract MS claims role if standard role is missing', () => {
      const token = createToken({ 
          'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'USER' 
      });
      service.setToken(token);

      expect(service.getRole()).toBe('USER');
      expect(service.isUser()).toBe(true);
    });

    it('should handle invalid token smoothly', () => {
      service.setToken('invalid-token');
      expect(service.getRole()).toBeNull();
      expect(mockLogger.warn).toHaveBeenCalled();
    });

    it('should logout and clear state', () => {
      const token = createToken({ role: 'ADMIN' });
      service.setToken(token);
      expect(service.isLoggedIn()).toBe(true);

      service.logout();

      expect(mockLocalStorage.removeItem).toHaveBeenCalledWith('token');
      expect(mockLocalStorage.removeItem).toHaveBeenCalledWith('role');
      expect(service.getToken()).toBeNull();
      expect(service.getRole()).toBeNull();
      expect(service.isLoggedIn()).toBe(false);
    });

    it('should return true for isTokenExpired if no token', () => {
        expect(service.isTokenExpired()).toBe(true);
    });

    it('should return true for isTokenExpired if token has no exp', () => {
        const token = createToken({ role: 'ADMIN' });
        service.setToken(token);
        expect(service.isTokenExpired()).toBe(true);
        expect(mockLogger.warn).toHaveBeenCalled();
    });
    
    it('should evaluate isTokenExpired correctly with valid exp', () => {
        // Expired 1 hour ago
        const expPast = Math.floor(Date.now() / 1000) - 3600;
        const expiredToken = createToken({ role: 'ADMIN', exp: expPast });
        service.setToken(expiredToken);
        expect(service.isTokenExpired()).toBe(true);

        // Expires in 1 hour
        const expFuture = Math.floor(Date.now() / 1000) + 3600;
        const validToken = createToken({ role: 'ADMIN', exp: expFuture });
        service.setToken(validToken);
        expect(service.isTokenExpired()).toBe(false);
    });
  });

  describe('with existing data in storage', () => {
      it('should initialize with token and role from storage', () => {
          const token = createToken({ role: 'ISSUER' });
          mockLocalStorage.getItem.mockReturnValue(token);
          
          service = TestBed.inject(AuthService);
          
          expect(service.getToken()).toBe(token);
          expect(service.getRole()).toBe('ISSUER');
          expect(service.isIssuer()).toBe(true);
      });
  });
});
