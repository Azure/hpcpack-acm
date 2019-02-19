import { Component, OnInit, ViewChild, ViewChildren, QueryList, NgZone } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatDialog } from '@angular/material';
import { Subscription } from 'rxjs/Subscription';
import { Observable } from 'rxjs/Observable';
import { ApiService, Loop } from '../../services/api.service';
import { CommandOutputComponent } from '../command-output/command-output.component';
import { CommandInputComponent } from '../command-input/command-input.component';
import { ConfirmDialogComponent } from '../../widgets/confirm-dialog/confirm-dialog.component';
import { JobStateService } from '../../services/job-state/job-state.service';
import { FormControl } from '@angular/forms';
import { TableService } from '../../services/table/table.service';
import { CdkTextareaAutosize } from '@angular/cdk/text-field';
import { take } from 'rxjs/operators';

@Component({
  selector: 'multi-cmds',
  templateUrl: './multi-cmds.component.html',
  styleUrls: ['./multi-cmds.component.scss'],
})
export class MultiCmdsComponent implements OnInit {
  @ViewChildren(CommandOutputComponent)
  private outputs: QueryList<CommandOutputComponent>;

  @ViewChild('autosize') autosize: CdkTextareaAutosize;

  public id: string;

  public result: any;

  private gotTasks: boolean = false;

  private subcription: Subscription;

  private jobLoop: object;

  private nodesLoop: object;

  private nodeLoop: object;

  private errorMsg: string;

  private autoload = true;

  private outputInitOffset = -8192;

  private outputPageSize = 8192;

  public canceling = false;

  public tabs = [];

  public newCmd = '';

  private commandIndex = 0;

  private scriptIndex = 0;

  public cmds = [];

  public commandLine = 'single';

  public timeout = 1800;

  private lastId = 0;
  public maxPageSize = 100;
  public scrolled = false;
  public loadFinished = false;
  private reverse = true;
  private selectedNodes = [];

  pivot = Math.round(this.maxPageSize / 2) - 1;

  startIndex = 0;
  lastScrolled = 0;

  public listLoading = false;
  public empty = true;
  private endId = -1;

  public scriptBlock: boolean = false;

  constructor(
    private route: ActivatedRoute,
    private api: ApiService,
    private jobStateService: JobStateService,
    private dialog: MatDialog,
    private tableService: TableService,
    private ngZone: NgZone,
  ) { }

  ngOnInit() {
    this.subcription = this.route.queryParams.subscribe(map => {
      this.result = { state: 'unknown', command: '', nodes: [] };
      this.id = map.firstJobId;
      this.tabs = [];
      this.tabs.push({ id: this.id, outputs: {}, command: '', state: '' });
      this.updateJob(this.id);
      this.updateNodes(this.id);
    });
  }

  get initializing() {
    return this.result.state == 'unknown' || this.result.state == '';
  }

  isSingleCmd(cmd) {
    let match = /\r|\n/.exec(cmd);
    return match ? false : true;
  }

  public toggleScriptBlock() {
    this.scriptBlock = !this.scriptBlock;
  }

  get isLoaded(): boolean {
    return this.gotTasks;
  }

  public stateClass(state) {
    return this.jobStateService.stateClass(state);
  }

  updateJob(id) {
    this.jobLoop = Loop.start(
      //observable:
      this.api.command.get(id),
      //observer:
      {
        next: (job: any) => {
          if (id != this.id) {
            return;
          }
          this.result.state = job.state;
          this.result.command = job.commandLine;
          this.result.progress = job.progress;
          this.tabs[this.selected.value].state = job.state;
          if (!this.tabs[this.selected.value].command) {
            this.tabs[this.selected.value].command = job.commandLine;
            this.timeout = job.maximumRuntimeSeconds;
            this.cmds.push({ mode: this.isSingleCmd(job.commandLine) ? 'single' : 'multiple', cmd: job.commandLine });
          }
          return true;
        },
        error: (err) => {
          this.errorMsg = err;
        }
      }
    );
  }

