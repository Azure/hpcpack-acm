import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MaterialsModule } from './materials.module';
import { WindowScrollDirective } from './widgets/window-scroll/window-scroll.directive';
import { ScrollToTopComponent } from './widgets/scroll-to-top/scroll-to-top.component';
import { LoadingProgressBarComponent } from './widgets/loading-progress-bar/loading-progress-bar.component';

@NgModule({
    imports: [CommonModule, MaterialsModule],
    declarations: [WindowScrollDirective, ScrollToTopComponent, LoadingProgressBarComponent],
    exports: [WindowScrollDirective, ScrollToTopComponent, LoadingProgressBarComponent]
})

export class SharedModule { }