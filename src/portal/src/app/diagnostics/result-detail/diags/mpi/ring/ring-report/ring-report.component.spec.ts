import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, Input, Directive, Output, EventEmitter } from '@angular/core';
import { of } from 'rxjs/observable/of';
import { RingReportComponent } from './ring-report.component';
import { MaterialsModule } from '../../../../../../materials.module';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ApiService } from '../../../../../../services/api.service';
import { TableSettingsService } from '../../../../../../services/table-settings.service';
import { TableDataService } from '../../../../../../services/table-data/table-data.service';
import { DiagReportService } from '../../../../../../services/diag-report/diag-report.service';

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

@Component({ selector: 'app-result-layout', template: '' })
class ResultLayoutComponent {
  @Input()
  result: any;

  @Input()
  aggregationResult: any;
}

@Component({ selector: 'app-nodes-info', template: '' })
class NodesInfoComponent {
  @Input()
  nodes: Array<any>;

  @Input()
  badNodes: Array<any>;
}

@Component({ selector: 'ring-overview-result', template: '' })
class RingOverviewResultComponent {
  @Input()
  result: any;
}

@Component({ selector: 'app-event-list', template: '' })
class EventListComponent {
  @Input()
  events: any;
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
    getJobAggregationResult: (id: any) => of({ Error: "error message" })
  }
}

const tableSettingsStub = {
  load: (key, initVal) => initVal,

  save: (key, val) => undefined
}

class TableDataServiceStub {
  updateData(newData, dataSource, propertyName) {
    return dataSource.data = newData;
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

fdescribe('RingReportComponent', () => {
  let component: RingReportComponent;
  let fixture: ComponentFixture<RingReportComponent>;

  let wrapperComponent: WrapperComponent;
  let wrapperFixture: ComponentFixture<WrapperComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [
        RouterLinkDirectiveStub,
        RingReportComponent,
        ResultLayoutComponent,
        NodesInfoComponent,
        RingOverviewResultComponent,
        DiagTaskTableComponent,
        EventListComponent,
        WrapperComponent
      ],
      imports: [MaterialsModule, NoopAnimationsModule],
      providers: [
        { provide: ApiService, useClass: ApiServiceStub },
        { provide: TableSettingsService, useValue: tableSettingsStub },
        { provide: TableDataService, useClass: TableDataServiceStub },
        { provide: DiagReportService, useClass: DiagReportServiceStub }
      ]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RingReportComponent);
    component = fixture.componentInstance;

    wrapperFixture = TestBed.createComponent(WrapperComponent);
    wrapperComponent = wrapperFixture.componentInstance;

    component.result = { aggregationResult: { Error: "error message" } };

    fixture.detectChanges();
    wrapperFixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show error message', () => {
    let wrapperComponent = wrapperFixture.componentInstance;
    let itemElement = wrapperFixture.debugElement.nativeElement;
    let text = itemElement.querySelector(".error-message").textContent;
    expect(text).toEqual('error message');
  });
});
