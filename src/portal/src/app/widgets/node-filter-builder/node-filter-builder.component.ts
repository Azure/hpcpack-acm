import { Component, OnInit, Inject } from '@angular/core';
import {MatDialog, MatDialogRef, MAT_DIALOG_DATA} from '@angular/material';

@Component({
  selector: 'app-node-filter-builder',
  templateUrl: './node-filter-builder.component.html',
  styleUrls: ['./node-filter-builder.component.css']
})
export class NodeFilterBuilderComponent implements OnInit {
  private states = ['online', 'offline'];

  private healths = ['ok', 'error'];

  private groups = ['HeadNode', 'WorkerNode'];

  private filter: string = '';
  private name: string;
  private state: string;
  private health: string;
  private group: string;

  constructor(
    public dialogRef: MatDialogRef<NodeFilterBuilderComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    this.filter = data.filter;
  }

  ngOnInit() {
  }

  build(): string {
    let result = this.name || '';
    if (this.state)
      result += ' state:' + this.state;
    if (this.health)
      result += ' health:' + this.health;
    if (this.group)
      result += ' group:' + this.group;
    return result.trim();
  }
}
