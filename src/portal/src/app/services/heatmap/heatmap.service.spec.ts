import { TestBed, inject } from '@angular/core/testing';

import { HeatmapService } from './heatmap.service';

fdescribe('HeatmapService', () => {
  let routerSpy = jasmine.createSpyObj('Router', ['navigate']);
  routerSpy.navigate.and.returnValue('');
  let heatmapService: HeatmapService;
  beforeEach(() => {
    heatmapService = new HeatmapService(routerSpy);
  });

  it('should be created', () => {
    expect(heatmapService).toBeTruthy();
  });

  it('#nodeClass should return right classs name', () => {
    let empty = heatmapService.nodeClass({ value: 0 });
    expect(empty).toBe('empty');
    let full = heatmapService.nodeClass({ value: 100 });
    expect(full).toBe('full');
    let error = heatmapService.nodeClass({});
    expect(error).toBe(undefined);
  });
});
