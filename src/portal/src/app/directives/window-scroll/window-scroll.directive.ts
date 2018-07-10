import { Directive, ElementRef, HostListener, Input, OnDestroy, Output, EventEmitter, OnInit } from '@angular/core';

@Directive({
  selector: '[appWindowScroll]'
})
export class WindowScrollDirective implements OnDestroy, OnInit {

  constructor(private el: ElementRef) { }

  @Input() currentData: Array<any>;
  @Input() dataSource: any;
  @Input() itemHeight: number;
  @Input() reverse: boolean;
  @Input() updatedSize: number;
  @Input() derelictSize: number;
  @Input() maxPageSize: number;
  @Output() scrollEvent = new EventEmitter();
  private scrolledHeight = 0;
  private downNum = 0;
  private upNum = 0;
  private jobIndex = 0;
  private componentPostion = 0;
  private scrolled = false;
  private loadFinished = false;
  private lastId = 0;

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

      let pageSize = this.currentData.length;
      let tableSize = this.dataSource.data.length;

      if (!this.scrolled) {
        this.lastId = 0;
        this.reverse = true;
        this.jobIndex = 0;
        this.downNum = 0;
        this.upNum = 0;
      }

      if (this.scrolledHeight > scrollPostion && this.scrolled) {
        if (this.reverse) {
          this.jobIndex -= Math.floor((this.scrolledHeight - scrollPostion) / this.itemHeight);
          this.reverse = false;
          this.jobIndex = pageSize - this.jobIndex;
          let itemInView = Math.floor(windowHeight / this.itemHeight);
          this.jobIndex -= itemInView;
        }
        else {
          this.jobIndex += Math.floor((this.scrolledHeight - scrollPostion) / this.itemHeight);
        }
        while (this.jobIndex >= this.updatedSize && (pageSize == this.maxPageSize)) {
          this.upNum++;
          if (this.downNum > 0) {
            this.downNum--;
          }
          this.jobIndex -= this.derelictSize;
          this.lastId = this.dataSource.data[tableSize - this.derelictSize * this.upNum]['id'];
        }
      }
      else if (this.scrolledHeight <= scrollPostion && this.scrolled) {
        if (!this.reverse) {
          this.jobIndex -= Math.floor((scrollPostion - this.scrolledHeight) / this.itemHeight);
          this.reverse = true;
          this.jobIndex = pageSize - this.jobIndex;
          let itemInView = Math.floor(windowHeight / this.itemHeight);
          this.jobIndex -= itemInView;
        }
        else {
          this.jobIndex += Math.floor((scrollPostion - this.scrolledHeight) / this.itemHeight);
        }
        while (this.jobIndex >= this.updatedSize && (pageSize == this.maxPageSize)) {
          this.downNum++;
          if (this.upNum > 0) {
            this.upNum--;
          }
          this.jobIndex -= this.derelictSize;
          this.lastId = this.dataSource.data[this.downNum * this.derelictSize - 1]['id'];
        }

        if (this.currentData.length < this.maxPageSize) {
          this.loadFinished = true;
        }
      }
      this.scrolledHeight = scrollPostion;
      this.scrollEvent.emit({ lastId: this.lastId, scrolled: this.scrolled, loadFinished: this.loadFinished, reverse: this.reverse });
    }, delay);
  }

  ngOnDestroy() {
    clearTimeout(this.scrollTimer);
  }
}
