import { Component, OnInit, Input, Output, EventEmitter, ContentChild, TemplateRef, OnChanges, SimpleChange } from '@angular/core';
import { ApiService } from '../../../services/api.service'

@Component({
  selector: 'app-result-layout',
  templateUrl: './result-layout.component.html',
  styleUrls: ['./result-layout.component.scss']
})
export class ResultLayoutComponent implements OnInit, OnChanges {
  @Input()
  result: any;

  @Input()
  tasks: any;

  @Output()
  filterNodes: EventEmitter<any> = new EventEmitter();

  @ContentChild('nodes')
  nodesTemplate: TemplateRef<any>;

  overviewData: any = {};

  overviewOption = {
    responsive: true,
    maintainAspectRatio: false,
    legend: {
      position: 'right',
    },
    onClick: (event, item) => {
      if (!item || item.length == 0)
        return;
      let index = item[0]._index;
      let text = index == 0 ? 'success' : 'failure';
      this.filterNodes.emit(text);
    },
    onHover: (event, item) => {
      event.target.style.cursor = item.length == 0 ? 'default' : 'pointer';
    },
  };

  constructor(
    private api : ApiService
  ) { }

  ngOnInit() {
    this.makeChartData();
  }

  changeLog = [];
  ngOnChanges(changes: { [propKey: string]: SimpleChange }) {
    this.makeChartData();
    this.api.diag.getDiagJob(this.result.id).subscribe(res => {
      this.result = res;
    });

  }

  makeChartData() {
    let finished = 0;
    let failed = 0;
    let queued = 0;
    let running = 0;

    this.tasks.forEach(task => {
      if (task.state == 'Finished')
        finished++;
      else if (task.state == 'Failed')
        failed++;
      else if (task.state == 'Queued')
        queued++;
      else if (task.state == 'Running')
        running++;
    });

    this.overviewData = {
      labels: ['Finished', 'Failed', 'Queued', 'Running'],
      datasets: [{
        data: [finished, failed, queued, running],
        backgroundColor: [
          '#4CAF50',
          '#F44336',
          '#FF9800',
          '#3F51B5',
        ]
      }],
    };
  }

  stateIcon(state) {
    switch (state) {
      case 'Finished': return 'done';
      case 'Queued': return 'blur_linear';
      case 'Failed': return 'clear';
      case 'Running': return 'blur_on';
      case 'Canceled': return 'cancel';
      default: return 'autonew';
    }
  }

  title(name, state) {
    let res = name;
    return res = res + ' ' + state;
  }

}
