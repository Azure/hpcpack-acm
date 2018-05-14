import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'app-event-list',
  templateUrl: './event-list.component.html',
  styleUrls: ['./event-list.component.css']
})
export class EventListComponent implements OnInit {
  @Input()
  events: any;

  constructor() { }

  ngOnInit() {
  }

  eventType(type) {
    if (type == 'node event') {
      return 'node-event';
    }
    else if (type == 'Azure scheduled event') {
      return 'azure-event';
    }
  }

}
