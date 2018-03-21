import { InMemoryDbService } from 'angular-in-memory-web-api';
import { NodeApi, CommandApi, TestApi } from './api.service';

export class InMemoryDataService implements InMemoryDbService {
  urlMap = [
    { url: NodeApi.url, coll: 'nodes' },
    { url: CommandApi.url, coll: 'commands' },
    { url: TestApi.url, coll: 'tests' },
  ];

  parseRequestUrl(url, utils) {
    let res = this.urlMap.find(e => url.indexOf(e.url) == 0);
    if (!res)
      return null;

    let parsed = utils.parseRequestUrl(url);
    parsed.collectionName = res.coll;
    if (url.length != res.url.length) {
      let id = url.substring(res.url.length + 1); //+1 to skip "/"
      parsed.id = id;
    }
    return parsed;
  }

  generateUsage(cb): any[] {
    let usage = [];
    const now = new Date().getTime();
    const span = 5 * 60 * 1000;
    const num = 12;
    for (let i = 0; i < num; i++) {
      let d = new Date(now - span * i);
      let item = cb(d, i, num);
      usage.push(item);
    }
    usage.reverse();
    return usage;
  }

  //Begin of nodes
  generateCpuUsage(): any[] {
    return this.generateUsage((d) => ({
      ts: d.getTime(),
      value: Math.round(Math.random() * 100)
    }));
  }

  generateNetworkUsage(): any[] {
    return this.generateUsage((d) => ({
      ts: d.getTime(),
      inbound: Math.random(),
      outbound: Math.random() * 10,
    }));
  }

  generateDiskUsage(): any[] {
    return this.generateUsage((d) => ({
      ts: d.getTime(),
      read: Math.random() * 10,
      write: Math.random(),
    }));
  }

  randomState(): string {
    const states = ['online', 'offline', 'unknown', 'provisioning', 'starting', 'draining', 'removing', 'rejected', 'not-deployed'];
    let idx = Math.random() < 0.7 ? 0 : (this.randomNum(100) % (states.length - 1) + 1);
    if (idx < 0 || idx >= states.length)
      console.log(idx);
    return states[idx];
  }

  randomHealth(): string {
    const states = ['ok', 'warning', 'error', 'transitional', 'unapproved'];
    let idx = Math.random() < 0.7 ? 0 : (this.randomNum(100) % (states.length - 1) + 1);
    return states[idx];
  }

  randomNum(scale: number): number {
    return Math.round(Math.random() * scale);
  }

  generateNames(num) {
    let a = [];
    for (let i = 1; i <= num; i++) {
      let prefix = Math.random() > 0.9 ? 'HN' : 'WN';
      let name = prefix + i;
      a.push(name);
    }
    return a;
  }

  generateNodes() {
    const now = new Date().getTime();
    let names = this.generateNames(200);
    let index = 1;
    let nodes = names.map(name => {
      let state = this.randomState();
      let health = this.randomHealth();
      let isHead = name.indexOf('HN') == 0;
      let res = {
        id: index,
        name: name,
        state: state,
        health: health,
        runningJobCount: state == 'online' && health == 'ok' ? this.randomNum(100) : 0,
        cpuUsage: this.generateCpuUsage(),
        networkUsage: this.generateNetworkUsage(),
        diskUsage: this.generateDiskUsage(),
        properties: {
          cpu: 'Intel(R) Xeon(R) CPU E5-2673 v3 @ 2.40 GHz',
          memory: 14336,
          os: 'Microsoft Windows NT 6.2.9200.0',
          nodeGroup: isHead ? ['WCFBrokerNodes', 'HeadNodes', 'CompuateNodes'] : ['CompuateNodes'],
          nodeTemplate: isHead ? 'HeadNode Template': 'Default ComputeNode Template',
          network: {
            mac: '00-0D-3A-A1-B2-17',
            ip: '10.0.0.' + index,
            subnet: '255.255.128.0',
            name: 'Enterprise',
            domain: 'reddog.microsoft.com',
          }
        },
        events: [],
      };
      if (health == 'warning') {
        res.events = [
          {
            id: '5d882999-f4a7-4319-bdb6-2fae35025a45',
            type: 'Freeze',
            resourceType: 'VirtualMachine',
            resources: ["FrontEnd_IN_0", "BackEnd_IN_0"],
            status: 'Started',
            notBefore: now,
          },
          {
            id: '81ec720b-98d8-4908-8b3a-436dc61f1114',
            type: 'Reboot',
            resourceType: 'VirtualMachine',
            resources: ["FrontEnd_IN_0", "BackEnd_IN_0"],
            status: 'Scheduled',
            notBefore: now + 15 * 60 * 1000,
          },
        ];
      }
      index++;
      return res;
    });
    return nodes;
  }
  //End of nodes

