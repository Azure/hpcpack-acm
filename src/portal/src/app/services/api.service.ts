import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { ErrorObservable } from 'rxjs/observable/ErrorObservable';
import { of } from 'rxjs/observable/of';
import { catchError, map } from 'rxjs/operators';
import 'rxjs/add/operator/first';
import { environment as env } from '../../environments/environment';
import { Node } from '../models/node';
import { CommandResult } from '../models/command-result';
import { TestResult } from '../models/test-result';
import { HeatmapNode } from '../models/heatmap-node';
import 'rxjs/add/operator/concatMap';

abstract class Resource<T> {
  static baseUrl = env.apiBase;

  constructor(protected http: HttpClient) {}

  protected abstract get url(): string;

  protected normalize(e: any): T {}

  getAll(): Observable<T[]> {
    return this.http.get<T[]>(this.url)
      .pipe(
        map(array => array.map(e => this.normalize(e))),
        catchError((error: any): Observable<T[]> => {
          console.error(error);
          return new ErrorObservable(error);
        })
      );
  }

  get(id: string): Observable<T> {
    return this.http.get<T>(this.url + '/' + id)
      .pipe(
        map(e => this.normalize(e)),
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

  protected normalize(node: any): Node {
    if (node.nodeInfo)
      node = node.nodeInfo;
    return {
      id: node.name,
      name: node.name,
      state: node.state,
      health: node.health,
      runningJobCount: node.runningJobCount,
    };
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

  protected normalize(result: any): CommandResult {
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
    return result;
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

export class HeatmapApi extends Resource<any> {
  static url = `${Resource.baseUrl}/heatmap`;

  protected get url(): string {
    return HeatmapApi.url;
  }

  protected normalize(result: any): any {
    result["results"] = new Array<HeatmapNode>();
    for(let key in result.values) {
      if(result.values[key]._Total == undefined) {
        result["results"].push({"id": key, "value": NaN});
      } else {
        result["results"].push({"id": key, "value": result.values[key]._Total});
      }
    }
    return result;
  }

  getCategories(): Observable<string[]> {
    let url = this.url + "/categories";
    return this.http.get<string[]>(url)
      .pipe(
        map(e => {
          return e;
        }),
        catchError((error: any): Observable<any> => {
          console.error(error);
          return new ErrorObservable(error);
        })
      );
  }

  get(category: string): Observable<any> {
    let url = this.url + '/values/' + category;
    return this.http.get<any>(url)
      .pipe(
        map(e => this.normalize(e)),
        catchError((error: any): Observable<any> => {
          console.error(error);
          return new ErrorObservable(error);
        })
      );
  }

  getMockData(category: string): Observable<any> {
    let url = this.url + '/values/' + category;
    return this.http.post(env.apiBase + '/commands/resetdb', {clear: true})
      .concatMap(() => {
        return this.http.get<any>(url)
          .pipe(
            map(e => this.normalize(e)),
            catchError((error: any): Observable<any> => {
              console.error(error);
              return new ErrorObservable(error);
            })
          )
      });
  }

}

@Injectable()
export class ApiService {
  private nodeApi: NodeApi;

  private testApi: TestApi;

  private commandApi: CommandApi;

  private heatmapApi: HeatmapApi;

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

  get heatmap(): HeatmapApi {
    if(!this.heatmapApi) {
      this.heatmapApi = new HeatmapApi(this.http);
    }
    return this.heatmapApi;
  }
}

export class Loop {
  //Subscribe an observable repeatedly with a time interval between each
  //subscription. Only the first emit of the observable is taken.
  //The observer parameter is an object of
  //{ next: (res) => {...}, error: (err) => {...} },
  //much like a Rxjs observer object with the differences in:
  //1) The next method's return value matters. A false value indicate the end
  //   of the loop, while true to continue. And, if an observable is returned,
  //   it will be subscribed in the next iteration instead of the one passed
  //   as start's parameter.
  //2) no complete callback on observable.
  //The interval parameter is the LEAST time between each subscription.
  static start(observable, observer, interval = 1500): object {
    let looper = { observable: observable, ended: false };
    let _loop = () => {
      if (looper.ended) {
        return;
      }
      let ts = new Date().getTime();
      looper.observable.first().subscribe(
        res => {
          if (looper.ended) {
            return;
          }
          let elapse = new Date().getTime() - ts;
          //Here the next return value determines if the loop should end.
          //This is a difference from a normal observer's next method, which has
          //no specification on the return value. Also note that observer.next
          //may return a new observable to be subscribed in the next iteration.
          let n = observer.next(res);
          if (!n) {
            looper.ended = true;
            return;
          }
          if (typeof(n) === 'object') {
            looper.observable = n;
          }
          let delta = interval - elapse;
          let _interval = delta > 0 ? delta : 0;
          setTimeout(_loop, _interval);
        },
        err => {
          looper.ended = true;
          if (observer.error) {
            observer.error(err);
          }
        }
      );
    };
    _loop();
    return looper;
  }

  static stop(looper: object): void {
    (looper as any).ended = true;
  }

  static isStopped(looper: object): boolean {
    return (looper as any).ended;
  }
}
