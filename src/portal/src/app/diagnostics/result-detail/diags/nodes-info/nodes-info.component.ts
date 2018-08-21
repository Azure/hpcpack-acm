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
  aggregationInfo: object;

  constructor() { }

  ngOnInit() {
  }

  hasData(data) {
    if (isArray(data)) {
      return data.length > 0;
    }
    if (isObject(data)) {
      let keys = Object.keys(data);
      if (keys.length == 0) {
        return false;
      }
      else {
        return keys.every(item => {
          return data[item] !== undefined && data[item] !== null;
        })
      }
    }
    return data !== undefined && data !== null;
  }

  inGroup(node, obj, propertyName) {
    if (this.hasData(obj) && this.hasData(obj[propertyName])) {
      let index = obj[propertyName].findIndex(item => item == node);
      return index == -1 ? false : true;
    }
  }

  isGood(node) {
    return this.inGroup(node, this.aggregationInfo, 'GoodNodes');
  }

  isBad(node) {
    return this.inGroup(node, this.aggregationInfo, 'BadNodes');
  }

  getLink(node) {
    let path = [];
    path.push('/resource');
    path.push(node);
    return path;
  }
}
