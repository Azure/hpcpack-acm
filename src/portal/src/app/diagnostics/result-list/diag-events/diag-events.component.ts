import { Component, OnInit, Inject } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { DatePipe } from '@angular/common';
import { concat } from 'rxjs/operator/concat';

@Component({
  selector: 'app-diag-events',
  templateUrl: './diag-events.component.html',
  styleUrls: ['./diag-events.component.css']
})
export class DiagEventsComponent implements OnInit {

  private job = {};
  private events = [];
  private message: string;
  private test = [];
  constructor(
    public dialogRef: MatDialogRef<DiagEventsComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    this.job = data.job;
    this.test = data.job.events;
    if (data.job.events == undefined || data.job.events.length == 0) {
      this.message = "There is no event to show!"
    }
    else {
      this.events = data.job.events;
    }
  }



  ngOnInit() {
  }

  private close() {
    this.dialogRef.close();
  }

  private setIcon(type) {
    switch (type) {
      case 'Alert': return "notifications_none";
      default: return "info_outline";
    }
  }
}
