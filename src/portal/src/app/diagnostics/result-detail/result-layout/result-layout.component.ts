import { Component, OnInit, Input, Output, EventEmitter, ContentChild, TemplateRef, OnChanges, SimpleChange } from '@angular/core';

@Component({
  selector: 'app-result-layout',
  templateUrl: './result-layout.component.html',
  styleUrls: ['./result-layout.component.scss']
})
export class ResultLayoutComponent implements OnInit {
  @Input()
  result: any;

  private done: boolean;

  @ContentChild('task')
  taskTemplate: TemplateRef<any>;

  @ContentChild('overview')
  overviewTemplate: TemplateRef<any>;

  constructor(
  ) { }

  ngOnInit() {
    this.isDone();
  }

  ngOnChanges(changes: { [propKey: string]: SimpleChange }) {
    this.isDone();
  }

  isDone() {
    if (this.result.state == "Failed" || this.result.state == "Finished") {
      this.done = true;
    }
  }
}
