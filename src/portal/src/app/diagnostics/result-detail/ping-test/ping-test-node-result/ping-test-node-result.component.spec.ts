import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PingTestNodeResultComponent } from './ping-test-node-result.component';

describe('PingTestNodeResultComponent', () => {
  let component: PingTestNodeResultComponent;
  let fixture: ComponentFixture<PingTestNodeResultComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PingTestNodeResultComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PingTestNodeResultComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
