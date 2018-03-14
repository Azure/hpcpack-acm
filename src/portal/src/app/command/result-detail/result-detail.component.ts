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
  private states = ['all', 'queued', 'running', 'finished', 'failed', 'canceled'];

  private state = 'all';

  private name = '';

  private dataSource = new MatTableDataSource();

  private displayedColumns = ['name', 'state'];

  private filteredNodes = [];

  private selectedNode: any = {};

  private result: CommandResult = {} as CommandResult;

  private subcription: Subscription;

  constructor(
    private route: ActivatedRoute,
    private api: ApiService,
  ) {}

  ngOnInit() {
    this.subcription = this.route.paramMap.subscribe(map => {
      let id = map.get('id');
      this.api.command.get(id).subscribe(result => {
        this.result = result;
        this.selectedNode = result.nodes[0];
        this.filteredNodes = result.nodes.map(e => e);
        this.dataSource.data = this.filteredNodes;
      });
    });
  }

  ngOnDestroy() {
    if (this.subcription)
      this.subcription.unsubscribe();
  }

  stateIcon(state) {
    let res;
    switch(state) {
      case 'success':
        res = 'check';
        break;
      case 'running':
        res = 'directions_run';
        break;
      case 'failure':
        res = 'close';
        break;
    }
    return res;
  }

  title(name, state) {
    let res = name;
    switch(state) {
      case 'success':
        res += ' Succeeded!';
        break;
      case 'running':
        res += ' is Running.';
        break;
      case 'failure':
        res += ' Failed!';
        break;
    }
    return res;
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
  }
}
