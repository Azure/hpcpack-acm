import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadingProgressBarComponent } from './loading-progress-bar.component';
import { MaterialsModule } from '../../materials.module';

fdescribe('LoadingProgressBarComponent', () => {
  let component: LoadingProgressBarComponent;
  let fixture: ComponentFixture<LoadingProgressBarComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [LoadingProgressBarComponent],
      imports: [MaterialsModule]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(LoadingProgressBarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
