import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'pingpong-good-nodes',
  templateUrl: './good-nodes.component.html',
  styleUrls: ['./good-nodes.component.scss']
})
export class GoodNodesComponent implements OnInit {
  @Input()
  nodeGroups: any;

  public groups = [];
  constructor() { }

  ngOnInit() {
    this.groups = this.nodeGroups;
  }

}
