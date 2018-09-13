import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { ChartModule } from 'angular2-chartjs';
import { JobOverviewComponent } from './job-overview.component';
import { Router, ActivatedRoute } from '@angular/router';

fdescribe('JobOverviewComponent', () => {
  let component: JobOverviewComponent;
  let fixture: ComponentFixture<JobOverviewComponent>;
  const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
  const activatedRouteSpy = jasmine.createSpyObj('ActivatedRoute', ['']);

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [
        JobOverviewComponent
      ],
      imports: [
        ChartModule
      ],
      providers: [
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: activatedRouteSpy }
      ]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(JobOverviewComponent);
    component = fixture.componentInstance;
    component.icon = "Test";
    component.jobs = {
      Queued: 10,
      Running: 1000,
      Finished: 10000,
      Canceling: 0,
      Canceled: 0,
      Failed: 10
    };
    component.jobCategory = "test";
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let category = fixture.nativeElement.querySelector('.headline .name').textContent;
    expect(category).toEqual('TEST');
    let icon = fixture.nativeElement.querySelector('.job').textContent;
    expect(icon).toEqual('Test');
    let runningNum = fixture.nativeElement.querySelectorAll('.item-value')[1].textContent;
    expect(runningNum).toEqual('1000');
  });
});
