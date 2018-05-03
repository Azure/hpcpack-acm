import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
  selector: 'dashboard-node-health',
  templateUrl: './node-health.component.html',
  styleUrls: ['./node-health.component.css']
})
export class NodeHealthComponent implements OnInit {
  healthData = {
    labels: ['OK', 'Warning', 'Error', 'Transitional', 'Unapproved'],
    datasets: [{
      data: [257, 33, 20, 18, 12],
      backgroundColor: [
        '#2da875',
        '#d19317',
        '#ce5039',
        '#0daeba',
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
    onClick: (event, items) => {
      if (!items || items.length == 0)
        return;
      let index = items[0]._index;
      let text = this.healthData.labels[index].toLowerCase();
      this.router.navigate(['..', 'resource'], { relativeTo: this.route, queryParams: { filter: text }});
    },
    onHover: (event, items) => {
      event.target.style.cursor = items.length == 0 ? 'default' : 'pointer';
    },
  };

  constructor(
    private router: Router,
    private route: ActivatedRoute,
  ) {}

  ngOnInit() {
  }

}
