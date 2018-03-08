import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { MatTableDataSource } from '@angular/material';
import { SelectionModel  } from '@angular/cdk/collections';
import { ApiService } from '../../api.service';

@Component({
  selector: 'app-result-list',
  templateUrl: './result-list.component.html',
  styleUrls: ['./result-list.component.css']
})
export class ResultListComponent implements OnInit {

  private dataSource = new MatTableDataSource();
  private displayedColumns = ['select', 'command', 'state', 'progress', 'startedAt', 'updatedAt', 'actions'];

  private selection = new SelectionModel(true, []);

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private api: ApiService
  ) {}

  ngOnInit() {
    this.api.command.getAll().subscribe(results => {
      this.dataSource.data = results;
    });
  }

  private hasNoSelection(): boolean {
    return this.selection.selected.length == 0;
  }

  private isAllSelected() {
    const numSelected = this.selection.selected.length;
    const numRows = this.dataSource.data.length;
    return numSelected == numRows;
  }

  private masterToggle() {
    this.isAllSelected() ?
      this.selection.clear() :
      this.dataSource.data.forEach(row => this.selection.select(row));
  }

  private select(node) {
    this.selection.clear();
    this.selection.toggle(node);
  }
}
