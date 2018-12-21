import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NodeSelectorComponent } from './node-selector.component';
import { MaterialsModule } from '../../materials.module';
import { FormsModule } from '@angular/forms';
import { JobStateService } from '../../services/job-state/job-state.service';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { SimpleChange, NO_ERRORS_SCHEMA } from '@angular/core';
import { ScrollingModule, CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { TableDataService } from '../../services/table-data/table-data.service';

class JobStateServiceStub {
  stateClass(state) {
    return 'finished';
  }
  stateIcon(state) {
    return 'done';
  }
}

class TableDataServiceStub {
  trackByFn(item, colums) {
    return false;
  }
}

fdescribe('NodeSelectorComponent', () => {
  let component: NodeSelectorComponent;
  let fixture: ComponentFixture<NodeSelectorComponent>;
  let viewport: CdkVirtualScrollViewport;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [NodeSelectorComponent],
      imports: [
        MaterialsModule,
        FormsModule,
        NoopAnimationsModule,
        ScrollingModule
      ],
      providers: [
        { provide: JobStateService, useClass: JobStateServiceStub },
        { provide: TableDataService, useClass: TableDataServiceStub }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NodeSelectorComponent);
    component = fixture.componentInstance;
    viewport = component.cdkVirtualScrollViewport;
    component.nodes = [
      {
        name: "testNode",
        state: 'Finished'
      }
    ];
    component.nodeOutputs = {};
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    component.ngOnChanges({
      nodes: new SimpleChange([], component.nodes, false)
    });
    fixture.detectChanges();
    expect(viewport.getDataLength()).toEqual(1);
  })
});
