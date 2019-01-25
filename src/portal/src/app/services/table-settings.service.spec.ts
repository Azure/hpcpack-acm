import { TableSettingsService } from './table-settings.service';

fdescribe('TableSettingsService', () => {
  let tableSettingsService: TableSettingsService;
  let userSettingsServiceSpy;

  beforeEach(() => {
    userSettingsServiceSpy = jasmine.createSpyObj('UserSettingsService', ['set', 'get', 'save']);
    userSettingsServiceSpy.set.and.returnValue();
    userSettingsServiceSpy.save.and.returnValue();
    userSettingsServiceSpy.get.and.returnValue(null);
    tableSettingsService = new TableSettingsService(userSettingsServiceSpy);
  });

  it('should be created', () => {
    expect(tableSettingsService).toBeTruthy();
  });

  it('#load should return all columns in initial state', () => {
    let res: Array<any>;
    res = tableSettingsService.load('testKey', [
      { name: 'state', displayed: true, displayName: 'State' },
      { name: 'os', displayed: true, displayName: 'OS' },
      { name: 'runningJobCount', displayed: true, displayName: 'Jobs' },
      { name: 'memory', displayed: true, displayName: 'Memory(MB)' },
    ]);
    expect(res.length).toBe(4);
  });
});
