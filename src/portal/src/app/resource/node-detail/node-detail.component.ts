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

  // events: MatTableDataSource<any> = new MatTableDataSource();

  // eventColumns = ['id', 'type', 'resourceType', 'resources', 'status', 'notBefore'];

  private subcription: Subscription;

  private historyLoop: object;

  private interval: number;

  private labels: string[];

  private cpuCoresName = [];

  private nodeInfo = {};

  private nodeRegistrationInfo = {};

  private compute = {};

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

      //get node Info
      this.api.node.get(id).subscribe(result => {
        this.nodeInfo = result;
        this.nodeRegistrationInfo = result["nodeRegistrationInfo"];
      });

      //get node metadata
      this.api.node.getMetadata(id).subscribe(result => {
        this.compute = result.compute;
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
        this.interval
      );

      this.api.node.getNodeEvents(id).subscribe(result => {
        this.events = result;
      });

      this.api.node.getNodeSheduledEvents(id).subscribe(result => {
        this.scheduledEvents = result.Events;
        if (this.scheduledEvents.length == 0) {
          //   this.scheduledEvents = [
          //     {
          //       EventId: "f020ba2e-3bc0-4c40-a10b-86575a9eabd5",
          //       EventType: "Freeze",
          //       ResourceType: "VirtualMachine",
          //       Resources: ["FrontEnd_IN_0", "BackEnd_IN_0"],
          //       EventStatus: "Scheduled",
          //       NotBefore: "Mon, 19 Sep 2016 18:29:47 GMT"
          //     },
          //     {
          //       EventId: "f020ba2e-3bc0-4c40-a10b-86575a9eabe7",
          //       EventType: "Reboot",
          //       ResourceType: "VirtualMachine",
          //       Resources: ["FrontEnd_IN_0", "BackEnd_IN_0"],
          //       EventStatus: "Started",
          //       NotBefore: "Mon, 19 Sep 2016 18:29:47 GMT"
          //     },
          //     {
          //       EventId: "f020ba2e-3bc0-4c40-a10b-86575a9eaba9",
          //       EventType: "Redeploy",
          //       ResourceType: "VirtualMachine",
          //       Resources: ["FrontEnd_IN_0", "BackEnd_IN_0"],
          //       EventStatus: "Scheduled",
          //       NotBefore: "Mon, 19 Sep 2016 18:29:47 GMT"
          //     }
          //   ];
        }
      });

      this.api.node.getNodeJobs(id).subscribe(result => {
        let fakeData = result.map((job, index) => {
          if (index % 3 == 0) {
            job.type = "Diagnostic";
          }
          else {
            job.type = "ClusRun";
          }
          job.state = "Finished";
          job.progress = "100%";
          return job;
        });
        this.dataSource.data = fakeData;
      });


    });
  }

  ngOnDestroy() {
    if (this.subcription)
      this.subcription.unsubscribe();

    if (this.historyLoop) {
      Loop.stop(this.historyLoop);
    }
  }


  private dataSource = new MatTableDataSource();
  // private displayedColumns = ['select', 'id', 'test', 'diagnostic', 'category', 'progress', 'state', 'actions'];
  private displayedColumns = ['id', 'content', 'type', 'state', 'progress'];

  getJobContent(type) {
    if (type == 'ClusRun') {
      return "This should show clusrun command";
    }
    else if (type == 'Diagnostic') {
      return "mpi-pingpong or other diagnostics name";
    }
  }

  getLink(jobId, type) {
    let path = [];
    if (type == 'ClusRun') {
      path.push('/command');
      path.push('results');
      path.push(jobId);
    }
    else if (type == "Diagnostic") {
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
