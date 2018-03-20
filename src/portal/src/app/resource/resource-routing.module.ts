import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuardService } from '../auth-guard.service';
import { ResourceComponent } from './resource.component';
import { NodeListComponent } from './node-list/node-list.component';
import { NodeHeatmapComponent } from './node-heatmap/node-heatmap.component';
import { NodeDetailComponent } from './node-detail/node-detail.component';

const routes: Routes = [{
  path: '',
  component: ResourceComponent,
  canActivate: [AuthGuardService],
  children: [
    { path: 'list', component: NodeListComponent, data: { breadcrumb: "List" }},
    { path: 'heatmap', component: NodeHeatmapComponent, data: { breadcrumb: "Heatmap" }},
    { path: ':id', component: NodeDetailComponent, data: { breadcrumb: "Node" }},
    { path: '', redirectTo: 'list', pathMatch: 'full' },
  ],
}];

@NgModule({
  imports: [ RouterModule.forChild(routes) ],
  exports: [ RouterModule ],
})
export class ResourceRoutingModule { }
