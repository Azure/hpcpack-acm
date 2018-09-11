import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatTableDataSource, MatDialog } from '@angular/material';
import { Subscription } from 'rxjs/Subscription';
import { Observable } from 'rxjs/Observable';
import { ApiService, Loop } from '../../services/api.service';
import { NodeSelectorComponent } from '../node-selector/node-selector.component';
import { CommandOutputComponent } from '../command-output/command-output.component';
import { CommandInputComponent } from '../command-input/command-input.component';
import { ConfirmDialogComponent } from '../../widgets/confirm-dialog/confirm-dialog.component';
import { JobStateService } from '../../services/job-state/job-state.service';

@Component({
  selector: 'app-result-detail',
  templateUrl: './result-detail.component.html',
  styleUrls: ['./result-detail.component.scss']
})
export class ResultDetailComponent implements OnInit {
  @ViewChild(NodeSelectorComponent)
  private selector: NodeSelectorComponent;

  @ViewChild('output')
  private output: CommandOutputComponent;

  public id: string;

  public result: any;

  private gotTasks: boolean = false;

  private subcription: Subscription;

  private jobLoop: object;

  private nodesLoop: object;

  private nodeLoop: object;

  private errorMsg: string;

  private nodeOutputs = {};

  private autoload = true;

  private outputInitOffset = -8192;

  private outputPageSize = 8192;

  public canceling = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiService,
    private jobStateService: JobStateService,
    private dialog: MatDialog,
  ) { }

  ngOnInit() {
    this.subcription = this.route.paramMap.subscribe(map => {
      this.result = { state: 'unknown', command: '', nodes: [] };
      this.nodeOutputs = {};
      this.id = map.get('id');
      this.updateJob(this.id);
      this.updateNodes(this.id);
    });
  }

  get isLoaded(): boolean {
    return this.gotTasks;
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
          if (!this.gotTasks) {
            this.result.nodes = job.targetNodes.map(node => ({ name: node, state: '' }));
          }
          return true;
        },
        error: (err) => {
          this.errorMsg = err;
        }
      }
    );
  }

  getNodesFromTasks(tasks) {
    return tasks.map((e: any) => ({
      name: e.node,
      state: e.state,
      taskId: e.id,
    }));
  }

  updateNodes(id) {
    this.nodesLoop = Loop.start(
      //observable:
      this.api.command.getTasks(id),
      //observer:
      {
        next: (tasks) => {
          if (id != this.id) {
            return;
          }
          this.gotTasks = true;
          this.result.nodes = this.getNodesFromTasks(tasks);
          return true;
        },
        error: (err) => {
          this.errorMsg = err;
        }
      }
    );
  }

  ngOnDestroy() {
    if (this.subcription) {
      this.subcription.unsubscribe();
    }
    if (this.jobLoop) {
      Loop.stop(this.jobLoop);
    }
    if (this.nodesLoop) {
      Loop.stop(this.nodesLoop);
    }
    if (this.nodeLoop) {
      Loop.stop(this.nodeLoop);
    }
    for (let key in this.nodeOutputs) {
      let loop = this.nodeOutputs[key].keyLoop;
      if (loop)
        Loop.stop(loop);
    }
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
        this.api.command.getTaskResult(this.id, node.taskId).subscribe(
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
          if (err.status = 404 && !this.isOver) {
            //return value is assigned to looper.ended in observer.err
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
    let output = this.nodeOutputs[node.name];
    if (!output) {
      output = this.nodeOutputs[node.name] = {
        content: '',
        next: this.outputInitOffset,
        start: undefined,
        end: undefined,
        loading: false,
        key: null,
        error: ''
      };
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
    let output = this.nodeOutputs[node.name];
    if (output.keyLoop) {
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
    if (typeof (output.start) === 'undefined') {
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
    return !this.selectedNode || !this.currentOutput.key;
  }

  get currentOutputUrl(): string {
    return this.isOutputDisabled ? '' : this.api.command.getDownloadUrl(this.currentOutput.key);
  }

  scrollOutputToBottom(): void {
    this.output.scrollToBottom();
  }

  isJobOver(state): boolean {
    state = state.toLowerCase();
    return state == 'finished' || state == 'failed' || state == 'canceled';
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
    let output = this.getNodeOutput(node)
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

  newCommand() {
    let dialogRef = this.dialog.open(CommandInputComponent, {
      width: '98%',
      data: { command: this.result.command }
    });
    dialogRef.afterClosed().subscribe(cmd => {
      if (cmd) {
        let names = this.result.nodes.map(node => node.name);
        this.api.command.create(cmd, names).subscribe(obj => {
          this.router.navigate([`/command/results/${obj.id}`]);
        });
      }
    });
  }

  cancelCommand() {
    let dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '90%',
      data: {
        title: 'Cancel',
        message: 'Are you sure to cancel the current run of command?'
      }
    });
    dialogRef.afterClosed().subscribe(res => {
      if (res) {
        this.canceling = true;
        this.api.command.cancel(this.id).subscribe();
      }
    });
  }
}
