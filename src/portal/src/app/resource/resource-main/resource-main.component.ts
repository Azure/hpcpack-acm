import { Component, OnInit, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { MatTabGroup } from '@angular/material/tabs'
import { MatTableDataSource } from '@angular/material';
import { Subscription } from 'rxjs/Subscription';
import { NodeService } from '../node.service';
import { NodeListComponent } from '../node-list/node-list.component';
import { NodeHeatmapComponent } from '../node-heatmap/node-heatmap.component';

@Component({
  selector: 'app-resource-main',
  templateUrl: './resource-main.component.html',
  styleUrls: ['./resource-main.component.css']
})
export class ResourceMainComponent implements OnInit {
  private dataSource = new MatTableDataSource();

  private query = { view: '', filter: '' };

  @ViewChild(MatTabGroup)
  private tabs: MatTabGroup;

  @ViewChild(NodeListComponent)
  private list: NodeListComponent;

  @ViewChild(NodeHeatmapComponent)
  private map: NodeHeatmapComponent;

  private subcription: Subscription;

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private nodeService: NodeService,
  ) {}

  ngOnInit() {
    this.nodeService.getNodes().subscribe(nodes => {
      this.dataSource.data = nodes;
    });
    this.subcription = this.route.queryParamMap.subscribe(params => {
      this.query.view = params.get('view') || 'list';
      this.query.filter = params.get('filter');
      this.updateUI();
    });
  }

  ngOnDestroy() {
    this.subcription.unsubscribe();
  }

  updateUrl() {
    this.router.navigate(['.'], { relativeTo: this.route, queryParams: this.query});
  }

  updateUI() {
    let view = this.query.view;
    this.tabs.selectedIndex = (view == 'heatmap') ? 1 : 0;

    let filter = this.query.filter;
    this.dataSource.filter = filter;
  }

  onTabChanged(event): void {
    this.query.view = event.index == 0 ? 'list' : 'heatmap';
    this.updateUrl();
  }

  viewNodeDetail(node) {
    this.router.navigate([node.id], { relativeTo: this.route })
  }

  hasNoSelection(): boolean {
    return this.list.selectedData.length == 0;
  }
}
