import { Component, OnInit, Input, ViewChild } from '@angular/core';
import { MatTableDataSource } from '@angular/material';
import { ApiService } from '../../../services/api.service';


@Component({
  selector: 'app-pingpong-test',
  templateUrl: './pingpong-test.component.html',
  styleUrls: ['./pingpong-test.component.css']
})
export class PingPongTestComponent implements OnInit {
  @Input() result: any;

  @ViewChild('filter')
  private filterInput;

  private dataSource = new MatTableDataSource();
  private displayedColumns = ['node', 'state', 'message', 'primaryTask', 'exited'];

  tasks = [];

  constructor(
    private api: ApiService
  ) { }

  ngOnInit() {
    let id = this.result.id;
    this.api.diag.getDiagTasks(id).subscribe(tasks => {
      this.dataSource.data = tasks;
      this.tasks = tasks;
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
