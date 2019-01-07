import { Component, OnInit, Inject, NgZone, ViewChild } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { CdkTextareaAutosize } from '@angular/cdk/text-field';
import { take } from 'rxjs/operators';

@Component({
  templateUrl: './command-input.component.html',
  styleUrls: ['./command-input.component.scss']
})
export class CommandInputComponent implements OnInit {
  public command: string = '';
  public timeout: number = 1800;
  public isSingleCmd: boolean;
  @ViewChild('autosize') autosize: CdkTextareaAutosize;

  constructor(
    public dialogRef: MatDialogRef<CommandInputComponent>,
    private ngZone: NgZone,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    this.command = data.command;
    this.timeout = data.timeout;
    this.isSingleCmd = data.isSingleCmd;
  }

  ngOnInit() { }

  runCmd() {
    let params = { command: this.command, timeout: this.timeout };
    this.dialogRef.close(params);
  }

  triggerResize() {
    // Wait for changes to be applied, then trigger textarea resize.
    this.ngZone.onStable.pipe(take(1))
      .subscribe(() => this.autosize.resizeToFitContent(true));
  }

}
