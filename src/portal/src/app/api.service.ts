import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { of } from 'rxjs/observable/of';
import { catchError } from 'rxjs/operators';
import { environment as env } from '../environments/environment';

@Injectable()
export class ApiService {

  private apiBase = env.apiBase;

  private nodesUrl = `${this.apiBase}/nodes`;

  constructor(private http: HttpClient) {}

  getNodes(): Observable<any[]> {
    return this.http.get<any[]>(this.nodesUrl)
      .pipe(
        catchError((error: any): Observable<any[]> => {
          console.error(error);
          return of([]);
        })
      );
  }
}
