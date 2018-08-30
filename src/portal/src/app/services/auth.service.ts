import { Injectable } from '@angular/core';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/observable/of';
import 'rxjs/add/operator/do';
import 'rxjs/add/operator/delay';
import { ApiService } from './api.service';

@Injectable()
export class AuthService {
  //For now, default to true for development convenience
  private loggedIn: boolean;

  constructor(private api: ApiService) { }

  get isLoggedIn() {
    return this.loggedIn || sessionStorage.getItem('access_token') !== null;
  };

  user = {
    name: '',
    pwd: ''
  }

  //Store the URL so we can redirect after logging in
  redirectUrl: string;

  login(): Observable<any> {
    return this.api.user.login().do((val) => {
      if (val.status == 204) {
        sessionStorage.setItem('access_token', btoa(`${this.user.name}:${this.user.pwd}`));
        this.loggedIn = true;
      }

    });
  }

  logout(): void {
    this.loggedIn = false;
    sessionStorage.removeItem('access_token');
  }
}
