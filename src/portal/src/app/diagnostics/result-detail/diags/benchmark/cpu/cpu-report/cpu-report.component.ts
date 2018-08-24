import { Component, OnInit, Input, OnDestroy } from '@angular/core';
import { MatTableDataSource } from '@angular/material';
import { ApiService, Loop } from '../../../../../../services/api.service';
import { TableSettingsService } from '../../../../../../services/table-settings.service';
import { TableDataService } from '../../../../../../services/table-data/table-data.service';

@Component({
  selector: 'app-cpu-report',
  templateUrl: './cpu-report.component.html',
  styleUrls: ['./cpu-report.component.scss']
})
export class CpuReportComponent implements OnInit {

  @Input() result: any;

  private dataSource = new MatTableDataSource();
  private currentData = [];
  private lastId = 0;
  private pageSize = 120;
  private loadFinished = false;
  private scrollDirection = 'down';

  private jobId: string;
  private interval: number;
  private tasksLoop: Object;
  private jobState: string;
  private tasks = [];
  private events = [];
  private nodes = [];
  private aggregationResult: any;

  private componentName = "cpuReport";

  private customizableColumns = [
    { name: 'node', displayName: 'Node', displayed: true },
    { name: 'size', displayName: 'Size', displayed: true },
  ];

  constructor(
    private api: ApiService,
    private settings: TableSettingsService,
    private tableDataService: TableDataService
  ) {
    this.interval = 5000;
  }

  ngOnInit() {
    this.jobId = this.result.id;
    if (this.result.aggregationResult !== undefined) {
      this.aggregationResult = this.result.aggregationResult;
    }
    this.tasksLoop = this.getTasksInfo();
  }

  get hasError() {
    return this.aggregationResult !== undefined && this.aggregationResult !== null && this.aggregationResult['Error'] !== undefined;
  }

  getTasksRequest() {
    return this.api.diag.getDiagTasksByPage(this.jobId, this.lastId, this.pageSize);
  }

  getTasksInfo(): any {
    return Loop.start(
      this.getTasksRequest(),
      {
        next: (result) => {
          this.currentData = result;
          this.tableDataService.updateData(result, this.dataSource, 'id');
          if (result.length < this.pageSize && this.scrollDirection == 'down') {
            this.loadFinished = true;
          }
          if (this.jobState == 'Finished' || this.jobState == 'Failed' || this.jobState == 'Canceled') {
            this.getAggregationResult();
          }
          this.getJobInfo();
          return this.getTasksRequest();
        }
      },
      this.interval
    );
  }

  onUpdateLastIdEvent(data) {
    this.lastId = data.lastId;
    this.scrollDirection = data.direction;
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

  getLink(node) {
    let path = [];
    path.push('/resource');
    path.push(node);
    return path;
  }
}
