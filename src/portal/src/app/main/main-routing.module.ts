import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MainComponent } from './main.component';

const routes: Routes = [{
    path: '',
    component: MainComponent,
    children: [
        {
            path: 'dashboard',
            loadChildren: 'app/dashboard/dashboard.module#DashboardModule',
            data: { breadcrumb: "Dashboard" }
        },
        {
            path: 'resource',
            loadChildren: 'app/resource/resource.module#ResourceModule',
            data: { breadcrumb: "Resource" }
        },
        {
            path: 'diagnostics',
            loadChildren: 'app/diagnostics/diagnostics.module#DiagnosticsModule',
            data: { breadcrumb: "Diagnostics" }
        },
        {
            path: 'command',
            loadChildren: 'app/command/command.module#CommandModule',
            data: { breadcrumb: "Cluster Run" }
        },
        {
            path: '',
            redirectTo: 'dashboard',
            pathMatch: 'full',
        },
    ]
}];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class MainRoutingModule { }
