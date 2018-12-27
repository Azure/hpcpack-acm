import { async, ComponentFixture, TestBed, inject } from '@angular/core/testing';
import { TaskDetailComponent } from './task-detail.component';
import { of } from 'rxjs/observable/of';
import { MaterialsModule } from '../../../../materials.module';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA, MatDialog } from '@angular/material';
import { BrowserDynamicTestingModule } from '@angular/platform-browser-dynamic/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ApiService } from '../../../../services/api.service';

class MatDialogModuleMock { }

class ApiServiceStub {
  static result = { nodeName: "testNode", message: { Detail: "test \n test \n" }, primaryTask: true };

  diag = {
    getDiagTaskResult: (jobId: any, taskId: any) => of(ApiServiceStub.result)
  }
}

fdescribe('TaskDetailComponent', () => {
  let component: TaskDetailComponent;
  let fixture: ComponentFixture<TaskDetailComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [TaskDetailComponent],
      imports: [MaterialsModule, MatDialogModule, NoopAnimationsModule],
      providers: [
        { provide: MatDialogRef, useClass: MatDialogModuleMock },
        { provide: MAT_DIALOG_DATA, useValue: { jobId: 1, taskId: 1, taskState: 'Finished' } },
        { provide: ApiService, useClass: ApiServiceStub }
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
    let text = fixture.nativeElement.querySelector(".msg-item").innerText;
    expect(text).toEqual("test \n test \n");
  });
});
