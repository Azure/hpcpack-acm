import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MatTableDataSource } from '@angular/material';
import { Subscription } from 'rxjs/Subscription';
import { CommandResult } from '../../models/command-result';
import { ApiService } from '../../api.service';

@Component({
  selector: 'app-result-detail',
  templateUrl: './result-detail.component.html',
  styleUrls: ['./result-detail.component.scss']
})
export class ResultDetailComponent implements OnInit {
  private id: string;

  private states = ['all', 'queued', 'running', 'finished', 'failed', 'canceled'];

  private state = 'all';

  private name = '';

  private dataSource = new MatTableDataSource();

  private displayedColumns = ['name', 'state'];

  private selectedNode: any;

  private result: CommandResult;

  private subcription: Subscription;

  private loaded: boolean;

  private retries = 0;

  private errorMsg: string;

  constructor(
    private route: ActivatedRoute,
    private api: ApiService,
  ) {}

  ngOnInit() {
    this.subcription = this.route.paramMap.subscribe(map => {
      this.id = map.get('id');
      this.retries = 0;
      this.loaded = false;
      this.updateResult(this.id);
    });
  }

  updateResult(id) {
    this.api.command.get(id).subscribe(result => {
      //id may change when result arrives in some time later.
      if (id != this.id) {
        return;
      }

      this.result = result;

      //If the job is not started, query it later.
      if (result.nodes.length == 0) {
        const max = 20;
        this.retries++;
        if (this.retries < max)
          setTimeout(() => this.updateResult(id), 2000);
        else
          this.errorMsg = `Tried ${max} times but the job seems not started yet. Please refresh the page later.`;
        return;
      }

      this.loaded = true;
      this.filter();
      let state = this.setResultState();
      if (state != 'finished' && state != 'failed')
        this.updateResult(id);
    });
  }

  ngOnDestroy() {
    if (this.subcription)
      this.subcription.unsubscribe();
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
    this.selectNode(res[0]); //TODO: select res[0] only when filter changes!
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
    if (node.end)
      return;
    this.api.command.getOuput(id, node.key, node.next).subscribe(result => {
      //id and/or node may change when result arrives in some time later.
      if (this.id != id || this.selectedNode.name != node.name)
        return;
      node.next = result.offset + result.size;
      node.end = result.size == 0;
      if (!node.end) {
        node.output += result.content;
        this.updateNodeResult(id, node);
      }
    });
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
}
