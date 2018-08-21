import { Component, OnInit, Input, Output, EventEmitter, ContentChild, TemplateRef, OnChanges, SimpleChange } from '@angular/core';
import { ApiService } from '../../../services/api.service';
import { MatDialog } from '@angular/material';
import { ConfirmDialogComponent } from '../../../widgets/confirm-dialog/confirm-dialog.component';
import { Router } from '@angular/router';
import { JobStateService } from '../../../services/job-state/job-state.service';

@Component({
  selector: 'app-result-layout',
  templateUrl: './result-layout.component.html',
  styleUrls: ['./result-layout.component.scss']
})
export class ResultLayoutComponent implements OnInit {
  @Input()
  result: any;

  @Input()
  aggregationResult: any;

  private done = false;
  private showOverview = false;

  @ContentChild('task')
  taskTemplate: TemplateRef<any>;

  @ContentChild('overview')
  overviewTemplate: TemplateRef<any>;

  private stateClass(state) {
    return this.jobStateService.stateClass(state);
  }

  constructor(
    private api: ApiService,
    private jobStateService: JobStateService,
    private dialog: MatDialog,
    private router: Router,
  ) { }

  ngOnInit() {
    this.isDone();
  }

  ngOnChanges(changes: { [propKey: string]: SimpleChange }) {
    this.isDone();
  }

  isDone() {
    if (this.result.state == "Failed" || this.result.state == "Finished" || this.result.state == "Canceled") {
      this.done = true;
      if (this.aggregationResult !== undefined && this.aggregationResult !== null) {
        this.showOverview = true;
      }
    }
  }
  private canceling = false;

  cancelDiag() {
    let dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '90%',
      data: {
        title: 'Cancel',
        message: 'Are you sure to cancel the current run of diagnostic?'
      }
    });
    dialogRef.afterClosed().subscribe(res => {
      if (res) {
        this.canceling = true;
        this.api.diag.cancel(this.result.id).subscribe(res => { });
      }
    });
  }

  rerunDiag() {
    let dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '90%',
      data: {
        title: 'Copy',
        message: 'Are you sure to copy the current diagnostic?'
      }
    });
    dialogRef.afterClosed().subscribe(res => {
      if (res) {
        let targetNodes = this.result.targetNodes;
        let diagnosticTest = this.result.diagnosticTest;
        let name = this.result.name == undefined ? '' : `${this.result.name}`;
        this.api.diag.create(name, targetNodes, diagnosticTest).subscribe(obj => {
          let returnData = obj['headers'].get('location').split('/');
          let jobId = returnData[returnData.length - 1];
          this.router.navigate([`/diagnostics/results/` + jobId]);
        });
      }
    });
  }
}