  private getTasksRequest() {
    return this.api.command.getTasksByPage(this.id, this.lastId, this.maxPageSize);
  }

  updateNodes(id) {
    this.nodesLoop = Loop.start(
      //observable:
      this.getTasksRequest(),
      //observer:
      {
        next: (tasks) => {
          if (id != this.id) {
            return;
          }
          this.empty = false;
          if (tasks.length > 0) {
            this.gotTasks = true;
            this.result.nodes = this.tableService.updateData(tasks, this.result.nodes, 'id');
            if (this.endId != -1 && tasks[tasks.length - 1].id != this.endId) {
              this.listLoading = false;
            }
          }
          if (this.reverse && tasks.length < this.maxPageSize) {
            this.loadFinished = true;
          }
          return this.getTasksRequest();
        },
        error: (err) => {
          this.errorMsg = JSON.stringify(err);
        }
      }
    );
  }

  onUpdateLastIdEvent(data) {
    this.lastId = data.lastId;
    this.endId = data.endId;
  }

  ngOnDestroy() {
    if (this.subcription) {
      this.subcription.unsubscribe();
    }
    this.stopCurrentLoop();
  }

  stopCurrentLoop() {
    if (this.jobLoop) {
      Loop.stop(this.jobLoop);
    }
    if (this.nodesLoop) {
      Loop.stop(this.nodesLoop);
    }
    if (this.nodeLoop) {
      Loop.stop(this.nodeLoop);
    }

    this.tabs.forEach(tab => {
      for (let node in tab.outputs) {
        let loop = tab.outputs[node].keyLoop;
        tab.outputs[node].loading = false;
        if (loop) {
          Loop.stop(loop);
        }
      }
    });
  }

  //This should work but not in fact! Because this.selector is set later than
  //selectNode is called. That seems the NodeSelectorComponent can fire events
  //before Angular captures it in this.selector. A surprise!
  //
  //get selectedNode(): any {
  //  return this.selector ? this.selector.selectedNode : null;
  //}

  selectedNode: any;

  selectNode({ node, prevNode }) {
    this.selectedNode = node;
    if (prevNode) {
      this.stopNodeOutputKeyLoop(prevNode);
      this.stopAutoload(prevNode);
    }
    if (!node) {
      return;
    }
    if (this.autoload) {
      this.startAutoload(node);
    }
    else {
      this.loadOnce(node);
    }
  }

  isSelected(node) {
    return node && this.selectedNode && node.name === this.selectedNode.name;
  }

  getNodeOutputKey(node, onGot) {
    return Loop.start(
      //observable:
      Observable.create((observer) => {
        this.api.command.getTaskResult(this.id, node.id).subscribe(
          result => {
            observer.next(result.resultKey);
            observer.complete();
          },
          error => {
            observer.error(error);
          }
        );
      }),
      //observer:
      {
        next: (key) => {
          if (key) {
            onGot(key);
            return false;
          }
          //TODO: When is it impossible to get a key?
          //if (this.isNodeOver(node)) {
          //  onGot(null);
          //  return false;
          //}
          return true;
        },
        error: (err) => {
          if (err.status == 404 && !this.isNodeOver(node)) {
            // return value is assigned to looper.ended in observer.err
            // false means continue to query key result
            return false;
          }
          else if (err.status == 404 && node.state == 'Finished') {
            return false;
          }
          else {
            onGot(err);
            return true;
          }
        }
      },
      //interval(in ms):
      1000,
    );
  }

