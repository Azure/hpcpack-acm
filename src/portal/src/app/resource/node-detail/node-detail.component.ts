import { Component, AfterViewInit, OnDestroy, ViewChild } from '@angular/core';
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

export class NodeDetailComponent implements AfterViewInit, OnDestroy {
  @ViewChild('cpuChart') cpuChart: ChartComponent;
  node: Node = {} as Node;

  nodeProperties: any = {};

  cpuData: any = {};

  cpuOptions = {
    responsive: true,
    maintainAspectRatio: false,
    animation: {
      duration: 1000,
      easing: 'linear'
    },
    legend: {
      display: false,
    },
    scales: {
      yAxes: [{
        ticks: {
          min: 0,
          max: 100,
          stepSize: 25,
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

  events: MatTableDataSource<any> = new MatTableDataSource();

  eventColumns = ['id', 'type', 'resourceType', 'resources', 'status', 'notBefore'];

  private subcription: Subscription;

  private historyLoop: object;

  private interval: number;

  private labels: string[];

  private cpuTotal: number[];

  private hasCpuData: boolean;

  constructor(
    private api: ApiService,
    private route: ActivatedRoute,
  ) {
    this.interval = 10000;
    this.labels = [];
    this.cpuTotal = [];
    this.hasCpuData = false;
  }

  ngAfterViewInit() {
    this.subcription = this.route.paramMap.subscribe(map => {
      let id = map.get('id');
      //this.api.node.get(id).subscribe(node => {
      //this.node = node;
      //this.nodeProperties = node.properties;
      //this.makeCpuData(node.cpuUsage);
      //this.makeNetworkData(node.networkUsage);
      //this.makeDiskData(node.diskUsage);
      //this.events.data = node.events;
      //});

      this.historyLoop = Loop.start(
        //in-memory-web-api to mock cpu usage history
        // this.api.nodeHistory.getMockData(id),
        this.api.nodeHistory.get(id),
        {
          next: (history) => {
            this.labels = this.makeLabels(history);
            this.cpuTotal = this.makeCpuTotalData(history);
            this.cpuData = { labels: this.labels, datasets: [{ data: this.cpuTotal, borderColor: '#215ebb' }] };
            return true;
          }
        },
        this.interval
      );

      // this.api.nodeHistory.get(id).subscribe(history => {
      //   this.makeCpuTotalData(history);
      //   console.log(this.cpuData);
      // })

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
      if (h.instanceValues._Total) {
        cpuTotal.push(h.instanceValues._Total);
      }
      else {
        cpuTotal.push(NaN);
      }
    });

    return cpuTotal;
  }

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
