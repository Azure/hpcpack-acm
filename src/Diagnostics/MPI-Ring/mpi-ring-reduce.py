import sys, json, copy

def main():
    result = json.load(sys.stdin)
    job = result['Job']
    tasks = result['Tasks']
    taskResults = result['TaskResults']
    
    nodes = job['TargetNodes']
    if len(nodes) < 2:
        printErrorAsJson('The number of nodes is less than 2.')
        return -1

    if len(tasks) != len(nodes) + 1:
        printErrorAsJson('Task count is not correct.')
        return -1

    try:
        for taskResult in taskResults:
            if taskResult['TaskId'] == 1:
                message = json.loads(taskResult['Message'])
                latency = message['Latency']
                throughput = message['Throughput']
                break
    except Exception as e:
        printErrorAsJson('Failed to parse task result. ' + str(e))
        return -1

    latencyThreshold = 1000
    throughputThreshold = 100
    try:
        if 'DiagnosticTest' in job and 'Arguments' in job['DiagnosticTest']:
            arguments = job['DiagnosticTest']['Arguments']
            if arguments:
                arguments = json.loads(arguments)
                for argument in arguments:
                    if argument['name'].lower() == 'Latency threshold'.lower():
                        latencyThreshold = int(argument['value'])
                        continue
                    if argument['name'].lower() == 'Throughput threshold'.lower():
                        throughputThreshold = int(argument['value'])
                        continue
    except Exception as e:
        printErrorAsJson('Failed to parse arguments. ' + str(e))
        return -1

    passed = True
    if latency > latencyThreshold:
        passed = False
    if throughput < throughputThreshold:
        passed = False

    result = {
        "Nodes" : nodes,
        "Passed" : passed,
        "Latency" : {
            "Value" : latency,
            "Threshold" : latencyThreshold,
            "Unit" : "us",
            },
        "Throughput" : {
            "Value" : throughput,
            "Threshold" : throughputThreshold,
            "Unit" : "MB/s",
            },
        }
    print(json.dumps(result))
    return 0
        
def printErrorAsJson(errormessage):
    print(json.dumps({"Error":errormessage}))
    
if __name__ == '__main__':
    sys.exit(main())
