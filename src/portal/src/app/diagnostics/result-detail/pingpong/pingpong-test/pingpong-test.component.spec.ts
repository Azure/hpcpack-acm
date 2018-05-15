import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PingPongTestComponent } from './pingpong-test.component';

describe('PingpongTestComponent', () => {
  let component: PingPongTestComponent;
  let fixture: ComponentFixture<PingPongTestComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PingPongTestComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PingPongTestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
