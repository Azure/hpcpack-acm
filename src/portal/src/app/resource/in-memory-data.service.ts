import { InMemoryDbService } from 'angular-in-memory-web-api';

export class InMemoryDataService implements InMemoryDbService {
  generateUsage(cb): any[] {
    let usage = [];
    let now = new Date().getTime();
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

  createDb() {
    let now = new Date().getTime();
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
        runningJobs: state == 'online' && health == 'ok' ? this.randomNum(100) : 0,
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
    return { nodes };
  }
}
