import { Component, OnInit, Input, TemplateRef, ContentChild, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-content-window',
  templateUrl: './content-window.component.html',
  styleUrls: ['./content-window.component.scss']
})
export class ContentWindowComponent implements OnInit {
  @Input()
  side = 'right';

  @Input()
  title = '';

  @Input()
  width: number;

  @Output()
  showWnd = new EventEmitter<boolean>();

  @ContentChild('wndContent')
  contentTemplate: TemplateRef<any>;

  constructor() { }

  ngOnInit() {
  }

  hideWnd() {
    this.showWnd.emit(false);
  }
}
