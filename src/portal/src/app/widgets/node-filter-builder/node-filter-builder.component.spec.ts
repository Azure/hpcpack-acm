import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NodeFilterBuilderComponent } from './node-filter-builder.component';

describe('NodeFilterBuilderComponent', () => {
  let component: NodeFilterBuilderComponent;
  let fixture: ComponentFixture<NodeFilterBuilderComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ NodeFilterBuilderComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NodeFilterBuilderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
