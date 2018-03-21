import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'app-notification',
  templateUrl: './notification.component.html',
  styleUrls: ['./notification.component.css']
})
export class NotificationComponent implements OnInit {
  @Input()
  items: any[];

  constructor() {}

  ngOnInit() {
  }

  private iconMap = {
    info: 'info_outline',
    error: 'error',
    warning: 'warning',
  };

  private icon(item): string {
    let ic = this.iconMap[item.type];
    if (!ic)
      ic = 'info_outline';
    return ic;
  }

  private removeItem(item): void {
    for(let i = 0; i < this.items.length; i++) {
      if (this.items[i].id == item.id) {
        this.items.splice(i, 1);
        break;
      }
    }
  }
}
