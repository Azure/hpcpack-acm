export class Node {
  name: string;
  state: string;
  health: string;
  runningJobs: number;
  cpuUsage: any;
  networkUsage: any;
  diskUsage: any;
  properties: {
    cpu: string;
    memory: number;
    os: string;
    nodeGroups: string[];
    nodeTemplate: string;
    network: {
      mac: string;
      ip: string;
      subnet: string;
      name: string;
      domain: string;
    };
  };
  events: {
    id: string;
    type: string;
    resourceType: string;
    resources: string[];
    status: string;
    notBefore: string;
  }[];
}
