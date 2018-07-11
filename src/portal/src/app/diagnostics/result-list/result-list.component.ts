import { Component, OnInit, OnDestroy, ViewChild, HostListener, ElementRef, ViewChildren } from '@angular/core';
import { MatTableDataSource, MatPaginator } from '@angular/material';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { SelectionModel } from '@angular/cdk/collections';
import { ApiService, Loop } from '../../services/api.service';
import { TableOptionComponent } from '../../widgets/table-option/table-option.component';
import { TableSettingsService } from '../../services/table-settings.service';
import { JobStateService } from '../../services/job-state/job-state.service';

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
  // private displayedColumns = ['select', 'id', 'test', 'diagnostic', 'category', 'progress', 'state', 'actions'];
  private displayedColumns = ['id', 'test', 'diagnostic', 'category', 'progress', 'state', 'createdAt', 'lastChangedAt'];

  private selection = new SelectionModel(true, []);
  private interval: number;
  private diagsLoop: Object;
  private lastId = 0;
  private maxPageSize = 120;
  private derelictSize = this.maxPageSize / 6;
  private updatedSize = this.maxPageSize / 4;
  private reverse = true;
  private currentData = [];
  private rowHeight = -1;
  private scrolled = false;
  private loadFinished = false;

  constructor(
    private api: ApiService,
    private jobStateService: JobStateService,
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
          if (result[0]['id'] < result[result.length - 1]['id']) {
            result = result.reverse();
          }

          this.currentData = result;
          this.updateDataSource(result);

          if (this.rowHeight < 0) {
            if (document.getElementsByTagName('mat-row')[0] !== undefined) {
              this.rowHeight = document.getElementsByTagName('mat-row')[0].clientHeight;
            }

          }
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

  updateDataSource(res) {
    let firstId = res[0]['id'];
    let lastId = res[res.length - 1]['id'];
    let firstIndex = this.dataSource.data.findIndex(item => {
      return item['id'] == firstId;
    });
    let lastIndex = this.dataSource.data.findIndex(item => {
      return item['id'] == lastId;
    });
    let startPart = [];
    let endPart = [];
    if (firstIndex != -1) {
      startPart = this.dataSource.data.slice(0, firstIndex);
    }
    if (lastIndex != -1) {
      endPart = this.dataSource.data.slice(lastIndex + 1);
    }
    if (firstIndex == -1 && lastIndex == -1) {
      this.dataSource.data = this.dataSource.data.concat(res);
    }
    else {
      this.dataSource.data = startPart.concat(res).concat(endPart);
    }
  }

  private scrollToTop() {
    window.scrollTo({ left: 0, top: 0, behavior: 'smooth' });
  }

  private onScrollEvent(data) {
    this.lastId = data.lastId;
    this.loadFinished = data.loadFinished;
    this.scrolled = data.scrolled;
    this.reverse = data.reverse;
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
