import { Component } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { MatTableDataSource } from '@angular/material';
import { SelectionModel  } from '@angular/cdk/collections';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'diagnostics-results',
  templateUrl: './result-list.component.html',
  styleUrls: ['./result-list.component.css']
})
export class ResultListComponent {

  private dataSource = new MatTableDataSource();
  private displayedColumns = ['select', 'testName', 'state', 'progress', 'startedAt', 'updatedAt', 'actions'];

  private selection = new SelectionModel(true, []);

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private api: ApiService
  ) {}

  ngOnInit() {
    this.api.test.getAll().subscribe(results => {
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
