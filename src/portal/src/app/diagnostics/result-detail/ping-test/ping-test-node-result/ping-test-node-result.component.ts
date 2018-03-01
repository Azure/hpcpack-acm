import { Component, OnInit, Inject } from '@angular/core';
import { MatTableDataSource, MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';

@Component({
  selector: 'app-ping-test-node-result',
  templateUrl: './ping-test-node-result.component.html',
  styleUrls: ['./ping-test-node-result.component.css']
})
export class PingTestNodeResultComponent implements OnInit {
  private dataSource = new MatTableDataSource();
  private displayedColumns = [
    'destination',
    'ip',
    'result',
    'average',
    'best',
    'worst',
    'successful',
    'failed',
  ];

  private node = {};

  constructor(
    public dialogRef: MatDialogRef<PingTestNodeResultComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    this.node = data.node;
    this.dataSource.data = (this.node as any).pings;
  }

  ngOnInit() {
  }

  applyFilter(text: string): void {
    this.dataSource.filter = text;
  }
}
