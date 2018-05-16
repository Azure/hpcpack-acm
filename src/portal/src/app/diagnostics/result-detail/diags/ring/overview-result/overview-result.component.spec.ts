import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RingOverviewResultComponent } from './overview-result.component';

describe('RingOverviewResultComponent', () => {
  let component: RingOverviewResultComponent;
  let fixture: ComponentFixture<RingOverviewResultComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RingOverviewResultComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RingOverviewResultComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
