import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms'
import { ChartModule } from 'angular2-chartjs';
import { MaterialsModule } from '../materials.module';
import { WidgetsModule } from '../widgets/widgets.module';
import { CommandRoutingModule } from './command-routing.module';
import { CommandComponent } from './command.component';
import { ResultListComponent } from './result-list/result-list.component';
import { ResultDetailComponent } from './result-detail/result-detail.component';

@NgModule({
  imports: [
    CommonModule,
    CommandRoutingModule,
    MaterialsModule,
    WidgetsModule,
    FormsModule,
    ChartModule,
  ],
  declarations: [CommandComponent, ResultListComponent, ResultDetailComponent],
})
export class CommandModule { }
