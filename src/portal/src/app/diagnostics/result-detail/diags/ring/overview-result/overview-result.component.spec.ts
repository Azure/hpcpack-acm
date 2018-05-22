import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RingOverviewResultComponent } from './overview-result.component';

fdescribe('RingOverviewResultComponent', () => {
  let component: RingOverviewResultComponent;
  let fixture: ComponentFixture<RingOverviewResultComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [RingOverviewResultComponent]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RingOverviewResultComponent);
    component = fixture.componentInstance;
    component.result = {
      Passed: true,
      Latency: {
        Value: 23.45,
        Unit: "Mb/s",
        Threshold: 34.56
      },
      Throughput: {
        Value: 78.56,
        Threshold: 1000,
        Unit: "us"
      }
    };
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let values = fixture.nativeElement.querySelectorAll(".overview-value");

    let text = values[0].textContent;
    expect(text).toEqual("23.45 Mb/s");

    text = values[3].textContent;
    expect(text).toEqual("1000 us");
  });
});
