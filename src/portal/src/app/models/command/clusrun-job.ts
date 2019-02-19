export class ClusrunJob {
    id: number;
    command: string;
    state: string;
    createdAt: number;
    updatedAt: number;
    progress: number;
    targetNodes: Array<String>;
}