import { TestBed, inject } from '@angular/core/testing';

import { DiagReportService } from './diag-report.service';

describe('DiagReportService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [DiagReportService]
    });
  });

  it('should be created', inject([DiagReportService], (service: DiagReportService) => {
    expect(service).toBeTruthy();
  }));
});
