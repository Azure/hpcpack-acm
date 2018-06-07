import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ScheduledEventsComponent } from './scheduled-events.component';

describe('ScheduledEventsComponent', () => {
  let component: ScheduledEventsComponent;
  let fixture: ComponentFixture<ScheduledEventsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ScheduledEventsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ScheduledEventsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
