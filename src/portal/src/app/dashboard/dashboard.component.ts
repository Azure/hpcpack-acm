import { Component, OnInit } from '@angular/core';
import { ApiService } from "../services/api.service";

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {

  constructor(
    private api: ApiService
  ) { }

  private nodes = {};
  private totalNodes = 0;

  private diags = {};
  private clusrun = {};

  ngOnInit() {
    this.getNodes();
    this.getDiags();
    this.getClusrun();
  }

  getNodes() {
    this.api.dashboard.getNodes().subscribe(res => {
      this.nodes = res.data;
      let states = Object.keys(this.nodes);
      for (let i = 0; i < states.length; i++) {
        this.totalNodes += this.nodes[states[i]];
      }
    });
  }

  getDiags() {
    this.api.dashboard.getDiags().subscribe(res => {
      this.diags = res.data;
    });
  }

  getClusrun() {
    this.api.dashboard.getClusrun().subscribe(res => {
      this.clusrun = res.data;
    });
  }

  onAutoNew(category: string) {
    if (category == "Diagnostics") {
      this.getDiags();
    }
    else if (category == "ClusRun") {
      this.getClusrun();
    }
  }

}
