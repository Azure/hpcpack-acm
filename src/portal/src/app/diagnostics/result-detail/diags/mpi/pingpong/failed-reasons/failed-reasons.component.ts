import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'pingpong-failed-reasons',
  templateUrl: './failed-reasons.component.html',
  styleUrls: ['./failed-reasons.component.scss']
})
export class FailedReasonsComponent implements OnInit {
  @Input()
  failedNodes: any;

  @Input()
  failedReasons: any;

  nodes: any;

  activeMode = "total";
  selectedNode: string;
  failure = [];
  reasons = [];
  constructor() { }

  ngOnInit() {
    this.nodes = Object.keys(this.failedNodes);
    this.showReasons();
  }

  setActiveMode(mode: string) {
    this.activeMode = mode;
    this.showReasons();
  }

  showReasons() {
    if (!this.selectedNode) {
      this.selectedNode = this.nodes[0];
    }
    if (this.activeMode == 'total') {
      this.failure = [];
      this.reasons = this.failedReasons;
    }
    else {
      if (this.failedNodes[this.selectedNode]) {
        this.failure = Object.keys(this.failedNodes[this.selectedNode]);
      }
      else {
        this.failure = [];
      }
    }
  }

  changeNode() {
    this.showReasons();
  }

  getLink(node) {
    let path = [];
    path.push('/resource');
    path.push(node);
    return path;
  }
}


