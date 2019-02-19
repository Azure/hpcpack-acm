import { Component, OnChanges, SimpleChanges, Input, Output, EventEmitter, ViewChild } from '@angular/core';
import { JobStateService } from '../../services/job-state/job-state.service';
import { MatDialog } from '@angular/material';
import { TaskErrorComponent } from './task-error/task-error.component';
import { VirtualScrollService } from '../../services/virtual-scroll/virtual-scroll.service';
import { TableService } from '../../services/table/table.service';
import { CdkVirtualScrollViewport } from '@angular/cdk/scrolling';

@Component({
  selector: 'node-selector',
  templateUrl: './node-selector.component.html',
  styleUrls: ['./node-selector.component.scss']
})
export class NodeSelectorComponent implements OnChanges {
  @ViewChild('content') cdkVirtualScrollViewport: CdkVirtualScrollViewport;

  @Input()
  nodes: Array<any>;

  @Input()
  nodeOutputs: any;

  @Input()
  loadFinished = false;

  @Output()
  select = new EventEmitter();

  @Output()
  updateLastIdEvent = new EventEmitter();

  @Input()
  empty = true;

  @Input()
  maxPageSize = 50;

  state = 'All';

  name = '';

  selectedNode: any;

  hasError(node) {
    return this.nodeOutputs && this.nodeOutputs[node.name] && this.nodeOutputs[node.name]['error'] !== '';
  }

  public states = ['All', 'Queued', 'Running', 'Finished', 'Failed', 'Canceled'];

  public displayedColumns = ['name', 'state'];


  public scrolled = false;

  pivot = Math.round(this.maxPageSize / 2) - 1;

  startIndex = 0;
  lastScrolled = 0;

  public loading = false;
  private endId = -1;
  private lastId = 0;

  constructor(
    private jobStateService: JobStateService,
    private dialog: MatDialog,
    private tableService: TableService,
    private virtualScrollService: VirtualScrollService
  ) { }

  stateClass(state) {
    return this.jobStateService.stateClass(state);
  }

  stateIcon(state) {
    return this.jobStateService.stateIcon(state);
  }

  isSelected(node) {
    return node && this.selectedNode && node.name === this.selectedNode.name;
  }

  ngOnChanges(changes: SimpleChanges) {
    this.filter();
  }

  public filter() {
    let res = this.nodes.filter(e => {
      if (this.state != 'All' && e.state != this.state)
        return false;
      if (e.name.toLowerCase().indexOf(this.name.toLowerCase()) < 0)
        return false;
      return true;
    });
    this.nodes = res;
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

  trackByFn(index, item) {
    return this.tableService.trackByFn(item, this.displayedColumns);
  }

  indexChanged($event) {
    let result = this.virtualScrollService.indexChangedCalc(this.maxPageSize, this.pivot, this.cdkVirtualScrollViewport, this.nodes, this.lastScrolled, this.startIndex);
    this.pivot = result.pivot;
    this.lastScrolled = result.lastScrolled;
    this.lastId = result.lastId == undefined ? this.lastId : result.lastId;
    this.endId = result.endId == undefined ? this.endId : result.endId;
    this.loading = result.loading;
    this.startIndex = result.startIndex;
    this.scrolled = result.scrolled;
    this.updateLastIdEvent.emit({ lastId: this.lastId, endId: this.endId });
  }

  get showScrollBar() {
    return this.tableService.isContentScrolled(this.cdkVirtualScrollViewport.elementRef.nativeElement);
  }
}
