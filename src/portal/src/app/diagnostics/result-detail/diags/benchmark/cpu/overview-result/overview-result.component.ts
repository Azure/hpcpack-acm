import { Component, OnInit, Input, OnChanges, SimpleChange } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';

@Component({
  selector: 'cpu-overview-result',
  templateUrl: './overview-result.component.html',
  styleUrls: ['./overview-result.component.scss']
})
export class OverviewResultComponent implements OnInit {
  @Input()
  result: any;

  constructor(private sanitizer: DomSanitizer) {
  }

  ngOnChanges(changes: { [propKey: string]: SimpleChange }) {
    this.updateOverviewData();
  }

  ngOnInit() {
    this.updateOverviewData();
  }

  res: any;
  description: string;
  title: string;
  updateOverviewData() {
    if (this.result !== undefined) {
      this.res = this.sanitizer.bypassSecurityTrustHtml(this.result.Html);
      this.title = this.result.Title;
      this.description = this.result.Description;
    }
  }

}
