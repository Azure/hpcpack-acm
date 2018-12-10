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
import { CommandOutputComponent } from './command-output/command-output.component';
import { NodeSelectorComponent } from './node-selector/node-selector.component';
import { CommandInputComponent } from './command-input/command-input.component';
import { SharedModule } from '../shared.module';
import { TaskErrorComponent } from './node-selector/task-error/task-error.component';
import { MultiCmdsComponent } from './multi-cmds/multi-cmds.component';
import { ScrollingModule } from '@angular/cdk/scrolling';

@NgModule({
  imports: [
    CommonModule,
    CommandRoutingModule,
    MaterialsModule,
    WidgetsModule,
    FormsModule,
    ChartModule,
    SharedModule,
    ScrollingModule
  ],
  declarations: [CommandComponent, ResultListComponent, ResultDetailComponent, CommandOutputComponent, NodeSelectorComponent, CommandInputComponent, TaskErrorComponent, MultiCmdsComponent],
  entryComponents: [CommandInputComponent, TaskErrorComponent],
})
export class CommandModule { }
