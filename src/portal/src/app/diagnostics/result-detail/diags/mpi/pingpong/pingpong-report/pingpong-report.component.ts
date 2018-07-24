import { Component, OnInit, Input, OnDestroy } from '@angular/core';
import { MatTableDataSource } from '@angular/material';
import { ApiService, Loop } from '../../../../../../services/api.service';
import { TableSettingsService } from '../../../../../../services/table-settings.service';

@Component({
  selector: 'app-pingpong-report',
  templateUrl: './pingpong-report.component.html',
  styleUrls: ['./pingpong-report.component.scss']
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
  private nodes = [];
  private aggregationResult: object;
  private latencyData: any;
  private throughputData: any;
  private nodesInfo = {};

  private componentName = "PingPongReport";

  private customizableColumns = [
    { name: 'nodes', displayName: 'Nodes', displayed: true },
  ];



  constructor(
    private api: ApiService,
    private settings: TableSettingsService,
  ) {
    this.interval = 5000;
  }

  ngOnInit() {
    this.jobId = this.result.id;
    if (this.result.aggregationResult !== undefined) {
      this.aggregationResult = this.result.aggregationResult;
      this.updateOverviewData();
    }
    this.tasksLoop = this.getTasksInfo();
  }

  updateOverviewData() {
    if (this.aggregationResult !== undefined) {
      this.latencyData = this.aggregationResult['Latency'];
      this.throughputData = this.aggregationResult['Throughput'];
      this.nodesInfo = { GoodNodes: this.aggregationResult['GoodNodes'], BadNodes: this.aggregationResult['BadNodes'] };
    }
  }

  get hasError() {
    return this.aggregationResult !== undefined && this.aggregationResult['Error'] !== undefined;
  }

  getTasksInfo(): any {
    return Loop.start(
      this.api.diag.getDiagTasks(this.jobId),
      {
        next: (result) => {
          this.dataSource.data = result;
          this.tasks = result;
          if (this.jobState == 'Finished' || this.jobState == 'Failed' || this.jobState == 'Canceled') {
            this.getAggregationResult();
          }
          this.getJobInfo();
          return true;
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
      this.nodes = res.targetNodes;
      if (res.events !== undefined) {
        this.events = res.events;
      }
    });
  }

  getAggregationResult() {
    this.api.diag.getJobAggregationResult(this.result.id).subscribe(
      res => {
        this.aggregationResult = res;
        this.updateOverviewData();
      },
      err => {
        let errInfo = err;
        if (ApiService.isJSON(err)) {
          if (err.error) {
            errInfo = err.error;
          }
          else {
            errInfo = JSON.stringify(err);
          }
        }
        this.aggregationResult = { Error: errInfo };
      });
  }
}
