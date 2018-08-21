import { Component, OnInit, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
@Component({
  selector: 'app-task-error',
  templateUrl: './task-error.component.html',
  styleUrls: ['./task-error.component.scss']
})
export class TaskErrorComponent implements OnInit {

  constructor(
    public dialogRef: MatDialogRef<TaskErrorComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) { }

  ngOnInit() {
  }

  private close() {
    this.dialogRef.close();
  }
}
