import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NewCommandComponent } from './new-command.component';

describe('NewCommandComponent', () => {
  let component: NewCommandComponent;
  let fixture: ComponentFixture<NewCommandComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ NewCommandComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NewCommandComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
