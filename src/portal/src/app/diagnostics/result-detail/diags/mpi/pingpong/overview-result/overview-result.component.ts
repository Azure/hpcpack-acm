import { Component, OnInit, Input, ViewChild, ElementRef, AfterViewInit, ChangeDetectorRef } from '@angular/core';
import { isArray } from 'util';

@Component({
  selector: 'pingpong-overview-result',
  templateUrl: './overview-result.component.html',
  styleUrls: ['./overview-result.component.scss']
})
export class PingPongOverviewResultComponent implements OnInit, AfterViewInit {
  @ViewChild('chart')
  chart: any;

  @Input()
  result: any;

  @Input()
  nodes: any;

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

  resultNodes = [];
  selectedNode: string;

  normal = true;

  constructor(private cd: ChangeDetectorRef) { }

  ngOnInit() {

  }

  ngAfterViewInit() {
    this.showOverview();
    this.cd.detectChanges();
  }

  showOverview() {
    if (this.result != undefined) {
      this.nodeData = this.result.ResultByNode;
      this.resultNodes = Object.keys(this.result.ResultByNode);
      if (!this.selectedNode) {
        this.selectedNode = this.nodes[0];
      }
      if (this.activeMode == 'total') {
        this.overviewResult = this.result.Result;
        this.normal = true;
      }
      else {
        if (this.selectedNode in this.result.ResultByNode) {
          this.overviewResult = this.nodeData[this.selectedNode];
          this.normal = true;
        }
        else {
          this.normal = false;
        }
      }
      this.packetSize = this.result['Packet_size'];
      this.unit = this.result['Unit'];
      this.threshold = this.result['Threshold'];
      if (this.normal) {
        this.updateView(this.overviewResult);
        this.chart.canvas.parentNode.style.height = `${this.overviewResult.Histogram[1].length * 40 + 20}px`;
      }
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
    maintainAspectRatio: false,
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

  updateView(data) {
    // this.chart.canvas.style.height = `${50 * data.Histogram[0].length}px`;
    this.overviewData = {
      labels: data.Histogram[1],
      datasets: [{
        borderColor: 'rgb(63, 81, 181)',
        borderWidth: 1,
        data: data.Histogram[0],
        backgroundColor: 'rgba(63, 81, 181, .7)'
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
    this.showOverview();
  }

  changeNode() {
    this.showOverview();
  }

  hasData(data) {
    if (isArray(data)) {
      return data.length > 0;
    }
    return data !== undefined && data !== null;
  }
}
