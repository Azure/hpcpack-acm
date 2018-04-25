import { TestBed, ComponentFixture, async } from '@angular/core/testing';
import { Component, Directive, Input } from '@angular/core';

import { AppComponent } from './app.component';
import { MaterialsModule } from './materials.module';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from './services/auth.service';
import { ApiService } from './services/api.service';

@Component({ selector: 'app-breadcrumb', template: '' })
class BreadcrumStubComponent {}

@Component({ selector: 'app-notification', template: '' })
class NotificationStubComponent {
  @Input() items = [];
}

@Component({ selector: 'router-outlet', template: '' })
class RouterOutletStubComponent {}

@Directive({
  selector: '[routerLink]',
  host: { '(click)': 'onClick()' }
})
class RouterLinkDirectiveStub {
  @Input('routerLink') linkParams: any;
  navigatedTo: any = null;

  onClick() {
    this.navigatedTo = this.linkParams;
  }
}

const authServiceStub = {
  isLoggedIn: true,
  user: { name: 'Test User' },
  logout: () => {},
}

const apiServiceStub = {}

const routerStub = {
  navigate: () => {},
}

const activatedRouteStub  = {}

fdescribe('AppComponent', () => {
  let component: AppComponent;
  let fixture: ComponentFixture<AppComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [
        AppComponent,
        BreadcrumStubComponent,
        NotificationStubComponent,
        RouterOutletStubComponent,
        RouterLinkDirectiveStub,
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
    let text = fixture.nativeElement.querySelector('mat-nav-list').textContent;
    AppComponent.items.forEach(item => expect(text).toContain(item.title));
  });
});
