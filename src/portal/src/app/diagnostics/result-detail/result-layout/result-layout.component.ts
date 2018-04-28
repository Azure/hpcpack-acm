import { Component, OnInit, Input, Output, EventEmitter, ContentChild, TemplateRef, OnChanges, SimpleChange } from '@angular/core';
import { ApiService } from '../../../services/api.service'

@Component({
  selector: 'app-result-layout',
  templateUrl: './result-layout.component.html',
  styleUrls: ['./result-layout.component.scss']
})
export class ResultLayoutComponent implements OnInit, OnChanges {
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

  overviewData: any = {};

  activeMode = "total";

  overviewOption = {
    responsive: true,
    maintainAspectRatio: false,
    legend: {
      position: 'top',
    },
    animation: false,
    onClick: (event, item) => {
      if (!item || item.length == 0)
        return;
      let index = item[0]._index;
      let text = index == 0 ? 'success' : 'failure';
      this.filterNodes.emit(text);
    },
    onHover: (event, item) => {
      event.target.style.cursor = item.length == 0 ? 'default' : 'pointer';
    },
  };

  overviewLatencyData: any = {};

  overviewLatencyOption = {
    responsive: true,
    maintainAspectRatio: true,
    scaleOverride: true,
    animation: false,
    legend: {
      display: false
    },
    scales: {
      xAxes: [{
        display: true,
        ticks: {
          beginAtZero: true,   // minimum value will be 0.
        }
      }]
    }
  };

  constructor(
    private api: ApiService
  ) { }

  totalBestPairs = [];
  totalBadPairs = [];
  nodeBestPairs = [];
  nodes = [];
  selectedNode: string;
  average: number;
  median: number;
  passed: boolean;
  standardDeviation: number;

  ngOnInit() {
    this.makeChartData();
    if (this.result.aggregationResult == undefined) {
      this.getJobState.emit(this.result.state);
      this.result.aggregationResult = "Waiting for the aggregation result...";
    }
    else {
      this.overviewLatencyData = {
        labels: this.result.aggregationResult.Latency.Result.Histogram[1],
        datasets: [{
          borderColor: 'rgb(63, 81, 181)',
          borderWidth: 1,
          data: this.result.aggregationResult.Latency.Result.Histogram[0],
          backgroundColor: 'rgba(63, 81, 181, .5)'
        }]
      };

      this.nodes = Object.keys(this.result.aggregationResult.Latency.ResultByNode);
      this.selectedNode = this.nodes[0];
      this.totalBestPairs = this.result.aggregationResult.Latency.Result['Best_pairs']['Pairs'];
      this.totalBadPairs = this.result.aggregationResult.Latency.Result['Bad_pairs'];
      this.average = this.result.aggregationResult.Latency.Result['Average'];
      this.median = this.result.aggregationResult.Latency.Result['Median'];
      this.passed = this.result.aggregationResult.Latency.Result['Passed'];
      this.standardDeviation = this.result.aggregationResult.Latency.Result['Standard_deviation'];
    }

  }

  changeLog = [];
  ngOnChanges(changes: { [propKey: string]: SimpleChange }) {
    console.log("chart changes.");
    this.makeChartData();
    this.api.diag.getDiagJob(this.result.id).subscribe(res => {
      this.result = res;
      this.getJobState.emit(res.state);
      if (this.result.aggregationResult == undefined) {
        this.result.aggregationResult = "Wating for the aggregation result...";
      }
    });
  }

  makeChartData() {
    let finished = 0;
    let failed = 0;
    let queued = 0;
    let running = 0;

    this.tasks.forEach(task => {
      if (task.state == 'Finished')
        finished++;
      else if (task.state == 'Failed')
        failed++;
      else if (task.state == 'Queued')
        queued++;
      else if (task.state == 'Running')
        running++;
    });

    this.overviewData = {
      labels: ['Finished', 'Failed', 'Queued', 'Running'],
      datasets: [{
        data: [finished, failed, queued, running],
        backgroundColor: [
          '#4CAF50',
          '#F44336',
          '#FF9800',
          '#3F51B5',
        ]
      }],
    };
  }

  stateIcon(state) {
    switch (state) {
      case 'Finished': return 'done';
      case 'Queued': return 'blur_linear';
      case 'Failed': return 'clear';
      case 'Running': return 'blur_on';
      case 'Canceled': return 'cancel';
      default: return 'autonew';
    }
  }

  title(name, state) {
    let res = name;
    return res = res + ' ' + state;
  }

  setActiveMode(mode: string) {
    this.activeMode = mode;
  }

}
