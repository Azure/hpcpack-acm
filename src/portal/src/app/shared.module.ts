import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WindowScrollDirective } from './widgets/window-scroll/window-scroll.directive';
import { ScrollToTopComponent } from './widgets/scroll-to-top/scroll-to-top.component';

@NgModule({
    imports: [CommonModule],
    declarations: [WindowScrollDirective, ScrollToTopComponent],
    exports: [WindowScrollDirective, ScrollToTopComponent]
})

export class SharedModule { }