  //Begin of commands
  generateDirResult(id, time, state, progress) {
    const dirResult = `
     Volume in drive C has no label.
     Volume Serial Number is 6C15-D365

     Directory of C:\\Users\\hpcadmin

    01/04/2018  01:50 AM    <DIR>          .
    01/04/2018  01:50 AM    <DIR>          ..
    01/04/2018  01:50 AM    <DIR>          Contacts
    01/04/2018  01:50 AM    <DIR>          Desktop
    01/12/2018  07:30 AM    <DIR>          Documents
    01/04/2018  01:50 AM    <DIR>          Downloads
    01/04/2018  01:50 AM    <DIR>          Favorites
    01/04/2018  01:50 AM    <DIR>          Links
    01/04/2018  01:50 AM    <DIR>          Music
    01/04/2018  01:50 AM    <DIR>          Pictures
    01/04/2018  01:50 AM    <DIR>          Saved Games
    01/04/2018  01:50 AM    <DIR>          Searches
    01/04/2018  01:50 AM    <DIR>          Videos
                   0 File(s)              0 bytes
                  13 Dir(s)  107,968,028,672 bytes free
    `
    const now = new Date().getTime();
    let result = {
      id: id,
      commandLine: 'dir',
      state: state,
      progress: progress,
      startedAt: time - 1000 * 60 * 2,
      updatedAt: time,
    };
    let names = this.generateNames(100);
    (result as any).nodes = names.map(name => {
      let s;
      if (state === 'running') {
        let r = Math.random();
        s = r < 0.5 ? 'running' : (r < 0.9 ? 'finished' : 'failed');
      }
      else {
        s = state;
      }
      return {
        name: name,
        state: s,
        output: name + "\n" + dirResult,
      };
    });
    return result;
  }

  generateCommands() {
    const now = new Date().getTime();
    let results = [
      this.generateDirResult(1, now, 'running', 0.6),
      this.generateDirResult(2, now - 5 * 60 * 1000, 'success', 1.0),
    ];
    return results;
  }
  //End of commands

  //Begin of tests
  generateServiceRunningTest(id) {
    const nodeServices = [
      'HPC Management Service',
      'HPC MPI Service',
      'HPC Node Manager Service',
      'HPC SOA Diag Mon Service',
      'HPC Monitoring Client Service',
    ];
    const headServices = [
      'HPC MPI Service',
      'HPC Node Manager Service',
      'HPC SOA Diag Mon Service',
      'HPC Monitoring Client Service',
      'HPC Broker Service',
    ];
    const sRun = "Running";
    const sStop = "Stopped";
    const now = new Date().getTime();
    let result = {
      id: id,
      testName: 'Service Running Test',
      state: 'failure',
      progress: 1.0,
      startedAt: now - 1000 * 60 * 2,
      updatedAt: now,
    };
    let names = this.generateNames(100);
    (result as any).nodes = names.map(name => {
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
    const now = new Date().getTime();
    let result = {
      id: id,
      testName: 'Ping Test',
      state: 'failure',
      progress: 1.0,
      startedAt: now - 1000 * 60 * 28,
      updatedAt: now - 1000 * 60 * 27,
    };

    let names = this.generateNames(100);
    (result as any).nodes = names.map(name => {
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

  generateTests() {
    let results = [
      this.generateServiceRunningTest(1),
      this.generatePingTest(2),
    ];

    return results;
  }
  //End of tests

  createDb() {
    let nodes = this.generateNodes();
    let commands = this.generateCommands();
    let tests = this.generateTests();
    return { nodes, commands, tests };
  }
}
