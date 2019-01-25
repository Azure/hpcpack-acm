import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ConnectivityComponent } from './connectivity.component';
import { MaterialsModule } from '../../../../../../materials.module';

fdescribe('ConnectivityComponent', () => {
  let component: ConnectivityComponent;
  let fixture: ComponentFixture<ConnectivityComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ConnectivityComponent],
      imports: [MaterialsModule]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ConnectivityComponent);
    component = fixture.componentInstance;
    component.nodes = [
      {
        testNode1: [
          {
            testNode3: {
              Connectivity: "Good",
              Latency: "256.35 us",
              Runtime: "4.375 s",
              Throughput: "90.49 MB/s"
            }
          },
          {
            testNode2: {
              Connectivity: "Good",
              Latency: "169.1 us",
              Runtime: "4.335 s",
              Throughput: "91.33 MB/s"
            }
          },
          {
            testNode1: {
              Connectivity: "Good",
              Latency: "1.65 us",
              Runtime: "0.249 s",
              Throughput: "4178.42 MB/s"
            }
          }
        ]
      },
      {
        testNode2: [
          {
            testNode3: {
              Connectivity: "Good",
              Latency: "607.2 us",
              Runtime: "4.532 s",
              Throughput: "91.98 MB/s"
            }
          },
          {
            testNode2: {
              Connectivity: "Good",
              Latency: "3.45 us",
              Runtime: "0.294 s",
              Throughput: "4007.74 MB/s"
            }
          }
        ]
      },
      {
        testNode3: [
          {
            testNode3: {
              Connectivity: "Good",
              Latency: "2.15 us",
              Runtime: "0.231 s",
              Throughput: "4910.52 MB/s"
            }
          }
        ]
      }
    ];
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();

    let tiles = fixture.nativeElement.querySelectorAll('.tile');
    expect(tiles.length).toEqual(6);
  });
});
