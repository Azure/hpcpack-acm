import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'dashboard-jobs',
  templateUrl: './jobs.component.html',
  styleUrls: ['./jobs.component.css']
})
export class JobsComponent implements OnInit {
  jobData = {
    labels: ['Configuring', 'Queued', 'Running', 'Finished', 'Failed', 'Canceled'],
    datasets: [{
      data: [57, 116, 98, 208, 13, 7],
      backgroundColor: [
        '#ececec',
        '#c1f9f7',
        '#20f5ed',
        '#44d42b',
        '#ff4e4e',
        '#6d6e71',
      ]
    }],
  };

  chartOption = {
    responsive: true,
    maintainAspectRatio: false,
    legend: {
      position: 'right',
    },
  };

  constructor() { }

  ngOnInit() {
  }

}
