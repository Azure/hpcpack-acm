import { Component, OnInit, OnDestroy, ViewChild, OnChanges } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { MatTableDataSource, MatPaginator } from '@angular/material';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { SelectionModel } from '@angular/cdk/collections';
import { ApiService, Loop } from '../../services/api.service';
import { DiagEventsComponent } from './diag-events/diag-events.component'

@Component({
  selector: 'diagnostics-results',
  templateUrl: './result-list.component.html',
  styleUrls: ['./result-list.component.css']
})
export class ResultListComponent implements OnInit, OnDestroy, OnChanges {
  @ViewChild(MatPaginator) paginator: MatPaginator;

  private dataSource = new MatTableDataSource();
  private displayedColumns = ['select', 'id', 'test', 'diagnostic', 'category', 'progress', 'state', 'events', 'result', 'actions'];

  private selection = new SelectionModel(true, []);
  private interval: number;
  private diagsLoop: Object;

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private api: ApiService,
    public dialog: MatDialog
  ) {
    this.interval = 5000;
  }

  ngOnInit() {
    this.diagsLoop = this.getDiags();
  }

  ngOnDestroy() {
    if (this.diagsLoop) {
      Loop.stop(this.diagsLoop);
    }
  }

  ngAfterViewInit() {
    this.dataSource.paginator = this.paginator;
  }

  ngOnChanges() {
    this.dataSource.paginator = this.paginator;
  }

  private changePage(e) {
    console.log("pageSize->" + e.pageSize);
    console.log("pageIndex->" + e.pageIndex);
    console.log("pageLength->" + e.length);
  }

  private getDiags(): any {
    return Loop.start(
      this.api.diag.getAll(),
      {
        next: (result) => {
          this.dataSource.data = (result.filter(e => {
            return e.diagnosticTest != undefined && e.name != undefined;
          })).reverse();
          return true;
        }
      },
      this.interval
    );
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

  private getResult(id) {
    this.router.navigate(['/diagnostics/results/' + id]);
  }

  applyFilter(text: string): void {
    this.dataSource.filter = text;
  }

  private showEvents(res) {
    console.log(res);
    this.dialog.open(DiagEventsComponent, {
      width: '98%',
      data: { job: res }
    });
  }
}
