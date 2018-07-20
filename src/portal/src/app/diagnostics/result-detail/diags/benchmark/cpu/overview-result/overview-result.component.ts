import { Component, OnInit, Input, OnChanges, SimpleChange } from '@angular/core';

@Component({
  selector: 'cpu-overview-result',
  templateUrl: './overview-result.component.html',
  styleUrls: ['./overview-result.component.scss']
})
export class OverviewResultComponent implements OnInit {
  @Input()
  result: any;

  constructor() { }

  ngOnChanges(changes: { [propKey: string]: SimpleChange }) {
    this.updateOverviewData();
  }

  ngOnInit() {
    this.updateOverviewData();
  }

  results: Array<any>;
  description: string;
  title: string;
  updateOverviewData() {
    if (this.result !== undefined) {
      this.results = this.result.Results;
      this.title = this.result.Title;
      this.description = this.result.Description;
    }
  }

}
