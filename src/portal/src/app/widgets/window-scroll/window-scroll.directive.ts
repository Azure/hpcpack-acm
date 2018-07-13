import { Directive, ElementRef, HostListener, Input, OnDestroy, Output, EventEmitter, OnInit } from '@angular/core';

@Directive({
  selector: '[appWindowScroll]'
})
export class WindowScrollDirective implements OnDestroy, OnInit {

  @Input() dataLength: number;
  @Input() pageSize: number;
  @Input() updatedSize = 30;
  @Input() derelictSize = 20;
  @Output() scrollEvent = new EventEmitter();
  private lastScrolledPosition = 0;
  private downNum = 0;
  private upNum = 0;
  private jobIndex = 0;
  private componentPostion = 0;
  public scrolled = false;
  public loadFinished = false;
  public dataIndex = -1;
  public scrollDirection = 'down';
  private reverse = true;

  private getEleOffset() {
    let el = this.el.nativeElement;
    var _x = 0;
    var _y = 0;
    while (el && !isNaN(el.offsetLeft) && !isNaN(el.offsetTop)) {
      _x += el.offsetLeft - el.scrollLeft;
      _y += el.offsetTop - el.scrollTop;
      el = el.offsetParent;
    }
    return { top: _y, left: _x };
  }

  ngOnInit() {
    this.componentPostion = this.getEleOffset().top;
  }


  constructor(private el: ElementRef) {
  }

  private scrollTimer = null;
  @HostListener("window:scroll") onScroll(delay: number = 100) {
    clearTimeout(this.scrollTimer);
    this.scrollTimer = setTimeout(() => {
      const windowHeight = window.innerHeight;
      const scrollPosition = window.pageYOffset;

      if (scrollPosition >= this.componentPostion) {
        this.scrolled = true;
      }
      else {
        this.scrolled = false;
      }

      let pageSize = this.dataLength;
      let containerHeight = this.el.nativeElement.clientHeight;
      let itemHeight = (<HTMLElement>document.querySelector('mat-row')).offsetHeight;
      let tableSize = Math.floor(containerHeight / itemHeight);

      if (!this.scrolled) {
        this.dataIndex = -1;
        this.reverse = true;
        this.scrollDirection = 'down';
        this.jobIndex = 0;
        this.downNum = 0;
        this.upNum = 0;
      }

      if (this.lastScrolledPosition > scrollPosition && this.scrolled) {
        this.scrollDirection = 'up';
        if (this.reverse) {
          this.jobIndex -= Math.floor((this.lastScrolledPosition - scrollPosition) / itemHeight);
          this.reverse = false;
          this.jobIndex = pageSize - this.jobIndex;
          let itemInView = Math.floor(windowHeight / itemHeight);
          this.jobIndex -= itemInView;
        }
        else {
          this.jobIndex += Math.floor((this.lastScrolledPosition - scrollPosition) / itemHeight);
        }
        while (this.jobIndex >= (this.updatedSize - 1) && (pageSize == this.pageSize)) {
          this.upNum++;
          if (this.downNum > 0) {
            this.downNum--;
          }
          this.jobIndex -= this.derelictSize;
          this.dataIndex = tableSize - this.derelictSize * this.upNum;
        }
      }
      else if (this.lastScrolledPosition < scrollPosition && this.scrolled) {
        this.scrollDirection = 'down';
        if (!this.reverse) {
          this.jobIndex -= Math.floor((scrollPosition - this.lastScrolledPosition) / itemHeight);
          this.reverse = true;
          this.jobIndex = pageSize - this.jobIndex;
          let itemInView = Math.floor(windowHeight / itemHeight);
          this.jobIndex -= itemInView;
        }
        else {
          this.jobIndex += Math.floor((scrollPosition - this.lastScrolledPosition) / itemHeight);
        }
        while (this.jobIndex >= (this.updatedSize - 1) && (pageSize == this.pageSize)) {
          this.downNum++;
          if (this.upNum > 0) {
            this.upNum--;
          }
          this.jobIndex -= this.derelictSize;
          this.dataIndex = this.downNum * this.derelictSize - 1;
        }

        if (this.dataLength < this.pageSize) {
          this.loadFinished = true;
        }
      }
      this.lastScrolledPosition = scrollPosition;
      this.scrollEvent.emit({ dataIndex: this.dataIndex, scrolled: this.scrolled, loadFinished: this.loadFinished, scrollDirection: this.scrollDirection });
    }, delay);
  }

  ngOnDestroy() {
    clearTimeout(this.scrollTimer);
  }
}
