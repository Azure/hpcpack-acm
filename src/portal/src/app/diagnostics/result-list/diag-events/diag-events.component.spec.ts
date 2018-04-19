import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DiagEventsComponent } from './diag-events.component';

describe('DiagEventsComponent', () => {
  let component: DiagEventsComponent;
  let fixture: ComponentFixture<DiagEventsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DiagEventsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DiagEventsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
