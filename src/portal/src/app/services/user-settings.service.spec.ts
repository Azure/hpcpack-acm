import { TestBed, inject } from '@angular/core/testing';

import { UserSettingsService } from './user-settings.service';

describe('UserSettingsService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [UserSettingsService]
    });
  });

  it('should be created', inject([UserSettingsService], (service: UserSettingsService) => {
    expect(service).toBeTruthy();
  }));
});