  getNodeOutput(node): any {
    if (!this.tabs[this.selected.value]) {
      return;
    }
    let selectedJobOutputs = this.tabs[this.selected.value].outputs;
    let output = null;
    if (selectedJobOutputs) {
      output = selectedJobOutputs[node.name];
    }
    if (!output) {
      selectedJobOutputs[node.name] = {
        content: '',
        next: this.outputInitOffset,
        start: undefined,
        end: undefined,
        loading: false,
        key: null,
        error: ''
      };
      output = selectedJobOutputs[node.name];
      let onKeyReady = (callback) => {
        if (output.key === null) {
          if (!output.keyLoop) {
            output.loading = 'key';
            output.keyLoop = this.getNodeOutputKey(node, (key) => {
              let keyType = typeof (key);
              output.loading = false;
              if (key && keyType == 'string') {
                output.key = key;
                callback(true);
              }
              else if (key && keyType == 'object') {
                output.error = key;
                callback(false);
              }
              else {
                output.key = false;
                callback(false);
              }
            });
          }
        }
        else if (output.key === false) { //No key, for no output
          callback(false);
        }
        else {
          callback(true);
        }
      }
      (output as any).onKeyReady = onKeyReady;
    }
    return output;
  }

  stopNodeOutputKeyLoop(node) {
    let output = this.tabs[this.selected.value].outputs[node.name];
    if (output && output.keyLoop) {
      Loop.stop(output.keyLoop);
      output.keyLoop = null;
      if (output.loading === 'key') {
        output.loading = false;
      }
    }
  }

  updateNodeOutput(output, result): boolean {
    //NOTE: There may be two inflight updates for the same piece of output, one
    //by autoload and the other one by manual trigger. Drop the one arrives later.
    if (output.next > result.offset) {
      return false;
    }
    //Update start field when and only when it's not updated yet.
    if (typeof (output.start) === 'undefined' && result.offset >= 0) {
      output.start = result.offset;
    }
    //NOTE: result.end depends on passing an opt.over parameter to API getOutput.
    output.end = result.end;
    if (result.content) {
      output.content += result.content;
    }
    output.next = result.offset + result.size;
    return result.content ? true : false;
  }

  updateNodeOutputBackward(output, result): boolean {
    //Update next field when and only when it's not updated yet.
    if (output.next === this.outputInitOffset) {
      output.next = result.offset + result.size;
    }
    if (result.content) {
      output.content = result.content + output.content;
    }
    output.start = result.offset;
    return result.content ? true : false;
  }

  stopAutoload(node): void {
    let output = this.getNodeOutput(node)
    output.loading = false;
    if (this.nodeLoop) {
      Loop.stop(this.nodeLoop);
      this.nodeLoop = null;
    }
  }

  startAutoload(node): void {
    let output = this.getNodeOutput(node)
    if (output.end || output.loading) {
      return;
    }
    output.onKeyReady((hasKey) => {
      if (!hasKey) {
        return;
      }
      output.loading = 'auto';
      this.nodeLoop = Loop.start(
        //observable:
        this.api.command.getOutput(output.key, output.next, this.outputPageSize),
        //observer:
        {
          next: (result) => {
            if (this.updateNodeOutput(output, result)) {
              setTimeout(() => this.scrollOutputToBottom(), 0);
            }
            let over = output.end || !this.autoload;
            if (over) {
              output.loading = false;
            }
            return over ? false :
              this.api.command.getOutput(output.key, output.next, this.outputPageSize);
          },
          error: (err) => {
            output.loading = false;
            output.error = err;
            return true;
          }
        },
        //interval(in ms):
        0,
      );
    });
  }

  loadOnce(node) {
    let output = this.getNodeOutput(node)
    if (output.content || output.end || output.loading) {
      return;
    }
    output.onKeyReady((hasKey) => {
      if (!hasKey) {
        return;
      }
      output.loading = 'once';
      let opt = { fulfill: true, timeout: 2000 };
      this.api.command.getOutput(output.key, output.next, this.outputPageSize, opt as any).subscribe(
        result => {
          output.loading = false;
          this.updateNodeOutput(output, result);
        },
        error => {
          output.loading = false;
          output.error = error;
        }
      );
    });
  }

  get loading(): boolean {
    let output = this.currentOutput;
    return output && output.loading;
  }

  get currentOutput(): any {
    if (!this.selectedNode)
      return;
    return this.getNodeOutput(this.selectedNode);
  }

  get isOutputDisabled(): boolean {
    return !this.selectedNode || this.currentOutput ? (!this.currentOutput.key) : true;
  }

