import { Component, OnInit, Input } from '@angular/core';
import { isArray } from 'util';

@Component({
  selector: 'app-nodes-info',
  templateUrl: './nodes-info.component.html',
  styleUrls: ['./nodes-info.component.scss']
})
export class NodesInfoComponent implements OnInit {
  @Input()
  nodes: Array<any>;

  @Input()
  aggregationInfo: object;

  constructor() { }

  ngOnInit() {
  }

  hasData(data) {
    if (isArray(data)) {
      return data.length > 0;
    }
    return data !== undefined && data !== null;
  }

  getLink(node) {
    let path = [];
    path.push('/resource');
    path.push(node);
    return path;
  }

}
