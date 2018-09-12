import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { MaterialsModule } from '../../../materials.module';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ResultLayoutComponent } from './result-layout.component';
import { of } from 'rxjs/observable/of';
import { ApiService } from '../../../services/api.service';
import { Router } from '@angular/router';
import { JobStateService } from '../../../services/job-state/job-state.service';

const routerStub = {
  navigate: () => { },
}

class ApiServiceStub {
  static result = "\"Job is canceled\"";

  diag = {
    cancel: (jobId: any) => of(ApiServiceStub.result)
  }
}

class JobStateServiceStub {
  stateClass(state) {
    return 'finished';
  }
  stateIcon(state) {
    return 'done';
  }
}

fdescribe('ResultLayoutComponent', () => {
  let component: ResultLayoutComponent;
  let fixture: ComponentFixture<ResultLayoutComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ResultLayoutComponent],
      imports: [
        NoopAnimationsModule,
        MaterialsModule
      ],
      providers: [
        { provide: Router, useValue: routerStub },
        { provide: JobStateService, useClass: JobStateServiceStub },
        { provide: ApiService, useClass: ApiServiceStub }
      ]

    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ResultLayoutComponent);
    component = fixture.componentInstance;
    component.result = { id: 1, name: "test", state: "Finished" };
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let text = fixture.nativeElement.querySelector('.name').textContent;
    expect(text).toContain("1 - test");
  });
});
