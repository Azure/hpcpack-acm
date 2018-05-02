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

  activeLatencyMode = "total";
  activeThroughputMode = "total";

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

  latencyData: any = {};
  throughputData: any = {};

  latencyOption = {
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
          min: 0,
          beginAtZero: true,   // minimum value will be 0.
          callback: function (value, index, values) {
            if (Math.floor(value) === value) {
              return value;
            }
          }
        }
      }]
    }
  };
  throughputOption = {
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
          min: 0,
          beginAtZero: true,   // minimum value will be 0.
          callback: function (value, index, values) {
            if (Math.floor(value) === value) {
              return value;
            }
          }
        }
      }]
    }
  };

  constructor(
    private api: ApiService
  ) { }

  latencyBestPairs = [];
  latencyBestPairsValue: number;
  latencyBadPairs = [];
  latencyWorstPairs = [];
  latencyWorstPairsValue: number;
  overviewLatencyResult: any;

  latencyAverage: number;
  latencyMedian: number;
  latencyPassed: boolean;
  latencyPacketSize: string;
  latencyStandardDeviation: number;
  latencyVariability: string;
  overviewLatencyData: any;
  nodeLatencyData: any;

  throughputBestPairs = [];
  throughputBestPairsValue: number;
  throughputBadPairs = [];
  throughputWorstPairs = [];
  throughputWorstPairsValue: number;
  overviewThroughputResult: any;

  throughputAverage: number;
  throughputMedian: number;
  throughputPassed: boolean;
  throughputPacketSize: string;
  throughputStandardDeviation: number;
  throughputVariability: string;
  overviewThroughputData: any;
  nodeThroughputData: any;

  nodes = [];
  selectedNode: string;

  ngOnInit() {
    this.updateAggregationResult();
  }

  updateAggregationResult() {
    this.makeChartData();
    this.getJobState.emit(this.result.state);
    if (this.result.aggregationResult == undefined) {
      this.result.aggregationResult = "Waiting for the aggregation result...";
    }
    else {
      let res = this.result.aggregationResult;
      this.getAggregationResult(res);
    }
  }

  updateLatencyView(data) {
    this.overviewLatencyData = {
      labels: data.Histogram[1],
      datasets: [{
        borderColor: 'rgb(63, 81, 181)',
        borderWidth: 1,
        data: data.Histogram[0],
        backgroundColor: 'rgba(63, 81, 181, .5)'
      }]
    };
    this.latencyData = this.overviewLatencyData;

    this.latencyBadPairs = data['Bad_pairs'];
    this.latencyBestPairs = data['Best_pairs']['Pairs'];
    this.latencyBestPairsValue = data['Best_pairs']['Value'];
    this.latencyAverage = data['Average'];
    this.latencyMedian = data['Median'];
    this.latencyPassed = data['Passed'];
    this.latencyWorstPairs = data['Worst_pairs']['Pairs'];
    this.latencyWorstPairsValue = data['Worst_pairs']['Value'];
    this.latencyStandardDeviation = data['Standard_deviation'];
    this.latencyVariability = data['Variability'];
  }

  updateThroughputView(data) {
    this.overviewThroughputData = {
      labels: data.Histogram[1],
      datasets: [{
        borderColor: 'rgb(63, 81, 181)',
        borderWidth: 1,
        data: data.Histogram[0],
        backgroundColor: 'rgba(63, 81, 181, .5)'
      }]
    };
    this.throughputData = this.overviewThroughputData;

    this.throughputBadPairs = data['Bad_pairs'];
    this.throughputBestPairs = data['Best_pairs']['Pairs'];
    this.throughputBestPairsValue = data['Best_pairs']['Value'];
    this.throughputAverage = data['Average'];
    this.throughputMedian = data['Median'];
    this.throughputPassed = data['Passed'];
    this.throughputWorstPairs = data['Worst_pairs']['Pairs'];
    this.throughputWorstPairsValue = data['Worst_pairs']['Value'];
    this.throughputStandardDeviation = data['Standard_deviation'];
    this.throughputVariability = data['Variability'];
  }

  getAggregationResult(result) {
    this.nodes = Object.keys(result.Latency.ResultByNode);
    this.nodeLatencyData = result.Latency.ResultByNode;
    this.nodeThroughputData = result.Throughput.ResultByNode;
    this.latencyPacketSize = result.Latency['Packet_size'];
    this.throughputPacketSize = result.Throughput['Packet_size'];

    this.selectedNode = this.nodes[0];
    this.overviewLatencyResult = result.Latency.Result;
    this.overviewThroughputResult = result.Throughput.Result;
    this.updateLatencyView(this.overviewLatencyResult);
    this.updateThroughputView(this.overviewThroughputResult);
  }


  changeLog = [];
  ngOnChanges(changes: { [propKey: string]: SimpleChange }) {
    this.api.diag.getDiagJob(this.result.id).subscribe(res => {
      this.result = res;
      this.updateAggregationResult();
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

  setLatencyActiveMode(mode: string) {
    this.activeLatencyMode = mode;
    let data;
    if (mode == 'node') {
      data = this.nodeLatencyData[this.selectedNode];
    }
    else if (mode == 'total') {
      data = this.overviewLatencyResult;
    }
    this.updateLatencyView(data);
  }

  setThroughputActiveMode(mode: string) {
    this.activeThroughputMode = mode;
    let data;
    if (mode == 'node') {
      data = this.nodeThroughputData[this.selectedNode];
    }
    else if (mode == 'total') {
      data = this.overviewThroughputResult;
    }
    this.updateThroughputView(data);
  }

  changeLatencyNode() {
    let data = this.nodeLatencyData[this.selectedNode];
    this.updateLatencyView(data);
  }

  changeThroughputNode() {
    let data = this.nodeLatencyData[this.selectedNode];
    this.updateThroughputView(data);
  }

}
