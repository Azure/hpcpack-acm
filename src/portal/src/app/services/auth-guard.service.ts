import { Injectable } from '@angular/core';
import { Route, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from './auth.service';

@Injectable()
export class AuthGuardService {

  constructor(private authService: AuthService, private router: Router) { }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    return this.checkLogin(state.url);
  }

  canLoad(route: Route): boolean {
    console.log(route);
    return this.checkLogin(`/${route.path}`);
  }

  checkLogin(url: string): boolean {
    if (this.authService.isLoggedIn)
      return true;

    // Store the attempted URL for redirecting
    this.authService.redirectUrl = url;

    // Navigate to the login page with extras
    this.router.navigate(['./login']);
    return false;
  }
}
