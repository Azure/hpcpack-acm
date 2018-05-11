import { Component, OnInit, Input, ViewChild, OnDestroy, OnChanges, SimpleChange } from '@angular/core';
import { MatTableDataSource } from '@angular/material';
import { ApiService, Loop } from '../../../services/api.service';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { TaskDetailComponent } from '../task-detail/task-detail.component';

@Component({
  selector: 'app-ring-test',
  templateUrl: './ring-test.component.html',
  styleUrls: ['./ring-test.component.css']
})
export class RingTestComponent implements OnInit {

  @Input() result: any;

  @ViewChild('filter')
  private filterInput;

  private dataSource = new MatTableDataSource();
  private displayedColumns = ['nodes', 'state', 'latency', 'throughput', 'detail'];
  // private displayedColumns = ['node', 'state', 'message', 'primaryTask', 'exited'];
  private jobId: string;
  private interval: number;
  private tasksLoop: Object;
  private jobState: string;

  tasks = [];

  constructor(
    private api: ApiService,
    private dialog: MatDialog,
  ) {
    this.interval = 5000;
  }

  ngOnInit() {
    this.jobId = this.result.id;
    this.tasksLoop = this.getTasksInfo();
  }

  getTasksInfo(): any {
    return Loop.start(
      this.api.diag.getDiagTasks(this.jobId),
      {
        next: (result) => {
          this.dataSource.data = result;
          this.tasks = result;
          if (this.jobState == 'Finished') {
            return false;
          }
          return true;
        }
      },
      this.interval
    );
  }

  ngOnChanges(changes: { [propKey: string]: SimpleChange }) {
    console.log("test task loop");
  }



  applyFilter(text: string): void {
    this.dataSource.filter = text;
  }

  filterNodes(state): void {
    this.applyFilter(state);
    this.filterInput.nativeElement.value = state;
  }

  stateIcon(state) {
    console.log(state);
    switch (state) {
      case 'Finished': return 'done';
      case 'Queued': return 'blur_linear';
      case 'Failed': return 'clear';
      case 'Running': return 'blur_on';
      case 'Canceled': return 'cancel';
      default: return 'autonew';
    }
  }

  private showDetail(message) {
    let dialogRef = this.dialog.open(TaskDetailComponent, {
      width: '98%',
      data: { msg: message }
    });
  }

  ngOnDestroy() {
    if (this.tasksLoop) {
      Loop.stop(this.tasksLoop);
    }
  }

  getJobState(state: any) {
    this.jobState = state;
  }

}
