import { Component, OnInit, ViewChild, ViewContainerRef, ComponentFactoryResolver } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs/Subscription';
import { TestResult } from '../../models/test-result';
import { ApiService } from '../../api.service';
import { ServiceRunningTestComponent } from './service-running-test/service-running-test.component';
import { PingTestComponent } from './ping-test/ping-test.component';

const map = {
  'Service Running Test': ServiceRunningTestComponent,
  'Ping Test': PingTestComponent,
}

@Component({
  templateUrl: './result-detail.component.html',
  styleUrls: ['./result-detail.component.css']
})
export class ResultDetailComponent implements OnInit {
  @ViewChild('result', { read: ViewContainerRef })
  resultViewRef: ViewContainerRef;

  private result: TestResult = {} as TestResult;

  private subcription: Subscription;

  constructor(
    private route: ActivatedRoute,
    private api: ApiService,
    private componentFactoryResolver: ComponentFactoryResolver
  ) {}

  ngOnInit() {
    this.subcription = this.route.paramMap.subscribe(map => {
      let id = map.get('id');
      this.api.test.get(id).subscribe(result => {
        this.result = result;
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

    let comp = map[this.result.testName];
    let compFactory = this.componentFactoryResolver.resolveComponentFactory(comp);
    let compRef = this.resultViewRef.createComponent(compFactory);
    (compRef.instance as any).result = this.result;
  }
}
