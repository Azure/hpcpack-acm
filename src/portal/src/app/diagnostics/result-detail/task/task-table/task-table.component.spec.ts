import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs/observable/of';
import { TaskTableComponent } from './task-table.component';
import { MaterialsModule } from '../../../../materials.module';
import { TableSettingsService } from '../../../../services/table-settings.service';
import { MatTableDataSource } from '@angular/material';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { JobStateService } from '../../../../services/job-state/job-state.service';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';

const tableSettingsStub = {
  load: (key, initVal) => initVal,

  save: (key, val) => undefined
}

class JobStateServiceStub {
  stateClass(state) {
    return 'finished';
  }
  stateIcon(state) {
    return 'done';
  }
}

fdescribe('TaskTableComponent', () => {
  let component: TaskTableComponent;
  let fixture: ComponentFixture<TaskTableComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [
        TaskTableComponent
      ],
      imports: [
        MaterialsModule,
        NoopAnimationsModule
      ],
      providers: [
        { provide: TableSettingsService, useValue: tableSettingsStub },
        { provide: JobStateService, useClass: JobStateServiceStub },
      ],
      schemas: [CUSTOM_ELEMENTS_SCHEMA]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TaskTableComponent);
    component = fixture.componentInstance;
    component.tableName = "test";
    component.customizableColumns = [
      { name: 'nodes', displayName: 'Nodes', displayed: true },
      { name: 'latency', displayName: 'Latency', displayed: true },
      { name: 'throughput', displayName: 'Throughput', displayed: true },
    ];
    component.dataSource = new MatTableDataSource();
    component.dataSource.data = [{
      customizedData: "node1,node2", state: "Finished", taskInfo: {
        message: {
          Latency: 0.42, Throughput: 5989.34
        }
      }
    }];
    component.currentData = [];
    component.loadFinished = false;
    component.maxPageSize = 120;

    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let text = fixture.nativeElement.querySelector('.mat-cell.mat-column-nodes .icon-cell .cell-text').textContent;
    expect(text).toEqual("node1,node2");
    text = fixture.nativeElement.querySelector('.mat-cell.mat-column-state .icon-cell .cell-text').textContent;
    expect(text).toEqual("Finished");
    text = fixture.nativeElement.querySelector('.mat-cell.mat-column-latency').textContent;
    expect(text).toEqual("0.42");
    text = fixture.nativeElement.querySelector('.mat-cell.mat-column-throughput').textContent;
    expect(text).toEqual("5989.34");
  });
});
