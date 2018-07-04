import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChartModule } from 'angular2-chartjs';
import { MaterialsModule } from '../materials.module';
import { DashboardRoutingModule } from './dashboard-routing.module';
import { DashboardComponent } from './dashboard.component';
import { NodeStateComponent } from './node-state/node-state.component';
import { LocationComponent } from './location/location.component';
import { NodeHealthHistoryComponent } from './node-health-history/node-health-history.component';
import { JobOverviewComponent } from './job-overview/job-overview.component';

@NgModule({
  imports: [
    CommonModule,
    DashboardRoutingModule,
    ChartModule,
    MaterialsModule,
  ],
  declarations: [DashboardComponent, NodeStateComponent, LocationComponent, NodeHealthHistoryComponent, JobOverviewComponent],
  //bootstrap: [HomeComponent]
})
export class DashboardModule { }
