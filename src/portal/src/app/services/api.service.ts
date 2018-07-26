import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
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

export abstract class Resource<T> {
  protected get baseUrl() {
    return env.apiBase
  }

  constructor(protected http: HttpClient) { }

  protected abstract get url(): string;

  protected normalize(e: any): T { return e as T; }

  get errorHandler() {
    return catchError(error => {
      console.error(error);
      //ErrorObservable is effectively an exception, like throw(...)
      return new ErrorObservable(error);
    })
  }

  httpGet(url, params = null, pipes = []) {
    let res = this.http.get<any>(url, params ? { params } : {});
    pipes = Array.from(pipes);
    pipes.push(this.errorHandler);
    res = res.pipe.apply(res, pipes);
    return res;
  }

  getAll(params?: any): Observable<T[]> {
    return this.httpGet(this.url, params, [
      map(array => (array as any[]).map(e => this.normalize(e))),
    ]);
  }

  get(id: string): Observable<T> {
    return this.httpGet(`${this.url}/${id}`, null, [
      map(e => this.normalize(e)),
    ]);
  }
}

export class NodeApi extends Resource<Node> {
  protected get url(): string {
    return `${this.baseUrl}/nodes`;
  }

  getNodesByPage(lastId, count): Observable<any> {
    return this.httpGet(`${this.url}?lastid=${lastId}&count=${count}`);
  }

  getHistoryData(id: string): Observable<any> {
    return this.httpGet(`${this.url}/${id}/metrichistory`, null, [
      map(e => this.normalizeHistory(e)),
    ]);
  }

  getMetadata(id: string): Observable<any> {
    return this.httpGet(`${this.url}/${id}/metadata`, null, [
      map(e => this.normalizeHistory(e)),
    ]);
  }

  protected normalizeHistory(history: any): any {
    if (history.data) {
      history.history = history.data.map(item => ({ label: item.time, data: item.metricItems }));
    }
    else {
      history.history = [];
    }
    if (history.rangeSeconds) {
      history.range = history.rangeSeconds;
    }
    return history;
  }

  getNodeEvents(id: string): Observable<any> {
    return this.httpGet(`${this.url}/${id}/events`);
  }

  getNodeSheduledEvents(id: string): Observable<any> {
    return this.httpGet(`${this.url}/${id}/scheduledevents`);
  }

  getNodeJobs(id: string): Observable<any> {
    return this.httpGet(`${this.url}/${id}/jobs`);
  }
}

export class TestApi extends Resource<TestResult> {
  protected get url(): string {
    return `${this.baseUrl}/diagnostics/jobs`;
  }
}

export class CommandApi extends Resource<CommandResult> {
  protected get url(): string {
    return `${this.baseUrl}/clusRun`;
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
        };
      }).sort((a, b) => (a.name < b.name ? -1 : (a.name > b.name ? 1 : 0)));
    }
    return result;
  }

  create(commandLine: string, targetNodes: string[]): any {
    return this.http.post<any>(this.url, { commandLine, targetNodes }, { observe: 'response', responseType: 'json' })
      .pipe(
        map(res => {
          let url = res.headers.get('Location');
          let idx = url.substring(url.lastIndexOf('/') + 1);
          let id = parseInt(idx);
          return { id };
        }),
        this.errorHandler
      );
  }

  cancel(jobId) {
    let url = `${this.url}/${jobId}`;
    return this.http.patch<any>(url, { request: 'cancel' })
      .pipe(this.errorHandler);
  }

  getTasks(jobId) {
    let url = `${this.url}/${jobId}/tasks`;
    return this.httpGet(url);
  }

  getTaskResult(jobId, taskId) {
    let url = `${this.url}/${jobId}/tasks/${taskId}/result`;
    return this.httpGet(url);
  }

  //You can't tell if the output reaches EOF by one simple GET API. You have
  //to call another API to get to know if the command generating the output is
  //over. When opt.fulfill is set to true, DO provide callback opt.over, otherwise
  //the method may never ends since it doesn't know EOF without opt.over!
  getOutput(key, offset, size = 1024, opt = { fulfill: false, over: undefined, timeout: undefined }) {
    return Observable.create(observer => {
      let res = { content: '', size: 0 };
      let url = `${this.baseUrl}/output/clusRun/${key}/page?offset=${offset}&pageSize=${size}`;
      let ts = new Date().getTime();
      Loop.start(
        this.http.get<any>(url),
        {
          next: result => {
            if (result.content) {
              res.content += result.content;
            }
            res.size += result.size;
            if (typeof ((res as any).offset) === 'undefined') {
              (res as any).offset = result.offset;
            }
            let eof = result.size === 0 && opt.over && opt.over();
            let elapse = new Date().getTime() - ts;
            if (!opt.fulfill || res.size == size || eof || (opt.timeout && elapse >= opt.timeout)) {
              (res as any).end = eof;
              observer.next(res);
              observer.complete();
              return false;
            }
            let nextOffset = result.offset + result.size;
            let nextSize = size - res.size;
            let nextUrl = `${this.baseUrl}/output/clusRun/${key}/page?offset=${nextOffset}&pageSize=${nextSize}`;
            return this.http.get<any>(nextUrl);
          },
          error: err => {
            console.error(err);
            observer.error(err);
          }
        },
        0
      );
    });
  }

  getDownloadUrl(key): string {
    let url = `${this.baseUrl}/output/clusRun/${key}/raw`;
    return url;
  }
}

