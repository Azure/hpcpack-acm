import { TestBed, ComponentFixture, async } from '@angular/core/testing';
import { Component, Directive, Input } from '@angular/core';

import { AppComponent } from './app.component';
import { MaterialsModule } from './materials.module';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from './services/auth.service';
import { ApiService } from './services/api.service';


@Component({ selector: 'router-outlet', template: '' })
class RouterOutletStubComponent { }

const authServiceStub = {
  isLoggedIn: true,
  user: { name: 'Test User' },
  logout: () => { },
  getUserInfo:() => {}
}

const apiServiceStub = {}

const routerStub = {
  navigate: () => { },
}

const activatedRouteStub = {}

fdescribe('AppComponent', () => {
  let component: AppComponent;
  let fixture: ComponentFixture<AppComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [
        AppComponent,
        RouterOutletStubComponent,
      ],
      imports: [
        NoopAnimationsModule,
        MaterialsModule,
      ],
      providers: [
        { provide: AuthService, useValue: authServiceStub },
        { provide: ApiService, useValue: apiServiceStub },
        { provide: Router, useValue: routerStub },
        { provide: ActivatedRoute, useValue: activatedRouteStub },
      ]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(AppComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  
  it('should create', () => {
    expect(component).toBeTruthy();
    fixture.detectChanges();
  });
});
