import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'pingpong-good-nodes',
  templateUrl: './good-nodes.component.html',
  styleUrls: ['./good-nodes.component.scss']
})
export class GoodNodesComponent {
  @Input()
  nodeGroups: any;

  constructor() { }

}
