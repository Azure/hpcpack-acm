import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'pingpong-connectivity',
  templateUrl: './connectivity.component.html',
  styleUrls: ['./connectivity.component.scss']
})
export class ConnectivityComponent implements OnInit {

  @Input()
  nodes: any;

  public selectedConnectivity = {
    'nodes': ' - ',
    'latency': ' - ',
    'throughput': ' - ',
    'runtime': ' - '
  };

  constructor() { }

  ngOnInit() {
  }

  getConnectivityInfo(node, connectivity) {
    let connectivityInfo = connectivity[Object.keys(connectivity)[0]];
    let connectNodes = `${this.getNodeName(node)} <---> ${Object.keys(connectivity)[0]}`;
    let latency = connectivityInfo['Latency'] == undefined ? ' - ' : connectivityInfo['Latency'];
    let throughput = connectivityInfo['Throughput'] == undefined ? ' - ' : connectivityInfo['Throughput'];
    let runtime = connectivityInfo['Runtime'] == undefined ? ' - ' : connectivityInfo['Runtime'];
    this.selectedConnectivity = {
      'nodes': connectNodes,
      'latency': latency,
      'throughput': throughput,
      'runtime': runtime
    };
  }

  connectivityTip(node, connectivity) {
    let connectivityInfo = connectivity[Object.keys(connectivity)[0]];
    let connectNodes = `${this.getNodeName(node)} <---> ${Object.keys(connectivity)[0]}`;
    let latency = connectivityInfo['Latency'] == undefined ? ' - ' : connectivityInfo['Latency'];
    let throughput = connectivityInfo['Throughput'] == undefined ? ' - ' : connectivityInfo['Throughput'];
    let runtime = connectivityInfo['Runtime'] == undefined ? ' - ' : connectivityInfo['Runtime'];
    return `${connectNodes}\r\nLatency: ${latency}\r\nThroughput: ${throughput}\r\nRuntime: ${runtime}`;
  }

  getConnectivity(node) {
    return node[Object.keys(node)[0]];
  }

  public colorMap = {
    'Bad': 'bad-connectivity',
    'Warning': 'warning-connectivity',
    'Good': 'good-connectivity'
  };

  connectivityClass(connectivity) {
    let color = connectivity[Object.keys(connectivity)[0]]['Connectivity'];
    return this.colorMap[color] ? this.colorMap[color] : 'bad-connectivity';
  }

  getNodeName(node) {
    return Object.keys(node)[0];
  }

  trackByNodeFn(index, item) {
    return index;
  }

  trackByConnectivityFn(index, item) {
    return index;
  }
}
