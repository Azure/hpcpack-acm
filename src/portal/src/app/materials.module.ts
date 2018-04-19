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
  MatProgressBarModule
} from '@angular/material';

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
  MatProgressBarModule
];

@NgModule({
  imports: modules,
  exports: modules,
})
export class MaterialsModule { }
