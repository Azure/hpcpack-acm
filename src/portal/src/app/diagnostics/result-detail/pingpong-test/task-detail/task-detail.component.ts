import { Component, OnInit, Inject } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';

@Component({
  selector: 'app-task-detail',
  templateUrl: './task-detail.component.html',
  styleUrls: ['./task-detail.component.css']
})
export class TaskDetailComponent implements OnInit {

  private msg = [];

  constructor(
    public dialogRef: MatDialogRef<TaskDetailComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    this.msg = this.normalizeMessage(data.msg.Detail);
  }

  ngOnInit() {
  }

  private normalizeMessage(msg) {
    let data = msg.split('\\n');
    return data;
  }

  private close() {
    this.dialogRef.close();
  }

}
