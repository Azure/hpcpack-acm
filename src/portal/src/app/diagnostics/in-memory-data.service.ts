import { InMemoryDbService } from 'angular-in-memory-web-api';
import { Result } from './result';

const now = new Date().getTime();

const nodeServices = [
  'HPC Management Service',
  'HPC MPI Service',
  'HPC Node Manager Service',
  'HPC SOA Diag Mon Service',
  'HPC Monitoring Client Service',
]

const headServices = [
  'HPC MPI Service',
  'HPC Node Manager Service',
  'HPC SOA Diag Mon Service',
  'HPC Monitoring Client Service',
  'HPC Broker Service',
]

const sRun = "Running"

const sStop = "Stopped"

export class InMemoryDataService implements InMemoryDbService {
  generateNames(num) {
    let a = [];
    for (let i = 1; i <= num; i++) {
      let prefix = Math.random() > 0.9 ? 'HN' : 'WN';
      let name = prefix + i;
      a.push(name);
    }
    return a;
  }

  generateServiceRunningTest(id) {
    let result = {
      id: id,
      testName: 'Service Running Test',
      state: 'failure',
      progress: 1.0,
      startedAt: now - 1000 * 60 * 2,
      updatedAt: now,
    } as Result;
    let names = this.generateNames(100);
    result.nodes = names.map(name => {
      let ok = Math.random() < 0.9;
      let services = name.match(/^HN/) ? headServices : nodeServices;
      if (ok) {
        services = services.map(sname => ({ name: sname, status: sRun })) as any;
      }
      else {
        services = services.map(sname => ({ name: sname, status: Math.random() < 0.1 ? sRun : sStop })) as any;
      }
      return {
        name: name,
        state: ok ? 'success' : 'failure',
        details: {
          services: services,
        },
      };
    });
    return result;
  }

  generatePingTest(id) {
    let result = {
      id: id,
      testName: 'Ping Test',
      state: 'failure',
      progress: 1.0,
      startedAt: now - 1000 * 60 * 28,
      updatedAt: now - 1000 * 60 * 27,
    } as Result;

    let names = this.generateNames(100);
    result.nodes = names.map(name => {
      let ok = Math.random() < 0.85;

      //Destination IP Address Result Average Best Worst Successful Failures
      //IAASCN000 fe80::eda3:a138:14ab:a23e Succeeded 0 ms 0 ms 0 ms 4 0
      //IAASCN001  Failed 0 ms 0 ms 0 ms 0 4
      //IAASCN002  Failed 0 ms 0 ms 0 ms 0 4
      //REF 10.0.0.5 Succeeded 0 ms 0 ms 0 ms 4 0

      let pings = names.map(pname => {
        if (name == pname) {
          return {
            destination: pname,
            ip: 'xxx.xxx.xxx.xxx',
            result: 'success' ,
            average: 0,
            best: 0,
            worst: 0,
            successful: 4,
            failed: 0,
          };
        }
        let ok2 = ok || Math.random() < 0.2;
        let best, worst, avg;
        if (ok2) {
          best = Math.round(Math.random() * 10);
          worst = best + Math.round(Math.random() * 100);
          avg = Math.round((best + worst) / (2 + Math.random()));
        }
        else {
          best = 0;
          worst = 0;
          avg = 0;
        }
        return {
          destination: pname,
          ip: 'xxx.xxx.xxx.xxx',
          result: ok2 ? 'success' : 'failure',
          average: avg,
          best: best,
          worst: worst,
          successful: ok2 ? 4 : 0,
          failed: ok2 ? 0 : 4,
        };
      });

      let best, worst, avg;
      if (ok) {
        best = Math.round(Math.random() * 10);
        worst = best + Math.round(Math.random() * 100);
        avg = Math.round((best + worst) / (2 + Math.random()));
      }
      else {
        best = 0;
        worst = 0;
        avg = 0;
      }

      return {
        name: name,
        state: ok ? 'success' : 'failure',
        best: best,
        worst: worst,
        average: avg,

        details: {
          pings: pings,
        },
      };
    });
    return result;
  }

  createDb() {
    let results = [
      this.generateServiceRunningTest(1),
      this.generatePingTest(2),
    ];

    return { results };
  }
}
