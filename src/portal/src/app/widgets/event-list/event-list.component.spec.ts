import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { EventListComponent } from './event-list.component';

fdescribe('EventListComponent', () => {
  let component: EventListComponent;
  let fixture: ComponentFixture<EventListComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [EventListComponent]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(EventListComponent);
    component = fixture.componentInstance;
    component.events = [
      {
        content: "Dummy node event.",
        time: "2018-05-22T08:07:30.7182062+00:00",
        type: "Information",
        source: "Node"
      }, {
        content: "Dummy node event.",
        time: "2018-05-22T08:07:30.7182066+00:00",
        type: "Warning",
        source: "Node"
      }
    ];
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let text = fixture.nativeElement.querySelector(".event-content").textContent;
    expect(text).toEqual("Dummy node event.");
  });

  it('should not show empty message', () => {
    let empty = fixture.nativeElement.querySelector(".empty-msg");
    expect(empty).toEqual(null);
  })
});
