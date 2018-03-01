import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { HttpClientInMemoryWebApiModule } from 'angular-in-memory-web-api';
import { FormsModule } from '@angular/forms'
import { TreeModule } from 'angular-tree-component';
import { ChartModule } from 'angular2-chartjs';
import { ResourceRoutingModule } from './resource-routing.module';
import { MaterialsModule } from '../materials.module';
import { WidgetsModule } from '../widgets/widgets.module';
import { ResourceComponent } from './resource.component';
import { NodeListComponent } from './node-list/node-list.component';
import { NodeDetailComponent } from './node-detail/node-detail.component';
import { NodeHeatmapComponent } from './node-heatmap/node-heatmap.component';
import { NodeService } from './node.service';
import { InMemoryDataService }  from './in-memory-data.service';
import { ResourceMainComponent } from './resource-main/resource-main.component';
import { NewDiagnosticsComponent } from './new-diagnostics/new-diagnostics.component';
import { NewCommandComponent } from './new-command/new-command.component';

@NgModule({
  imports: [
    CommonModule,
    HttpClientModule,

    // The HttpClientInMemoryWebApiModule module intercepts HTTP requests
    // and returns simulated server responses.
    // Remove it when a real server is ready to receive requests.
    HttpClientInMemoryWebApiModule.forFeature(InMemoryDataService),

    ResourceRoutingModule,
    MaterialsModule,
    FormsModule,
    TreeModule,
    WidgetsModule,
    ChartModule,
  ],
  declarations: [ResourceComponent, NodeListComponent, NodeDetailComponent, NodeHeatmapComponent, ResourceMainComponent, NewDiagnosticsComponent, NewCommandComponent],
  providers: [NodeService],
  entryComponents: [NewDiagnosticsComponent, NewCommandComponent],
})
export class ResourceModule { }
