import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { ApiService, Loop } from '../../services/api.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'resource-node-heatmap',
  templateUrl: './node-heatmap.component.html',
  styleUrls: ['./node-heatmap.component.scss']
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
    private api: ApiService
  ) {
    this.interval = 3000;
    this.selectedCategory = "cpu";
  }

  ngOnInit() {
    this.api.heatmap.getCategories().subscribe(categories => {
      this.categories = categories;
    })

    this.heatmapLoop = this.getHeatmapInfo();
  }

  categoryCtrl(): void {
    this.nodes = [];

    if (this.heatmapLoop) {
      Loop.stop(this.heatmapLoop);
    }
    this.heatmapLoop = this.getHeatmapInfo();
  }

  getHeatmapInfo(): any {
    return Loop.start(
      //observable
      //If you want to emulate the get operation, please call the in-memory web api function below.
      //this.api.heatmap.getMockData(this.selectedCategory),
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

  private colorMap = [{
    value: 0, color: 'ten'
  }, {
    value: 1, color: 'twenty'
  }, {
    value: 2, color: 'thirty'
  }, {
    value: 3, color: 'forty'
  }, {
    value: 4, color: 'fifty'
  }, {
    value: 5, color: 'sixty'
  }, {
    value: 6, color: 'seventy'
  }, {
    value: 7, color: 'eighty'
  }, {
    value: 8, color: 'ninety'
  }, {
    value: 9, color: 'full'
  }];

  nodeClass(node): string {
    let res;
    if (isNaN(node.value)) {
      return;
    }

    if (node.value == 0) {
      return res = 'empty';
    }
    if (node.value == 100) {
      return res = 'full';
    }
    let val = Math.floor(node.value / 10);
    let item = this.colorMap.find(item => {
      return item.value == val;
    })
    res = item.color;
    return res;
  }

  nodeTip(node): string {
    return `${node.id} : `.concat(isNaN(node.value) ? `realtime data is not available` : `${node.value} %`);
  }

  clickNode(node): void {
    this.router.navigate(['..', node.id], { relativeTo: this.route })
  }

  ngOnDestroy() {
    if (this.heatmapLoop) {
      Loop.stop(this.heatmapLoop);
    }
  }
}
