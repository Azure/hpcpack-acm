import { Component, OnInit } from '@angular/core';
import { MatTableDataSource, MatDialog } from '@angular/material';
import { SelectionModel } from '@angular/cdk/collections';
import { TableOptionComponent } from '../../widgets/table-option/table-option.component';
import { ApiService, Loop } from '../../services/api.service';
import { TableSettingsService } from '../../services/table-settings.service';
import { JobStateService } from '../../services/job-state/job-state.service';
import { TableDataService } from '../../services/table-data/table-data.service';

@Component({
  selector: 'app-result-list',
  templateUrl: './result-list.component.html',
  styleUrls: ['./result-list.component.scss']
})
export class ResultListComponent implements OnInit {

  public dataSource = new MatTableDataSource();

  static customizableColumns = [
    { name: 'createdAt', displayName: 'Created', displayed: true },
    { name: 'command', displayName: 'Command', displayed: true },
    { name: 'state', displayName: 'State', displayed: true },
    { name: 'progress', displayName: 'Progress', displayed: true },
    { name: 'lastChangedAt', displayName: 'Last Changed', displayed: true },
  ];

  private availableColumns;

  public displayedColumns;

  private selection = new SelectionModel(true, []);

  private lastId = 0;

  private commandLoop: object;
  public maxPageSize = 120;
  private reverse = true;
  public currentData = [];
  public scrolled = false;
  public loadFinished = false;
  private interval = 2000;

  private loading = false;

  constructor(
    private api: ApiService,
    private jobStateService: JobStateService,
    private tableDataService: TableDataService,
    private dialog: MatDialog,
    private settings: TableSettingsService,
  ) { }

  ngOnInit() {
    this.loadSettings();
    this.getDisplayedColumns();

    this.commandLoop = Loop.start(
      this.getCommandRequest(),
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

  public onScrollEvent(data) {
    this.lastId = data.dataIndex == -1 ? 0 : this.dataSource.data[data.dataIndex]['id'];
    this.loadFinished = data.loadFinished;
    this.scrolled = data.scrolled;
    this.reverse = data.scrollDirection == 'down' ? true : false;
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
}
