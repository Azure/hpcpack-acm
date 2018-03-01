import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'dashboard-node-health-history',
  templateUrl: './node-health-history.component.html',
  styleUrls: ['./node-health-history.component.css']
})
export class NodeHealthHistoryComponent implements OnInit {
  chartData = {};

  chartOption = {
    responsive: true,
    maintainAspectRatio: false,
    legend: {
      position: 'bottom',
    },
    scales: {
      xAxes: [{
        stacked: true,
      }],
      yAxes: [{
        stacked: true
      }]
    }
  };

  history = {};

  constructor() {}

  ngOnInit() {
    this.changeHistory('2h');
  }

  generateTuple(total) {
    //These are for ok, warning, error, transitional and unapproved, in the order.
    const portions = [60, 20, 10, 5, 5];
    let rand = portions.map(e => Math.random() * e);
    let sum = rand.reduce((acc, e) => acc + e);
    return rand.map(e => Math.round((e / sum) * total));
  }

  formatTime(d, withDay = false) {
    let h:any = d.getHours();
    if (h < 10)
      h = '0' + h;
    let m:any = d.getMinutes();
    if (m < 10)
      m = '0' + m;
    let time = '' + h + ':' + m;
    if (withDay) {
      let day:any = d.getDate();
      if (day < 10)
        day = '0' + day;
      let mon:any = d.getMonth() + 1;
      if (mon < 10)
        mon = '0' + mon;
      let md:any = '' + mon + '/' + day;
      time = md + ' ' + time;
    }
    return time;
  }

  generateTimeLabels(now, span, num) {
    let labels = [];
    for (let i = 0; i < num; i++) {
      let d = new Date(now - span * i);
      labels.push(this.formatTime(d, true));
    }
    labels.reverse();
    return labels;
  }

  generateChartData(span, columns) {
    const labels = [
      'OK',
      'Warning',
      'Error',
      'Transitional',
      'Unapproved',
    ]
    const colors = [
      '#44d42b',
      '#ffee0a',
      '#ff4e4e',
      '#20f5ed',
      '#6d6e71',
    ]
    let now = new Date().getTime();
    let values = [];
    for (let i = 0; i < columns; i++) {
      values.push(this.generateTuple(330));
    }
    let datasets = [];
    for (let i = 0; i < 5; i++) {
      let data = [];
      for (let j = 0; j < columns; j++) {
        data.push(values[j][i]);
      }
      datasets.push({ label: labels[i], data: data, backgroundColor: colors[i] });
    }

    this.chartData = {
      labels: this.generateTimeLabels(now, span, columns),
      datasets: datasets,
    };
  }

  changeHistory(time) {
    let result = history[time];
    if (result)
      return result;

    if (time == '2h') {
      //span = 5 minutes
      result = this.generateChartData(10 * 60 * 1000, 12);
    }
    else if (time == '24h') {
      //span = 2 hours
      result = this.generateChartData(1 * 60 * 60 * 1000, 24);
    }
    else if (time == '7d') {
      //span = 12 hours
      result = this.generateChartData(24 * 60 * 60 * 1000, 7);
    }
    history[time] = result;
    return result;
  }
}
