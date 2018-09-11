export class ListNode {
    id: string;
    name: string;
    state: string;
    health: string;
    eventCount: number;
    runningJobCount: number;
    nodeRegistrationInfo: {
        distroInfo: string;
        memoryMegabytes: number;
    };
}