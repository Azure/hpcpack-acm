import { Component, OnInit, Input, ViewChild } from '@angular/core';
import { MatTableDataSource } from '@angular/material';
import { Result } from '../../result';

@Component({
  selector: 'app-service-running-test',
  templateUrl: './service-running-test.component.html',
  styleUrls: ['./service-running-test.component.scss']
})
export class ServiceRunningTestComponent implements OnInit {
  @Input() result: Result;

  @ViewChild('filter')
  private filterInput;

  private dataSource = new MatTableDataSource();
  private displayedColumns = ['Node', 'State', 'HPC Management Service', 'HPC MPI Service', 'HPC Node Manager Service', 'HPC SOA Diag Mon Service', 'HPC Monitoring Client Service', 'HPC Broker Service'];

  constructor() { }

  ngOnInit() {
    this.dataSource.data = this.result.nodes.map(node => {
      let res = { 'Node': node.name, 'State': node.state };
      node.details.services.forEach(e => {
        res[e.name] = e.status;
      });
      return res;
    });
  }

  applyFilter(text: string): void {
    this.dataSource.filter = text;
  }

  filterNodes(state): void {
    this.applyFilter(state);
    this.filterInput.nativeElement.value = state;
  }
}
