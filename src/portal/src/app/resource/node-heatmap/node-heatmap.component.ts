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
    // this.api.node.getAll().subscribe(nodes => {
    //   this.nodes = nodes;
    // });

    this.api.heatmapInfo.getCategories().subscribe(categories => {
      this.categories = categories;
      console.log(categories);
    })

    TimerObservable.create(0, this.interval)
      .takeWhile(() => this.alive)
      .subscribe(() => {

        this.api.heatmapInfo.getHeatmapInfo(this.selectedCategory)
          .subscribe((data) => {
            this.nodes = this.api.heatmapInfo.normalizeHeatmapInfo(data);
          });
        
        // this.api.heatmapInfo.getFakedHeatmapInfo()
        //   .subscribe((data) => {
        //     this.nodes = data;
        //   });
      })  
  }


  nodeClass(node): string {
    let res;
    // if (node.runningJobCount < 10)
    //   res = 'low';
    // else if (node.runningJobCount < 50)
    //   res = 'median';
    // else
    //   res = 'high';

    if (node.value._Total < 5 || !node.value._Total){
      res = 'low';
    } else if (node.value._Total < 10){
      res = 'median';
    }else{
      res = 'high';
    }
    return res;
  }

  nodeTip(node): string {
    // return `${node.name}: ${node.runningJobCount} jobs`;
    if(!node.value._Total){
      return `${node.nodeName}`;
    }
    return `${node.nodeName}: ${(node.value._Total).toFixed(2)} %`;
  }

  clickNode(node): void {
    this.router.navigate(['..', node.id], { relativeTo: this.route })
  }

  ngOnDestroy(){
    this.alive = false;
  }
}
