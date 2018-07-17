import { Injectable } from '@angular/core';

@Injectable()
export class JobStateService {
  private state = {
    'Finished': 'finished',
    'Queued': 'queued',
    'Failed': 'failed',
    'Running': 'running',
    'Canceled': 'canceled'
  };

  private icon = {
    'Finished': 'done',
    'finished': 'done',
    'Queued': 'blur_linear',
    'queued': 'blur_linear',
    'Failed': 'clear',
    'failed': 'clear',
    'Running': 'blur_on',
    'running': 'blur_on',
    'Canceled': 'cancel',
    'canceled': 'cancel'
  };

  constructor() { }

  stateClass(state) {
    return this.state[state] || '';
  }

  stateIcon(state) {
    return this.icon[state] || 'autorenew';
  }

}
