import { Component, OnInit, ViewChild } from '@angular/core';
import { MatDialog } from '@angular/material';
import { TableOptionComponent } from '../../widgets/table-option/table-option.component';
import { ApiService, Loop } from '../../services/api.service';
import { TableSettingsService } from '../../services/table-settings.service';
import { JobStateService } from '../../services/job-state/job-state.service';
import { TableDataService } from '../../services/table-data/table-data.service';
import { CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { VirtualScrollService } from '../../services/virtual-scroll/virtual-scroll.service';

@Component({
  selector: 'app-result-list',
  templateUrl: './result-list.component.html',
  styleUrls: ['./result-list.component.scss']
})
export class ResultListComponent implements OnInit {
  @ViewChild('content') cdkVirtualScrollViewport: CdkVirtualScrollViewport;
  public dataSource = [];

  static customizableColumns = [
    { name: 'createdAt', displayed: true },
    { name: 'command', displayed: true },
    { name: 'state', displayed: true },
    { name: 'progress', displayed: true },
    { name: 'updatedAt', displayed: true },
  ];

  private availableColumns;

  public displayedColumns;

  private lastId = 0;

  private commandLoop: object;
  public maxPageSize = 300;
  private reverse = true;
  public scrolled = false;
  public loadFinished = false;
  private interval = 2000;

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
    private dialog: MatDialog,
    private settings: TableSettingsService,
    private virtualScrollService: VirtualScrollService
  ) { }

  ngOnInit() {
    this.loadSettings();
    this.getDisplayedColumns();

    this.commandLoop = Loop.start(
      this.getCommandRequest(),
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
          return this.getCommandRequest();
        }
      },
      this.interval
    );
  }

  ngOnDestroy() {
    if (this.commandLoop) {
      Loop.stop(this.commandLoop);
    }
  }
  private stateIcon(state) {
    return this.jobStateService.stateIcon(state);
  }

  private stateClass(state) {
    return this.jobStateService.stateClass(state);
  }

  private getCommandRequest() {
    return this.api.command.getJobsByPage({ lastId: this.lastId, count: this.maxPageSize, reverse: this.reverse });
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
