import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuardService } from '../auth-guard.service';
import { ResourceComponent } from './resource.component';
import { NodeDetailComponent } from './node-detail/node-detail.component';
import { ResourceMainComponent } from './resource-main/resource-main.component';

const routes: Routes = [{
  path: '',
  component: ResourceComponent,
  canActivate: [AuthGuardService],
  children: [
    { path: '', component: ResourceMainComponent },
    { path: ':id', component: NodeDetailComponent, data: { breadcrumb: "Node" }},
  ],
}];

@NgModule({
  imports: [ RouterModule.forChild(routes) ],
  exports: [ RouterModule ],
})
export class ResourceRoutingModule { }
