import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NewDiagnosticsComponent } from './new-diagnostics.component';

describe('NewDiagnosticsComponent', () => {
  let component: NewDiagnosticsComponent;
  let fixture: ComponentFixture<NewDiagnosticsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ NewDiagnosticsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NewDiagnosticsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
