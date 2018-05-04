import { NgModule } from '@angular/core';

import {
  MatToolbarModule,
  MatSidenavModule,
  MatListModule,
  MatButtonModule,
  MatButtonToggleModule,
  MatIconModule,
  MatTableModule,
  MatTabsModule,
  MatFormFieldModule,
  MatInputModule,
  MatCheckboxModule,
  MatRadioModule,
  MatSelectModule,
  MatTooltipModule,
  MatStepperModule,
  MatDialogModule,
  MatMenuModule,
  MatExpansionModule,
  MatProgressBarModule,
  MatPaginatorModule,
  MatProgressSpinnerModule
} from '@angular/material';

import { DragulaModule } from 'ng2-dragula';
import { InfiniteScrollModule } from 'ngx-infinite-scroll';

const modules = [
  MatToolbarModule,
  MatSidenavModule,
  MatListModule,
  MatButtonModule,
  MatButtonToggleModule,
  MatIconModule,
  MatTableModule,
  MatTabsModule,
  MatFormFieldModule,
  MatInputModule,
  MatCheckboxModule,
  MatRadioModule,
  MatSelectModule,
  MatTooltipModule,
  MatStepperModule,
  MatDialogModule,
  MatMenuModule,
  MatExpansionModule,
  MatProgressBarModule,
  MatPaginatorModule,
  MatProgressSpinnerModule,
  DragulaModule,
  InfiniteScrollModule,
];

@NgModule({
  imports: modules,
  exports: modules,
})
export class MaterialsModule { }
