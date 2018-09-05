import { Injectable } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable()
export class BasicInterceptor implements HttpInterceptor {
    constructor(private authService: AuthService) { }
    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        // add authorization header with basic auth token if available
        let authInfo = sessionStorage.getItem('access_token') ? sessionStorage.getItem('access_token') : btoa(`${this.authService.username}:${this.authService.pwd}`);
        request = request.clone({
            setHeaders: {
                Authorization: `Basic ${authInfo}`
            }
        });
        return next.handle(request);
    }
}