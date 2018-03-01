import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { of } from 'rxjs/observable/of';
import { catchError } from 'rxjs/operators';
import { Node } from './node';

@Injectable()
export class NodeService {

  private nodesUrl = '/api/nodes';

  constructor(private http: HttpClient) {}

  getNodes(): Observable<Node[]> {
    return this.http.get<Node[]>(this.nodesUrl)
      .pipe(
        catchError((error: any): Observable<Node[]> => {
          console.error(error);
          return of([]);
        })
      );
  }

  getNode(id: string): Observable<Node> {
    return this.http.get<Node>(this.nodesUrl + '/' + id)
      .pipe(
        catchError((error: any): Observable<Node> => {
          console.error(error);
          return of({} as Node);
        })
      );
  }
}
