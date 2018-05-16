import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RingReportComponent } from './ring-report.component';

describe('RingReportComponent', () => {
  let component: RingReportComponent;
  let fixture: ComponentFixture<RingReportComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RingReportComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RingReportComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
