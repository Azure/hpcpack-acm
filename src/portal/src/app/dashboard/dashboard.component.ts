import { Component, OnInit, OnDestroy } from '@angular/core';
import { ApiService, Loop } from "../services/api.service";

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {

  constructor(
    private api: ApiService
  ) { }

  private nodes = {};
  private totalNodes = 0;

  private diags = {};
  private clusrun = {};
  private nodesLoop: object;
  private diagsLoop: object;
  private clusrunLoop: object;
  private interval = 3000;

  ngOnInit() {
    this.nodesLoop = Loop.start(
      this.api.dashboard.getNodes(),
      {
        next: (res) => {
          this.totalNodes = 0;
          this.nodes = res.data;
          let states = Object.keys(this.nodes);
          for (let i = 0; i < states.length; i++) {
            this.totalNodes += this.nodes[states[i]];
          }
          return true;
        }
      },
      this.interval
    );

    this.diagsLoop = Loop.start(
      this.api.dashboard.getDiags(),
      {
        next: (res) => {
          this.diags = res.data;
          return true;
        }
      },
      this.interval
    );

    this.clusrunLoop = Loop.start(
      this.api.dashboard.getClusrun(),
      {
        next: (res) => {
          this.clusrun = res.data;
          return true;
        }
      },
      this.interval
    );
  }

  ngOnDestroy() {
    if (this.nodesLoop) {
      Loop.stop(this.nodesLoop);
    }
    if (this.diagsLoop) {
      Loop.stop(this.diagsLoop);
    }
    if (this.clusrunLoop) {
      Loop.stop(this.clusrunLoop);
    }
  }

}
