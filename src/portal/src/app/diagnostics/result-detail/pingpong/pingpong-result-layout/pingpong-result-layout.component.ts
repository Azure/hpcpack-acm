import { Component, OnInit, Input, Output, EventEmitter, ContentChild, TemplateRef, OnChanges, SimpleChange } from '@angular/core';
import { ApiService } from '../../../../services/api.service'

@Component({
  selector: 'app-pingpong-result-layout',
  templateUrl: './pingpong-result-layout.component.html',
  styleUrls: ['./pingpong-result-layout.component.scss']
})
export class PingPongResultLayoutComponent implements OnInit, OnChanges {
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


  latencyData: any;
  throughputData: any;

  ngOnInit() {
    this.updateJobState();
  }

  updateJobState() {
    this.getJobState.emit(this.result.state);

    if ((this.result.aggregationResult !== undefined && this.api.diag.isJSON(this.result.aggregationResult))) {
      let res = this.result.aggregationResult;
      this.latencyData = res.Latency;
      this.throughputData = res.Throughput;
    }
  }



  ngOnChanges(changes: { [propKey: string]: SimpleChange }) {
    this.api.diag.getDiagJob(this.result.id).subscribe(res => {
      this.result = res;
      this.updateJobState();
    });
  }

  title(name, state) {
    let res = name;
    return res = res + ' ' + state;
  }

}
