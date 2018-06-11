import { Component, OnInit, Input, Output, EventEmitter, ContentChild, TemplateRef, OnChanges, SimpleChange } from '@angular/core';
import { ApiService } from '../../../services/api.service';
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
    private api: ApiService
  ) { }

  ngOnInit() {
    this.isDone();
  }

  ngOnChanges(changes: { [propKey: string]: SimpleChange }) {
    this.isDone();
  }

  isDone() {
    if (this.result.state == "Failed" || this.result.state == "Finished" || this.result.state == "Canceled") {
      this.done = true;
    }
  }
  cancelDiag() {
    this.api.diag.cancel(this.result.id).subscribe(res => {
      // console.log(res);
      console.trace();
    });
  }
}
