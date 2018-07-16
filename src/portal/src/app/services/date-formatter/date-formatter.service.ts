import { Injectable } from '@angular/core';
import { getLocaleDateFormat } from '@angular/common';

@Injectable()
export class DateFormatterService {

  constructor() { }

  paddingZero(val) {
    return val >= 10 ? val.toString() : `0${val}`;
  }

  dateTime(date) {
    return `${this.dateString(date)} ${this.timeString(date)}`;
  }

  dateString(date) {
    return `${date.getFullYear()}-${this.paddingZero(date.getMonth() + 1)}-${this.paddingZero(date.getDate())}`;
  }

  timeString(date) {
    return `${this.paddingZero(date.getHours())}:${this.paddingZero(date.getMinutes())}:${this.paddingZero(date.getSeconds())}`
  }
}
