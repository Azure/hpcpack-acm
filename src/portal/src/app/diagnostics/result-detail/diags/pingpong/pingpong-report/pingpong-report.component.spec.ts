import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PingPongReportComponent } from './pingpong-report.component';

describe('PingpongTestComponent', () => {
  let component: PingPongReportComponent;
  let fixture: ComponentFixture<PingPongReportComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PingPongReportComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PingPongReportComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
