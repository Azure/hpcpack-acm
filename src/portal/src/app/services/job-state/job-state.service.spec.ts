import { TestBed, inject } from '@angular/core/testing';

import { JobStateService } from './job-state.service';

fdescribe('JobStateService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [JobStateService]
    });
  });

  it('should be created', inject([JobStateService], (service: JobStateService) => {
    expect(service).toBeTruthy();
  }));

  it('should return expected state icon name', inject([JobStateService], (service: JobStateService) => {
    let iconName = service.stateIcon('finished');
    expect(iconName).toEqual('done');
  }));

  it('should return expected state class name', inject([JobStateService], (service: JobStateService) => {
    let className = service.stateClass('Finished');
    expect(className).toEqual('finished');
  }));

});
