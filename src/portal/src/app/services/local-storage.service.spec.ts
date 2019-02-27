import { TestBed, inject } from '@angular/core/testing';

import { LocalStorageService } from './local-storage.service';

fdescribe('LocalStorageService', () => {
  let localStorageService: LocalStorageService;
  beforeEach(() => {
    localStorageService = new LocalStorageService();
  });

  it('should be created', () => {
    expect(localStorageService).toBeTruthy();
  });
});
