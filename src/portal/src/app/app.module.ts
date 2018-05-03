import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgModule } from '@angular/core';
import { HttpClientModule } from '@angular/common/http';
import { HttpClientInMemoryWebApiModule } from 'angular-in-memory-web-api';
import { MaterialsModule } from './materials.module';


import { environment as env } from '../environments/environment';
import { AppRoutingModule } from './app-routing.module';
import { AuthGuardService } from './services/auth-guard.service';
import { AuthService } from './services/auth.service';
import { LoginGuardService } from './services/login-guard.service';
import { ApiService } from './services/api.service';
import { UserSettingsService } from './services/user-settings.service';
import { LocalStorageService } from './services/local-storage.service';
import { InMemoryDataService }  from './services/in-memory-data.service';
import { AppComponent } from './app.component';
import { LoginComponent } from './login/login.component';
import { NotificationComponent } from './notification/notification.component';
import { BreadcrumbComponent } from './breadcrumb/breadcrumb.component';


@NgModule({
  declarations: [
    AppComponent,
    LoginComponent,
    NotificationComponent,
    BreadcrumbComponent,
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    //HttpClientInMemoryWebApiModule.forRoot(
    //  InMemoryDataService,
    //  {
    //    apiBase: env.apiBase,
    //    passThruUnknownUrl: true
    //  }
    //),
    MaterialsModule,
    AppRoutingModule,
  ],
  providers: [AuthGuardService, AuthService, LoginGuardService, ApiService,
    UserSettingsService, LocalStorageService],
  bootstrap: [AppComponent]
})
export class AppModule { }
