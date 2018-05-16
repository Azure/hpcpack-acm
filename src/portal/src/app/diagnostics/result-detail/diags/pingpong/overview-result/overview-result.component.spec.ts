import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PingPongOverviewResultComponent } from './overview-result.component';

describe('PingPongOverviewResultComponent', () => {
  let component: PingPongOverviewResultComponent;
  let fixture: ComponentFixture<PingPongOverviewResultComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [PingPongOverviewResultComponent]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PingPongOverviewResultComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
