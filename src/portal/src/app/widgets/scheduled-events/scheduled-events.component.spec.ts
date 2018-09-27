import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ScheduledEventsComponent } from './scheduled-events.component';
import { MaterialsModule } from '../../materials.module';

fdescribe('ScheduledEventsComponent', () => {
  let component: ScheduledEventsComponent;
  let fixture: ComponentFixture<ScheduledEventsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ScheduledEventsComponent],
      imports: [MaterialsModule]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ScheduledEventsComponent);
    component = fixture.componentInstance;
    component.events = [
      {
        EventId: "f020ba2e-3bc0-4c40-a10b-86575a9eabd5",
        EventType: "Freeze",
        ResourceType: "VirtualMachine",
        Resources: ["FrontEnd_IN_0", "BackEnd_IN_0"],
        EventStatus: "Scheduled",
        NotBefore: "Mon, 19 Sep 2016 18:29:47 GMT"
      },
      {
        EventId: "f020ba2e-3bc0-4c40-a10b-86575a9eabe7",
        EventType: "Reboot",
        ResourceType: "VirtualMachine",
        Resources: ["FrontEnd_IN_0", "BackEnd_IN_0"],
        EventStatus: "Started",
        NotBefore: "Mon, 19 Sep 2016 18:29:47 GMT"
      },
      {
        EventId: "f020ba2e-3bc0-4c40-a10b-86575a9eaba9",
        EventType: "Redeploy",
        ResourceType: "VirtualMachine",
        Resources: ["FrontEnd_IN_0", "BackEnd_IN_0"],
        EventStatus: "Scheduled",
        NotBefore: "Mon, 19 Sep 2016 18:29:47 GMT"
      }
    ];
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let time = fixture.nativeElement.querySelectorAll('.event-time');
    expect(time.length).toEqual(3);
    let reboot = fixture.nativeElement.querySelector('.Started').textContent;
    expect(reboot).toEqual('Started');
  });
});
