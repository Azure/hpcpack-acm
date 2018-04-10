import { Component, OnInit, Input, Output, EventEmitter, ContentChild, TemplateRef } from '@angular/core';


@Component({
  selector: 'app-result-layout',
  templateUrl: './result-layout.component.html',
  styleUrls: ['./result-layout.component.scss']
})
export class ResultLayoutComponent implements OnInit {
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

  constructor() { }

  ngOnInit() {
    this.makeChartData();
  }

  makeChartData() {
    let success = 0;
    let failure = 0;

    this.tasks.forEach(task => {
      if (task.state == 'Finished')
        success++;
      else
        failure++;
    });

    console.log(this.tasks);
    console.log(success);
    console.log(failure);
    this.overviewData = {
      labels: ['Finished', 'Running'],
      datasets: [{
        data: [success, failure],
        backgroundColor: [
          '#3f51b5',
          '#4B4F66',
        ]
      }],
    };
  }

  stateIcon(state) {
    switch (state) {
      case 'Finished':
        return 'check';
      case 'Success':
        return 'check';
      case 'Error':
        return 'close';
      default: return 'close';
    }
  }

  title(name, state) {
    let res = name;
    // if (state === 'success') {
    //   res += ' Succeeded!';
    // }
    // else {
    //   res += ' Failed!';
    // }
    return res = res + ' ' + state;
  }
}
