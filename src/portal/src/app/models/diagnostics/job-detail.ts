export class DiagJobDetail {
    id: number;
    name: string;
    progress: number;
    state: string;
    diagnosticTest: {
        name: string,
        category: string
    };
    targetNodes: string[]
}