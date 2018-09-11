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
  public nodes = [];
  public categories = [];
  private interval: number;
  public selectedCategory: string;
  private heatmapLoop: Object;
  private categoryWays = { cpu: ['By Node', 'By Core'] };
  public ways = [];
  public activeMode;

  constructor(
    private api: ApiService
  ) {
    this.interval = 3000;
    this.selectedCategory = "cpu";
    this.ways = this.categoryWays[this.selectedCategory];
    this.activeMode = this.ways[0];
  }

  ngOnInit() {
    this.api.heatmap.getCategories().subscribe(categories => {
      this.categories = categories;
    })

    this.heatmapLoop = this.getHeatmapInfo();
  }

  setActiveMode(way) {
    this.activeMode = way;
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

  ngOnDestroy() {
    if (this.heatmapLoop) {
      Loop.stop(this.heatmapLoop);
    }
  }
}
