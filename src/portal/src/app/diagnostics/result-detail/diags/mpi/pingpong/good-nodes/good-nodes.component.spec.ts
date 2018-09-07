import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { GoodNodesComponent } from './good-nodes.component';

describe('GoodNodesComponent', () => {
  let component: GoodNodesComponent;
  let fixture: ComponentFixture<GoodNodesComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ GoodNodesComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(GoodNodesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
