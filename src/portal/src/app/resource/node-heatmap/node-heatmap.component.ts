import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { Observable } from 'rxjs';
import { TimerObservable } from 'rxjs/observable/TimerObservable';
import 'rxjs/add/operator/takeWhile';
import { FormControl } from '@angular/forms';

@Component({
  selector: 'resource-node-heatmap',
  templateUrl: './node-heatmap.component.html',
  styleUrls: ['./node-heatmap.component.css']
})
export class NodeHeatmapComponent implements OnInit, OnDestroy {
  private nodes = [];
  private categories = [];
  private alive: boolean;
  private interval: number;
  private selectedCategory: string;

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private api: ApiService,
  ){
    this.alive = true;
    this.interval = 3000;
    this.selectedCategory = "cpu";
  }

  ngOnInit() {
    this.api.heatmap.getCategories().subscribe(categories => {
      this.categories = categories;
      console.log(categories);
    })

    TimerObservable.create(0, this.interval)
      .takeWhile(() => this.alive)
      .subscribe(() => {
        //If you want to emulate the get operation, please call the in-memory web api function below.
        //this.api.heatmapInfo.getMockData(this.selectedCategory)
        this.api.heatmap.get(this.selectedCategory)
          .subscribe((data) => {
            this.nodes = data.results;
          });
      })
  }

  nodeClass(node): string {
    let res;
    if (!node.value) {
      return;
    }

    if (node.value < 5) {
      res = 'low';
    } else if (node.value < 10) {
      res = 'median';
    } else {
      res = 'high';
    }
    return res;
  }

  nodeTip(node): string {
    return `${node.id} : `.concat(node.value ? `${node.value} %` : 'offline');
  }

  clickNode(node): void {
    this.router.navigate(['..', node.nodeName], { relativeTo: this.route })
  }

  ngOnDestroy(){
    this.alive = false;
  }
}
