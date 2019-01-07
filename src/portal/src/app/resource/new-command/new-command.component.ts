import { Component, OnInit, NgZone, ViewChild } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { CdkTextareaAutosize } from '@angular/cdk/text-field';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-new',
  templateUrl: './new-command.component.html',
  styleUrls: ['./new-command.component.scss']
})
export class NewCommandComponent implements OnInit {
  public command: string = '';
  public timeout: number = 1800;
  public multiCmds: boolean = false;
  public commandLine: string = 'single';
  @ViewChild('autosize') autosize: CdkTextareaAutosize;

  constructor(
    public dialog: MatDialogRef<NewCommandComponent>,
    private ngZone: NgZone
  ) { }

  ngOnInit() {
  }

  close() {
    let params = { command: this.command, timeout: this.timeout, multiCmds: this.multiCmds };
    this.dialog.close(params);
  }

  triggerResize() {
    // Wait for changes to be applied, then trigger textarea resize.
    this.ngZone.onStable.pipe(take(1))
      .subscribe(() => this.autosize.resizeToFitContent(true));
  }
}
