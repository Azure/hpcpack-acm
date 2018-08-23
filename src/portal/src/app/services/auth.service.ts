import { Injectable } from '@angular/core';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/observable/of';
import 'rxjs/add/operator/do';
import 'rxjs/add/operator/delay';

@Injectable()
export class AuthService {
  //For now, default to true for development convenience
  private loggedIn: boolean;
  get isLoggedIn() {
    return this.loggedIn || sessionStorage.getItem('isLoggedIn') == 'true';
  };

  user = {
    name: 'Lei.Zhang@microsoft.com',
  }

  //Store the URL so we can redirect after logging in
  redirectUrl: string;

  login(): Observable<boolean> {
    return Observable.of(true).delay(1000).do(val => {
      sessionStorage.setItem('isLoggedIn', 'true');
      this.loggedIn = true
    });
  }

  logout(): void {
    this.loggedIn = false;
    sessionStorage.removeItem('isLoggedIn');
  }
}
