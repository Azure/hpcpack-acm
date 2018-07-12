import { NgModule } from '@angular/core';
import { WindowScrollDirective } from './widgets/window-scroll/window-scroll.directive';

@NgModule({
    declarations: [WindowScrollDirective],
    exports: [WindowScrollDirective]
})

export class SharedModule { }