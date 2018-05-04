import { Component, OnInit } from '@angular/core';
import { MatTableDataSource } from '@angular/material';
import { SelectionModel  } from '@angular/cdk/collections';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-result-list',
  templateUrl: './result-list.component.html',
  styleUrls: ['./result-list.component.css']
})
export class ResultListComponent implements OnInit {

  private dataSource = new MatTableDataSource();

  private displayedColumns = ['select', 'id', 'command', 'state', 'progress', 'actions'];

  private selection = new SelectionModel(true, []);

  private lastId = 0;

  private pageSize = 25;

  private loading = false;

  constructor(
    private api: ApiService
  ) {}

  ngOnInit() {
    this.loadMoreResults();
  }

  private loadMoreResults() {
    this.loading = true;
    this.api.command.getAll({ lastId: this.lastId, count: this.pageSize }).subscribe(results => {
      this.loading = false;
      if (results.length > 0) {
        this.dataSource.data = this.dataSource.data.concat(results);
        this.lastId = results[results.length - 1].id;
      }
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

  private onScroll() {
    this.loadMoreResults();
  }
}
