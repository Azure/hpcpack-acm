import { TestBed, inject } from '@angular/core/testing';

import { UserSettingsService } from './user-settings.service';

fdescribe('UserSettingsService', () => {
  let userSettings: UserSettingsService;
  let storageSpy;
  beforeEach(() => {
    storageSpy = jasmine.createSpyObj('LocalStorageService', ['setItem', 'getItem']);
    storageSpy.setItem.and.returnValue();
    storageSpy.getItem.and.returnValue();
    userSettings = new UserSettingsService(storageSpy);
  });

  it('should be created', () => {
    expect(userSettings).toBeTruthy();
  });


});
