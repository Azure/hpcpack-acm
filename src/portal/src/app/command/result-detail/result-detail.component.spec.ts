import { async, fakeAsync, flush, ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, OnInit, Input, Output, EventEmitter, ViewChild, ElementRef, SimpleChanges } from '@angular/core';
import { of } from 'rxjs/observable/of';
import { _throw } from 'rxjs/observable/throw';

import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { FormsModule } from '@angular/forms'
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MaterialsModule } from '../../materials.module';
import { ResultDetailComponent } from './result-detail.component';

@Component({ selector: 'command-output', template: '' })
class CommandOutputStubComponent {
  @Output()
  loadPrev = new EventEmitter<any>();

  @Output()
  loadNext = new EventEmitter<any>();

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
  static job = { commandLine: 'TEST COMMAND', state: 'finished' };

  static tasks = [{ id: 1, node: 'TEST NODE', state: 'finished' }];

  static taskResult1 = { resultKey: 'key001' };

  static outputContent = 'TEST CONTENT';

  static outputUrl = 'TESTURL';

  command: any;

  constructor() {
    this.command = jasmine.createSpyObj('Command', ['get', 'getTasks', 'getTaskResult', 'getOutput', 'getDownloadUrl']);
    this.command.get.and.returnValue(of(ApiServiceStub.job));
    this.command.getTasks.and.returnValue(of(ApiServiceStub.tasks));
    this.command.getTaskResult.and.returnValue(of(ApiServiceStub.taskResult1));
    let value = { content: ApiServiceStub.outputContent, size: ApiServiceStub.outputContent.length, offset: 0, end: true };
    this.command.getOutput.and.returnValues(of(value));
    this.command.getDownloadUrl.and.returnValue(ApiServiceStub.outputUrl);
  }
}

const activatedRouteStub = {
  paramMap: of({ get: () => 1 })
}

const routerStub = {
  navigate: () => {},
}

fdescribe('ResultDetailComponent', () => {
  let component: ResultDetailComponent;
  let fixture: ComponentFixture<ResultDetailComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [
        NodeSelectorStubComponent,
        CommandOutputStubComponent,
        ResultDetailComponent,
      ],
      imports: [
        NoopAnimationsModule,
        FormsModule,
        MaterialsModule,
      ],
      providers: [
        { provide: ApiService, useClass: ApiServiceStub },
        { provide: ActivatedRoute, useValue: activatedRouteStub },
        { provide: Router, useValue: routerStub },
      ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ResultDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', fakeAsync(() => {
    flush();

    expect(component).toBeTruthy();
    let text = fixture.nativeElement.querySelector('.command').textContent;
    expect(text).toContain(ApiServiceStub.job.commandLine);
    text = fixture.nativeElement.querySelector('.state').textContent;
    expect(text).toContain('Finished');

    expect(component.currentOutput.content).toEqual(ApiServiceStub.outputContent);

    let selectedNode = component.selectedNode;
    expect(selectedNode.name).toEqual(ApiServiceStub.tasks[0].node);
    expect(selectedNode.state).toEqual(ApiServiceStub.tasks[0].state);
  }));
});
