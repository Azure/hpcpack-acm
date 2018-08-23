import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {

  message: string;
  hide = true;

  constructor(public authService: AuthService, public router: Router) {
    this.setMessage();
  }

  setMessage() {
    this.message = 'Logged ' + (this.logged ? 'in' : 'out');
  }

  get logged() {
    return this.authService.isLoggedIn || localStorage.getItem('isLoggedIn') == 'true';
  }

  login() {
    this.message = 'Trying to log in ...';

    this.authService.login().subscribe(() => {
      this.setMessage();
      if (this.logged) {
        // Get the redirect URL from our auth service
        // If no redirect has been set, use the default
        let redirect = this.authService.redirectUrl ? this.authService.redirectUrl : '/';

        // Redirect the user
        this.router.navigate([redirect]);
      }
    });
  }

  logout() {
    this.authService.logout();
    this.setMessage();
  }

}
