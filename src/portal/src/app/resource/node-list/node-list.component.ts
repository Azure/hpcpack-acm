import { Component, Input } from '@angular/core';
import { MatTableDataSource, MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { SelectionModel } from '@angular/cdk/collections';
import { NewDiagnosticsComponent } from '../new-diagnostics/new-diagnostics.component';
import { NewCommandComponent } from '../new-command/new-command.component';

@Component({
  selector: 'resource-node-list',
  templateUrl: './node-list.component.html',
  styleUrls: ['./node-list.component.css']
})
export class NodeListComponent {
  @Input()
  dataSource: MatTableDataSource<any> = new MatTableDataSource();

  private displayedColumns = ['select', 'name', 'health', 'state', 'runningJobs', 'actions'];

  private selection = new SelectionModel(true, []);

  constructor(private dialog: MatDialog) {}

  get selectedData(): any[] {
    return this.selection.selected;
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

  runDiagnostics() {
    let dialogRef = this.dialog.open(NewDiagnosticsComponent, {
      width: '98%',
      data: {}
    });

    //TODO: Run diagnostic tests on user selected nodes...
    //dialogRef.afterClosed().subscribe(result => {
    //});
  }

  runCommand() {
    let dialogRef = this.dialog.open(NewCommandComponent, {
      width: '98%',
      data: {}
    });

    //TODO: Run diagnostic tests on user selected nodes...
    //dialogRef.afterClosed().subscribe(result => {
    //});
  }
}
