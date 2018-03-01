import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { HttpClientInMemoryWebApiModule } from 'angular-in-memory-web-api';
import { FormsModule } from '@angular/forms'
import { ChartModule } from 'angular2-chartjs';
import { MaterialsModule } from '../materials.module';
import { WidgetsModule } from '../widgets/widgets.module';
import { DiagnosticsRoutingModule } from './diagnostics-routing.module';
import { DiagnosticsComponent } from './diagnostics.component';
import { ResultListComponent } from './result-list/result-list.component';
import { ResultDetailComponent } from './result-detail/result-detail.component';
import { InMemoryDataService }  from './in-memory-data.service';
import { DiagnosticsService } from './diagnostics.service';
import { ResultLayoutComponent } from './result-detail/result-layout/result-layout.component';
import { ServiceRunningTestComponent } from './result-detail/service-running-test/service-running-test.component';
import { PingTestComponent } from './result-detail/ping-test/ping-test.component';
import { PingTestNodeResultComponent } from './result-detail/ping-test/ping-test-node-result/ping-test-node-result.component';

@NgModule({
  imports: [
    CommonModule,
    HttpClientModule,

    // The HttpClientInMemoryWebApiModule module intercepts HTTP requests
    // and returns simulated server responses.
    // Remove it when a real server is ready to receive requests.
    HttpClientInMemoryWebApiModule.forFeature(InMemoryDataService, { apiBase: 'api/diagnostics/' }),

    DiagnosticsRoutingModule,
    MaterialsModule,
    WidgetsModule,
    ChartModule,
    FormsModule,
  ],
  declarations: [DiagnosticsComponent, ResultListComponent, ResultDetailComponent, ResultLayoutComponent, ServiceRunningTestComponent, PingTestComponent, PingTestNodeResultComponent],
  providers: [DiagnosticsService],
  entryComponents: [ServiceRunningTestComponent, PingTestComponent, PingTestNodeResultComponent],
})
export class DiagnosticsModule { }
