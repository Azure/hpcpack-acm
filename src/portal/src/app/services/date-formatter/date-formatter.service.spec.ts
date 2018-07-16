import { TestBed, inject } from '@angular/core/testing';

import { DateFormatterService } from './date-formatter.service';

fdescribe('DateFormatterService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [DateFormatterService]
    });
  });

  it('should be created', inject([DateFormatterService], (service: DateFormatterService) => {
    expect(service).toBeTruthy();
  }));

  it('should padding zero when val is less than 10', inject([DateFormatterService], (service: DateFormatterService) => {
    expect(service.paddingZero(9)).toEqual('09');
  }));

  it('should return formatted date string with given date', inject([DateFormatterService], (service: DateFormatterService) => {
    let testDate = new Date('May 25, 2018 09:24:00');
    let formatRes = service.dateString(testDate);
    expect(formatRes).toEqual('2018-05-25');
  }));

  it('should return fomatted time string with given date', inject([DateFormatterService], (service: DateFormatterService) => {
    let testDate = new Date('May 25, 2018 09:24:00');
    let formatRes = service.timeString(testDate);
    expect(formatRes).toEqual('09:24:00');
  }));

  it('should return date time string with given date', inject([DateFormatterService], (service: DateFormatterService) => {
    let testDate = new Date('May 25, 2018 09:24:00');
    let formatRes = service.dateTime(testDate);
    expect(formatRes).toEqual('2018-05-25 09:24:00');
  }));
});
