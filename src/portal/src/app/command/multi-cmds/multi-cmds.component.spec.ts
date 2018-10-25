import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MultiCmdsComponent } from './multi-cmds.component';

describe('MultiCmdsComponent', () => {
  let component: MultiCmdsComponent;
  let fixture: ComponentFixture<MultiCmdsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ MultiCmdsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MultiCmdsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
