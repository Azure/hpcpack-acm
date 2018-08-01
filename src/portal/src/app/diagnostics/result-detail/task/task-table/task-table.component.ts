import { Component, OnInit, ViewChild, Input, Output, EventEmitter } from '@angular/core';
import { TableOptionComponent } from '../../../../widgets/table-option/table-option.component';
import { TableSettingsService } from '../../../../services/table-settings.service';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { MatTableDataSource } from '@angular/material';
import { TaskDetailComponent } from '../task-detail/task-detail.component';
import { JobStateService } from '../../../../services/job-state/job-state.service';

@Component({
  selector: 'diag-task-table',
  templateUrl: './task-table.component.html',
  styleUrls: ['./task-table.component.scss']
})
export class TaskTableComponent implements OnInit {
  @ViewChild('filter')
  private filterInput;

  @Input()
  tableName: string;

  @Input()
  customizableColumns: any;

  private displayedColumns: any;

  @Input()
  dataSource: any;

  @Input()
  currentData: any;

  @Input()
  loadFinished: boolean;

  @Input()
  maxPageSize: number;

  @Output()
  updateLastIdEvent = new EventEmitter();

  private scrolled = false;
  private direction = 'down';

  private availableColumns;

  tasks = [];

  constructor(
    private dialog: MatDialog,
    private settings: TableSettingsService,
    private jobStateService: JobStateService
  ) { }

  ngOnInit() {
    this.loadSettings();
    this.getDisplayedColumns();
  }

  private onScrollEvent(data) {
    this.loadFinished = data.loadFinished;
    this.scrolled = data.scrolled;
    this.direction = data.direction;
    this.updateLastIdEvent.emit({ lastId: data.dataIndex == -1 ? 0 : this.dataSource.data[data.dataIndex]['id'], direction: this.direction });
  }

  private setIcon(state) {
    return this.jobStateService.stateIcon(state);
  }

  private stateClass(state) {
    return this.jobStateService.stateClass(state);
  }

  private showDetail(jobId, taskId, taskState) {
    let dialogRef = this.dialog.open(TaskDetailComponent, {
      width: '70%',
      data: { jobId: jobId, taskId: taskId, taskState: taskState }
    });
  }

  getDisplayedColumns(): void {
    let columns = this.availableColumns.filter(e => e.displayed).map(e => e.name);
    columns.push('state');
    columns.push('detail');
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
    this.settings.save(this.tableName, this.availableColumns);
  }

  loadSettings(): void {
    this.availableColumns = this.settings.load(this.tableName, this.customizableColumns);
  }

  applyFilter(text: string): void {
    this.dataSource.filter = text;
  }

  filterNodes(state): void {
    this.applyFilter(state);
    this.filterInput.nativeElement.value = state;
  }
}
