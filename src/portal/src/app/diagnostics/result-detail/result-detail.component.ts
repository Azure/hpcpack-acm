import { Component, OnInit, ViewChild, ViewContainerRef, ComponentFactoryResolver } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs/Subscription';
import { TestResult } from '../../models/test-result';
import { ApiService } from '../../services/api.service';
import { PingPongReportComponent } from './diags/mpi/pingpong/pingpong-report/pingpong-report.component';
import { RingReportComponent } from './diags/mpi/ring/ring-report/ring-report.component';
import { CpuReportComponent } from './diags/benchmark/cpu/cpu-report/cpu-report.component';
import { switchMap } from 'rxjs/operators';

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
      case 'Pingpong': comp = PingPongReportComponent; break;
      case 'Ring': comp = RingReportComponent; break;
      case 'CPU': comp = CpuReportComponent; break;
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
