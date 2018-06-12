import { Component, OnInit, Input, OnDestroy } from '@angular/core';
import { MatTableDataSource } from '@angular/material';
import { ApiService, Loop } from '../../../../../services/api.service';
import { TableSettingsService } from '../../../../../services/table-settings.service';

@Component({
  selector: 'app-pingpong-report',
  templateUrl: './pingpong-report.component.html',
  styleUrls: ['./pingpong-report.component.css']
})
export class PingPongReportComponent implements OnInit {
  @Input() result: any;

  private dataSource = new MatTableDataSource();

  private jobId: string;
  private interval: number;
  private tasksLoop: Object;
  private jobState: string;
  private tasks = [];
  private events = [];

  private componentName = "PingPongReport";

  private customizableColumns = [
    // { name: 'latency', displayName: 'Latency', displayed: true },
    // { name: 'throughput', displayName: 'Throughput', displayed: true },
  ];

  latencyData: any;
  throughputData: any;

  constructor(
    private api: ApiService,
    private settings: TableSettingsService,
  ) {
    this.interval = 5000;
  }

  ngOnInit() {
    this.jobId = this.result.id;
    this.tasksLoop = this.getTasksInfo();
    this.updateOverviewData();
  }

  updateOverviewData() {
    if ((this.result.aggregationResult !== undefined && this.api.diag.isJSON(this.result.aggregationResult))) {
      let res = this.result.aggregationResult;
      this.latencyData = res.Latency;
      this.throughputData = res.Throughput;
    }
  }

  isError() {
    return this.result.aggregationResult != undefined && this.result.aggregationResult.Error !== undefined;
  }

  getTasksInfo(): any {
    return Loop.start(
      this.api.diag.getDiagTasks(this.jobId),
      {
        next: (result) => {
          this.dataSource.data = result;
          this.tasks = result;
          if (this.jobState == 'Finished' || this.jobState == 'Failed' || this.jobState == 'Canceled') {
            return false;
          }
          else {
            this.getJobInfo();
            return true;
          }
        }
      },
      this.interval
    );
  }

  ngOnDestroy() {
    if (this.tasksLoop) {
      Loop.stop(this.tasksLoop);
    }
  }

  getJobInfo() {
    this.api.diag.getDiagJob(this.result.id).subscribe(res => {
      this.jobState = res.state;
      this.result = res;
      if (res.events !== undefined) {
        this.events = res.events;
      }
      this.updateOverviewData();
    });
  }
}
