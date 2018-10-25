import { Component, OnInit } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { NodeFilterBuilderComponent } from '../../widgets/node-filter-builder/node-filter-builder.component';

@Component({
  selector: 'app-new',
  templateUrl: './new-command.component.html',
  styleUrls: ['./new-command.component.scss']
})
export class NewCommandComponent implements OnInit {
  public command: string = '';
  public timeout: number = 1800;
  public multiCmds: boolean = false;

  constructor(public dialog: MatDialogRef<NewCommandComponent>) { }

  ngOnInit() {
  }

  close() {
    let params = { command: this.command, timeout: this.timeout, multiCmds: this.multiCmds };
    this.dialog.close(params);
  }
}
