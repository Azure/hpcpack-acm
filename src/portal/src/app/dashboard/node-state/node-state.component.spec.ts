import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NodeStateComponent } from './node-state.component';
import { Router, ActivatedRoute } from '@angular/router';

fdescribe('NodeStateComponent', () => {
  let component: NodeStateComponent;
  let fixture: ComponentFixture<NodeStateComponent>;
  const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
  const activatedRouteSpy = jasmine.createSpyObj('ActivatedRoute', ['']);

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [NodeStateComponent],
      providers: [
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: activatedRouteSpy }
      ]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NodeStateComponent);
    component = fixture.componentInstance;
    component.state = "OK",
      component.stateNum = 1000;
    component.stateIcon = "lightbulb_outline";
    component.total = 1001;

    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let icon = fixture.nativeElement.querySelector('.icon .material-icons').textContent;
    expect(icon).toEqual('lightbulb_outline');
    let state = fixture.nativeElement.querySelector('.info-title .OK').textContent;
    expect(state).toEqual(' OK ');
    let num = fixture.nativeElement.querySelector('.info-content .OK').textContent;
    expect(num).toEqual('1000');
    let total = fixture.nativeElement.querySelector('.info-content .total').textContent;
    expect(total).toEqual('1001');
  });
});
