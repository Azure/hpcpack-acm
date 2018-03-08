import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { of } from 'rxjs/observable/of';
import { catchError } from 'rxjs/operators';
import { environment as env } from '../environments/environment';
import { Node } from './models/node';
import { CommandResult } from './models/command-result';
import { TestResult } from './models/test-result';

abstract class Resource<T> {
  protected baseUrl = env.apiBase;

  constructor(protected http: HttpClient) {}

  protected abstract get url(): string;

  getAll(): Observable<T[]> {
    return this.http.get<T[]>(this.url)
      .pipe(
        catchError((error: any): Observable<T[]> => {
          console.error(error);
          return of([]);
        })
      );
  }

  get(id: string): Observable<T> {
    return this.http.get<T>(this.url + '/' + id)
      .pipe(
        catchError((error: any): Observable<T> => {
          console.error(error);
          return of({} as T);
        })
      );
  }
}

class NodeApi extends Resource<Node> {
  protected get url(): string {
    return `${this.baseUrl}/nodes`;
  }
}

class TestApi extends Resource<TestResult> {
  protected get url(): string {
    return `${this.baseUrl}/diagnostics/jobs`;
  }
}

class CommandApi extends Resource<CommandResult> {
  protected get url(): string {
    return `${this.baseUrl}/clusRun`;
  }
}

@Injectable()
export class ApiService {
  private nodeApi: NodeApi;

  private testApi: TestApi;

  private commandApi: CommandApi;

  constructor(private http: HttpClient) {}

  get node(): NodeApi {
    if (!this.nodeApi) {
      this.nodeApi = new NodeApi(this.http);
    }
    return this.nodeApi;
  }

  get command(): CommandApi {
    if (!this.commandApi) {
      this.commandApi = new CommandApi(this.http);
    }
    return this.commandApi;
  }

  get test(): TestApi {
    if (!this.testApi) {
      this.testApi = new TestApi(this.http);
    }
    return this.testApi;
  }

}
