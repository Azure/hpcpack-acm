import { Component, OnInit, Inject } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';

@Component({
  templateUrl: './table-option.component.html',
  styleUrls: ['./table-option.component.scss']
})
export class TableOptionComponent implements OnInit {
  private options: any[];

  private selected: any[];

  constructor(@Inject(MAT_DIALOG_DATA) public data: any) {
    this.options = data.columns.filter(e => !e.displayed);
    this.selected = data.columns.filter(e => e.displayed);
  }

  ngOnInit() {
  }

  get result() {
    this.options.forEach(e => (e as any).displayed = false);
    this.selected.forEach(e => (e as any).displayed = true);
    return { columns: this.selected.concat(this.options) };
  }
}
