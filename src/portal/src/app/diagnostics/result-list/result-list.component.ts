import { Component, OnInit, OnDestroy, ViewChild, HostListener, ElementRef } from '@angular/core';
import { MatTableDataSource, MatPaginator } from '@angular/material';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { SelectionModel } from '@angular/cdk/collections';
import { ApiService, Loop } from '../../services/api.service';
import { TableOptionComponent } from '../../widgets/table-option/table-option.component';
import { TableSettingsService } from '../../services/table-settings.service';
import { DiagEventsComponent } from './diag-events/diag-events.component';

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
  private lastId = Number.MAX_VALUE;
  private currentPageSize = 50;
  private scrolled = false;

  throttle = 300;
  scrollDistance = 1;
  scrollUpDistance = 2;
  loadFinish = false;

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
    // this.diagsLoop = this.getDiags(this.lastId, this.currentPageSize);
    this.getDiags(this.lastId, this.currentPageSize);
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
    //   if (this.diagsLoop) {
    //     Loop.stop(this.diagsLoop);
    //   }
  }

  private getDiags(pageIndex: any, pageSize: any): any {
    this.hasReceivedData = false;
    this.api.diag.getDiagsByPage(pageIndex, pageSize).subscribe(result => {
      if (result.length > 0) {
        this.lastId = result[result.length - 1].id;
      }
      let diags = result.filter(e => {
        return e.diagnosticTest != undefined && e.name != undefined;
      });
      this.hasReceivedData = true;
      if (diags.length == 0) {
        this.loadFinish = true;
        return false;
      }
      this.dataSource.data = this.dataSource.data.concat(diags);
    });

    // return Loop.start(
    // this.api.diag.getDiagsByPage(pageIndex, pageSize),
    // {
    //   next: (result) => {
    //     if (result.length > 0) {
    //       this.lastId = result[result.length - 1].id;
    //     }
    //     let diags = result.filter(e => {
    //       return e.diagnosticTest != undefined && e.name != undefined;
    //     });
    //     this.hasReceivedData = true;
    //     if (diags.length == 0) {
    //       // this.currentPageIndex -= this.currentPageSize;
    //       this.loadFinish = true;
    //       return false;
    //     }

    //     let exsit = this.updateData(diags);
    //     if (!exsit) {
    //       this.dataSource.data = this.dataSource.data.concat(diags);
    //       // this.dataSource.data = diags;
    //     }
    //     return true;
    //   }
    // },
    // this.interval
    // );
  }

  // updateData(data) {
  //   let firstIndex = data[0].id;
  //   let length = data.length;
  //   let index = this.dataSource.data.findIndex(item => {
  //     return item['id'] == firstIndex;
  //   });
  //   if (index == -1) {
  //     return false;
  //   }
  //   let firstPart = this.dataSource.data.slice(0, index);
  //   let lastPart = this.dataSource.data.slice(index + length);
  //   this.dataSource.data = firstPart.concat(data).concat(lastPart);
  //   return true;
  // }

  onScrollDown(ev) {
    if (!this.loadFinish && this.hasReceivedData) {
      //how to decide the last api has returned data and rendered
      // if (this.diagsLoop) {
      //   Loop.stop(this.diagsLoop);
      // }
      // this.diagsLoop = this.getDiags(this.lastId, this.currentPageSize);
      this.getDiags(this.lastId, this.currentPageSize);
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

  onUp(ev) {
    console.log('scrolled up!!', ev);
    // console.log(document.documentElement.scrollTop);
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
