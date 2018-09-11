export class Node {
  id: string;
  name: string;
  state: string;
  health: string;
  eventCount: number;
  runningJobCount: number;
  nodeRegistrationInfo: {
    coreCount: number;
    distroInfo: string;
    gpuInfo: {}[];
    memoryMegabytes: number;
    networksInfo: {
      name: string;
      macAddress: string;
      ipV4: string;
      ipV6: string;
      isIB: boolean;
    }[];
    nodeName: string;
    socketCount: number
  };
}
