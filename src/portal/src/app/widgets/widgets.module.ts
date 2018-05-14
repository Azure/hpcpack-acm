import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms'
import { MaterialsModule } from '../materials.module';
import { NodeFilterBuilderComponent } from './node-filter-builder/node-filter-builder.component';
import { BackButtonComponent } from './back-button/back-button.component';
import { TableOptionComponent } from './table-option/table-option.component';

const components = [
  NodeFilterBuilderComponent,
  BackButtonComponent,
  TableOptionComponent,
];

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    MaterialsModule,
  ],
  declarations: components,
  entryComponents: components,
  exports: components,
})
export class WidgetsModule {}
