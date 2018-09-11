import { Component, OnInit, Inject } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { ApiService } from '../../../../services/api.service';

@Component({
  selector: 'app-task-detail',
  templateUrl: './task-detail.component.html',
  styleUrls: ['./task-detail.component.scss']
})
export class TaskDetailComponent implements OnInit {

  private msg = [];
  public message: any;
  private result = {};
  public hasResult = true;
  private taskState = "";

  constructor(
    private api: ApiService,
    public dialogRef: MatDialogRef<TaskDetailComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    // this.msg = this.normalizeMessage(data.msg.Detail);
  }

  ngOnInit() {
    this.taskState = this.data.taskState;
    if (this.data.taskState !== "Finished" && this.data.taskState !== "Failed" && this.data.taskState !== "Canceled") {
      this.hasResult = false;
    }
    else {
      this.api.diag.getDiagTaskResult(this.data.jobId, this.data.taskId).subscribe(result => {
        this.result = result;
        this.message = result.message;
      });
    }
  }

  public close() {
    this.dialogRef.close();
  }

}
