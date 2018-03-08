import { Component, Input, OnInit, ViewChild } from '@angular/core';
import { MatTableDataSource } from '@angular/material';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { TestResult } from '../../../models/test-result';
import { PingTestNodeResultComponent } from './ping-test-node-result/ping-test-node-result.component';

@Component({
  selector: 'app-ping-test',
  templateUrl: './ping-test.component.html',
  styleUrls: ['./ping-test.component.scss']
})
export class PingTestComponent implements OnInit {
  @Input() result: TestResult;

  @ViewChild('filter')
  private filterInput;

  private dataSource = new MatTableDataSource();
  private displayedColumns = ['Node', 'State', 'Worst', 'Best', 'Average'];

  constructor(public dialog: MatDialog) {}

  ngOnInit() {
    this.dataSource.data = this.result.nodes.map(node => {
      let res = {
        'Node': node.name,
        'State': node.state,
        'Worst': node.worst,
        'Best': node.best,
        'Average': node.average,
        pings: node.details.pings,
      };
      return res;
    });
  }

  applyFilter(text: string): void {
    this.dataSource.filter = text;
  }

  viewDetail(node): void {
    this.dialog.open(PingTestNodeResultComponent, {
      width: '98%',
      data: { node: node }
    });
  }

  filterNodes(state): void {
    this.applyFilter(state);
    this.filterInput.nativeElement.value = state;
  }
}
