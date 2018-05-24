import { Component, OnInit, OnDestroy, ViewChild, HostListener, ElementRef, ViewChildren } from '@angular/core';
import { MatTableDataSource, MatPaginator } from '@angular/material';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { SelectionModel } from '@angular/cdk/collections';
import { ApiService, Loop } from '../../services/api.service';
import { TableOptionComponent } from '../../widgets/table-option/table-option.component';
import { TableSettingsService } from '../../services/table-settings.service';
import { DiagEventsComponent } from './diag-events/diag-events.component';
import { NUMBER_TYPE } from '@angular/compiler/src/output/output_ast';

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
  private currentPageSize = 18;
  private scrolled = false;
  private rowHeight = 0;
  public autoScroll = false;

  throttle = 500;
  scrollDistance = 1;
  scrollUpDistance = 2;
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
                if (window.scrollY == this.rowHeight) {
                  this.previousPageLastId = this.currentPageLastId;
                  this.currentPageLastId = this.nextPageLastId;
                  // this.nextPageLastId = result[result.length - 1].id;
                }

                let tableRow = this.el.nativeElement.querySelector(".table-row");
                let table = this.el.nativeElement.querySelector(".diags-table");

                window.scrollTo({ left: 0, top: this.rowHeight, behavior: 'smooth' });
               
                if (table !== null && this.rowHeight == 0) {
                  this.rowHeight += table.getBoundingClientRect().top;
                }
             
                if (tableRow !== null) {
                  this.rowHeight += tableRow.offsetHeight * result.length;
                }   
              }
              this.nextPageLastId = result[result.length - 1].id;
            }
            else if (this.scrollDirection == "up") {
              if (this.previousPageLastId != this.firstItemId + 1) {
                this.nextPageLastId = this.currentPageLastId;
                this.currentPageLastId = this.previousPageLastId;
                this.previousPageLastId = this.previousPageLastId + this.currentPageSize;
              }
              else {
                this.nextPageLastId = Number.MAX_VALUE;
                this.currentPageLastId = Number.MAX_VALUE;
                this.previousPageLastId = Number.MAX_VALUE;
              }

            }

            if (this.scrollDirection == "up") {
              this.loadFinish = false;
            }
            let exsit = this.updateData(result);
            if (!exsit) {
              this.dataSource.data = this.dataSource.data.concat(result);
            }

          }
          return true;
        }
      },
      this.interval
    );
  }

  updateData(data) {
    let firstId = data[0].id;
    let length = data.length;
    let firstIndex = this.dataSource.data.findIndex(item => {
      return item['id'] == firstId;
    });
    if (firstIndex == -1) {
      return false;
    }
    let firstPart = this.dataSource.data.slice(0, firstIndex);
    let lastPart = this.dataSource.data.slice(firstIndex + length);
    this.dataSource.data = firstPart.concat(data).concat(lastPart);
    return true;
  }

  updateTableLoop(pageIndex, pageSize) {
    if (this.diagsLoop) {
      Loop.stop(this.diagsLoop);
    }
    this.diagsLoop = this.getDiags(pageIndex, pageSize);
  }

  onScrollDown(ev) {
    // console.log("window scroll Y: " + window.scrollY);
    // console.log("now row height: " + this.rowHeight);
    // if (window.scrollY !== this.rowHeight) {
    //   return;
    // }
    // console.log("trigger down func.");
    this.scrollDirection = "down";
    if (this.hasReceivedData && !this.loadFinish) {
      // console.log("down down down");
      this.updateTableLoop(this.nextPageLastId, this.currentPageSize);
    }
  }

  onUp() {
    this.scrollDirection = "up";
    // console.log("up up up");
    if (this.hasReceivedData && !this.loadFinish) {
      this.updateTableLoop(this.previousPageLastId, this.currentPageSize);
    }
    else if (this.hasReceivedData && this.loadFinish) {
      // this.nextPageLastId = this.currentPageLastId;
      // this.currentPageLastId = this.previousPageLastId;
      // this.previousPageLastId = this.previousPageLastId + this.currentPageSize;

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
