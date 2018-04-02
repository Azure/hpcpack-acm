import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';

import { CommandComponent } from './command.component';

@Component({ selector: 'router-outlet', template: '' })
class RouterOutletStubComponent {}

fdescribe('CommandComponent', () => {
  let component: CommandComponent;
  let fixture: ComponentFixture<CommandComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CommandComponent, RouterOutletStubComponent ],
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CommandComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
