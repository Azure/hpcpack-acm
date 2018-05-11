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
import { PingPongResultLayoutComponent } from './result-detail/pingpong-result-layout/pingpong-result-layout.component';
import { ServiceRunningTestComponent } from './result-detail/service-running-test/service-running-test.component';
import { PingTestComponent } from './result-detail/ping-test/ping-test.component';
import { PingTestNodeResultComponent } from './result-detail/ping-test/ping-test-node-result/ping-test-node-result.component';
import { PingPongTestComponent } from './result-detail/pingpong-test/pingpong-test.component';
import { DiagEventsComponent } from './result-list/diag-events/diag-events.component';
import { TaskDetailComponent } from './result-detail/task-detail/task-detail.component';
import { RingResultLayoutComponent } from './result-detail/ring-result-layout/ring-result-layout.component';
import { RingTestComponent } from './result-detail/ring-test/ring-test.component';

@NgModule({
  imports: [
    CommonModule,
    DiagnosticsRoutingModule,
    MaterialsModule,
    WidgetsModule,
    ChartModule,
    FormsModule,
  ],
  declarations: [DiagnosticsComponent, ResultListComponent, ResultDetailComponent, PingPongResultLayoutComponent, ServiceRunningTestComponent, PingTestComponent, PingTestNodeResultComponent, PingPongTestComponent, DiagEventsComponent, TaskDetailComponent, RingResultLayoutComponent, RingTestComponent],
  entryComponents: [ServiceRunningTestComponent, PingTestComponent, PingTestNodeResultComponent, RingTestComponent, PingPongTestComponent, DiagEventsComponent, TaskDetailComponent]
})
export class DiagnosticsModule { }
