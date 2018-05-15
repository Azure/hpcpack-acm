import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { OverviewResultComponent } from './overview-result.component';

describe('OverviewResultComponent', () => {
  let component: OverviewResultComponent;
  let fixture: ComponentFixture<OverviewResultComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ OverviewResultComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(OverviewResultComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
