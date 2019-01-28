import { MainModule } from './main.module';

fdescribe('MainModule', () => {
  let mainModule: MainModule;

  beforeEach(() => {
    mainModule = new MainModule();
  });

  it('should create an instance', () => {
    expect(mainModule).toBeTruthy();
  });
});
