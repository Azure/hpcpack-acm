import { async, ComponentFixture, TestBed, inject } from '@angular/core/testing';
import { OverlayContainer } from '@angular/cdk/overlay';
import { TaskDetailComponent } from './task-detail.component';
import { of } from 'rxjs/observable/of';
import { MaterialsModule } from '../../../../materials.module';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA, MatDialog } from '@angular/material';
import { BrowserDynamicTestingModule } from '@angular/platform-browser-dynamic/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ApiService } from '../../../../services/api.service';

class MatDialogModuleMock { }

class ApiServiceStub {
  static results = [{ nodeName: "testNode", message: { Latency: 513.43, Throughput: 337.58, Detail: "" }, name: 'test', primaryTask: true }];

  diag = {
    getDiagTaskResult: (jobId: any, taskId: any) => of(ApiServiceStub.results)
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
        { provide: MAT_DIALOG_DATA, useValue: { msg: { Detail: "message" } } },
        { provide: ApiService, useValue: ApiServiceStub }
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

  it('should show task detail', () => {
    let details = fixture.nativeElement.querySelector(".overview-value");
    let text = details[0].textContent;
    expect(text).toEqual("testNode");
    text = details[1].textContent;
    expect(text).toEqual("513.43");
  });
});
