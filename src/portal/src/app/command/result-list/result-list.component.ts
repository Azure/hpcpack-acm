import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { MatDialog } from '@angular/material';
import { TableOptionComponent } from '../../widgets/table-option/table-option.component';
import { ApiService, Loop } from '../../services/api.service';
import { TableSettingsService } from '../../services/table-settings.service';
import { JobStateService } from '../../services/job-state/job-state.service';
import { TableDataService } from '../../services/table-data/table-data.service';
import { CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { VirtualScrollService } from '../../services/virtual-scroll/virtual-scroll.service';
import { Router, ActivatedRoute } from '@angular/router';
import { ListJob } from '../../models/diagnostics/list-job';

@Component({
  selector: 'app-result-list',
  templateUrl: './result-list.component.html',
  styleUrls: ['./result-list.component.scss']
})
export class ResultListComponent implements OnInit {
  @ViewChild('content') cdkVirtualScrollViewport: CdkVirtualScrollViewport;
  @ViewChild('nodes') nodes: ElementRef;
  public dataSource = [];

  static customizableColumns = [
    { name: 'createdAt', displayed: true, displayName: 'Created' },
    { name: 'nodes', display: true, displayName: 'Nodes' },
    { name: 'command', displayed: true, displayName: 'Command' },
    { name: 'state', displayed: true, displayName: 'State' },
    { name: 'progress', displayed: true, displayName: 'Progress' },
    { name: 'updatedAt', displayed: true, displayName: 'Last Changed' },
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

  public targetNodes: Array<ListJob>;
  public showTargetNodes = false;
  public selectedJobId = -1;
  public windowTitle: string;

  constructor(
    private api: ApiService,
    private router: Router,
    private route: ActivatedRoute,
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
          if (result.length > 0) {
            this.dataSource = this.tableDataService.updateData(result, this.dataSource, 'id');
            if (this.endId != -1 && result[result.length - 1].id != this.endId) {
              this.loading = false;
            }
          }
          if (this.reverse && result.length < this.maxPageSize) {
            this.loadFinished = true;
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
      width: '60%',
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

  goDetailPage(id) {
    this.router.navigate(['.', `${id}`], { relativeTo: this.route });
  }

  getTargetNodes(id, nodes) {
    this.showTargetNodes = true;
    if (this.nodes) {
      this.nodes.nativeElement.scrollTop = 0;
    }
    this.selectedJobId = id;
    this.windowTitle = `${nodes.length} Nodes`;
    this.targetNodes = nodes;
  }

  onShowWnd(condition: boolean) {
    this.showTargetNodes = condition;
    if (!condition) {
      this.selectedJobId = -1;
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
