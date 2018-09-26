import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NodeSelectorComponent } from './node-selector.component';
import { MaterialsModule } from '../../materials.module';
import { FormsModule } from '@angular/forms';
import { JobStateService } from '../../services/job-state/job-state.service';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { SimpleChange } from '@angular/core';

class JobStateServiceStub {
  stateClass(state) {
    return 'finished';
  }
  stateIcon(state) {
    return 'done';
  }
}

fdescribe('NodeSelectorComponent', () => {
  let component: NodeSelectorComponent;
  let fixture: ComponentFixture<NodeSelectorComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [NodeSelectorComponent],
      imports: [
        MaterialsModule,
        FormsModule,
        NoopAnimationsModule
      ],
      providers: [
        { provide: JobStateService, useClass: JobStateServiceStub }
      ]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NodeSelectorComponent);
    component = fixture.componentInstance;
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
    let name = fixture.nativeElement.querySelector('.mat-column-name .cell-text').textContent;
    expect(name).toEqual('testNode');
    let state = fixture.nativeElement.querySelector('.mat-column-state .cell-text').textContent;
    expect(state).toEqual('Finished');
  })
});
