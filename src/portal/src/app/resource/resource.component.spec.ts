import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ResourceComponent } from './resource.component';
import { Component } from '@angular/core';

@Component({ selector: 'router-outlet', template: '' })
class RouterOutletStubComponent { }

fdescribe('ResourceComponent', () => {
  let component: ResourceComponent;
  let fixture: ComponentFixture<ResourceComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ResourceComponent, RouterOutletStubComponent]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ResourceComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
