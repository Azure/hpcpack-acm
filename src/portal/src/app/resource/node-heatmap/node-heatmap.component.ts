import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'resource-node-heatmap',
  templateUrl: './node-heatmap.component.html',
  styleUrls: ['./node-heatmap.component.css']
})
export class NodeHeatmapComponent implements OnInit {
  private nodes = [];

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private api: ApiService,
  ) {}

  ngOnInit() {
    this.api.node.getAll().subscribe(nodes => {
      this.nodes = nodes;
    });
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

  clickNode(node): void {
    this.router.navigate(['..', node.id], { relativeTo: this.route })
  }
}
