import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NodeHealthHistoryComponent } from './node-health-history.component';

describe('NodeHealthHistoryComponent', () => {
  let component: NodeHealthHistoryComponent;
  let fixture: ComponentFixture<NodeHealthHistoryComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ NodeHealthHistoryComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NodeHealthHistoryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
