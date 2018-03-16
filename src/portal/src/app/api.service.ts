import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { ErrorObservable } from 'rxjs/observable/ErrorObservable';
import { of } from 'rxjs/observable/of';
import { catchError, map } from 'rxjs/operators';
import { environment as env } from '../environments/environment';
import { Node } from './models/node';
import { CommandResult } from './models/command-result';
import { TestResult } from './models/test-result';

abstract class Resource<T> {
  static baseUrl = env.apiBase;

  constructor(protected http: HttpClient) {}

  protected abstract get url(): string;

  //TODO: return a new one instead of modifying in place.
  protected normalize(e: T): void {}

  getAll(): Observable<T[]> {
    return this.http.get<T[]>(this.url)
      .pipe(
        map(array => {
          array.forEach(e => this.normalize(e));
          return array;
        }),
        catchError((error: any): Observable<T[]> => {
          console.error(error);
          return new ErrorObservable(error);
        })
      );
  }

  get(id: string): Observable<T> {
    return this.http.get<T>(this.url + '/' + id)
      .pipe(
        map(e => {
          this.normalize(e);
          return e;
        }),
        catchError((error: any): Observable<T> => {
          console.error(error);
          return new ErrorObservable(error);
        })
      );
  }
}

export class NodeApi extends Resource<Node> {
  static url = `${Resource.baseUrl}/nodes`;

  protected get url(): string {
    return NodeApi.url;
  }

  protected normalize(node: Node): void {
    if (!node.id)
      node.id = node.name;
  }
}

export class TestApi extends Resource<TestResult> {
  static url = `${Resource.baseUrl}/diagnostics/jobs`;

  protected get url(): string {
    return TestApi.url;
  }
}

export class CommandApi extends Resource<CommandResult> {
  static url = `${Resource.baseUrl}/clusRun`;

  protected get url(): string {
    return CommandApi.url;
  }

  protected normalize(result: CommandResult): void {
    result.state = result.state.toLowerCase();
    result.command = result['commandLine'];
    if (result['results']) {
      result.nodes = result['results'].map(e => {
        let odd = e.results[0];
        return {
          name: e.nodeName,
          state: odd.taskInfo ? (odd.taskInfo.exitCode == 0 ? 'finished' : 'failed') : 'running',
          key: odd.resultKey,
          output: '',
          next: 0,
        };
      }).sort((a, b) => (a.name < b.name ? -1 : (a.name > b.name ? 1 : 0)));
    }
  }

  create(commandLine: string, targetNodes: string[]): any {
    return this.http.post<any>(this.url, { commandLine, targetNodes }, { observe: 'response', responseType: 'json' })
      .pipe(
        catchError((error: any): Observable<any> => {
          console.error(error);
          return new ErrorObservable(error);
        })
      );
  }

  getOuput(id, key, next) {
    let url = `${Resource.baseUrl}/taskoutput/getpage/${id}/${key}?offset=${next}`;
    return this.http.get<any>(url)
      .pipe(
        catchError((error: any): Observable<any> => {
          console.error(error);
          return new ErrorObservable(error);
        })
      );
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
