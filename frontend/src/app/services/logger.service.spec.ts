import { TestBed } from '@angular/core/testing';
import { LoggerService } from './logger.service';
import { environment } from '../../environments/environment';
import { vi } from 'vitest';

describe('LoggerService', () => {
  let service: LoggerService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LoggerService);
    
    vi.spyOn(console, 'debug').mockImplementation(() => {});
    vi.spyOn(console, 'log').mockImplementation(() => {});
    vi.spyOn(console, 'warn').mockImplementation(() => {});
    vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should log warn and error messages always', () => {
    service.warn('TestContext', 'test warn');
    expect(console.warn).toHaveBeenCalled();
    
    service.error('TestContext', 'test error');
    expect(console.error).toHaveBeenCalled();
  });

  describe('in development', () => {
    beforeEach(() => {
        // Environment is non-prod by default in tests
        expect(environment.production).toBeFalsy();
    });

    it('should log debug and log messages', () => {
      service.debug('TestContext', 'test debug');
      expect(console.debug).toHaveBeenCalled();
      
      service.log('TestContext', 'test log');
      expect(console.log).toHaveBeenCalled();
    });
  });

  describe('in production', () => {
    let originalIsProd: any;

    beforeEach(() => {
       originalIsProd = (service as any).isProd;
       (service as any).isProd = true;
    });

    afterEach(() => {
        (service as any).isProd = originalIsProd;
    });

    it('should suppress debug and log messages in prod', () => {
      service.debug('TestContext', 'test debug');
      expect(console.debug).not.toHaveBeenCalled();
      
      service.log('TestContext', 'test log');
      expect(console.log).not.toHaveBeenCalled();
    });
    
    it('should still log warn and error messages in prod', () => {
      service.warn('TestContext', 'test warn');
      expect(console.warn).toHaveBeenCalled();
      
      service.error('TestContext', 'test error');
      expect(console.error).toHaveBeenCalled();
    });
  });
});
