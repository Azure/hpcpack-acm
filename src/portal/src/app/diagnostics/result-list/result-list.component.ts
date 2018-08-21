import { Component, OnInit, OnDestroy, ViewChild, HostListener, ElementRef, ViewChildren } from '@angular/core';
import { MatTableDataSource, MatPaginator } from '@angular/material';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { SelectionModel } from '@angular/cdk/collections';
import { ApiService, Loop } from '../../services/api.service';
import { TableOptionComponent } from '../../widgets/table-option/table-option.component';
import { TableSettingsService } from '../../services/table-settings.service';
import { JobStateService } from '../../services/job-state/job-state.service';
import { TableDataService } from '../../services/table-data/table-data.service';

@Component({
  selector: 'diagnostics-results',
  templateUrl: './result-list.component.html',
  styleUrls: ['./result-list.component.scss'],
})
export class ResultListComponent implements OnInit, OnDestroy {
  static customizableColumns = [
    { name: 'createdAt', displayName: 'Created', displayed: true },
    { name: 'test', displayName: 'Test', displayed: true },
    { name: 'diagnostic', displayName: 'Diagnostic', displayed: true },
    { name: 'category', displayName: 'Category', displayed: true },
    { name: 'state', displayName: 'State', displayed: true },
    { name: 'progress', displayName: 'Progress', displayed: true },
    { name: 'lastChangedAt', displayName: 'Last Changed', displayed: true }
  ];

  private availableColumns;

  private dataSource = new MatTableDataSource();
  private displayedColumns = ['id', 'test', 'diagnostic', 'category', 'progress', 'state', 'createdAt', 'lastChangedAt'];

  private selection = new SelectionModel(true, []);
  private interval: number;
  private diagsLoop: Object;
  private lastId = 0;
  private maxPageSize = 120;
  private reverse = true;
  private currentData = [];
  private scrolled = false;
  private loadFinished = false;

  constructor(
    private api: ApiService,
    private jobStateService: JobStateService,
    private tableDataService: TableDataService,
    private settings: TableSettingsService,
    public dialog: MatDialog,
    public el: ElementRef,
  ) {
    this.interval = 2000;
  }

  getDiagRequest() {
    return this.api.diag.getDiagsByPage(this.lastId, this.maxPageSize, this.reverse);
  }

  private stateClass(state) {
    return this.jobStateService.stateClass(state);
  }

  private stateIcon(state) {
    return this.jobStateService.stateIcon(state);
  }

  ngOnInit() {
    this.loadSettings();
    this.getDisplayedColumns();

    this.diagsLoop = Loop.start(
      this.getDiagRequest(),
      {
        next: (result) => {
          if (result.length > 0 && result[0]['id'] <= result[result.length - 1]['id']) {
            result = result.reverse();
          }
          this.currentData = result;
          if (this.reverse && result.length < this.maxPageSize) {
            this.loadFinished = true;
          }
          this.tableDataService.updateData(result, this.dataSource, 'id');
          return this.getDiagRequest();
        }
      },
      this.interval
    );
  }

  ngOnDestroy() {
    if (this.diagsLoop) {
      Loop.stop(this.diagsLoop);
    }
  }

  private onScrollEvent(data) {
    this.lastId = data.dataIndex < 0 ? 0 : this.dataSource.data[data.dataIndex]['id'];
    this.loadFinished = data.loadFinished;
    this.scrolled = data.scrolled;
    this.reverse = data.scrollDirection == 'down' ? true : false;
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

  applyFilter(text: string): void {
    this.dataSource.filter = text;
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
    this.settings.save('DiagList', this.availableColumns);
  }

  loadSettings(): void {
    this.availableColumns = this.settings.load('DiagList', ResultListComponent.customizableColumns);
  }
}
