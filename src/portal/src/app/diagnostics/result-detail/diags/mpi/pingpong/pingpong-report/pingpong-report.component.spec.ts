import { async, fakeAsync, ComponentFixture, TestBed, flush } from '@angular/core/testing';
import { Component, Input, ViewChild, Output, EventEmitter, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { of } from 'rxjs/observable/of';
import { PingPongReportComponent } from './pingpong-report.component';
import { MaterialsModule } from '../../../../../../materials.module';
import { ApiService } from '../../../../../../services/api.service';
import { TableSettingsService } from '../../../../../../services/table-settings.service';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { TableDataService } from '../../../../../../services/table-data/table-data.service';
import { DiagReportService } from '../../../../../../services/diag-report/diag-report.service';

@Component({ selector: 'app-result-layout', template: '' })
class ResultLayoutComponent {
  @Input()
  result: any;

  @Input()
  aggregationResult: any;
}

@Component({ selector: 'pingpong-overview-result', template: '' })
class PingPongOverviewResultComponent {
  @Input()
  result: any;
}

@Component({ selector: 'pingpong-good-nodes', template: '' })
class GoodNodesComponent {
  @Input()
  nodeGroups;
}

@Component({ selector: 'pingpong-failed-reasons', template: '' })
class FailedReasonsComponent {
  @Input()
  failedNodes: any;

  @Input()
  failedReasons: any;

  @Input()
  nodes: any;
}

@Component({ selector: 'app-event-list', template: '' })
class EventListComponent {
  @Input()
  events: any;
}

@Component({ selector: 'app-nodes-info', template: '' })
class NodesInfoComponent {
  @Input()
  nodes: Array<any>;

  @Input()
  badNodes: Array<any>;
}

@Component({ selector: 'diag-task-table', template: '' })
class DiagTaskTableComponent {
  @Input()
  dataSource: any;

  @Input()
  currentData: any;

  @Input()
  customizableColumns: any;

  @Input()
  tableName: any;

  @Input()
  loadFinished: boolean;

  @Input()
  maxPageSize: number;

  @Output()
  updateLastIdEvent = new EventEmitter();

  @Input()
  public empty: boolean;
}

@Component({
  template: `
    <div class="error-message" *ngIf="result.aggregationResult != undefined && result.aggregationResult.Error != undefined">{{result.aggregationResult.Error}}</div>
  `
})
class WrapperComponent {
  public result = { aggregationResult: { Error: "error message" } };
}

class ApiServiceStub {
  static taskResult = [{
    customizedData: "evancvmss000002,evancvmss000006",
    jobId: 302,
    nodeName: "EVANCVMSS000002",
    state: "Finished"
  }];

  static jobResult = {
    id: 302,
    name: "pingpong",
    aggregationResult: { Error: "error message" }
  };

  diag = {
    getDiagTasksByPage: (id: any, lastId, count) => of(ApiServiceStub.taskResult),
    getDiagJob: (id: any) => of(ApiServiceStub.jobResult),
    getJobAggregationResult: (id: any) => of({ Error: "error message" }),
    getJobEvents: (id: any) => of([])
  }
}

const tableSettingsStub = {
  load: (key, initVal) => initVal,

  save: (key, val) => undefined
}

class TableDataServiceStub {
  updateData(newData, dataSource, propertyName) {
    return newData;
  }
}


class DiagReportServiceStub {
  hasError(result) {
    return true;
  }

  jobFinished(state) {
    return true;
  }

  getErrorMsg(err) {
    return { Error: err };
  }
}

fdescribe('PingPongReportComponent', () => {
  let component: PingPongReportComponent;
  let fixture: ComponentFixture<PingPongReportComponent>;

  let wrapperComponent: WrapperComponent;
  let wrapperFixture: ComponentFixture<WrapperComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [
        PingPongReportComponent,
        ResultLayoutComponent,
        PingPongOverviewResultComponent,
        DiagTaskTableComponent,
        EventListComponent,
        NodesInfoComponent,
        WrapperComponent,
        GoodNodesComponent,
        FailedReasonsComponent
      ],
      imports: [MaterialsModule, NoopAnimationsModule],
      providers: [
        { provide: ApiService, useClass: ApiServiceStub },
        { provide: TableSettingsService, useValue: tableSettingsStub },
        { provide: TableDataService, useClass: TableDataServiceStub },
        { provide: DiagReportService, useClass: DiagReportServiceStub }
      ],
      schemas: [CUSTOM_ELEMENTS_SCHEMA]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PingPongReportComponent);
    component = fixture.componentInstance;

    wrapperFixture = TestBed.createComponent(WrapperComponent);
    wrapperComponent = wrapperFixture.componentInstance;

    component.result = { aggregationResult: { Error: "error message" } };

    fixture.detectChanges();
    wrapperFixture.detectChanges();
  });

  it('should create', fakeAsync(() => {
    expect(component).toBeTruthy();
  }));

  it('should show error message', () => {
    let wrapperComponent = wrapperFixture.componentInstance;
    let itemElement = wrapperFixture.debugElement.nativeElement;
    let text = itemElement.querySelector(".error-message").textContent;
    expect(text).toEqual('error message');
  });
});