export class HeatmapApi extends Resource<any> {
  protected get url(): string {
    return `${this.baseUrl}/metrics`;
  }

  protected normalize(result: any): void {
    result.results = result.values.map(e => ({ id: e.node, value: e.data._Total >= 0 ? e.data._Total : NaN }));
    return result;
  }

  getCategories(): Observable<string[]> {
    let url = this.url + '/categories';
    return this.httpGet(url);
  }

  get(category: string): Observable<any> {
    let url = this.url + '/' + category;
    return this.httpGet(url, null, [
      map(e => this.normalize(e)),
    ]);
  }

  getMockData(category: string): Observable<any> {
    let url = `${this.url}/${category}`;
    return this.http.post(`${env.apiBase}/commands/resetdb`, { clear: true })
      .concatMap(() => {
        return this.http.get<any>(url)
          .pipe(
            map(e => this.normalize(e)),
            this.errorHandler
          )
      });
  }

}

export class DiagApi extends Resource<any> {
  protected get url(): string {
    return `${this.baseUrl}/diagnostics`;
  }

  getCreatedTime() {
    let date = new Date();
    let year = date.getFullYear();
    let month = this.fomateDateNumber(date.getMonth() + 1);
    let day = this.fomateDateNumber(date.getDate());
    let hour = this.fomateDateNumber(date.getHours());
    let minutes = this.fomateDateNumber(date.getMinutes());
    let seconds = this.fomateDateNumber(date.getSeconds());
    return `${year}-${month}-${day} ${hour}:${minutes}:${seconds}`;
  }

  fomateDateNumber(num) {
    if (num > 9) {
      return num;
    }
    else {
      return `0${num}`;
    }
  }

  protected normalizeTests(result: any): any {
    let data = [];
    let tests = [];
    for (let i = 0; i < result.length; i++) {
      let index = data.findIndex(item => {
        return item.name == result[i].category;
      });
      if (index != -1) {
        data[index]['children'].push(result[i]);
      }
      else {
        data.push({
          name: result[i].category,
          children: [result[i]]
        });
      }
    }
    return data;
  }
  getDiagTests() {
    let url = `${this.url}/tests`;
    return this.httpGet(url, null, [
      map(e => this.normalizeTests(e)),
    ]);
  }

  getDiagJob(id: string) {
    return this.httpGet(`${this.url}/${id}`);
  }

  getDiagsByPage(lastId, count, reverse) {
    return this.httpGet(`${this.url}?lastid=${lastId}&count=${count}&reverse=${reverse}`);
  }

  getJobAggregationResult(id: string) {
    return this.httpGet(`${this.url}/${id}/aggregationResult`);
  }

  getDiagTasks(id: string) {
    return this.httpGet(`${this.url}/${id}/tasks`);
  }

  getDiagTaskResult(jobId: string, taskId: string) {
    return this.httpGet(`${this.url}/${jobId}/tasks/${taskId}/result`, null, [
      map((item: any) => {
        if (ApiService.isJSON(item.message)) {
          item.message = JSON.parse(item.message);
        }
        return item;
      }),
    ]);
  }

  create(name: string, targetNodes: string[], diagnosticTest: any, jobType = 'diagnostics') {
    return this.http.post<any>(
      this.url,
      { name, targetNodes, diagnosticTest, jobType },
      { observe: 'response', responseType: 'json' }
    ).pipe(this.errorHandler);
  }

  cancel(jobId: string) {
    let url = `${this.url}/${jobId}`;
    return this.http.patch<any>(url, { request: 'cancel' }, {
      headers: new HttpHeaders({
        'Accept': 'application/json'
      })
    }).pipe(this.errorHandler)
  }
}

export class DashboradApi extends Resource<any>{
  protected get url(): string {
    return `${this.baseUrl}/dashboard`;
  }

  getNodes(): Observable<any> {
    let url = `${this.url}/nodes`;
    return this.httpGet(url);
  }

  getClusrun(): Observable<any> {
    let url = `${this.url}/clusrun`;
    return this.httpGet(url);
  }

  getDiags(): Observable<any> {
    let url = `${this.url}/diagnostics`;
    return this.httpGet(url);
  }
}

@Injectable()
export class ApiService {
  private nodeApi: NodeApi;

  private testApi: TestApi;

  private commandApi: CommandApi;

  private heatmapApi: HeatmapApi;

  private diagApi: DiagApi;

  private dashboardApi: DashboradApi;

  constructor(private http: HttpClient) { }

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
    if (!this.heatmapApi) {
      this.heatmapApi = new HeatmapApi(this.http);
    }
    return this.heatmapApi;
  }

  get diag(): DiagApi {
    if (!this.diagApi) {
      this.diagApi = new DiagApi(this.http);
    }
    return this.diagApi;
  }

  get dashboard(): DashboradApi {
    if (!this.dashboardApi) {
      this.dashboardApi = new DashboradApi(this.http);
    }
    return this.dashboardApi;
  }

  static isJSON(item) {
    item = typeof item !== "string"
      ? JSON.stringify(item)
      : item;

    try {
      item = JSON.parse(item);
    } catch (e) {
      return false;
    }

    if (typeof item === "object" && item !== null) {
      return true;
    }

    return false;
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
          if (typeof (n) === 'object') {
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
