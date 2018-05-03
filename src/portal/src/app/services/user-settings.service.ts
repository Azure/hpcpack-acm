import { Injectable } from '@angular/core';
import { LocalStorageService } from './local-storage.service';

@Injectable()
export class UserSettingsService {
  static key = 'user-settings';

  private data;

  constructor(
    public storage: LocalStorageService
  ) {
    let value = storage.getItem(UserSettingsService.key);
    if (value) {
      this.data = JSON.parse(value);
    }
  }

  save(): void {
    if (this.data) {
      this.storage.setItem(UserSettingsService.key, JSON.stringify(this.data));
    }
  }

  get(key: string): any {
    return this.data ? this.data[key] : null;
  }

  set(key: string, value: any): void {
    if (!this.data) {
      this.data = {};
    }
    (this.data as any)[key] = value;
  }

}
