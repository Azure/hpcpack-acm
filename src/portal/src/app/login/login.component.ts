import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { FormControl, Validators, FormGroup } from '@angular/forms';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  private errorMsg: string;
  constructor(public authService: AuthService, public router: Router) {
  }

  get logged() {
    return this.authService.isLoggedIn;
  }

  login(username, pwd) {
    if (username == undefined || pwd == undefined) {
      this.errorMsg = 'Username and password is required !'
      return;
    }
    this.authService.username = username;
    this.authService.pwd = pwd;
    this.authService.login().subscribe(
      (val) => {
        if (this.logged) {
          // Get the redirect URL from our auth service
          // If no redirect has been set, use the default
          let redirect = this.authService.redirectUrl ? this.authService.redirectUrl : '/';
          // Redirect the user
          this.router.navigate([redirect]);
        }
      },
      (err) => {
        this.errorMsg = 'Incorrect username or password!'
      });
  }

  logout() {
    this.authService.logout();
  }

}
