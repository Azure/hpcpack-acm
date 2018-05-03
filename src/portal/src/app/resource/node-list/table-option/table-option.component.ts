import { Component, OnInit, Inject } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';

@Component({
  templateUrl: './table-option.component.html',
  styleUrls: ['./table-option.component.css']
})
export class TableOptionComponent implements OnInit {
  private options: string[];

  private selected: string[];

  constructor(@Inject(MAT_DIALOG_DATA) public data: any) {
    this.options = data.availableColumns.filter(e => !e.displayed);
    this.selected = data.availableColumns.filter(e => e.displayed);
  }

  ngOnInit() {
  }

  get result() {
    this.options.forEach(e => (e as any).displayed = false);
    this.selected.forEach(e => (e as any).displayed = true);
    return { options: this.options, selected: this.selected };
  }
}
