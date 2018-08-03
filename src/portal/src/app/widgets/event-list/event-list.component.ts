import { Component, OnInit, Input, OnChanges, SimpleChange } from '@angular/core';

@Component({
  selector: 'app-event-list',
  templateUrl: './event-list.component.html',
  styleUrls: ['./event-list.component.scss']
})
export class EventListComponent implements OnInit {
  @Input()
  events: any;

  hasTraceStack = false;

  message: string;

  constructor(
  ) { }

  ngOnInit() {
    if (this.events.length < 1) {
      this.message = "No event to show!";
    }
    else {
      this.events.forEach(element => {
        element.content = this.getEventInfo(element.content);
      });
    }
  }

  ngOnChanges(changes: { [propKey: string]: SimpleChange }) {
    if (this.events.length > 0) {
      this.events.forEach(element => {
        if (element.content) {
          element.content = this.getEventInfo(element.content);
        }
      });
    }
  }


  eventType(source, type) {
    let eventType = `${source} ${type}`;
    switch (eventType) {
      case 'Node Information': return 'node-information';
      case 'Node Warning': return 'node-warning';
      case 'Job Alert': return 'job-alert';
      default: return 'default';
    }
  }

  getEventInfo(content: string) {
    let _content = content.split('\n');
    return _content;
  }

}
