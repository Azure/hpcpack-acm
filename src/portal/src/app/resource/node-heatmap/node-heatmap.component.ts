import { Component, OnInit, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'resource-node-heatmap',
  templateUrl: './node-heatmap.component.html',
  styleUrls: ['./node-heatmap.component.css']
})
export class NodeHeatmapComponent implements OnInit {
  @Input() nodes = [];
  @Output() clickNode: EventEmitter<any> = new EventEmitter();

  constructor() { }

  ngOnInit() {
  }

  nodeClass(node): string {
    let res;
    if (node.runningJobCount < 10)
      res = 'low';
    else if (node.runningJobCount < 50)
      res = 'median';
    else
      res = 'high';
    return res;
  }

  nodeTip(node): string {
    return `${node.name}: ${node.runningJobCount} jobs`;
  }

  clickRow(node): void {
    this.clickNode.emit(node);
  }
}
