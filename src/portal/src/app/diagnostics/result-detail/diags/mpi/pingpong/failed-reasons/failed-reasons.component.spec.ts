import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { FailedReasonsComponent } from './failed-reasons.component';

describe('FailedReasonsComponent', () => {
  let component: FailedReasonsComponent;
  let fixture: ComponentFixture<FailedReasonsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ FailedReasonsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(FailedReasonsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
