import { Component, OnInit, ViewChild, ViewContainerRef, ComponentFactoryResolver } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs/Subscription';
import { TestResult } from '../../models/test-result';
import { ApiService } from '../../services/api.service';
import { PingPongReportComponent } from './diags/mpi/pingpong/pingpong-report/pingpong-report.component';
import { RingReportComponent } from './diags/mpi/ring/ring-report/ring-report.component';
import { CpuReportComponent } from './diags/benchmark/cpu/cpu-report/cpu-report.component';
import { switchMap } from 'rxjs/operators';

const map = {
  'test': PingPongReportComponent,
  'pingpong': PingPongReportComponent,
  'ring': RingReportComponent,
  'cpu': CpuReportComponent
}

@Component({
  templateUrl: './result-detail.component.html',
  styleUrls: ['./result-detail.component.css']
})
export class ResultDetailComponent implements OnInit {
  @ViewChild('result', { read: ViewContainerRef })
  resultViewRef: ViewContainerRef;

  private result: any;

  private subcription: Subscription;

  constructor(
    private route: ActivatedRoute,
    private api: ApiService,
    private componentFactoryResolver: ComponentFactoryResolver
  ) { }

  ngOnInit() {
    this.subcription = this.route.paramMap
      .pipe(
        switchMap(map => this.api.diag.getDiagJob(map.get('id')))
      )
      .subscribe(result => {
        this.result = result;
        if (result.state == 'Finished' || result.state == 'Failed' || result.state == 'Canceled') {
          this.getAggregationResult();
        }
        else {
          this.loadComponent();
        }
      });
  }

  getAggregationResult() {
    this.api.diag.getJobAggregationResult(this.result.id).subscribe(
      res => {
        this.result.aggregationResult = res;
        this.loadComponent();
      },
      err => {
        let errInfo = err;
        if (ApiService.isJSON(err)) {
          if (err.error) {
            errInfo = err.error;
          }
          else {
            errInfo = JSON.stringify(err);
          }
        }
        this.result.aggregationResult = { Error: errInfo };
        this.loadComponent();
      });
  }

  ngOnDestroy() {
    if (this.subcription) {
      this.subcription.unsubscribe();
    }
  }

  getComponent(name) {
    let comp;
    switch (name) {
      case 'pingpong': comp = PingPongReportComponent; break;
      case 'ring': comp = RingReportComponent; break;
      case 'cpu': comp = CpuReportComponent; break;
      default: comp = CpuReportComponent;
    }
    return comp;

  }

  loadComponent() {
    //Revmoe previously created component.
    this.resultViewRef.remove();
    let comp = this.getComponent(this.result.diagnosticTest.name);
    let compFactory = this.componentFactoryResolver.resolveComponentFactory(comp);
    let compRef = this.resultViewRef.createComponent(compFactory);
    (compRef.instance as any).result = this.result;
  }
}
