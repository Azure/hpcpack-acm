import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuardService } from '../services/auth-guard.service';
import { CommandComponent } from './command.component';
import { ResultListComponent } from './result-list/result-list.component';
import { ResultDetailComponent } from './result-detail/result-detail.component';

const routes: Routes = [{
  path: '',
  component: CommandComponent,
  canActivate: [AuthGuardService],
  children: [
    { path: 'results', component: ResultListComponent, data: { breadcrumb: "Results" }},
    { path: 'results/:id', component: ResultDetailComponent, data: { breadcrumb: "Result" }},
    { path: '', redirectTo: 'results', pathMatch: 'full' },
  ],
}];

@NgModule({
  imports: [ RouterModule.forChild(routes) ],
  exports: [ RouterModule ],
})
export class CommandRoutingModule { }