  get currentOutputUrl(): string {
    return this.isOutputDisabled ? '' : this.api.command.getDownloadUrl(this.currentOutput.key);
  }

  scrollOutputToBottom(): void {
    this.outputs.forEach(e => {
      e.scrollToBottom();
    });
  }

  isJobOver(state): boolean {
    return state == 'Finished' || state == 'Failed' || state == 'Canceled';
  }

  get isOver(): boolean {
    let state = this.result.state;
    return this.isJobOver(state);
  }

  isNodeOver(node): boolean {
    let state = node.state;
    return this.isJobOver(state);
  }

  toggleAutoload(enabled) {
    this.autoload = enabled;
    if (enabled) {
      this.startAutoload(this.selectedNode);
    }
    else {
      this.stopAutoload(this.selectedNode);
    }
  }

  private scrollTop;

  private scrollHeight;

  loadPrevAndScroll(node, elem) {
    this.scrollTop = elem.scrollTop;
    this.scrollHeight = elem.scrollHeight;
    this.loadPrev(node,
      () => elem.scrollTop = elem.scrollHeight - this.scrollHeight + this.scrollTop);
  }

  loadPrev(node, onload = undefined) {
    let output = this.getNodeOutput(node);
    if (output.start === 0 || output.loading) {
      return;
    }
    let prev;
    let pageSize = this.outputPageSize;
    if (output.start) {
      prev = output.start - this.outputPageSize;
      if (prev < 0) {
        prev = 0;
        pageSize = output.start;
      }
    }
    else {
      prev = this.outputInitOffset;
    }
    output.onKeyReady((hasKey) => {
      if (!hasKey) {
        return;
      }
      output.loading = 'prev';
      let opt = { fulfill: true };
      this.api.command.getOutput(output.key, prev, pageSize, opt as any)
        .subscribe(
          result => {
            output.loading = false;
            if (this.updateNodeOutputBackward(output, result) && onload
              && this.selectedNode && this.selectedNode.name == node.name) {
              setTimeout(onload, 0);
            }
          },
          error => {
            output.loading = false;
            output.error = error;
          });
    });
  }

  loadNext(node) {
    let output = this.getNodeOutput(node)
    if (output.end || output.loading) {
      return;
    }
    output.onKeyReady((hasKey) => {
      if (!hasKey) {
        return;
      }
      output.loading = 'next';
      let opt = { fulfill: true, timeout: 2000 };
      this.api.command.getOutput(output.key, output.next, this.outputPageSize, opt as any)
        .subscribe(
          result => {
            output.loading = false;
            this.updateNodeOutput(output, result);
          },
          error => {
            output.loading = false;
            output.error = error;
          });
    });
  }

  loadFromBeginAndScroll(node, elem) {
    this.loadFromBegin(node, () => elem.scrollTop = 0);
  }

  loadFromBegin(node, onload) {
    let output = this.getNodeOutput(node)
    if (output.loading) {
      return;
    }
    if (output.start === 0 && onload) {
      setTimeout(onload, 0);
      return;
    }
    //Reset output for loading from the begin
    output.content = '';
    output.start = undefined;
    output.end = undefined;
    output.next = 0;
    output.error = '';
    output.onKeyReady((hasKey) => {
      if (!hasKey) {
        return;
      }
      output.loading = 'top';
      let opt = { fulfill: true, timeout: 2000 };
      this.api.command.getOutput(output.key, output.next, this.outputPageSize, opt as any).subscribe(
        result => {
          output.loading = false;
          this.updateNodeOutput(output, result);
          setTimeout(onload, 0);
        },
        error => {
          output.loading = false;
          output.error = error;
        });
    });
  }

  selected = new FormControl(0);
  excuteCmd() {
    if (this.newCmd) {
      let names = this.result.nodes.map(node => node.name);
      this.api.command.create(this.newCmd, names, this.timeout).subscribe(obj => {
        this.id = obj.id;
        this.tabs.push({ id: this.id, outputs: {}, command: this.newCmd, state: '' });
        this.cmds.push({ mode: this.isSingleCmd(this.newCmd) ? 'single' : 'multiple', cmd: this.newCmd });
        this.selected.setValue(this.tabs.length - 1);
        this.newCmd = '';
        this.commandIndex = this.cmds.length - 1;
        this.scriptIndex = this.cmds.length - 1;
      });
    }
  }

