export class ListJob {
    id: number;
    name: string;
    updatedAt: string;
    diagnosticTest: {
        name: string;
        category: string
    };
    state: string;
    progress: number;
    createdAt: string;
}