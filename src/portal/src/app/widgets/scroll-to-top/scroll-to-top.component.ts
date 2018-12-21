import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'app-scroll-to-top',
  templateUrl: './scroll-to-top.component.html',
  styleUrls: ['./scroll-to-top.component.scss']
})
export class ScrollToTopComponent implements OnInit {

  constructor() { }

  @Input()
  scrolled: boolean;

  @Input()
  targetEle: HTMLInputElement;

  ngOnInit() {
  }

  private scrollToTop() {
    if (this.targetEle) {
      this.targetEle.scrollTo({ top: 0, behavior: 'smooth' });
    }
    else {
      window.scrollTo({ left: 0, top: 0, behavior: 'smooth' });
    }
  }
}
