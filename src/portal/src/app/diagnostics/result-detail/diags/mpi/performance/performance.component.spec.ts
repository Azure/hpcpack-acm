import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PerformanceComponent } from './performance.component';
import { ChartModule } from 'angular2-chartjs';

fdescribe('PerformanceComponent', () => {
  let component: PerformanceComponent;
  let fixture: ComponentFixture<PerformanceComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [PerformanceComponent],
      imports: [
        ChartModule
      ]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PerformanceComponent);
    component = fixture.componentInstance;
    component.result = {
      Result: [
        {
          Latency: {
            unit: 'usec',
            value: '244.46'
          },
          Message_Size: {
            unit: 'Bytes',
            value: '0'
          },
          Throughput: {
            unit: 'Mbytes/sec',
            value: '0.00'
          }
        }
      ]
    };
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
