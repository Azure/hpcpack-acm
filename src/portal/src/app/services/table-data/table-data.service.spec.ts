import { TestBed, inject } from '@angular/core/testing';

import { TableDataService } from './table-data.service';

fdescribe('TableDataService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [TableDataService]
    });
  });

  it('should be created', inject([TableDataService], (service: TableDataService) => {
    expect(service).toBeTruthy();
  }));

  it('should add new data to dataSource', inject([TableDataService], (service: TableDataService) => {
    let dataSource = { data: [{ id: '1' }] };
    let newData = [{ id: '2' }];
    service.updateDatasource(newData, dataSource, 'id');
    expect(dataSource.data).toContain({ id: '2' });
  }));

  it('shoud update exsited data in dataSource', inject([TableDataService], (service: TableDataService) => {
    let dataSource = { data: [{ id: '1', content: 'old data' }] };
    let newData = [{ id: '1', content: 'new data' }];
    service.updateDatasource(newData, dataSource, 'id');
    expect(dataSource.data).toContain({ id: '1', content: 'new data' });
  }));

  it('shoud keep dataSource unchanged when newData is empty', inject([TableDataService], (service: TableDataService) => {
    let dataSource = { data: [{ id: '1', content: 'old data' }] };
    let newData = [];
    service.updateDatasource(newData, dataSource, 'id');
    expect(dataSource.data.length).toEqual(1);
    expect(dataSource.data).toEqual([{ id: '1', content: 'old data' }]);
  }));

  it('shoud keep dataSource unchanged when newData is not array', inject([TableDataService], (service: TableDataService) => {
    let dataSource = { data: [{ id: '1', content: 'old data' }] };
    let newData = "test";
    service.updateDatasource(newData, dataSource, 'id');
    expect(dataSource.data.length).toEqual(1);
    expect(dataSource.data).toEqual([{ id: '1', content: 'old data' }]);
  }));

  it('shoud keep dataSource unchanged when newData has no specified property', inject([TableDataService], (service: TableDataService) => {
    let dataSource = { data: [{ id: '1', content: 'old data' }] };
    let newData = [{ content: 'new data' }];
    service.updateDatasource(newData, dataSource, 'id');
    expect(dataSource.data.length).toEqual(1);
    expect(dataSource.data).toEqual([{ id: '1', content: 'old data' }]);
  }));

});
