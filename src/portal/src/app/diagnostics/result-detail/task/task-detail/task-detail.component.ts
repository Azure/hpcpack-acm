import { Component, OnInit, Inject } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { ApiService } from '../../../../services/api.service';

@Component({
  selector: 'app-task-detail',
  templateUrl: './task-detail.component.html',
  styleUrls: ['./task-detail.component.css']
})
export class TaskDetailComponent implements OnInit {

  private msg = [];
  private message = {};
  private result = {};

  constructor(
    private api: ApiService,
    public dialogRef: MatDialogRef<TaskDetailComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    // this.msg = this.normalizeMessage(data.msg.Detail);
  }

  ngOnInit() {
    this.api.diag.getDiagTaskResult(this.data.jobId, this.data.taskId).subscribe(result => {
      this.result = result;
    });
  }

  private close() {
    this.dialogRef.close();
  }

}
