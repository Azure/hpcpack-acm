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

  private dataSource = [];
  private lastId = 0;
  public pageSize = 300;
  public loadFinished = false;

  private jobId: string;
  private interval: number;
  private tasksLoop: Object;
  private jobState: string;
  public tasks = [];
  public events = [];
  public nodes = [];
  public failedNodes: any;
  public failedReasons: any;
  public aggregationResult: object;
  public latencyData: any;
  public throughputData: any;
  public goodGroups: Array<any>;
  public badNodes: Array<any>;
  public connectivityData: Array<any>;

  public loading = false;
  public empty = true;
  private endId = -1;

  private componentName = "PingPongReport";

  public customizableColumns = [
    { name: 'node', displayed: true },
    { name: 'state', displayed: true },
    { name: 'remark', displayed: true },
    { name: 'detail', displayed: true }
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
    this.jobState = this.result.state;
    if (this.jobFinished) {
      this.getAggregationResult();
    }
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
      this.connectivityData = this.aggregationResult['Connectivity'];
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
          this.empty = false;
          if (this.endId != -1 && result[result.length - 1].id != this.endId) {
            this.loading = false;
          }
          if (result.length < this.pageSize) {
            this.loadFinished = true;
          }
          if (result.length > 0) {
            this.dataSource = this.tableDataService.updateData(result, this.dataSource, 'id');
          }
          if (this.jobFinished) {
            this.getAggregationResult();
          }
          this.getJobInfo();
          this.getEvents();
          return this.getTasksRequest();
        }
      },
      this.interval
    );
  }

  onUpdateLastIdEvent(data) {
    this.lastId = data.lastId;
    this.endId = data.endId;
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
    });
  }

  getEvents() {
    this.api.diag.getJobEvents(this.result.id).subscribe(res => {
      this.events = res;
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
