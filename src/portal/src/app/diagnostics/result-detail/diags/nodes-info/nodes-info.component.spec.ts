import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NodesInfoComponent } from './nodes-info.component';

describe('NodesInfoComponent', () => {
  let component: NodesInfoComponent;
  let fixture: ComponentFixture<NodesInfoComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ NodesInfoComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NodesInfoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
