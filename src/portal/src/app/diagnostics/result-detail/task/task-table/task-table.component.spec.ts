import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs/observable/of';
import { TaskTableComponent } from './task-table.component';
import { MaterialsModule } from '../../../../materials.module';
import { TableSettingsService } from '../../../../services/table-settings.service';
import { MatTableDataSource } from '@angular/material';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { JobStateService } from '../../../../services/job-state/job-state.service';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { ScrollingModule, CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { TableDataService } from '../../../../services/table-data/table-data.service';

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

class TableDataServiceStub {
  updateData(newData, dataSource, propertyName) {
    return newData;
  }
}

fdescribe('TaskTableComponent', () => {
  let component: TaskTableComponent;
  let fixture: ComponentFixture<TaskTableComponent>;
  let viewport: CdkVirtualScrollViewport;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [
        TaskTableComponent
      ],
      imports: [
        MaterialsModule,
        ScrollingModule,
        NoopAnimationsModule
      ],
      providers: [
        { provide: TableSettingsService, useValue: tableSettingsStub },
        { provide: JobStateService, useClass: JobStateServiceStub },
        { provide: TableDataService, useClass: TableDataServiceStub }
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
      { name: 'nodes', displayName: 'Nodes', displayed: true }
    ];
    component.dataSource = [{
      customizedData: "node1,node2",
      state: "Finished"
    }];
    component.loadFinished = false;
    component.maxPageSize = 120;
    viewport = component.cdkVirtualScrollViewport;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    expect(viewport.getDataLength()).toEqual(1);
  });
});
