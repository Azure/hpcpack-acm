import { Component, OnInit, Input, OnDestroy } from '@angular/core';
import { ApiService, Loop } from '../../../../../services/api.service';
import { TableService } from '../../../../../services/table/table.service';
import { DiagReportService } from '../../../../../services/diag-report/diag-report.service';

@Component({
  selector: 'app-general-report',
  templateUrl: './general-report.component.html',
  styleUrls: ['./general-report.component.scss']
})
export class GeneralReportComponent implements OnInit {

  @Input() result: any;

  private dataSource = [];
  private lastId = 0;
  private pageSize = 300;
  public loadFinished = false;

  private jobId: string;
  private interval: number;
  private tasksLoop: Object;
  private jobState: string;
  public tasks = [];
  public events = [];
  public nodes = [];
  public aggregationResult: any;

  public loading = false;
  public empty = true;
  private endId = -1;

  public customizableColumns = [
    { name: 'node', displayed: true },
    { name: 'state', displayed: true },
    { name: 'remark', displayed: true },
    { name: 'detail', displayed: true }
  ];

  constructor(
    private api: ApiService,
    private tableService: TableService,
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
            this.dataSource = this.tableService.updateData(result, this.dataSource, 'id');
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

  getAggregationResult() {
    this.api.diag.getJobAggregationResult(this.result.id).subscribe(
      res => {
        this.aggregationResult = res;
      },
      err => {
        this.aggregationResult = this.diagReportService.getErrorMsg(err);
      });
  }

  getEvents() {
    this.api.diag.getJobEvents(this.result.id).subscribe(res => {
      this.events = res;
    });
  }

  getLink(node) {
    let path = [];
    path.push('/resource');
    path.push(node);
    return path;
  }
}
