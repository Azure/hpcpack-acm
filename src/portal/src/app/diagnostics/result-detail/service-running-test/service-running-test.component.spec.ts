import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ServiceRunningTestComponent } from './service-running-test.component';

describe('ServiceRunningTestComponent', () => {
  let component: ServiceRunningTestComponent;
  let fixture: ComponentFixture<ServiceRunningTestComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ServiceRunningTestComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ServiceRunningTestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
