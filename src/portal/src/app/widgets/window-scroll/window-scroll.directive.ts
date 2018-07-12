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
  private scrolledHeight = 0;
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
      const scrollPostion = window.pageYOffset;

      if (scrollPostion >= this.componentPostion) {
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
        this.jobIndex = 0;
        this.downNum = 0;
        this.upNum = 0;
      }

      if (this.scrolledHeight > scrollPostion && this.scrolled) {
        this.scrollDirection = 'up';
        if (this.reverse) {
          this.jobIndex -= Math.floor((this.scrolledHeight - scrollPostion) / itemHeight);
          this.reverse = false;
          this.jobIndex = pageSize - this.jobIndex;
          let itemInView = Math.floor(windowHeight / itemHeight);
          this.jobIndex -= itemInView;
        }
        else {
          this.jobIndex += Math.floor((this.scrolledHeight - scrollPostion) / itemHeight);
        }
        while (this.jobIndex >= this.updatedSize && (pageSize == this.pageSize)) {
          this.upNum++;
          if (this.downNum > 0) {
            this.downNum--;
          }
          this.jobIndex -= this.derelictSize;
          this.dataIndex = tableSize - this.derelictSize * this.upNum;
        }
      }
      else if (this.scrolledHeight <= scrollPostion && this.scrolled) {
        this.scrollDirection = 'down';
        if (!this.reverse) {
          this.jobIndex -= Math.floor((scrollPostion - this.scrolledHeight) / itemHeight);
          this.reverse = true;
          this.jobIndex = pageSize - this.jobIndex;
          let itemInView = Math.floor(windowHeight / itemHeight);
          this.jobIndex -= itemInView;
        }
        else {
          this.jobIndex += Math.floor((scrollPostion - this.scrolledHeight) / itemHeight);
        }
        while (this.jobIndex >= this.updatedSize && (pageSize == this.pageSize)) {
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
      this.scrolledHeight = scrollPostion;
      this.scrollEvent.emit({ dataIndex: this.dataIndex, scrolled: this.scrolled, loadFinished: this.loadFinished, scrollDirection: this.scrollDirection });
    }, delay);
  }

  ngOnDestroy() {
    clearTimeout(this.scrollTimer);
  }
}
