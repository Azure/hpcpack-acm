import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { ApiService, Loop } from '../../services/api.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'resource-node-heatmap',
  templateUrl: './node-heatmap.component.html',
  styleUrls: ['./node-heatmap.component.css']
})
export class NodeHeatmapComponent implements OnInit, OnDestroy {
  private nodes = [];
  private categories = [];
  private interval: number;
  private selectedCategory: string;
  private heatmapLoop: Object;

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private api: ApiService,
  ){
    this.interval = 3000;
    this.selectedCategory = "cpu";
  }

  ngOnInit() {
    this.api.heatmap.getCategories().subscribe(categories => {
      this.categories = categories;
    })

    this.heatmapLoop = Loop.start(
      //observable
      //If you want to emulate the get operation, please call the in-memory web api function below.
      //this.api.heatmapInfo.getMockData(this.selectedCategory)
      this.api.heatmap.get(this.selectedCategory),
      //observer
      {
        next: (result) => {
          this.nodes = result.results;
          return true;
        }
      },
      //interval in ms
      this.interval
    );
  }

  categoryCtrl(): void {
    this.nodes = [];
  }

  nodeClass(node): string {
    let res;
    if (isNaN(node.value)) {
      return;
    }

    if (node.value < 5) {
      res = 'low';
    }
    else if (node.value < 10) {
      res = 'median';
    }
    else {
      res = 'high';
    }
    return res;
  }

  nodeTip(node): string {
    return `${node.id} : `.concat(isNaN(node.value) ? 'offline' : `${node.value} %`);
  }

  clickNode(node): void {
    this.router.navigate(['..', node.id], { relativeTo: this.route })
  }

  ngOnDestroy() {
    if(this.heatmapLoop) {
      Loop.stop(this.heatmapLoop);
    }
  }
}
