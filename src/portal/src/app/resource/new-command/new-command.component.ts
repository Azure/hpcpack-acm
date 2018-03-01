import { Component, OnInit } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { NodeFilterBuilderComponent } from '../../widgets/node-filter-builder/node-filter-builder.component';

@Component({
  selector: 'app-new',
  templateUrl: './new-command.component.html',
  styleUrls: ['./new-command.component.css']
})
export class NewCommandComponent implements OnInit {
  private nodeFilter: string = '';

  constructor(public dialog: MatDialog) {}

  ngOnInit() {
  }

  private openFilterBuilder(): void {
    let dialogRef = this.dialog.open(NodeFilterBuilderComponent, {
      //width: '250px',
      data: { filter: this.nodeFilter }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result)
        this.nodeFilter = result;
    });
  }
}
