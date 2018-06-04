import { Injectable } from '@angular/core';
import { UserSettingsService } from './user-settings.service';

@Injectable()
export class TableSettingsService {

  constructor(
    public settings: UserSettingsService
  ) {}

  save(key, columns): void {
    let selected = columns.filter(e => e.displayed).map(e => e.name);
    this.settings.set(key, { selected });
    this.settings.save();
  }

  load(key, initColumns): void {
    let availableColumns = initColumns;
    let data = this.settings.get(key);
    let res;
    if (data) {
      //A column name may change between saving and loading, and thus the map
      //result array may have null element.
      let selected = data.selected
        .map(name => availableColumns.find(col => col.name === name))
        .filter(col => col);
      selected.forEach(e => e.displayed = true);

      //Non-selected options don't need ordering.
      let options = availableColumns.filter(col => !data.selected.includes(col.name));
      options.forEach(e => e.displayed = false);
      res = selected.concat(options);
    }
    else {
      res = availableColumns ;
    }
    return res;
  }
}
