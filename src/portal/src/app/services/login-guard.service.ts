import { Injectable } from '@angular/core';
import { Route, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from './auth.service';

@Injectable()
export class LoginGuardService {

  constructor(private authService: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    return this.checkLogin(state.url);
  }

  checkLogin(url: string): boolean {
    if (!this.authService.isLoggedIn)
      return true;

    //Redirect to root if user is already logged in. Here url may be the login
    //url so simply redirect back to it may cause dead lock.
    this.router.navigate(['/']);
    return false;
  }
}
