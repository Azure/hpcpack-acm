import { TestBed, inject } from '@angular/core/testing';
import { DiagReportService } from './diag-report.service';
import { identifierModuleUrl } from '@angular/compiler';

fdescribe('DiagReportService', () => {
  let diagReportService: DiagReportService;
  beforeEach(() => {
    diagReportService = new DiagReportService();
  });

  it('should be created', () => {
    expect(diagReportService).toBeTruthy();
  });

  it('#jobFinished should return right result', () => {
    expect(diagReportService.jobFinished('Finished')).toBe(true);
    expect(diagReportService.jobFinished('Running')).toBe(false);
    expect(diagReportService.jobFinished(undefined)).toBe(false);
    expect(diagReportService.jobFinished(null)).toBe(false);
  });

  it('#hasResult should be true if result is not null or undefined', () => {
    expect(diagReportService.hasResult(null)).toBe(false);
    expect(diagReportService.hasResult(undefined)).toBe(false);
    expect(diagReportService.hasResult('')).toBe(true);
    expect(diagReportService.hasResult({})).toBe(true);
  })

  it('#hasError should determine if result has error', () => {
    expect(diagReportService.hasError(null)).toBe(false);
    expect(diagReportService.hasError(undefined)).toBe(false);
    let result = { Error: 'test' };
    expect(diagReportService.hasError(result)).toBe(true);
    let emptyResult = {};
    expect(diagReportService.hasError(emptyResult)).toBe(false);
  })

  it('#getErrorMsg should return error message', () => {
    let apiServiceSpy = jasmine.createSpyObj('ApiService', ['isJSON']);
    apiServiceSpy.isJSON.and.returnValue(true);
    let errorMsg = diagReportService.getErrorMsg({ error: 'test' });
    expect(errorMsg.Error).toBe('test');
  })
});
