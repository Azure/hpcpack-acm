import { Component, OnInit, Input, OnDestroy } from '@angular/core';
import { MatTableDataSource } from '@angular/material';
import { ApiService, Loop } from '../../../../../../services/api.service';
import { TableSettingsService } from '../../../../../../services/table-settings.service';
import { TableDataService } from '../../../../../../services/table-data/table-data.service';
import { DiagReportService } from '../../../../../../services/diag-report/diag-report.service';

@Component({
  selector: 'app-pingpong-report',
  templateUrl: './pingpong-report.component.html',
  styleUrls: ['./pingpong-report.component.scss']
})
export class PingPongReportComponent implements OnInit {
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
  private failedNodes: any;
  private failedReasons: any;
  public aggregationResult: object;
  private latencyData: any;
  private throughputData: any;
  public goodGroups: Array<any>;
  public badNodes: Array<any>;

  private componentName = "PingPongReport";

  private customizableColumns = [
    { name: 'nodes', displayName: 'Nodes', displayed: true },
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
    this.tasksLoop = this.getTasksInfo();
  }

  updateOverviewData() {
    if (this.aggregationResult !== undefined && this.aggregationResult !== null) {
      this.latencyData = this.aggregationResult['Latency'];
      this.throughputData = this.aggregationResult['Throughput'];
      this.failedNodes = this.aggregationResult['FailedNodes'];
      this.failedReasons = this.aggregationResult['FailedReasons'];
      this.goodGroups = this.aggregationResult['GoodNodesGroups'];
      this.badNodes = this.aggregationResult['BadNodes'];
    }
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
        this.updateOverviewData();
      },
      err => {
        this.aggregationResult = this.diagReportService.getErrorMsg(err);
      });
  }
}
