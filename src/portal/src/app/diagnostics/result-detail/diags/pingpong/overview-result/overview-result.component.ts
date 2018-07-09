import { Component, OnInit, Input, OnChanges, SimpleChange } from '@angular/core';
import { isArray } from 'util';

@Component({
  selector: 'pingpong-overview-result',
  templateUrl: './overview-result.component.html',
  styleUrls: ['./overview-result.component.scss']
})
export class PingPongOverviewResultComponent implements OnInit, OnChanges {

  @Input()
  result: any;

  ngOnChanges(changes: { [propKey: string]: SimpleChange }) {
    this.showOverview();
  }

  constructor() { }

  ngOnInit() {
    this.showOverview();
  }

  showOverview() {
    if (this.result != undefined) {
      this.nodeData = this.result.ResultByNode;
      this.overviewResult = this.result.Result;
      this.nodes = Object.keys(this.result.ResultByNode);
      this.packetSize = this.result['Packet_size'];
      this.unit = this.result['Unit'];
      this.threshold = this.result['Threshold'];
      this.selectedNode = this.nodes[0];
      this.updateView(this.overviewResult);
      this.overviewOption.scales.yAxes = [{
        display: true,
        ticks: {
          callback: (value, index, values) => {
            return value + ' ' + this.unit;
          }
        }
      }];
    }
  }

  overviewOption = {
    responsive: true,
    maintainAspectRatio: true,
    scaleOverride: true,
    animation: false,
    legend: {
      display: false,
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
      }],
      yAxes: [{
        display: true,
        ticks: {
          callback: (value, index, values) => {
            return value + ' ' + this.unit;
          }
        }
      }]
    }
  };
  activeMode = "total";
  overviewData: any = {};
  bestPairs = [];
  bestPairsValue: number;
  badPairs = [];
  worstPairs = [];
  worstPairsValue: number;
  overviewResult: any;
  unit: any;
  threshold: any;

  average: number;
  median: number;
  passed: boolean;
  packetSize: string;
  standardDeviation: number;
  variability: string;
  overviewThroughputData: any;
  nodeData: any;

  nodes = [];
  selectedNode: string;

  updateView(data) {
    this.overviewData = {
      labels: data.Histogram[1],
      datasets: [{
        borderColor: 'rgb(63, 81, 181)',
        borderWidth: 1,
        data: data.Histogram[0],
        backgroundColor: 'rgba(63, 81, 181, .5)'
      }]
    };

    this.badPairs = data['Bad_pairs'];
    this.bestPairs = data['Best_pairs']['Pairs'];
    this.bestPairsValue = data['Best_pairs']['Value'];
    this.average = data['Average'];
    this.median = data['Median'];
    this.passed = data['Passed'];
    this.worstPairs = data['Worst_pairs']['Pairs'];
    this.worstPairsValue = data['Worst_pairs']['Value'];
    this.standardDeviation = data['Standard_deviation'];
    this.variability = data['Variability'];
  }

  setActiveMode(mode: string) {
    this.activeMode = mode;
    let data;
    if (this.result !== undefined) {
      if (mode == 'node') {
        if (this.nodeData != undefined) {
          data = this.nodeData[this.selectedNode];
        }
      }
      else if (mode == 'total') {
        data = this.overviewResult;
      }
      if (data != undefined) {
        this.updateView(data);
      }
    }
  }

  changeNode() {
    let data = this.nodeData[this.selectedNode];
    this.updateView(data);
  }

  hasData(data) {
    if (isArray(data)) {
      return data.length > 0;
    }
    return data !== undefined && data !== null;
  }
}
