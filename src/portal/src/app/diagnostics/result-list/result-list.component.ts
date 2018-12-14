import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { MatDialog } from '@angular/material';
import { ApiService, Loop } from '../../services/api.service';
import { TableOptionComponent } from '../../widgets/table-option/table-option.component';
import { TableSettingsService } from '../../services/table-settings.service';
import { JobStateService } from '../../services/job-state/job-state.service';
import { TableDataService } from '../../services/table-data/table-data.service';
import { VirtualScrollService } from '../../services/virtual-scroll/virtual-scroll.service';
import { CdkVirtualScrollViewport } from '@angular/cdk/scrolling';

@Component({
  selector: 'diagnostics-results',
  templateUrl: './result-list.component.html',
  styleUrls: ['./result-list.component.scss'],
})
export class ResultListComponent implements OnInit, OnDestroy {
  @ViewChild('content') cdkVirtualScrollViewport: CdkVirtualScrollViewport;

  static customizableColumns = [
    { name: 'createdAt', displayed: true, displayName: 'Created' },
    { name: 'test', displayed: true, displayName: 'Test Name' },
    { name: 'diagnostic', displayed: true, displayName: 'Diagnostic' },
    { name: 'category', displayed: true, displayName: 'Category' },
    { name: 'state', displayed: true, displayName: 'State' },
    { name: 'progress', displayed: true, displayName: 'Progress' },
    { name: 'updatedAt', displayed: true, displayName: 'Last Changed' }
  ];

  private availableColumns;

  public dataSource = [];
  public displayedColumns = [];

  private interval: number;
  private diagsLoop: Object;
  private lastId = 0;
  public maxPageSize = 300;
  private reverse = true;
  public scrolled = false;
  public loadFinished = false;

  pivot = Math.round(this.maxPageSize / 2) - 1;

  startIndex = 0;
  lastScrolled = 0;

  public loading = false;
  public empty = true;
  private endId = -1;


  constructor(
    private api: ApiService,
    private jobStateService: JobStateService,
    private tableDataService: TableDataService,
    private settings: TableSettingsService,
    public dialog: MatDialog,
    private virtualScrollService: VirtualScrollService
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
          this.empty = false;
          if (this.endId != -1 && result[result.length - 1].id != this.endId) {
            this.loading = false;
          }
          if (this.reverse && result.length < this.maxPageSize) {
            this.loadFinished = true;
          }
          if (result.length > 0) {
            this.dataSource = this.tableDataService.updateData(result, this.dataSource, 'id');
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

  trackByFn(index, item) {
    return this.tableDataService.trackByFn(item, this.displayedColumns);
  }

  getColumnOrder(col) {
    let index = this.displayedColumns.findIndex(item => {
      return item == col;
    });

    let order = index + 1;
    if (order) {
      return { 'order': index + 1 };
    }
    else {
      return { 'display': 'none' };
    }
  }


  indexChanged($event) {
    let result = this.virtualScrollService.indexChangedCalc(this.maxPageSize, this.pivot, this.cdkVirtualScrollViewport, this.dataSource, this.lastScrolled, this.startIndex);
    this.pivot = result.pivot;
    this.lastScrolled = result.lastScrolled;
    this.lastId = result.lastId == undefined ? this.lastId : result.lastId;
    this.endId = result.endId == undefined ? this.endId : result.endId;
    this.loading = result.loading;
    this.startIndex = result.startIndex;
    this.scrolled = result.scrolled;
  }
}
