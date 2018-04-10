import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PingpongTestComponent } from './pingpong-test.component';

describe('PingpongTestComponent', () => {
  let component: PingpongTestComponent;
  let fixture: ComponentFixture<PingpongTestComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PingpongTestComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PingpongTestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
