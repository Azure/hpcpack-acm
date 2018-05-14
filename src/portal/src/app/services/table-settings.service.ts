import { Injectable } from '@angular/core';
import { UserSettingsService } from './user-settings.service';

@Injectable()
export class TableSettingsService {

  constructor(
    public settings: UserSettingsService
  ) {}

  save(key, columns): void {
    let options = columns.filter(e => !e.displayed).map(e => e.name);
    let selected = columns.filter(e => e.displayed).map(e => e.name);
    this.settings.set(key, { options, selected });
    this.settings.save();
  }

  load(key, initColumns): void {
    let availableColumns = initColumns;
    let data = this.settings.get(key);
    let res;
    if (data) {
      let selected = data.selected.map(name => availableColumns.find(col => col.name === name));
      selected.forEach(e => e.displayed = true);
      let options = data.options.map(name => availableColumns.find(col => col.name === name));
      options.forEach(e => e.displayed = false);
      res = selected.concat(options);
    }
    else {
      res = availableColumns ;
    }
    return res;
  }
}
