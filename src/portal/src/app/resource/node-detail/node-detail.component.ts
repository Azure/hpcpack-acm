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
  styleUrls: ['./node-detail.component.scss']
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

  events = [];
  scheduledEvents = [];

  private subcription: Subscription;

  private historyLoop: object;

  private jobLoop: object;

  private historyInterval: number;

  private jobInterval: number;

  private labels: string[];

  private cpuCoresName = [];

  private nodeInfo = {};

  private nodeRegistrationInfo = {};

  private compute = {};

  constructor(
    private api: ApiService,
    private route: ActivatedRoute,
  ) {
    this.historyInterval = 10000;
    this.jobInterval = 3000;
    this.labels = [];
  }

  ngOnInit() {
    this.subcription = this.route.paramMap.subscribe(map => {
      let id = map.get('id');

      //get node Info
      this.api.node.get(id).subscribe(result => {
        this.nodeInfo = result;
        this.nodeRegistrationInfo = result["nodeRegistrationInfo"];
      });

      //get node metadata
      this.api.node.getMetadata(id).subscribe(result => {
        if (result.compute !== undefined) {
          this.compute = result.compute;
        }
      });

      this.historyLoop = Loop.start(
        this.api.node.getHistoryData(id),
        {
          next: (res) => {
            this.labels = this.makeLabels(res.history);
            let cpuTotal = this.makeCpuTotalData(res.history);
            this.cpuData = { labels: this.labels, datasets: [{ label: 'CPU usage', data: cpuTotal, borderColor: '#215ebb' }] };
            return true;
          }
        },
        this.historyInterval
      );

      this.api.node.getNodeEvents(id).subscribe(result => {
        this.events = result;
      });

      this.api.node.getNodeSheduledEvents(id).subscribe(result => {
        this.scheduledEvents = result.Events;
      });

      this.jobLoop = Loop.start(
        this.api.node.getNodeJobs(id),
        {
          next: (res) => {
            this.dataSource.data = res;
            return true;
          }
        },
        this.jobInterval
      );

    });
  }

  ngOnDestroy() {
    if (this.subcription)
      this.subcription.unsubscribe();

    if (this.historyLoop) {
      Loop.stop(this.historyLoop);
    }

    if (this.jobLoop) {
      Loop.stop(this.jobLoop);
    }
  }

  private setIcon(state) {
    switch (state) {
      case 'Finished': return 'done';
      case 'Queued': return 'blur_linear';
      case 'Failed': return 'clear';
      case 'Running': return 'blur_on';
      case 'Canceled': return 'cancel';
      default: return 'autorenew';
    }
  }

  private dataSource = new MatTableDataSource();
  private displayedColumns = ['id', 'created', 'content', 'type', 'state', 'progress', 'updated'];

  getJobContent(job) {
    if (job.type == 'ClusRun') {
      return job.commandLine;
    }
    else if (job.type == 'Diagnostics') {
      return `${job.diagnosticTest.category} - ${job.diagnosticTest.name}`;
    }
  }

  getLink(jobId, type) {
    let path = [];
    if (type == 'ClusRun') {
      path.push('/command');
      path.push('results');
      path.push(jobId);
    }
    else if (type == "Diagnostics") {
      path.push('/diagnostics');
      path.push('results');
      path.push(jobId);
    }
    return path;
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
}
