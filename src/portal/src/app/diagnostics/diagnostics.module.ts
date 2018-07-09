import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms'
import { ChartModule } from 'angular2-chartjs';
import { MaterialsModule } from '../materials.module';
import { WidgetsModule } from '../widgets/widgets.module';
import { DiagnosticsRoutingModule } from './diagnostics-routing.module';
import { DiagnosticsComponent } from './diagnostics.component';
import { ResultListComponent } from './result-list/result-list.component';
import { ResultDetailComponent } from './result-detail/result-detail.component';
import { PingPongReportComponent } from './result-detail/diags/pingpong/pingpong-report/pingpong-report.component';
import { TaskDetailComponent } from './result-detail/task/task-detail/task-detail.component';
import { PingPongOverviewResultComponent } from './result-detail/diags/pingpong/overview-result/overview-result.component';
import { TaskTableComponent } from './result-detail/task/task-table/task-table.component';
import { ResultLayoutComponent } from './result-detail/result-layout/result-layout.component';
import { RingReportComponent } from './result-detail/diags/ring/ring-report/ring-report.component';
import { RingOverviewResultComponent } from './result-detail/diags/ring/overview-result/overview-result.component';
import { NodesInfoComponent } from './result-detail/diags/nodes-info/nodes-info.component';

@NgModule({
  imports: [
    CommonModule,
    DiagnosticsRoutingModule,
    MaterialsModule,
    WidgetsModule,
    ChartModule,
    FormsModule,
  ],
  declarations: [DiagnosticsComponent, ResultListComponent, ResultDetailComponent, PingPongReportComponent, TaskDetailComponent, PingPongOverviewResultComponent, TaskTableComponent, ResultLayoutComponent, RingReportComponent, RingOverviewResultComponent, NodesInfoComponent],
  entryComponents: [RingReportComponent, RingOverviewResultComponent, PingPongReportComponent, TaskDetailComponent]
})
export class DiagnosticsModule { }