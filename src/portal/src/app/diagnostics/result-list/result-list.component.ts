import { Component, OnInit, OnDestroy, ViewChild, HostListener, ElementRef, ViewChildren } from '@angular/core';
import { MatTableDataSource, MatPaginator } from '@angular/material';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { SelectionModel } from '@angular/cdk/collections';
import { ApiService, Loop } from '../../services/api.service';
import { TableOptionComponent } from '../../widgets/table-option/table-option.component';
import { TableSettingsService } from '../../services/table-settings.service';
import { DiagEventsComponent } from './diag-events/diag-events.component';
import { NUMBER_TYPE } from '@angular/compiler/src/output/output_ast';
import { last } from 'rxjs/operators';

@Component({
  selector: 'diagnostics-results',
  templateUrl: './result-list.component.html',
  styleUrls: ['./result-list.component.css'],
})
export class ResultListComponent implements OnInit, OnDestroy {
  static customizableColumns = [
    { name: 'test', displayName: 'Test', displayed: true },
    { name: 'diagnostic', displayName: 'Diagnostic', displayed: true },
    { name: 'category', displayName: 'Category', displayed: true },
    { name: 'progress', displayName: 'Progress', displayed: true },
    { name: 'state', displayName: 'State', displayed: true },
  ];

  private availableColumns;

  private dataSource = new MatTableDataSource();
  private displayedColumns = ['select', 'id', 'test', 'diagnostic', 'category', 'progress', 'state', 'actions'];

  private selection = new SelectionModel(true, []);
  private interval: number;
  private diagsLoop: Object;
  private nextPageLastId = Number.MAX_VALUE;
  private currentPageLastId = Number.MAX_VALUE;
  private previousPageLastId = Number.MAX_VALUE;
  private firstItemId = Number.MAX_VALUE;
  private currentPageSize = 30;
  private maxPageSize = 40;
  private scrolled = false;
  private rowHeight = 0;
  public autoScroll = false;

  throttle = 500;
  scrollDistance = 0;
  scrollUpDistance = 1;
  loadFinish = false;
  scrollDirection = "down";

  hasReceivedData = false;

  constructor(
    private api: ApiService,
    private settings: TableSettingsService,
    public dialog: MatDialog,
    public el: ElementRef
  ) {
    this.interval = 5000;
  }

  ngOnInit() {
    // console.log(Math.max(document.documentElement.clientWidth, window.innerWidth || 0));
    // console.log(Math.max(document.documentElement.clientHeight, window.innerHeight || 0));
    this.loadSettings();
    this.getDisplayedColumns();
    this.diagsLoop = this.getDiags(this.nextPageLastId, this.currentPageSize);

    /*configure filter*/
    this.dataSource.filterPredicate = (data: any, filter: string) => {
      return data.name.indexOf(filter) != -1 ||
        data.state.indexOf(filter) != -1 ||
        (data.id).toString() == filter.toString() ||
        data.diagnosticTest.name.indexOf(filter) != -1 ||
        data.diagnosticTest.category.indexOf(filter) != -1;
    };
  }

  ngOnDestroy() {
    if (this.diagsLoop) {
      Loop.stop(this.diagsLoop);
    }
  }

  private getDiags(pageIndex: any, pageSize: any): any {
    this.hasReceivedData = false;

    return Loop.start(
      this.api.diag.getDiagsByPage(pageIndex, pageSize),
      {
        next: (result) => {
          this.hasReceivedData = true;
          if (result.length == 0) {
            this.loadFinish = true;

            this.nextPageLastId = this.currentPageLastId;
            this.currentPageLastId = this.previousPageLastId;
            this.previousPageLastId = this.previousPageLastId + this.currentPageSize;

            this.updateTableLoop(this.nextPageLastId, this.currentPageSize);
            return false;
          }
          else {
            if (this.nextPageLastId == Number.MAX_VALUE) {
              this.firstItemId = result[0].id;
            }
            if (this.scrollDirection == "down") {
              if (this.nextPageLastId !== result[result.length - 1].id) {
                this.previousPageLastId = this.currentPageLastId;
                this.currentPageLastId = this.nextPageLastId;
              }
              this.nextPageLastId = result[result.length - 1].id;
            }
            else if (this.scrollDirection == "up") {
              if (this.currentPageLastId !== result[0].id + 1) {
                this.nextPageLastId = this.currentPageLastId;
                this.currentPageLastId = result[0].id + 1;
                this.previousPageLastId = this.currentPageLastId + this.currentPageSize;
              }
            }

            if (this.scrollDirection == "up") {
              this.loadFinish = false;
            }
            this.updateData(result);
            if (result.length < this.currentPageSize) {
              this.loadFinish = true;
            }
          }
          return true;
        }
      },
      this.interval
    );
  }

