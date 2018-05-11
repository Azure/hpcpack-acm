import { TestBed, inject } from '@angular/core/testing';

import { TableSettingsService } from './table-settings.service';

describe('TableSettingsService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [TableSettingsService]
    });
  });

  it('should be created', inject([TableSettingsService], (service: TableSettingsService) => {
    expect(service).toBeTruthy();
  }));
});
