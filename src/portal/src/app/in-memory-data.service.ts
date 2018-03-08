import { InMemoryDbService } from 'angular-in-memory-web-api';

export class InMemoryDataService implements InMemoryDbService {
  createDb() {
    return {};
  }

  get(reqInfo: RequestInfo) {
    return undefined;
  }
}