  updateData(data) {
    if (this.dataSource.data.length == 0) {
      this.dataSource.data = data;
      return;
    }
    let currentLength = this.dataSource.data.length;
    let firstId = data[0].id;
    let currentStartId = this.dataSource.data[0]['id'];
    let currentEndId = this.dataSource.data[currentLength - 1]['id'];

    let length = data.length;
    let lastId = data[length - 1].id;
    let firstIndex = this.dataSource.data.findIndex(item => {
      return item['id'] == firstId;
    });
    let lastIndex = this.dataSource.data.findIndex(item => {
      return item['id'] == lastId;
    });
    if (firstIndex == -1 && lastIndex == -1) {
      if (firstId < currentEndId) {
        this.dataSource.data = this.dataSource.data.concat(data);
      }
      else if (lastId > currentStartId) {
        this.dataSource.data = data.concat(this.dataSource.data);
      }
    }
    else if (firstIndex == -1) {
      let backPart = this.dataSource.data.slice(lastIndex + 1);
      this.dataSource.data = data.concat(backPart);
    }
    else if (lastIndex == -1) {
      let frontPart = this.dataSource.data.slice(0, firstIndex);
      this.dataSource.data = frontPart.concat(data);
    }
    else {
      let frontPart = this.dataSource.data.slice(0, firstIndex);
      let backPart = this.dataSource.data.slice(lastIndex + 1);
      this.dataSource.data = frontPart.concat(data).concat(backPart);
    }

    if (this.dataSource.data.length > this.maxPageSize) {
      if (this.scrollDirection == "down") {
        let startIndex = this.dataSource.data.length - this.maxPageSize;
        this.dataSource.data = this.dataSource.data.slice(startIndex);
        window.scrollTo({ left: 0, top: window.scrollY / 3 * 2, behavior: 'smooth' });
      }
      else if (this.scrollDirection == "up") {
        this.dataSource.data = this.dataSource.data.slice(0, this.maxPageSize);
        var h = Math.max(document.documentElement.clientHeight, window.innerHeight || 0);
        window.scrollTo({ left: 0, top: h / 4, behavior: 'smooth' });
      }

    }
    // console.log(window.scrollY);
    return true;
  }

  updateTableLoop(pageIndex, pageSize) {
    if (this.diagsLoop) {
      Loop.stop(this.diagsLoop);
    }
    this.diagsLoop = this.getDiags(pageIndex, pageSize);
  }

  onScrollDown(ev) {
    this.scrollDirection = "down";
    if (this.hasReceivedData && !this.loadFinish) {
      this.updateTableLoop(this.nextPageLastId, this.currentPageSize);
    }
  }

  onUp() {
    this.scrollDirection = "up";
    if (this.hasReceivedData && !this.loadFinish) {
      this.updateTableLoop(this.previousPageLastId, this.currentPageSize);
    }
    else if (this.hasReceivedData && this.loadFinish) {
      this.updateTableLoop(this.previousPageLastId, this.currentPageSize);
    }
  }

  private scrollToTop() {
    window.scrollTo({ left: 0, top: 0, behavior: 'smooth' });
  }

  private scrollTimer = null;
  @HostListener("window:scroll", ['$event']) onWndScroll(delay: number = 300) {
    clearTimeout(this.scrollTimer);
    let self = this;
    this.scrollTimer = setTimeout(() => {
      const componentPostion = this.el.nativeElement.offsetTop;
      const scrollPostion = window.pageYOffset;

      if (scrollPostion >= componentPostion) {
        this.scrolled = true;
      }
      else {
        this.scrolled = false;
      }
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
      default: return 'autonew';
    }
  }

  applyFilter(text: string): void {
    this.dataSource.filter = text;
  }

  private showEvents(res) {
    this.dialog.open(DiagEventsComponent, {
      width: '98%',
      data: { job: res }
    });
  }

  getDisplayedColumns(): void {
    let columns = this.availableColumns.filter(e => e.displayed).map(e => e.name);
    columns.push('actions');
    this.displayedColumns = ['select', 'id'].concat(columns);
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
