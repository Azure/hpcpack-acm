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
    return this.loggedIn || sessionStorage.getItem('username') !== null;
  };


  private user = {
    name: ''
  };

  get username() {
    return this.user.name || sessionStorage.getItem('username');
  }

  //Store the URL so we can redirect after logging in
  redirectUrl: string;

  logout(): void {
    this.api.user.logout().subscribe((res) => {
      console.log('User has logged out.');
    });
  }

  getUserInfo(): void {
    this.api.user.getUserInfo().subscribe((res) => {
      if (res.status == 404) {
        this.user.name = 'Anonymous';
        this.loggedIn = true;
        sessionStorage.setItem('username', this.user.name);
      }
      else if (res.status == 200) {
        this.user.name = res[0].user_id;
        this.loggedIn = true;
        sessionStorage.setItem('username', this.user.name);
      }
    });
  }
}