  newCommand() {
    let dialogRef = this.dialog.open(CommandInputComponent, {
      width: '98%',
      data: { command: this.result.command, timeout: this.timeout, isSingleCmd: this.isSingleCmd(this.result.command) }
    });
    dialogRef.afterClosed().subscribe(params => {
      if (params && params.command) {
        let names = this.result.nodes.map(node => node.name);
        this.api.command.create(params.command, names, params.timeout).subscribe(obj => {
          this.id = obj.id;
          this.tabs.push({ id: this.id, outputs: {}, command: params.command, state: '' });
          this.cmds.push({ mode: this.isSingleCmd(params.command) ? 'single' : 'multiple', cmd: params.command });
          this.selected.setValue(this.tabs.length - 1);
          this.newCmd = '';
          this.commandIndex = this.cmds.length - 1;
          this.scriptIndex = this.cmds.length - 1;
        });
      }
    });
  }

  changeTab(e) {
    this.id = this.tabs[e].id;
    this.selected.setValue(e);
    this.stopCurrentLoop();
    this.updateJob(this.id);
    this.updateNodes(this.id);
    if (this.autoload) {
      this.startAutoload(this.selectedNode);
    }
    else {
      this.loadOnce(this.selectedNode);
    }
  }

  cancelCommand() {
    let dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '45%',
      data: {
        title: 'Cancel',
        message: 'Are you sure to cancel the current run of command?'
      }
    });
    dialogRef.afterClosed().subscribe(res => {
      if (res) {
        this.canceling = true;
        this.api.command.cancel(this.id).subscribe(res => {
          this.canceling = false;
        });
      }
    });
  }

  closeTab(index) {
    this.tabs.splice(index, 1);
  }


  getPreviousCmd() {
    let index = this.commandLine == 'single' ? this.commandIndex : this.scriptIndex;
    let previousCmd = this.cmds[index];
    let tempMode;
    let targetCmd;
    while (index >= 0) {
      let tempCmd = this.cmds[index--];
      tempMode = tempCmd['mode'];
      targetCmd = tempCmd['cmd'];
      if (tempMode == this.commandLine)
        break;
    }
    if (tempMode != this.commandLine) {
      if (previousCmd['mode'] == this.commandLine) {
        this.newCmd = previousCmd['cmd'];
      }
    }
    else {
      this.newCmd = targetCmd;
      if (this.commandLine == 'single') {
        this.commandIndex = index < 0 ? 0 : index;
      }
      else {
        this.scriptIndex = index < 0 ? 0 : index;
      }
    }
  }

  getNextCmd() {
    let index = this.commandLine == 'single' ? this.commandIndex : this.scriptIndex;
    if (index + 1 == this.cmds.length) {
      this.newCmd = '';
      return;
    }
    let tempMode;
    while (index + 1 < this.cmds.length) {
      let tempCmd = this.cmds[++index];
      tempMode = tempCmd['mode'];
      this.newCmd = tempCmd['cmd'];
      if (tempMode == this.commandLine)
        break;
    }
    if (tempMode != this.commandLine) {
      this.newCmd = '';
    }
    else {
      if (this.commandLine == 'single') {
        this.commandIndex = index;
      }
      else {
        this.scriptIndex = index;
      }
    }

  }

  changeMode() {
    this.commandIndex = (this.cmds.length - 1) > 0 ? this.cmds.length - 1 : 0;
    this.scriptIndex = (this.cmds.length - 1) > 0 ? this.cmds.length - 1 : 0;
    this.newCmd = '';
  }

  triggerResize() {
    // Wait for changes to be applied, then trigger textarea resize.
    this.ngZone.onStable.pipe(take(1))
      .subscribe(() => this.autosize.resizeToFitContent(true));
  }
}
