import { Component, OnInit, Input, OnDestroy } from '@angular/core';
import { MatTableDataSource } from '@angular/material';
import { ApiService, Loop } from '../../../../../services/api.service';
import { TableSettingsService } from '../../../../../services/table-settings.service';

@Component({
  selector: 'app-ring-report',
  templateUrl: './ring-report.component.html',
  styleUrls: ['./ring-report.component.scss']
})
export class RingReportComponent implements OnInit {
  @Input() result: any;

  private dataSource = new MatTableDataSource();

  private jobId: string;
  private interval: number;
  private tasksLoop: Object;
  private jobState: string;
  private tasks = [];
  private aggregationResult: any;

  private componentName = "RingReport";

  private customizableColumns = [
    // { name: 'latency', displayName: 'Latency', displayed: true },
    // { name: 'throughput', displayName: 'Throughput', displayed: true },
  ];

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
      this.aggregationResult = this.result.aggregationResult;
    }
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
      this.updateOverviewData();
    });
  }
}
