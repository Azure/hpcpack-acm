import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MatTableDataSource } from '@angular/material';
import { Subscription } from 'rxjs/Subscription';
import { CommandResult } from '../../models/command-result';
import { ApiService, Loop } from '../../services/api.service';

@Component({
  selector: 'app-result-detail',
  templateUrl: './result-detail.component.html',
  styleUrls: ['./result-detail.component.scss']
})
export class ResultDetailComponent implements OnInit {
  @ViewChild('output')
  private output: ElementRef;

  private id: string;

  private states = ['all', 'queued', 'running', 'finished', 'failed', 'canceled'];

  private state = 'all';

  private name = '';

  private dataSource = new MatTableDataSource();

  private displayedColumns = ['name', 'state'];

  private selectedNode: any;

  private result: CommandResult;

  private subcription: Subscription;

  private mainLoop: object;

  private nodeLoop: object;

  private errorMsg: string;

  private nodeOutputs = {};

  constructor(
    private route: ActivatedRoute,
    private api: ApiService,
  ) {}

  ngOnInit() {
    this.subcription = this.route.paramMap.subscribe(map => {
      this.nodeOutputs = {};
      this.id = map.get('id');
      this.updateResult(this.id);
    });
  }

  isLoaded(): boolean {
    return this.result && this.result.nodes.length > 0;
  }

  updateResult(id) {
    this.mainLoop = Loop.start(
      //observable:
      this.api.command.get(id),
      //observer:
      {
        next: (result) => {
          if (id != this.id) {
            return; //A false value indicates the end of the loop
          }
          this.result = result;
          if (result.nodes.length == 0) {
            return true;
          }
          this.filter();
          let state = this.setResultState();
          return !this.isOver;
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
    if (this.mainLoop) {
      Loop.stop(this.mainLoop);
    }
    if (this.nodeLoop) {
      Loop.stop(this.nodeLoop);
    }
  }

  filter() {
    let res = this.result.nodes.filter(e => {
      if (this.state != 'all' && e.state != this.state)
        return false;
      if (e.name.toLowerCase().indexOf(this.name.toLowerCase()) < 0)
        return false;
      return true;
    });
    this.dataSource.data = res;
    if (!this.selectedNode || res.findIndex((e) => e.name == this.selectedNode.name) < 0)
      this.selectNode(res[0]);
  }

  selectNode(node) {
    if (!node) {
      this.selectedNode = node;
      return;
    }
    if (this.selectedNode && node.name == this.selectedNode.name) {
      return;
    }
    this.selectedNode = node;
    this.updateNodeResult(this.id, node);
  }

  updateNodeResult(id, node) {
    //Cancel previous loop if any
    if (this.nodeLoop) {
      Loop.stop(this.nodeLoop);
    }
    let output = this.nodeOutputs[node.name];
    if (!output) {
      output = this.nodeOutputs[node.name] = { content: '', next: 0 };
    }
    if (output.end) {
      return;
    }
    this.nodeLoop = Loop.start(
      //observable:
      this.api.command.getOutput(id, node.key, node.next),
      //observer:
      {
        next: (result) => {
          //id and/or node may change when result arrives in some time later.
          if (this.id != id || this.selectedNode.name != node.name) {
            //End the loop by returning a false value.
            return;
          }
          //let output = this.nodeOutputs[node.name];
          if (result.content) {
            output.content += result.content;
            setTimeout(() => this.scrollOutputToBottom(), 0);
          }
          output.next = result.offset + result.size;
          output.end = result.size == 0 && this.isOver;
          return output.end ? false : this.api.command.getOutput(id, node.key, output.next);
        }
      },
      //interval(in ms):
      500
    );
  }

  get currentOutput(): string {
    if (!this.selectedNode)
      return '';
    let output = this.nodeOutputs[this.selectedNode.name];
    return output ? output.content : '';
  }

  scrollOutputToBottom(): void {
    let elem = this.output.nativeElement;
    elem.scrollTop = elem.scrollHeight;
  }

  setResultState() {
    let stats = { running: 0, finished: 0, failed: 0 };
    this.result.nodes.forEach(e => {
      stats[e.state]++;
    });
    let state;
    if (stats.running > 0)
      state = 'running';
    else if (stats.failed > 0)
      state = 'failed';
    else
      state = 'finished';
    this.result.state = state;
    return state;
  }

  get isOver(): boolean {
    let state = this.result.state;
    return state == 'finished' || state == 'failed';
  }
}
