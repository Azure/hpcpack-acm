import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';

@Injectable({
  providedIn: 'root'
})
export class DiagReportService {

  constructor() { }

  jobFinished(state) {
    return state == 'Finished' || state == 'Failed' || state == 'Canceled';
  }

  hasError(result) {
    return this.hasResult(result) && this.hasResult(result['Error']);
  }

  hasResult(result) {
    return result !== undefined && result !== null;
  }

  getErrorMsg(err) {
    let errInfo = err;
    if (ApiService.isJSON(err)) {
      if (err.error) {
        errInfo = err.error;
      }
      else {
        errInfo = JSON.stringify(err);
      }
    }
    return { Error: errInfo };
  }

}
