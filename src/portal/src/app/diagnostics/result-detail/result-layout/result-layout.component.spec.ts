import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { MaterialsModule } from '../../../materials.module';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ResultLayoutComponent } from './result-layout.component';

fdescribe('ResultLayoutComponent', () => {
  let component: ResultLayoutComponent;
  let fixture: ComponentFixture<ResultLayoutComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ResultLayoutComponent],
      imports: [
        NoopAnimationsModule,
        MaterialsModule
      ]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ResultLayoutComponent);
    component = fixture.componentInstance;
    component.result = { id: 1, name: "test", state: "Finished" };
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let text = fixture.nativeElement.querySelector('.name').textContent;
    expect(text).toEqual("1 - test Finished");
  });
});
