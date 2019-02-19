import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgModule } from '@angular/core';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { HttpClientInMemoryWebApiModule } from 'angular-in-memory-web-api';
import { MaterialsModule } from './materials.module';
import { AppRoutingModule } from './app-routing.module';
import { AuthService } from './services/auth.service';
import { LoginGuardService } from './services/login-guard.service';
import { ApiService } from './services/api.service';
import { UserSettingsService } from './services/user-settings.service';
import { LocalStorageService } from './services/local-storage.service';
import { InMemoryDataService } from './services/in-memory-data.service';
import { AppComponent } from './app.component';
import { WidgetsModule } from './widgets/widgets.module';
import { JobStateService } from './services/job-state/job-state.service';
import { TableService } from './services/table/table.service';
import { VirtualScrollService } from './services/virtual-scroll/virtual-scroll.service';
import { DateFormatterService } from './services/date-formatter/date-formatter.service';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { DiagReportService } from './services/diag-report/diag-report.service';
import { DragulaModule } from 'ng2-dragula';
import { ScrollingModule } from '@angular/cdk/scrolling';

@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    MaterialsModule,
    WidgetsModule,
    AppRoutingModule,
    DragulaModule.forRoot(),
    ScrollingModule
  ],
  providers: [
    AuthService,
    LoginGuardService,
    ApiService,
    JobStateService,
    DateFormatterService,
    TableService,
    VirtualScrollService,
    UserSettingsService,
    LocalStorageService,
    DiagReportService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
