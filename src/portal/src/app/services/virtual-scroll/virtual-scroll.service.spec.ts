import { TestBed } from '@angular/core/testing';

import { VirtualScrollService } from './virtual-scroll.service';

describe('VirtualScrollService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: VirtualScrollService = TestBed.get(VirtualScrollService);
    expect(service).toBeTruthy();
  });
});
