import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DashboardComponent } from './dashboard.component';
import { NodeStateComponent } from './node-state/node-state.component';
import { JobOverviewComponent } from './job-overview/job-overview.component';
import { ChartModule } from 'angular2-chartjs';
import { of } from 'rxjs';
import { ApiService } from '../services/api.service';
import { Router, ActivatedRoute } from '@angular/router';

class ApiServiceStub {
  nodes = { data: { Error: 1, OK: 97, Warning: 0 } };
  diags = {
    data: {
      Canceled: 55,
      Canceling: 0,
      Failed: 106,
      Finished: 102,
      Finishing: 0,
      Queued: 0,
      Running: 1
    }
  };
  clusrun = {
    data: {
      Canceled: 64,
      Canceling: 0,
      Failed: 16,
      Finished: 12,
      Finishing: 0,
      Queued: 10,
      Running: 1
    }
  };
  dashboard = {
    getNodes: () => of(this.nodes),
    getDiags: () => of(this.diags),
    getClusrun: () => of(this.clusrun)
  }
}

const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
const activatedRouteSpy = jasmine.createSpyObj('ActivatedRoute', ['']);

fdescribe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;


  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [DashboardComponent, NodeStateComponent, JobOverviewComponent],
      imports: [ChartModule],
      providers: [
        { provide: ApiService, useClass: ApiServiceStub },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: activatedRouteSpy }
      ]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
