import { TestBed, inject } from '@angular/core/testing';

import { TableService } from './table.service';

fdescribe('TableDataService', () => {
  let userSettingsServiceSpy;
  let tableService: TableService;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [TableService]
    });

    userSettingsServiceSpy = jasmine.createSpyObj('UserSettingsService', ['set', 'get', 'save']);
    userSettingsServiceSpy.set.and.returnValue();
    userSettingsServiceSpy.save.and.returnValue();
    userSettingsServiceSpy.get.and.returnValue(null);
    tableService = new TableService(userSettingsServiceSpy);
  });

  it('should be created',  () => {
    expect(tableService).toBeTruthy();
  });

  it('should add new data to dataSource',() => {
    let dataSource = { data: [{ id: '1' }] };
    let newData = [{ id: '2' }];
    tableService.updateDatasource(newData, dataSource, 'id');
    expect(dataSource.data).toContain({ id: '2' });
  });

  it('shoud update exsited data in dataSource', () => {
    let dataSource = { data: [{ id: '1', content: 'old data' }] };
    let newData = [{ id: '1', content: 'new data' }];
    tableService.updateDatasource(newData, dataSource, 'id');
    expect(dataSource.data).toContain({ id: '1', content: 'new data' });
  });

  it('shoud keep dataSource unchanged when newData is empty', () => {
    let dataSource = { data: [{ id: '1', content: 'old data' }] };
    let newData = [];
    tableService.updateDatasource(newData, dataSource, 'id');
    expect(dataSource.data.length).toEqual(1);
    expect(dataSource.data).toEqual([{ id: '1', content: 'old data' }]);
  });

  it('shoud keep dataSource unchanged when newData is not array', () => {
    let dataSource = { data: [{ id: '1', content: 'old data' }] };
    let newData = "test";
    tableService.updateDatasource(newData, dataSource, 'id');
    expect(dataSource.data.length).toEqual(1);
    expect(dataSource.data).toEqual([{ id: '1', content: 'old data' }]);
  });

  it('shoud keep dataSource unchanged when newData has no specified property', () => {
    let dataSource = { data: [{ id: '1', content: 'old data' }] };
    let newData = [{ content: 'new data' }];
    tableService.updateDatasource(newData, dataSource, 'id');
    expect(dataSource.data.length).toEqual(1);
    expect(dataSource.data).toEqual([{ id: '1', content: 'old data' }]);
  });

  it('#load should return all columns in initial state', () => {
    let res: Array<any>;
    res = tableService.loadSetting('testKey', [
      { name: 'state', displayed: true, displayName: 'State' },
      { name: 'os', displayed: true, displayName: 'OS' },
      { name: 'runningJobCount', displayed: true, displayName: 'Jobs' },
      { name: 'memory', displayed: true, displayName: 'Memory(MB)' },
    ]);
    expect(res.length).toBe(4);
  });

});
