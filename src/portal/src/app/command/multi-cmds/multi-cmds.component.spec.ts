import { async, ComponentFixture, TestBed, flush, fakeAsync } from '@angular/core/testing';
import { MultiCmdsComponent } from './multi-cmds.component';
import { Component, Output, EventEmitter, Input, SimpleChanges, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { of } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { FormsModule } from '@angular/forms';
import { MaterialsModule } from '../../materials.module';
import { ApiService } from '../../services/api.service';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { TableDataService } from '../../services/table-data/table-data.service';
import { JobStateService } from '../../services/job-state/job-state.service';

@Component({ selector: 'command-output', template: '' })
class CommandOutputStubComponent {
  @Output()
  loadPrev = new EventEmitter<any>();

  @Output()
  loadNext = new EventEmitter<any>();

  @Output()
  gotoTop = new EventEmitter<any>();

  @Input()
  content: string = '';

  @Input()
  disabled: boolean = false;

  @Input()
  loading: string | boolean = false;

  //Got Begin of File
  @Input()
  bof: boolean = false;

  //Got End of File
  @Input()
  eof: boolean = false;

  scrollToBottom() { }
}

@Component({ selector: 'node-selector', template: '' })
class NodeSelectorStubComponent {
  @Input()
  nodes: any[];

  @Output()
  select = new EventEmitter();

  selectedNode: any;

  ngOnChanges(changes: SimpleChanges) {
    if (changes.nodes) {
      let prevNode = this.selectedNode;
      this.selectedNode = this.nodes[0];
      this.select.emit({ node: this.nodes[0], prevNode });
    }
  }
}


class ApiServiceStub {
  static job = { commandLine: 'TEST COMMAND', state: 'Finished', targetNodes: ['TEST NODE'] };

  static tasks = [{ id: 1, name: 'TEST NODE', state: 'Finished' }];

  static taskResult1 = { resultKey: 'key001' };

  static outputContent = 'TEST CONTENT';

  static outputUrl = 'TESTURL';

  command: any;

  constructor() {
    this.command = jasmine.createSpyObj('Command', ['get', 'getTasksByPage', 'getTaskResult', 'getOutput', 'getDownloadUrl']);
    this.command.get.and.returnValue(of(ApiServiceStub.job));
    this.command.getTasksByPage.and.returnValue(of(ApiServiceStub.tasks));
    this.command.getTaskResult.and.returnValue(of(ApiServiceStub.taskResult1));
    let value = { content: ApiServiceStub.outputContent, size: ApiServiceStub.outputContent.length, offset: 0, end: true };
    this.command.getOutput.and.returnValues(of(value));
    this.command.getDownloadUrl.and.returnValue(ApiServiceStub.outputUrl);
  }
}

const activatedRouteStub = {
  queryParams: of({ firstJobId: 1 })
}

class JobStateServiceStub {
  stateClass(state) {
    return 'finished';
  }
  stateIcon(state) {
    return 'done';
  }
}

class TableDataServiceStub {
  updateData(newData, dataSource, propertyName) {
    return newData;
  }
}

fdescribe('MultiCmdsComponent', () => {
  let component: MultiCmdsComponent;
  let fixture: ComponentFixture<MultiCmdsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [
        MultiCmdsComponent,
        NodeSelectorStubComponent,
        CommandOutputStubComponent
      ],
      imports: [
        NoopAnimationsModule,
        FormsModule,
        MaterialsModule
      ],
      providers: [
        { provide: ApiService, useClass: ApiServiceStub },
        { provide: JobStateService, useClass: JobStateServiceStub },
        { provide: ActivatedRoute, useValue: activatedRouteStub },
        { provide: TableDataService, useClass: TableDataServiceStub }
      ],
      schemas: [CUSTOM_ELEMENTS_SCHEMA]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MultiCmdsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', fakeAsync(() => {
    flush();
    expect(component).toBeTruthy();
    let text = fixture.nativeElement.querySelector('.job-state .name').textContent;
    expect(text).toContain(ApiServiceStub.job.commandLine);
    text = fixture.nativeElement.querySelector('.state-text').textContent;
    expect(text).toContain('Finished');

    expect(component.currentOutput.content).toEqual(ApiServiceStub.outputContent);

    let selectedNode = component.selectedNode;
    expect(selectedNode.name).toEqual(ApiServiceStub.tasks[0].name);
    expect(selectedNode.state).toEqual(ApiServiceStub.tasks[0].state);

    let tabs = component.tabs;
    expect(tabs.length).toEqual(1);
    expect(tabs[0].id).toEqual(ApiServiceStub.tasks[0].id);
  }));
});
