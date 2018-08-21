import { Component, OnChanges, SimpleChanges, Input, Output, EventEmitter } from '@angular/core';
import { MatTableDataSource } from '@angular/material';
import { JobStateService } from '../../services/job-state/job-state.service';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { TaskErrorComponent } from './task-error/task-error.component';

@Component({
  selector: 'node-selector',
  templateUrl: './node-selector.component.html',
  styleUrls: ['./node-selector.component.scss']
})
export class NodeSelectorComponent implements OnChanges {
  @Input()
  nodes: any[];

  @Input()
  nodeOutputs: any;

  @Output()
  select = new EventEmitter();

  state = 'all';

  name = '';

  selectedNode: any;

  hasError(node) {
    return this.nodeOutputs && this.nodeOutputs[node.name] && this.nodeOutputs[node.name]['error'] !== '';
  }

  private states = ['all', 'queued', 'running', 'finished', 'failed', 'canceled'];

  private displayedColumns = ['name', 'state'];

  private dataSource = new MatTableDataSource();

  constructor(
    private jobStateService: JobStateService,
    private dialog: MatDialog
  ) { }

  ngOnChanges(changes: SimpleChanges) {
    if (changes.nodes) {
      this.filter();
    }
  }

  stateClass(state) {
    return this.jobStateService.stateClass(state);
  }

  stateIcon(state) {
    return this.jobStateService.stateIcon(state);
  }

  isSelected(node) {
    return node && this.selectedNode && node.name === this.selectedNode.name;
  }

  private filter() {
    let res = this.nodes.filter(e => {
      if (this.state != 'all' && e.state != this.state)
        return false;
      if (e.name.toLowerCase().indexOf(this.name.toLowerCase()) < 0)
        return false;
      return true;
    });
    this.dataSource.data = res;
    if (!this.selectedNode || res.findIndex((e) => e.name == this.selectedNode.name) < 0) {
      this.selectNode(res[0]);
    }
  }

  selectNode(node) {
    if ((node && this.selectedNode && node.name == this.selectedNode.name)
      || (!node && !this.selectedNode)) {
      return;
    }
    let prevNode = this.selectedNode;
    this.selectedNode = node;
    this.select.emit({ node, prevNode });
  }

  showError(node) {
    let dialog = this.dialog.open(TaskErrorComponent, {
      width: '70%',
      data: this.nodeOutputs[node.name].error
    });
  }
}
