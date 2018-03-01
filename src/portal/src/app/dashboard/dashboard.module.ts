import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChartModule } from 'angular2-chartjs';
import { MaterialsModule } from '../materials.module';
import { DashboardRoutingModule } from './dashboard-routing.module';
import { DashboardComponent } from './dashboard.component';
import { NodeHealthComponent } from './node-health/node-health.component';
import { NodeStateComponent } from './node-state/node-state.component';
import { JobsComponent } from './jobs/jobs.component';
import { LocationComponent } from './location/location.component';
import { NodeHealthHistoryComponent } from './node-health-history/node-health-history.component';

@NgModule({
  imports: [
    CommonModule,
    DashboardRoutingModule,
    ChartModule,
    MaterialsModule,
  ],
  declarations: [DashboardComponent, NodeHealthComponent, NodeStateComponent, JobsComponent, LocationComponent, NodeHealthHistoryComponent],
  //bootstrap: [HomeComponent]
})
export class DashboardModule { }
