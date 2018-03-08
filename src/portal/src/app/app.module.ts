import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgModule } from '@angular/core';
import { HttpClientModule } from '@angular/common/http';
import { MaterialsModule } from './materials.module';


import { AppRoutingModule } from './app-routing.module';
import { AuthGuardService } from './auth-guard.service';
import { AuthService } from './auth.service';
import { LoginGuardService } from './login-guard.service';
import { ApiService } from './api.service';
import { AppComponent } from './app.component';
import { LoginComponent } from './login/login.component';
import { NotificationsComponent } from './notifications/notifications.component';
import { BreadcrumbComponent } from './breadcrumb/breadcrumb.component';


@NgModule({
  declarations: [
    AppComponent,
    LoginComponent,
    NotificationsComponent,
    BreadcrumbComponent,
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    MaterialsModule,
    AppRoutingModule,
  ],
  providers: [AuthGuardService, AuthService, LoginGuardService, ApiService],
  bootstrap: [AppComponent]
})
export class AppModule { }
