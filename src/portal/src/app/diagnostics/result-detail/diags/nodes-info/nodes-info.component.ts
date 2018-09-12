import { Component, OnInit, Input } from '@angular/core';
import { isArray, isObject } from 'util';

@Component({
  selector: 'app-nodes-info',
  templateUrl: './nodes-info.component.html',
  styleUrls: ['./nodes-info.component.scss']
})
export class NodesInfoComponent implements OnInit {
  @Input()
  nodes: Array<any>;

  @Input()
  badNodes: Array<any>;

  constructor() { }

  ngOnInit() {
  }

  getLink(node) {
    let path = [];
    path.push('/resource');
    path.push(node);
    return path;
  }

  isBad(node) {
    if (this.badNodes) {
      return this.badNodes.indexOf(node) !== -1;
    }
    return false;
  }
}
