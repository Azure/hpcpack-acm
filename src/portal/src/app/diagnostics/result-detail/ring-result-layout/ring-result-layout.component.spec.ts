import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RingResultLayoutComponent } from './ring-result-layout.component';

describe('RingResultLayoutComponent', () => {
  let component: RingResultLayoutComponent;
  let fixture: ComponentFixture<RingResultLayoutComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RingResultLayoutComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RingResultLayoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
