import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NodeStateComponent } from './node-state.component';

describe('NodeStateComponent', () => {
  let component: NodeStateComponent;
  let fixture: ComponentFixture<NodeStateComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ NodeStateComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NodeStateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
