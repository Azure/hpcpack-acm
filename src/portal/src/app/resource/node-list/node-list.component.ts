import { Component, Input } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { MatTableDataSource, MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { SelectionModel } from '@angular/cdk/collections';
import { Subscription } from 'rxjs/Subscription';
import { NewDiagnosticsComponent } from '../new-diagnostics/new-diagnostics.component';
import { NewCommandComponent } from '../new-command/new-command.component';
import { TableOptionComponent } from '../../widgets/table-option/table-option.component';
import { ApiService } from '../../services/api.service';
import { TableSettingsService } from '../../services/table-settings.service';

@Component({
  selector: 'resource-node-list',
  templateUrl: './node-list.component.html',
  styleUrls: ['./node-list.component.css']
})
export class NodeListComponent {
  private query = { filter: '' };

  private subcription: Subscription;

  private dataSource: MatTableDataSource<any> = new MatTableDataSource();

  static customizableColumns = [
    { name: 'health', displayName: 'Health',  displayed: true,  },
    { name: 'state',  displayName: 'State',   displayed: true,  },
    { name: 'runningJobCount',  displayName: 'Jobs',  displayed: true },
    { name: 'coreCount',displayName: 'Cores', displayed: true },
    { name: 'memory',   displayName: 'Memory',displayed: true },
    { name: 'os',       displayName: 'OS',    displayed: true },
  ];

  private availableColumns;

  private displayedColumns;

  private selection = new SelectionModel(true, []);

  constructor(
    private dialog: MatDialog,
    private api: ApiService,
    private router: Router,
    private route: ActivatedRoute,
    private settings: TableSettingsService,
  ) { }

  ngOnInit() {
    this.loadSettings();
    this.getDisplayedColumns();
    this.api.node.getAll().subscribe(nodes => {
      this.dataSource.data = nodes;
    });
    this.subcription = this.route.queryParamMap.subscribe(params => {
      this.query.filter = params.get('filter');
      this.updateUI();
    });
  }

  updateUI() {
    let filter = this.query.filter;
    this.dataSource.filter = filter;
  }

  updateUrl() {
    this.router.navigate(['.'], { relativeTo: this.route, queryParams: this.query });
  }

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
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        let targetNodes = this.selection.selected.map(e => e.name);
        let diagnosticTest = { name: result['selectedTest']['name'], category: result['selectedTest']['category'], arguments: JSON.stringify(result['selectedTest']['arguments']) };
        let name = result['diagTestName'];

        console.log(diagnosticTest);
        this.api.diag.create(name, targetNodes, diagnosticTest).subscribe(obj => {
          let returnData = obj.headers.get('location').split('/');
          let jobId = returnData[returnData.length - 1];
          this.router.navigate([`/diagnostics/results/` + jobId]);
        });
      }
    });
  }

  runCommand() {
    let dialogRef = this.dialog.open(NewCommandComponent, {
      width: '98%',
      data: {}
    });

    dialogRef.afterClosed().subscribe(cmd => {
      if (cmd) {
        let names = this.selection.selected.map(e => e.name);
        this.api.command.create(cmd, names).subscribe(obj => {
          this.router.navigate([`/command/results/${obj.id}`]);
        });
      }
    });
  }

  hasNoSelection(): boolean {
    return this.selectedData.length == 0;
  }

  getDisplayedColumns(): void {
    let columns = this.availableColumns.filter(e => e.displayed).map(e => e.name);
    columns.push('actions');
    this.displayedColumns = ['select', 'name'].concat(columns);
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
    this.settings.save('NodeListComponent', this.availableColumns);
  }

  loadSettings(): void {
    this.availableColumns = this.settings.load('NodeListComponent', NodeListComponent.customizableColumns);
  }
}
