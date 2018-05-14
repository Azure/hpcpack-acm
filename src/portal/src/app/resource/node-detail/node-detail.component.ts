import { Component, AfterViewInit, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MatTableDataSource } from '@angular/material';
import { Subscription } from 'rxjs/Subscription';
import { Node } from '../../models/node';
import { ApiService, Loop } from '../../services/api.service';
import { ChartComponent } from 'angular2-chartjs';

@Component({
  selector: 'app-node-detail',
  templateUrl: './node-detail.component.html',
  styleUrls: ['./node-detail.component.css']
})

export class NodeDetailComponent implements OnInit, OnDestroy {
  @ViewChild('cpuChart') cpuChart: ChartComponent;

  nodeProperties: any = {};

  cpuData: any = {};

  cpuOptions = {
    responsive: true,
    maintainAspectRatio: false,
    animation: false,
    legend: {
      display: false,
    },
    scales: {
      yAxes: [{
        ticks: {
          min: 0,
          max: 100,
          stepSize: 20,
        },
        scaleLabel: {
          display: true,
          labelString: 'Percentage'
        }
      }]
    },
    tooltips: {
      mode: 'index',
      intersect: false,
      callbacks: {
        label: function (tooltipItem, data) {
          var label = data.datasets[tooltipItem.datasetIndex].label || '';
          if (label) {
            label += ': ';
          }
          label = label + tooltipItem.yLabel + '%';
          return label;
        }
      }
    },
  };

  networkData: any = {};

