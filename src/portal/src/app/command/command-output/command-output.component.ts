import { Component, OnInit, Input, Output, EventEmitter, ViewChild, ElementRef } from '@angular/core';

@Component({
  selector: 'command-output',
  templateUrl: './command-output.component.html',
  styleUrls: ['./command-output.component.css']
})
export class CommandOutputComponent implements OnInit {
  @Output()
  loadPrev = new EventEmitter<any>();

  @Output()
  loadNext = new EventEmitter<any>();

  @Output()
  gotoTop = new EventEmitter<any>();

  @Input()
  content: string = '';

  @Input()
  disabled: boolean = false;

  @Input()
  loading: string | boolean = false;

  //Got Begin of File
  @Input()
  bof: boolean = false;

  //Got End of File
  @Input()
  eof: boolean = false;

  @ViewChild('output')
  private output: ElementRef;

  constructor() { }

  ngOnInit() {
  }

  private scrollPos = 0;

  private scrollTimer;

  private scrollDelay = 200;

  private scrollThreshold = 0.20;

  onScroll($event, debounced = false, downward = undefined) {
    if (this.disabled) {
      return;
    }
    if (!debounced) {
      if (this.scrollTimer) {
        clearTimeout(this.scrollTimer);
      }
      let top = $event.srcElement.scrollTop;
      let downward = top >= this.scrollPos;
      this.scrollTimer = setTimeout(() => this.onScroll($event, true, downward), this.scrollDelay);
      this.scrollPos = top;
    }
    else {
      clearTimeout(this.scrollTimer);
      this.scrollTimer = null;

      let elem = $event.srcElement;
      let height = elem.scrollHeight;
      let up = elem.scrollTop / height;
      let mid = elem.clientHeight / height;
      let down = 1 - up - mid;

      if (downward) {
        if (down <= this.scrollThreshold) {
          this.loadNext.emit(elem);
        }
      }
      else if (up <= this.scrollThreshold) {
        this.loadPrev.emit(elem);
      }
    }
  }

  scrollToBottom(): void {
    let elem = this.output.nativeElement;
    elem.scrollTop = elem.scrollHeight;
  }

}
