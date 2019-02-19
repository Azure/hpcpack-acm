import { Injectable } from '@angular/core';
import { isArray } from 'util';
import { UserSettingsService } from '../user-settings.service';

@Injectable()
export class TableService {

  constructor(
    public settings: UserSettingsService
  ) { }

  hashCode(val) {
    let s = val.toString();
    let hash = 0, i, chr;
    if (s.length === 0) {
      return hash;
    }
    for (let i = 0; i < s.length; i++) {
      chr = s.charAt(i);
      hash = ((hash << 5) - hash) + chr;
      hash |= 0;
    }
    return hash;
  }

  trackByFn(item, displayedCols) {
    let identity = "";
    for (let i = 0; i < displayedCols.length; i++) {
      identity += item[displayedCols[i]];
    }
    return this.hashCode(identity);
  }

  isContentScrolled(tableEle) {
    let clientHeight = tableEle.clientHeight;
    let scrolledHeight = tableEle.scrollHeight;
    if (clientHeight < scrolledHeight) {
      return true;
    }
    return false;
  }

  updateDatasource(newData, dataSource, propertyName) {
    if (!isArray(newData)) {
      return;
    }
    let data = dataSource.data;
    let length = newData.length;
    if (length == 0) {
      return;
    }
    let legalData = newData.every(element => {
      return element[propertyName] !== undefined;
    });
    if (!legalData) {
      return;
    }
    let firstPropVal = newData[0][propertyName];
    let lastPropVal = newData[length - 1][propertyName];
    let firstIndex = data.findIndex(item => {
      return item[propertyName] == firstPropVal;
    });
    let lastIndex = data.findIndex(item => {
      return item[propertyName] == lastPropVal;
    });
    let startPart = [];
    let endPart = [];
    if (firstIndex != -1) {
      startPart = data.slice(0, firstIndex);
    }
    if (lastIndex != -1) {
      endPart = data.slice(lastIndex + 1);
    }
    if (firstIndex == -1 && lastIndex == -1) {
      data = data.concat(newData);
    }
    else {
      data = startPart.concat(newData).concat(endPart);
    }
    dataSource.data = data;
  }

  updateData(newData, data, propertyName) {
    if (!isArray(newData)) {
      return;
    }
    let length = newData.length;
    if (length == 0) {
      return;
    }
    let legalData = newData.every(element => {
      return element[propertyName] !== undefined;
    });
    if (!legalData) {
      return;
    }
    let firstPropVal = newData[0][propertyName];
    let lastPropVal = newData[length - 1][propertyName];
    let firstIndex = data.findIndex(item => {
      return item[propertyName] == firstPropVal;
    });
    let lastIndex = data.findIndex(item => {
      return item[propertyName] == lastPropVal;
    });
    let startPart = [];
    let endPart = [];
    if (firstIndex != -1) {
      startPart = data.slice(0, firstIndex);
    }
    if (lastIndex != -1) {
      endPart = data.slice(lastIndex + 1);
    }
    if (firstIndex == -1 && lastIndex == -1) {
      data = data.concat(newData);
    }
    else {
      data = startPart.concat(newData).concat(endPart);
    }
    return data;
  }

  saveSetting(key, columns): void {
    let selected = columns.filter(e => e.displayed).map(e => e.name);
    this.settings.set(key, { selected });
    this.settings.save();
  }

  loadSetting(key, initColumns): Array<any> {
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
      res = availableColumns;
    }
    return res;
  }
}
