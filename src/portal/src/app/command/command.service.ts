import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { of } from 'rxjs/observable/of';
import { catchError } from 'rxjs/operators';
import { Result } from './result';

@Injectable()
export class CommandService {

  private resultsUrl = '/api/command/results';

  constructor(private http: HttpClient) {}

  getResults(): Observable<Result[]> {
    return this.http.get<Result[]>(this.resultsUrl)
      .pipe(
        catchError((error: any): Observable<Result[]> => {
          console.error(error);
          return of([]);
        })
      );
  }

  getResult(id: string): Observable<Result> {
    return this.http.get<Result>(this.resultsUrl + '/' + id)
      .pipe(
        catchError((error: any): Observable<Result> => {
          console.error(error);
          return of({} as Result);
        })
      );
  }
}
