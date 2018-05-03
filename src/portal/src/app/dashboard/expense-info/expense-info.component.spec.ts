import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ExpenseInfoComponent } from './expense-info.component';

describe('ExpenseInfoComponent', () => {
  let component: ExpenseInfoComponent;
  let fixture: ComponentFixture<ExpenseInfoComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ExpenseInfoComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ExpenseInfoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
