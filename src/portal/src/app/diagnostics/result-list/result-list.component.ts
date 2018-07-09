import { Component, OnInit, OnDestroy, ViewChild, HostListener, ElementRef, ViewChildren } from '@angular/core';
import { MatTableDataSource, MatPaginator } from '@angular/material';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { SelectionModel } from '@angular/cdk/collections';
import { ApiService, Loop } from '../../services/api.service';
import { TableOptionComponent } from '../../widgets/table-option/table-option.component';
import { TableSettingsService } from '../../services/table-settings.service';

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
  private lastId = Number.MAX_VALUE;
  private firstItemId = Number.MAX_VALUE;
  private jobIndex = 0;
  private maxPageSize = 200;
  private derelictSize = 80;
  private reverse = true;
  private currentData = [];
  private rowHeight = -1;
  private scrolledHeight = 0;
  private beginningId = Number.MAX_VALUE;
  private scrolled = false;


  scrollDirection = "down";
  hasReceivedData = false;
  loadFinished = false;

  constructor(
    private api: ApiService,
    private settings: TableSettingsService,
    public dialog: MatDialog,
    public el: ElementRef
  ) {
    this.interval = 3000;
  }

  getDiagRequest() {
    return this.api.diag.getDiagsByPage(this.lastId, this.maxPageSize, this.reverse);
  }

  private stateClass(state) {
    switch (state) {
      case 'Finished': return 'finished';
      case 'Queued': return 'queued';
      case 'Failed': return 'failed';
      case 'Running': return 'running';
      case 'Canceled': return 'canceled';
      default: return '';
    }
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
          if (this.lastId == Number.MAX_VALUE) {
            this.beginningId = result[0].id;
          }

          //get new data to hide progress bar
          if (this.firstItemId !== result[0]['id']) {
            this.firstItemId = result[0].id;
            this.hasReceivedData = true;
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
    clearTimeout(this.scrollTimer);
  }

  updateDataSource(res) {
    let firstId = res[0]['id'];
    let lastId = res[res.length - 1]['id'];
    let firstIndex = this.dataSource.data.findIndex(item => {
      return item['id'] == firstId;
    });
    let lastIndex = this.dataSource.data.findIndex(item => {
      return item['id'] === lastId;
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

  private downNum = 0;
  private upNum = 0;
  private scrollTimer = null;
  @HostListener("window:scroll", ['$event'])
  onWndScroll(delay: number = 100) {
    clearTimeout(this.scrollTimer);
    this.scrollTimer = setTimeout(() => {
      const componentPostion = this.el.nativeElement.offsetTop;
      const scrollPostion = window.pageYOffset;

      if (scrollPostion >= componentPostion) {
        this.scrolled = true;
      }
      else {
        this.scrolled = false;
      }

      let pageSize = this.currentData.length;
      let tableSize = this.dataSource.data.length;

      if (this.scrolledHeight > scrollPostion) {
        this.scrollDirection = "up";
        if (this.reverse) {
          this.jobIndex -= Math.floor((this.scrolledHeight - window.pageYOffset) / this.rowHeight);
          this.reverse = false;
          this.jobIndex = pageSize - this.jobIndex;
        }
        else {
          this.jobIndex += Math.floor((this.scrolledHeight - window.pageYOffset) / this.rowHeight);
        }
        while (this.jobIndex >= pageSize / 2 && (pageSize == this.maxPageSize)) {
          this.upNum++;
          if (this.downNum > 0) {
            this.downNum--;
          }
          this.lastId = this.dataSource.data[tableSize - this.derelictSize * this.upNum]['id'];
          this.jobIndex -= this.derelictSize;
          this.hasReceivedData = false;
        }
        if (!this.scrolled) {
          this.lastId = Number.MAX_VALUE;
          this.reverse = true;
          this.jobIndex = 0;
          this.downNum = 0;
          this.upNum = 0;
        }

      }
      else if (this.scrolledHeight <= scrollPostion) {
        this.scrollDirection = "down";
        if (!this.reverse) {
          //At top of window
          if (this.currentData[0]['id'] == this.beginningId) {
            this.lastId = Number.MAX_VALUE;
          }

          this.jobIndex -= Math.floor((window.pageYOffset - this.scrolledHeight) / this.rowHeight);
          this.reverse = true;
          this.jobIndex = pageSize - this.jobIndex;
        }
        else {
          this.jobIndex += Math.floor((window.pageYOffset - this.scrolledHeight) / this.rowHeight);
        }

        while (this.jobIndex >= this.maxPageSize / 2 && (pageSize == this.maxPageSize)) {
          this.downNum++;
          if (this.upNum > 0) {
            this.upNum--;
          }
          this.lastId = this.dataSource.data[this.downNum * this.derelictSize - 1]['id'];
          this.jobIndex -= this.derelictSize;
          this.hasReceivedData = false;
        }

        if (this.currentData.length < this.maxPageSize) {
          this.loadFinished = true;
        }
        //At bottom of window at once 
        if ((window.innerHeight + window.scrollY) >= document.body.offsetHeight) {
          if (this.lastId > this.dataSource.data[tableSize - pageSize]['id']) {
            this.lastId = this.dataSource.data[tableSize - pageSize - 1 + Math.floor(pageSize / 2)]['id'];
            let idIndex = this.dataSource.data.findIndex(item => {
              return item['id'] == this.lastId;
            });
            this.downNum = Math.floor(idIndex / this.derelictSize);
            this.jobIndex = Math.floor(pageSize / 2);
            this.upNum = 0;
          }
        }
      }
      this.scrolledHeight = window.pageYOffset;
    }, delay);
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

  private setIcon(state) {
    switch (state) {
      case 'Finished': return 'done';
      case 'Queued': return 'blur_linear';
      case 'Failed': return 'clear';
      case 'Running': return 'blur_on';
      case 'Canceled': return 'cancel';
      default: return 'autorenew';
    }
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
