import { VirtualScrollService } from './virtual-scroll.service';

fdescribe('VirtualScrollService', () => {
  let virtualScrollService: VirtualScrollService;
  beforeEach(() => {
    virtualScrollService = new VirtualScrollService();
  });

  it('should be created', () => {
    expect(virtualScrollService).toBeTruthy();
  });
});
