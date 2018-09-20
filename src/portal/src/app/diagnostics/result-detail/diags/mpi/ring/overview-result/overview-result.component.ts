import { Component, OnInit, Input, OnChanges, SimpleChange } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';

@Component({
  selector: 'ring-overview-result',
  templateUrl: './overview-result.component.html',
  styleUrls: ['./overview-result.component.scss']
})
export class RingOverviewResultComponent {

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
    }
  }

}


