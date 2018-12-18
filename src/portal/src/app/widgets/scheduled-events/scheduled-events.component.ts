import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'app-scheduled-events',
  templateUrl: './scheduled-events.component.html',
  styleUrls: ['./scheduled-events.component.scss']
})
export class ScheduledEventsComponent implements OnInit {

  @Input()
  events: any;

  message: string;

  constructor(
  ) { }

  ngOnInit() {
    if (this.events.length < 1) {
      this.message = "No scheduled event to show!";
    }
  }

  typeColor(type) {
    switch (type) {
      case 'Reboot': return 'reboot';
      case 'Redeploy': return 'redeploy';
      case 'Freeze': return 'freeze';
      default: return '';
    }
  }
}