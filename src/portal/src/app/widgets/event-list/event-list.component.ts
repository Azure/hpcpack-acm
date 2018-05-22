import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'app-event-list',
  templateUrl: './event-list.component.html',
  styleUrls: ['./event-list.component.css']
})
export class EventListComponent implements OnInit {
  @Input()
  events: any;

  message: string;

  constructor(
  ) { }

  ngOnInit() {
    if (this.events.length < 1) {
      this.message = "No event to show!";
    }
  }

  eventType(source, type) {
    let eventType = source + ' ' + type;

    if (eventType == 'Node Information') {
      return 'node-information';
    }
    else if (eventType == 'Node Warning') {
      return 'node-warning';
    }
  }

}
