import { Component, OnInit, Input, ChangeDetectorRef, ViewChild } from '@angular/core';

@Component({
  selector: 'mpi-performance',
  templateUrl: './performance.component.html',
  styleUrls: ['./performance.component.scss']
})
export class PerformanceComponent implements OnInit {
  @Input()
  result: any;

  @ViewChild('throughputChart')
  throughputChart: any;

  @ViewChild('latencyChart')
  latencyChart: any;

  latency;
  latencyData: Array<string> = [];

  latencyOption: any;
  throughput;
  throughputData: Array<string> = [];

  throughputOption: any;
  packageSize: Array<string> = [];


  constructor(private cd: ChangeDetectorRef) {
  }

  ngOnInit() { }

  ngAfterViewInit() {
    this.getChartData(this.result.Result);
    this.cd.detectChanges();
  }

  getChartData(res) {
    let throughput_unit;
    let latency_unit;
    let packageSize_unit;
    for (let i = 0; i < res.length; i++) {
      this.latencyData.push(res[i].Latency.value);
      this.throughputData.push(res[i].Throughput.value);
      this.packageSize.push(res[i].Message_Size.value);
      if (!latency_unit) {
        latency_unit = res[i].Latency.unit;
      }
      if (!throughput_unit) {
        throughput_unit = res[i].Throughput.unit;
      }
      if (!packageSize_unit) {
        packageSize_unit = res[i].Message_Size.unit;
      }
    }
    this.latency = {
      labels: this.packageSize,
      datasets: [{
        borderColor: 'rgb(63, 81, 181)',
        borderWidth: 1,
        data: this.latencyData,
        fill: false
      }]
    };

    this.throughput = {
      labels: this.packageSize,
      datasets: [{
        borderColor: 'rgb(63, 81, 181)',
        borderWidth: 1,
        data: this.throughputData,
        fill: false
      }]
    };
    this.throughputChart.canvas.parentNode.style.height = `35vh`;
    this.latencyChart.canvas.parentNode.style.height = `35vh`;
    this.throughputOption = {
      maintainAspectRatio: false,
      responsive: true,
      legend: {
        display: false,
      },
      scales: {
        xAxes: [{
          display: true,
          ticks: {
            min: 0,
            beginAtZero: true,
            callback: function (value, index, values) {
              return `${value}`;
            }
          },
          scaleLabel: {
            display: true,
            labelString: `Package Size ( ${packageSize_unit} )`
          }
        }],
        yAxes: [{
          display: true,
          ticks: {
            callback: (value, index, values) => {
              return `${value}`;
            }
          },
          scaleLabel: {
            display: true,
            labelString: `Throughput ( ${throughput_unit} )`
          }
        }]
      },
      tooltips: {
        callbacks: {
          label: function (tooltipItem, data) {
            return `${tooltipItem.yLabel}  ${throughput_unit}`;
          },
          title: function (tooltipItem, data) {
            return `${tooltipItem[0].xLabel} ${packageSize_unit}`;
          }
        }
      }
    };

    this.latencyOption = {
      maintainAspectRatio: false,
      responsive: true,
      legend: {
        display: false,
      },
      scales: {
        xAxes: [{
          display: true,
          ticks: {
            min: 0,
            beginAtZero: true,
            callback: function (value, index, values) {
              return `${value}`;
            }
          },
          scaleLabel: {
            display: true,
            labelString: `Package Size ( ${packageSize_unit} )`
          }
        }],
        yAxes: [{
          display: true,
          ticks: {
            callback: (value, index, values) => {
              return `${value}`;
            }
          },
          scaleLabel: {
            display: true,
            labelString: `Latency ( ${latency_unit} )`
          }
        }]
      },
      tooltips: {
        callbacks: {
          label: function (tooltipItem, data) {
            return `${tooltipItem.yLabel}  ${latency_unit}`;
          },
          title: function (tooltipItem, data) {
            return `${tooltipItem[0].xLabel} ${packageSize_unit}`;
          }
        }
      }
    };
  }

}
