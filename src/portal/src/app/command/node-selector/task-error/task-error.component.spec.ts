import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TaskErrorComponent } from './task-error.component';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { MaterialsModule } from '../../../materials.module';

class MatDialogModuleMock {
  public close() { }
}

fdescribe('TaskErrorComponent', () => {
  let component: TaskErrorComponent;
  let fixture: ComponentFixture<TaskErrorComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [TaskErrorComponent],
      imports: [MaterialsModule],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: { message: 'error message' } },
        { provide: MatDialogRef, useClass: MatDialogModuleMock }
      ],
      schemas: [CUSTOM_ELEMENTS_SCHEMA]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TaskErrorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let errorMsg = fixture.nativeElement.querySelector('.error-message').textContent;
    expect(errorMsg).toEqual(' error message ');
  });
});
