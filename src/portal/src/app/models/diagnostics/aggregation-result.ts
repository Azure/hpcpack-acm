export class AggregationResult {
    BadNodes: string[];
    FailedNodes: Object;
    FailedReasons: Array<Object>;
    GoodNodesGroups: Array<Array<string>>;
    Latency: Object;
    Throughput: Object;
}