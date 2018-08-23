import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MainRoutingModule } from './main-routing.module';
import { NotificationComponent } from '../notification/notification.component';
import { BreadcrumbComponent } from '../breadcrumb/breadcrumb.component';
import { MaterialsModule } from '../materials.module';
import { WidgetsModule } from '../widgets/widgets.module';
import { MainComponent } from './main.component';

@NgModule({
  declarations: [
    MainComponent,
    NotificationComponent,
    BreadcrumbComponent
  ],
  imports: [
    CommonModule,
    MainRoutingModule,
    MaterialsModule,
    WidgetsModule
  ]
})
export class MainModule { }
