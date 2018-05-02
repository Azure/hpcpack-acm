import { Component, OnInit, Inject } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';

@Component({
  templateUrl: './table-option.component.html',
  styleUrls: ['./table-option.component.css']
})
export class TableOptionComponent implements OnInit {

  constructor(@Inject(MAT_DIALOG_DATA) public data: any) { }

  ngOnInit() {
  }

  apply(columns): void {
    console.log(columns);
    let selected = columns.selected.map(e => e.value);
    this.data.availableColumns.forEach(col => {
      col.displayed = selected.indexOf(col.name) >= 0;
    });
  }
}