  networkOptions = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      yAxes: [{
        ticks: {
          stepSize: 2,
          beginAtZero: true,
        },
        scaleLabel: {
          display: true,
          labelString: 'GB'
        }
      }]
    },
    tooltips: {
      mode: 'index',
      intersect: false,
    },
  };

  diskData: any = {};

  diskOptions = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      yAxes: [{
        ticks: {
          stepSize: 2,
          beginAtZero: true,
        },
        scaleLabel: {
          display: true,
          labelString: 'GB'
        }
      }]
    },
    tooltips: {
      mode: 'index',
      intersect: false,
    },
  };

  gpuData: any = {};

  gpuOptions = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      yAxes: [{
        ticks: {
          min: 0,
          max: 100,
          stepSize: 20,
        },
        scaleLabel: {
          display: true,
          labelString: 'Percentage'
        }
      }]
    },
    tooltips: {
      mode: 'index',
      intersect: false,
    },
  };

  events = [{
    time: new Date(),
    content: 'This is node event, test event by JJ',
    type: 'node event'
  }, {
    time: new Date(),
    content: 'This is node event, test event by JJ',
    type: 'node event'
  }, {
    time: new Date(),
    content: 'This is node event, test event by JJ',
    type: 'node event'
  }, {
    time: new Date(),
    content: 'This is Azure scheduled event, test event by JJ',
    type: 'Azure scheduled event'
  }, {
    time: new Date(),
    content: 'This is node event, test event by JJ',
    type: 'node event'
  }, {
    time: new Date(),
    content: 'This is Azure scheduled event, test event by JJ',
    type: 'Azure scheduled event'
  }];

  // events: MatTableDataSource<any> = new MatTableDataSource();

  // eventColumns = ['id', 'type', 'resourceType', 'resources', 'status', 'notBefore'];

  private subcription: Subscription;

  private historyLoop: object;

  private interval: number;

  private labels: string[];

  private cpuCoresName = [];

  private nodeInfo = {};

  private nodeRegistrationInfo = {};

  // private cpuUsage = [];

  // private colors = ['#3e95cd', '#8e5ea2', '#3cba9f', '#e8c3b9', '#c45850', '#FFA07A', '#DB7093', '#FFA500', '#E6E6FA', '#8FBC8B', '#E0FFFF', '#B0C4DE', '#FFDEAD', '#BC8F8F',
  //   '#A0522D', '#D3D3D3', '#778899', '#F4A460', '#ADD8E6', '#F0E68C'];


  constructor(
    private api: ApiService,
    private route: ActivatedRoute,
  ) {
    this.interval = 10000;
    this.labels = [];
  }

  ngOnInit() {
    this.subcription = this.route.paramMap.subscribe(map => {
      let id = map.get('id');
      this.historyLoop = Loop.start(
        //in-memory-web-api to mock cpu usage history
        // this.api.nodeHistory.getMockData(id),
        this.api.nodeHistory.get(id),
        {
          next: (res) => {
            this.labels = this.makeLabels(res.history);
            this.nodeInfo = res.nodeInfo;
            this.nodeRegistrationInfo = res.nodeInfo.nodeRegistrationInfo;
            let cpuTotal = this.makeCpuTotalData(res.history);
            this.cpuData = { labels: this.labels, datasets: [{ label: 'CPU usage', data: cpuTotal, borderColor: '#215ebb' }] };
            return true;
          }
        },
        this.interval
      );
    });
  }

  ngOnDestroy() {
    if (this.subcription)
      this.subcription.unsubscribe();

    if (this.historyLoop) {
      Loop.stop(this.historyLoop);
    }
  }

  dateFormat(value): string {
    return value >= 10 ? value.toString() : '0' + value;
  }

  //get history lables sort by time
  makeLabels(history): string[] {
    let labels = [];
    history.forEach(v => {
      labels.push(new Date(v.label));
    });
    labels.sort((a, b) => {
      return a - b;
    });
    labels = labels.map(v => {
      return this.dateFormat(v.getHours()) + ':' + this.dateFormat(v.getMinutes()) + ':' + this.dateFormat(v.getSeconds());
    });
    return labels;
  }

  makeCpuTotalData(history): number[] {
    let cpuHistory = [];
    let cpuTotal = [];

    //sort history data by time
    history.sort((a, b) => {
      return (new Date(a.label)).getTime() - (new Date(b.label)).getTime();
    });

    //select recent cpu history data from all categories
    //[{ category: 'cpu', instanceValues:{_Total: xxx, _1: xxx, ...} }]
    history.forEach(h => {
      cpuHistory = cpuHistory.concat(
        h.data.filter(v => {
          return v.category == 'cpu';
        })
      );
    });

    //get cpu data in the format of array
    //[v1, v2, v3, ...]
    cpuHistory.forEach(h => {
      if (h.instanceValues._Total == undefined) {
        cpuTotal.push(NaN);
      }
      else {
        cpuTotal.push(h.instanceValues._Total);
      }
    });

    return cpuTotal;
  }

  /* get every cores' resource usage
   makeCpuUsageData(history): any {
     let cpuHistory = [];


     //sort history data by time
     history.sort((a, b) => {
       return (new Date(a.label)).getTime() - (new Date(b.label)).getTime();
     });

     //select recent cpu history data from all categories
     //[{ category: 'cpu', instanceValues:{_Total: xxx, _1: xxx, ...} }]
     history.forEach(h => {
       cpuHistory = cpuHistory.concat(
         h.data.filter(v => {
           return v.category == 'cpu';
         })
       );
     });

     if (cpuHistory[0].instanceValues && this.cpuCoresName.length == 0) {
       this.cpuCoresName = Object.keys(cpuHistory[0].instanceValues);
     }

     for (let i = 0; i < this.cpuCoresName.length; i++) {
       if (this.cpuUsage.length !== this.cpuCoresName.length) {
         this.cpuUsage.push({ label: this.cpuCoresName[i], data: [], borderColor: this.colors[i] });
       }
       else {
         if (this.cpuUsage[i] !== undefined) {
           this.cpuUsage[i]['data'] = [];
         }
       }
     }

     cpuHistory.forEach(h => {
       for (let key in h.instanceValues) {
         let usageIndex = this.cpuUsage.findIndex((ele) => {
           return ele.label == key;
         });
         if (this.cpuUsage[usageIndex] !== undefined) {
           this.cpuUsage[usageIndex].data.push(h.instanceValues[key]);
         }
       }
     });

     console.log(this.cpuUsage);
     // return cpuUsage;
   }
   */

  // makeNetworkData(usage): void {
  //   let labels = this.makeLabels(usage);
  //   let data1 = usage.map(point => point.inbound.toFixed(2));
  //   let data2 = usage.map(point => point.outbound.toFixed(2));
  //   this.networkData = {
  //     labels: labels,
  //     datasets: [
  //       { label: 'In', data: data1, fill: false, borderColor: '#215ebb' },
  //       { label: 'Out', data: data2, fill: false, borderColor: '#1aab02' }
  //     ]
  //   };
  // }

  // makeDiskData(usage): void {
  //   let labels = this.makeLabels(usage);
  //   let data1 = usage.map(point => point.read.toFixed(2));
  //   let data2 = usage.map(point => point.write.toFixed(2));
  //   this.diskData = {
  //     labels: labels,
  //     datasets: [
  //       { label: 'Read', data: data1, fill: false, borderColor: '#215ebb' },
  //       { label: 'Write', data: data2, fill: false, borderColor: '#1aab02' }
  //     ]
  //   };
  // }
}
