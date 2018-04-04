import { fakeAsync, flush } from '@angular/core/testing';
import { Resource, CommandApi, ApiService, Loop } from './api.service';
import { of } from 'rxjs/observable/of';
import { _throw } from 'rxjs/observable/throw';

fdescribe('Resource', () => {
  class TestResource extends Resource<any> {
    protected get url(): string {
      return '';
    }
  }

  let httpSpy;
  let resource;

  beforeEach(() => {
    httpSpy = jasmine.createSpyObj('HttpClient', ['get']);
    resource = new TestResource(httpSpy);
  });

  it('should get all', () => {
    let value = [1, 2, 3];
    httpSpy.get.and.returnValue(of(value));
    resource.getAll().subscribe(res => expect(res).toEqual(value));
  });

  it('should get', () => {
    let value = 1;
    httpSpy.get.and.returnValue(of(value));
    resource.get('id').subscribe(res => expect(res).toBe(value));
  });

  it('should normalize', () => {
    httpSpy.get.and.returnValue(of(1));
    spyOn(resource, 'normalize').and.returnValue(2);
    resource.get('id').subscribe(res => expect(res).toBe(2));
  });
});

fdescribe('CommandApi', () => {
  let httpSpy;
  let resource;

  beforeEach(() => {
    httpSpy = jasmine.createSpyObj('HttpClient', ['get', 'post']);
    resource = new CommandApi(httpSpy);
  });

  it('should create', () => {
    let value = 1;
    httpSpy.post.and.returnValue(of(value));
    resource.create('a command', []).subscribe(res => expect(res).toBe(value));
  });

  it('should get output', () => {
    let value = 1;
    httpSpy.get.and.returnValue(of(value));
    resource.getOutput('id', 'key', 0).subscribe(res => expect(res).toBe(value));
  });
});

fdescribe('ApiService', () => {
  let apiService;

  beforeEach(() => {
    apiService = new ApiService(null);
  });

  it('should get node', () => {
    let res = apiService.node;
    expect(typeof res).toBe('object');
  });

  it('should get command', () => {
    let res = apiService.command;
    expect(typeof res).toBe('object');
  });

  it('should get heatmap', () => {
    let res = apiService.heatmap;
    expect(typeof res).toBe('object');
  });
});

fdescribe('Loop', () => {
  it('should call next once and stop', fakeAsync(() => {
    let spy = jasmine.createSpyObj('observer', ['next', 'error']);
    spy.next.and.returnValue(undefined);

    let looper = Loop.start(of(100), spy, 0);
    expect(spy.next.calls.count()).toBe(1);
    expect(spy.next).toHaveBeenCalledWith(100);
    expect(spy.error.calls.count()).toBe(0);
    expect(Loop.isStopped(looper)).toBe(true);

    flush();

    expect(spy.next.calls.count()).toBe(1);
    expect(spy.next).toHaveBeenCalledWith(100);
    expect(spy.error.calls.count()).toBe(0);
    expect(Loop.isStopped(looper)).toBe(true);
  }));

  it('should call next twice and stop', fakeAsync(() => {
    let spy = jasmine.createSpyObj('observer', ['next', 'error']);
    spy.next.and.returnValues(true, undefined);

    let looper = Loop.start(of(100, 200), spy, 0);
    expect(spy.next.calls.count()).toBe(1);
    expect(spy.next).toHaveBeenCalledWith(100);
    expect(spy.error.calls.count()).toBe(0);
    expect(Loop.isStopped(looper)).toBe(false);

    flush();

    expect(spy.next.calls.count()).toBe(2);
    expect(spy.next.calls.allArgs()).toEqual([[100], [100]]);
    expect(spy.error.calls.count()).toBe(0);
    expect(Loop.isStopped(looper)).toBe(true);
  }));

  it('could be stopped', fakeAsync(() => {
    let spy = jasmine.createSpyObj('observer', ['next', 'error']);
    spy.next.and.returnValues(true, undefined);

    let looper = Loop.start(of(100, 200), spy, 0);
    expect(spy.next.calls.count()).toBe(1);
    expect(spy.next).toHaveBeenCalledWith(100);
    expect(spy.error.calls.count()).toBe(0);
    expect(Loop.isStopped(looper)).toBe(false);

    Loop.stop(looper);
    expect(Loop.isStopped(looper)).toBe(true);

    flush();

    expect(spy.next.calls.count()).toBe(1);
    expect(spy.next).toHaveBeenCalledWith(100);
    expect(spy.error.calls.count()).toBe(0);
    expect(Loop.isStopped(looper)).toBe(true);
  }));

  it('should call error and stop', fakeAsync(() => {
    let spy = jasmine.createSpyObj('observer', ['next', 'error']);
    spy.next.and.returnValues(true, undefined);

    let error = '!ERROR!';
    let looper = Loop.start(_throw(error), spy, 0);
    expect(spy.next.calls.count()).toBe(0);
    expect(spy.error.calls.count()).toBe(1);
    expect(spy.error).toHaveBeenCalledWith(error);
    expect(Loop.isStopped(looper)).toBe(true);

    flush();

    expect(spy.next.calls.count()).toBe(0);
    expect(spy.error.calls.count()).toBe(1);
    expect(Loop.isStopped(looper)).toBe(true);
  }));
});

