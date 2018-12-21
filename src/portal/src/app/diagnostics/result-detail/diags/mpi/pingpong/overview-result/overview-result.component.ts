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

  @Input()
  category: string;

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
  selectedNode: string;
  normal = true;
  overviewOption: any;

  constructor(private cd: ChangeDetectorRef) { }

  ngOnInit() {

  }

  ngAfterViewInit() {
    this.showOverview();
    this.cd.detectChanges();
  }

  isEmpty(obj){
    for(let prop in obj){
      if(obj.hasOwnProperty(prop)){
        return false;
      }
    }
    return true;
  }

  showOverview() {
    if(this.isEmpty(this.result)){
      return;
    }
    if (this.result != undefined) {
      this.nodeData = this.result.ResultByNode;
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
      if (this.normal && this.overviewResult) {
        this.updateView(this.overviewResult);
        this.chart.canvas.parentNode.style.height = `${this.overviewResult.Histogram[1].length * 40 + 20}px`;
      }
      let maxXAexsValue = Math.max.apply(null, this.result.Result.Histogram[0]);
      this.overviewOption = {
        maintainAspectRatio: false,
        animation: false,
        responsive: true,
        legend: {
          display: false,
        },
        scales: {
          xAxes: [{
            display: true,
            ticks: {
              min: 0,
              suggestedMax: maxXAexsValue,
              beginAtZero: true,
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
                return value;
              }
            },
            scaleLabel: {
              display: true,
              labelString: `${this.category} ( ${this.unit} )`
            }
          }]
        },
        tooltips: {
          callbacks: {
            title: (tooltipItem, data) => {
              return `${tooltipItem[0].xLabel} ${this.unit}`;
            }
          }
        }
      };
    }
  }



  updateView(data) {
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
