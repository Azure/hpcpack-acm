import { async, ComponentFixture, TestBed, inject } from '@angular/core/testing';
import { OverlayContainer } from '@angular/cdk/overlay';
import { TaskDetailComponent } from './task-detail.component';
import { MaterialsModule } from '../../../../materials.module';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA, MatDialog } from '@angular/material';
import { BrowserDynamicTestingModule } from '@angular/platform-browser-dynamic/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

class MatDialogModuleMock { }

fdescribe('TaskDetailComponent', () => {
  let component: TaskDetailComponent;
  let fixture: ComponentFixture<TaskDetailComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [TaskDetailComponent],
      imports: [MaterialsModule, MatDialogModule, NoopAnimationsModule],
      providers: [
        { provide: MatDialogRef, useClass: MatDialogModuleMock },
        { provide: MAT_DIALOG_DATA, useValue: { msg: { Detail: "message" } } }
      ]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TaskDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let text = fixture.nativeElement.querySelector(".msg-item").textContent;
    expect(text).toEqual("message");
  });
});
