import { Component, OnInit, Input, OnChanges, SimpleChange } from '@angular/core';

@Component({
  selector: 'ring-overview-result',
  templateUrl: './overview-result.component.html',
  styleUrls: ['./overview-result.component.scss']
})
export class RingOverviewResultComponent implements OnInit, OnChanges {
  @Input()
  result: any;

  ngOnChanges(changes: { [propKey: string]: SimpleChange }) {
    this.updateOverviewData();
  }

  constructor() { }
  latency: any;
  latencyThreshold: any;
  latencyUnit: any;
  passed: any;
  throughput: any;
  throughputThreshold: any;
  throughputUnit: any;

  ngOnInit() {
    this.updateOverviewData();
  }

  updateOverviewData() {
    if (this.result !== undefined) {
      this.passed = this.result.Passed;
      let latencyData = this.result.Latency;
      if (latencyData !== undefined) {
        this.latency = latencyData.Value;
        this.latencyThreshold = latencyData.Threshold;
        this.latencyUnit = latencyData.Unit;
      }
      let throughputData = this.result.Throughput;
      if (throughputData !== undefined) {
        this.throughput = this.result.Throughput.Value;
        this.throughputThreshold = this.result.Throughput.Threshold;
        this.throughputUnit = throughputData.Unit;
      }
    }

  }
}


