import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PingPongOverviewResultComponent } from './overview-result.component';
import { MaterialsModule } from '../../../../../../materials.module';
import { FormsModule } from '@angular/forms';
import { ChartModule } from 'angular2-chartjs';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

fdescribe('PingPongOverviewResultComponent', () => {
  let component: PingPongOverviewResultComponent;
  let fixture: ComponentFixture<PingPongOverviewResultComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [PingPongOverviewResultComponent],
      imports: [
        MaterialsModule,
        FormsModule,
        ChartModule,
        BrowserAnimationsModule
      ]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PingPongOverviewResultComponent);
    component = fixture.componentInstance;
    component.result = {
      Threshold: 1000,
      ResultByNode: {
        evancvmss000006: {
          Worst_pairs: {
            Pairs: ["evancvmss000002,evancvmss000006"],
            Value: 78.56
          },
          Variability: "Low", Bad_pairs: [], Standard_deviation: 0.0, Average: 78.56, Median: 78.56,
          Histogram: [[0, 1], ["78.06-78.56", "78.56-79.06"]], Passed: true, Best_pairs: {
            Pairs: ["evancvmss000002,evancvmss000006"],
            Value: 78.56
          }
        }
      },
      Result: {
        Worst_pairs: {
          Pairs: ["evancvmss000002,evancvmss000006"],
          Value: 78.56
        },
        Variability: "Low", Bad_pairs: [], Standard_deviation: 0.0, Average: 78.56, Median: 78.56,
        Histogram: [[0, 1], ["78.06-78.56", "78.56-79.06"]], Passed: true, Best_pairs: {
          Pairs: ["evancvmss000002,evancvmss000006"],
          Value: 78.56
        }
      },
      Unit: "us", Packet_size: "1 Bytes"
    };
 
    component.nodes = ["evancvmss000002,evancvmss000006"];
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let values = fixture.nativeElement.querySelectorAll(".overview-value");
    let text = values[0].textContent;
    expect(text).toEqual('1 Bytes');
    text = values[2].textContent;
    expect(text).toEqual('78.56 us');
  });
});
