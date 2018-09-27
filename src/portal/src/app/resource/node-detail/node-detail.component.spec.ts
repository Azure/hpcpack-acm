import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NodeDetailComponent } from './node-detail.component';
import { MaterialsModule } from '../../materials.module';
import { ChartModule } from 'angular2-chartjs';
import { of, ReplaySubject } from 'rxjs';
import { Component, Input, Directive, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { ApiService } from '../../services/api.service';
import { JobStateService } from '../../services/job-state/job-state.service';
import { DateFormatterService } from '../../services/date-formatter/date-formatter.service';
import { Params, convertToParamMap, ParamMap, ActivatedRoute } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

class ApiServiceStub {
  result = {
    health: 'OK',
    name: 'testNode',
    nodeRegistrationInfo: {
      coreCount: 16,
      distroInfo: "Linux",
      memoryMegabytes: 112796,
      socketCount: 2,
      networksInfo: [
        {
          ipV4: '127.0.0.1/8',
          ipV6: '::1/128',
          isIB: false,
          macAddress: '00:00:00:00:00:00',
          name: 'lo:'
        }
      ]
    }
  };

  metadata = {
    compute: {
      location: 'southcentralus',
      name: 'testNode',
      offer: 'CentOS-HPC',
      osType: 'Linux',
      placementGroupId: '59d5a759-4af5-4506-92b2-47ac689ee23b',
      platformFaultDomain: '1',
      platformUpdateDomain: '1',
      publisher: 'OpenLogic',
      resourceGroupName: 'EVANC-RDMA-SOUTHUS',
      sku: '7.4',
      subscriptionId: 'a486e243-747b-42de-8c4c-379f8295a746',
      tags: '',
      version: '7.4.20180719',
      vmId: 'd57d2a8d-d872-4000-933a-659785d29827',
      vmSize: 'Standard_H16r'
    }
  };

  events = [
    {
      content: 'Dummy node event',
      source: 'Node',
      time: '2018-09-27T02:57:15.1125885+00:00',
      type: 'Information'
    }
  ];

  scheduledevents = {
    Events: []
  };

  jobs = [];

  node = {
    get: (id: any) => of(this.result),
    getMetadata: (id: any) => of(this.metadata),
    getHistoryData: (id: any) => of(),
    getNodeEvents: (id: any) => of(this.events),
    getNodeSheduledEvents: (id: any) => of(this.scheduledevents),
    getNodeJobs: (id: any) => of(this.jobs),

  }
}

class JobStateServiceStub {
  stateClass(state) {
    return 'finished';
  }
  stateIcon(state) {
    return 'done';
  }
}

class DateFormatterServiceStub {
  constructor() { }

  paddingZero(val) {
    return val >= 10 ? val.toString() : `0${val}`;
  }

  dateTime(date) {
    return `${this.dateString(date)} ${this.timeString(date)}`;
  }

  dateString(date) {
    return `${date.getFullYear()}-${this.paddingZero(date.getMonth() + 1)}-${this.paddingZero(date.getDate())}`;
  }

  timeString(date) {
    return `${this.paddingZero(date.getHours())}:${this.paddingZero(date.getMinutes())}:${this.paddingZero(date.getSeconds())}`
  }
}

@Component({ selector: 'app-event-list', template: '' })
class EventListComponent {
  @Input()
  events: any;
}

@Directive({
  selector: '[routerLink]',
  host: { '(click)': 'onClick()' }
})
class RouterLinkDirectiveStub {
  @Input('routerLink') linkParams: any;
  navigatedTo: any = null;

  onClick() {
    this.navigatedTo = this.linkParams;
  }
}

export class ActivatedRouteStub {
  // Use a ReplaySubject to share previous values with subscribers
  // and pump new values into the `paramMap` observable
  private subject = new ReplaySubject<ParamMap>();

  constructor(initialParams?: Params) {
    this.setParamMap(initialParams);
  }

  /** The mock paramMap observable */
  readonly paramMap = this.subject.asObservable();

  /** Set the paramMap observables's next value */
  setParamMap(params?: Params) {
    this.subject.next(convertToParamMap(params));
  };
}

fdescribe('NodeDetailComponent', () => {
  let component: NodeDetailComponent;
  let fixture: ComponentFixture<NodeDetailComponent>;
  let activatedRoute = new ActivatedRouteStub({ id: 'testNode' });

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [
        NodeDetailComponent,
        RouterLinkDirectiveStub,
        EventListComponent
      ],
      imports: [MaterialsModule, ChartModule, NoopAnimationsModule],
      providers: [
        { provide: ActivatedRoute, useValue: activatedRoute },
        { provide: ApiService, useClass: ApiServiceStub },
        { provide: JobStateService, useClass: JobStateServiceStub },
        { provide: DateFormatterService, useClass: DateFormatterServiceStub }
      ],
      schemas: [CUSTOM_ELEMENTS_SCHEMA]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NodeDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let nodeName = fixture.nativeElement.querySelector('.node-name').textContent;
    expect(nodeName).toEqual('testNode');
    let nodeTag = fixture.nativeElement.querySelector('.node-tag').textContent;
    expect(nodeTag).toEqual(' OK ');
  });
});
