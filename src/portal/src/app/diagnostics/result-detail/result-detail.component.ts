import { Component, OnInit, ViewChild, ViewContainerRef, ComponentFactoryResolver } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs/Subscription';
import { TestResult } from '../../models/test-result';
import { ApiService } from '../../services/api.service';
import { ServiceRunningTestComponent } from './service-running-test/service-running-test.component';
import { PingTestComponent } from './ping-test/ping-test.component';
import { PingPongTestComponent } from './pingpong-test/pingpong-test.component';

const map = {
  'Service Running Test': ServiceRunningTestComponent,
  'Ping Test': PingTestComponent,
  'test': PingPongTestComponent
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
  ) {}

  ngOnInit() {
    this.subcription = this.route.paramMap.subscribe(map => {
      let id = map.get('id');
      this.api.diag.getDiagJob(id).subscribe(result => {
        this.result = result;
        console.log(this.result);
        this.loadComponent();
      });
    });
  }

  ngOnDestroy() {
    if (this.subcription)
      this.subcription.unsubscribe();
  }

  loadComponent() {
    //Revmoe previously created component.
    this.resultViewRef.remove();

    let comp = map[this.result.diagnosticTest.category];
    let compFactory = this.componentFactoryResolver.resolveComponentFactory(comp);
    let compRef = this.resultViewRef.createComponent(compFactory);
    (compRef.instance as any).result = this.result;
  }
}
