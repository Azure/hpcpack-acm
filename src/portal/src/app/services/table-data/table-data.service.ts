import { Injectable } from '@angular/core';
import { isArray } from 'util';

@Injectable()
export class TableDataService {

  constructor() { }

  updateData(newData, dataSource, propertyName) {
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
    let lastPropVal = newData[newData.length - 1][propertyName];
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

}
