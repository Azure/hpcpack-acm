import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuardService } from '../services/auth-guard.service';
import { MainComponent } from './main.component';

const routes: Routes = [{
    path: '',
    component: MainComponent,
    children: [
        {
            path: 'dashboard',
            loadChildren: 'app/dashboard/dashboard.module#DashboardModule',
            data: { breadcrumb: "Dashboard" },
            canActivate: [AuthGuardService]
        },
        {
            path: 'resource',
            loadChildren: 'app/resource/resource.module#ResourceModule',
            data: { breadcrumb: "Resource" },
            canActivate: [AuthGuardService]
        },
        {
            path: 'diagnostics',
            loadChildren: 'app/diagnostics/diagnostics.module#DiagnosticsModule',
            data: { breadcrumb: "Diagnostics" },
            canActivate: [AuthGuardService]
        },
        {
            path: 'command',
            loadChildren: 'app/command/command.module#CommandModule',
            data: { breadcrumb: "Cluster Run" },
            canActivate: [AuthGuardService]
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
