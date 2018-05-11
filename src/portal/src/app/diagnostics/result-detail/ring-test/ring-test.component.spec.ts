import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RingTestComponent } from './ring-test.component';

describe('RingTestComponent', () => {
  let component: RingTestComponent;
  let fixture: ComponentFixture<RingTestComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RingTestComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RingTestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
