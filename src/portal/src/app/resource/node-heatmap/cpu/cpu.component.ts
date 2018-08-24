import { Component, OnInit, Input } from '@angular/core';
import { HeatmapService } from '../../../services/heatmap/heatmap.service';

@Component({
  selector: 'heatmap-cpu',
  templateUrl: './cpu.component.html',
  styleUrls: ['./cpu.component.scss']
})
export class CpuComponent implements OnInit {

  @Input()
  activeMode: string;

  @Input()
  nodes: Array<any>;

  constructor(private heatmapService: HeatmapService) { }

  ngOnInit() {
  }

  nodeTip(node): string {
    return `${node.id} : `.concat(isNaN(node.value) ? `realtime data is not available` : `${node.value} %`);
  }

  nodeClass(core) {
    return this.heatmapService.nodeClass(core);
  }

  clickNode(node): void {
    return this.heatmapService.clickNode(node);
  }
}
