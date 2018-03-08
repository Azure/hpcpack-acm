import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { of } from 'rxjs/observable/of';
import { catchError } from 'rxjs/operators';
import { environment as env } from '../environments/environment';

class ApiBase {
  protected baseUrl = env.apiBase;

  constructor(protected http: HttpClient) {}
}

class NodeApi extends ApiBase {
  private url = `${this.baseUrl}/nodes`;

  getAll(): Observable<any[]> {
    return this.http.get<any[]>(this.url)
      .pipe(
        catchError((error: any): Observable<any[]> => {
          console.error(error);
          return of([]);
        })
      );
  }
}

class DiagnosticsApi extends ApiBase {
}

class CommandApi extends ApiBase {
}

@Injectable()
export class ApiService {
  private nodeApi: NodeApi;

  constructor(private http: HttpClient) {}

  get nodes(): NodeApi {
    if (!this.nodeApi) {
      this.nodeApi = new NodeApi(this.http);
    }
    return this.nodeApi;
  }
}
