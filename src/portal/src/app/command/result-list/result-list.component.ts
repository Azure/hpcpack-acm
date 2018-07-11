import { Component, OnInit } from '@angular/core';
import { MatTableDataSource, MatDialog } from '@angular/material';
import { SelectionModel } from '@angular/cdk/collections';
import { TableOptionComponent } from '../../widgets/table-option/table-option.component';
import { ApiService } from '../../services/api.service';
import { TableSettingsService } from '../../services/table-settings.service';
import { JobStateService } from '../../services/job-state/job-state.service';

@Component({
  selector: 'app-result-list',
  templateUrl: './result-list.component.html',
  styleUrls: ['./result-list.component.scss']
})
export class ResultListComponent implements OnInit {

  private dataSource = new MatTableDataSource();

  static customizableColumns = [
    { name: 'createdAt', displayName: 'Created', displayed: true },
    { name: 'command', displayName: 'Command', displayed: true },
    { name: 'state', displayName: 'State', displayed: true },
    { name: 'progress', displayName: 'Progress', displayed: true },
    { name: 'lastChangedAt', displayName: 'Last Changed', displayed: true },
  ];

  private availableColumns;

  private displayedColumns;

  private selection = new SelectionModel(true, []);

  private lastId = 0;

  private pageSize = 25;

  private loading = false;

  constructor(
    private api: ApiService,
    private jobStateService: JobStateService,
    private dialog: MatDialog,
    private settings: TableSettingsService,
  ) { }

  ngOnInit() {
    this.loadSettings();
    this.getDisplayedColumns();
    this.loadMoreResults();
  }

  private stateIcon(state) {
    return this.jobStateService.stateIcon(state);
  }


  private loadMoreResults() {
    this.loading = true;
    this.api.command.getAll({ lastId: this.lastId, count: this.pageSize, reverse: true }).subscribe(results => {
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
    console.log("down");
    this.loadMoreResults();
  }

  getDisplayedColumns(): void {
    let columns = this.availableColumns.filter(e => e.displayed).map(e => e.name);
    // columns.push('actions');
    this.displayedColumns = ['id'].concat(columns);
  }

  customizeTable(): void {
    let dialogRef = this.dialog.open(TableOptionComponent, {
      width: '98%',
      data: { columns: this.availableColumns }
    });
    dialogRef.afterClosed().subscribe(res => {
      if (res) {
        this.availableColumns = res.columns;
        this.getDisplayedColumns();
        this.saveSettings();
      }
    });
  }

  saveSettings(): void {
    this.settings.save('CommandList', this.availableColumns);
  }

  loadSettings(): void {
    this.availableColumns = this.settings.load('CommandList', ResultListComponent.customizableColumns);
  }
}
