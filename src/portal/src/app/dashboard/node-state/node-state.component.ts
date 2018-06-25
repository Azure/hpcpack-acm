import { Component, OnInit, Input } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
  selector: 'dashboard-node-state',
  templateUrl: './node-state.component.html',
  styleUrls: ['./node-state.component.scss']
})
export class NodeStateComponent implements OnInit {
  @Input() state: string;
  @Input() stateNum: number;
  @Input() stateIcon: string;
  @Input() total: number;


  nodesInfo() {
    this.router.navigate(['..', 'resource'], { relativeTo: this.route, queryParams: { filter: this.state } });
  }

  constructor(
    private router: Router,
    private route: ActivatedRoute
  ) { }

  ngOnInit() {
  }

}
