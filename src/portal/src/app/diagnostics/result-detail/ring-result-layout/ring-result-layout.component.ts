import { Component, OnInit, Input, Output, EventEmitter, ContentChild, TemplateRef, OnChanges, SimpleChange } from '@angular/core';
import { ApiService } from '../../../services/api.service'

@Component({
  selector: 'app-ring-result-layout',
  templateUrl: './ring-result-layout.component.html',
  styleUrls: ['./ring-result-layout.component.scss']
})
export class RingResultLayoutComponent implements OnInit, OnChanges {
  @Input()
  result: any;

  @Input()
  tasks: any;

  @Output()
  filterNodes: EventEmitter<any> = new EventEmitter();

  @Output()
  getJobState: EventEmitter<any> = new EventEmitter();

  @ContentChild('nodes')
  nodesTemplate: TemplateRef<any>;

  constructor(
    private api: ApiService
  ) { }


  latency: any;
  latencyThreshold: any;
  latencyUnit: any;

  throughput:any;
  throughputThreshold: any;
  aggregationResult = {};

  ngOnInit() {
    this.updateJobState();
  }

  updateJobState() {
    this.getJobState.emit(this.result.state);
  }

  changeLog = [];
  ngOnChanges(changes: { [propKey: string]: SimpleChange }) {
    this.api.diag.getDiagJob(this.result.id).subscribe(res => {
      this.result = res;
      this.updateJobState();
    });
  }
}
