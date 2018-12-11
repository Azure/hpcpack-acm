import { Component, OnInit, Input, OnDestroy } from '@angular/core';
import { MatTableDataSource } from '@angular/material';
import { ApiService, Loop } from '../../../../../../services/api.service';
import { TableSettingsService } from '../../../../../../services/table-settings.service';
import { TableDataService } from '../../../../../../services/table-data/table-data.service';
import { DiagReportService } from '../../../../../../services/diag-report/diag-report.service';

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
  public aggregationResult: any;

  private componentName = "cpuReport";

  private customizableColumns = [
    { name: 'node', displayName: 'Node', displayed: true }
  ];

  constructor(
    private api: ApiService,
    private settings: TableSettingsService,
    private tableDataService: TableDataService,
    private diagReportService: DiagReportService
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
    return this.diagReportService.hasError(this.aggregationResult);
  }

  get jobFinished() {
    return this.diagReportService.jobFinished(this.jobState);
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
          this.tableDataService.updateDatasource(result, this.dataSource, 'id');
          if (result.length < this.pageSize && this.scrollDirection == 'down') {
            this.loadFinished = true;
          }
          if (this.jobFinished) {
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
        this.aggregationResult = this.diagReportService.getErrorMsg(err);
      });
  }

  getLink(node) {
    let path = [];
    path.push('/resource');
    path.push(node);
    return path;
  }
}
