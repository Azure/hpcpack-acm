import { Component, OnInit } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { NodeFilterBuilderComponent } from '../../widgets/node-filter-builder/node-filter-builder.component';

@Component({
  selector: 'app-new',
  templateUrl: './new-command.component.html',
  styleUrls: ['./new-command.component.css']
})
export class NewCommandComponent implements OnInit {
  private command: string = '';

  constructor(public dialog: MatDialogRef<NewCommandComponent>) {}

  ngOnInit() {
  }

  close() {
    this.dialog.close(this.command);
  }
}
