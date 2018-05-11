import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PingPongResultLayoutComponent } from './pingpong-result-layout.component';

describe('PingPongResultLayoutComponent', () => {
  let component: PingPongResultLayoutComponent;
  let fixture: ComponentFixture<PingPongResultLayoutComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PingPongResultLayoutComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PingPongResultLayoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
