import { Component, OnInit, Inject } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';

@Component({
  templateUrl: './command-input.component.html',
  styleUrls: ['./command-input.component.scss']
})
export class CommandInputComponent implements OnInit {
  public command: string = '';
  public timeout: number = 1800;

  constructor(
    public dialogRef: MatDialogRef<CommandInputComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    this.command = data.command;
    this.timeout = data.timeout;
  }

  ngOnInit() { }

  runCmd() {
    let params = { command: this.command, timeout: this.timeout };
    this.dialogRef.close(params);
  }

}
