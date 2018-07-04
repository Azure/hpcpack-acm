import { Component, OnInit, Input, OnChanges, SimpleChanges, Output, EventEmitter } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
  selector: 'dashboard-job-overview',
  templateUrl: './job-overview.component.html',
  styleUrls: ['./job-overview.component.scss']
})
export class JobOverviewComponent implements OnInit, OnChanges {
  @Input() icon: string;
  @Input() jobs: any;
  @Input() jobCategory: string;
  @Output() autoNew = new EventEmitter();

  private totalJobs = 0;
  private activeJobs = 0;
  private loading = true;

  private labels = [
    'Queued',
    'Running',
    'Finishing',
    'Finished',
    'Canceling',
    'Canceled',
    'Failed'
  ];
  private colors = [
    'rgba(66, 134, 244, .8)',
    'rgba(63, 81, 181, .8)',
    'rgba(153, 229, 100, .8)',
    'rgba(47, 196, 134, .8)',
    'rgba(232, 199, 37, .8)',
    'rgba(206, 206, 206, .8)',
    'rgba(229, 83, 57, .8)'
  ];

  chartData = {};

  chartOption = {
    responsive: true,
    maintainAspectRatio: false,
    legend: {
      display: false
    },
    scales: {
      yAxes: [{
        ticks: {
          beginAtZero: true,
          min: 0,
          callback: function (value, index, values) {
            if (Math.floor(value) === value) {
              return value;
            }
          }
        }
      }]
    }
  };


  constructor(
    private router: Router,
    private route: ActivatedRoute
  ) { }

  ngOnInit() {

  }

  ngOnChanges(changes: SimpleChanges) {
    let states = Object.keys(this.jobs);
    if (states.length > 0) {
      this.loading = false;
      for (let i = 0; i < states.length; i++) {
        this.totalJobs += this.jobs[states[i]];
        if (states[i] !== 'Finished' && states[i] !== 'Failed' && states[i] !== 'Canceled') {
          this.activeJobs += this.jobs[states[i]];
        }
      }
      this.generateChartData(this.jobs);
    }

  }

  generateChartData(jobs) {

    let data = [];
    for (let i = 0; i < this.labels.length; i++) {
      data.push(jobs[this.labels[i]]);
    }

    this.chartData = {
      labels: this.labels,
      datasets: [{
        data: data,
        backgroundColor: this.colors
      }]
    };
  }

  autoNewAction() {
    this.loading = true;
    this.autoNew.emit(this.jobCategory);
  }

  jobInfo() {
    let category = ['..'];
    if (this.jobCategory == 'Diagnostics') {
      category.push('diagnostics');
    }
    else if (this.jobCategory == 'ClusRun') {
      category.push('command');
    }
    category.push('results');
    this.router.navigate(category, { relativeTo: this.route });
  }
}
