import sys, json, copy, numpy

def main():
    stdin = json.load(sys.stdin)
    job = stdin['Job']
    tasks = stdin['Tasks']
    taskResults = stdin['TaskResults']

    nodes = job['TargetNodes']
    nodesNumber = len(nodes)
    if nodesNumber < 2:
        printErrorAsJson('The number of nodes is less than 2.')
        return -1

    expectedTaskCount = nodesNumber + nodesNumber*(nodesNumber-1)/2
    if len(tasks) != expectedTaskCount:
        printErrorAsJson('Task count ' + str(len(tasks)) + ' is not correct, should be ' + str(expectedTaskCount) + '.')
        return -1
    if len(taskResults) != expectedTaskCount:
        printErrorAsJson('Task result count ' + str(len(tasks)) + ' is not correct, should be ' + str(expectedTaskCount) + '.')
        return -1

    taskId2nodePair = {}
    try:
        for task in tasks:
            nodePair = task['CustomizedData']
            # filter out tasks running on one node
            if ',' in nodePair:
                taskId2nodePair[task['Id']] = nodePair
    except Exception as e:
        printErrorAsJson('Failed to parse tasks. ' + str(e))
        return -1
                
    messages = {}
    try:
        for taskResult in taskResults:
            taskId = taskResult['TaskId']
            if taskId in taskId2nodePair:
                messages[taskId2nodePair[taskId]] = json.loads(taskResult['Message'])
    except Exception as e:
        printErrorAsJson('Failed to parse task results. ' + str(e))
        return -1

    latencyThreshold = 1000
    throughputThreshold = 100
    packetSize = 1
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
                    if argument['name'].lower() == 'Packet size'.lower():
                        packetSize = 2**int(argument['value'])
                        continue
    except Exception as e:
        printErrorAsJson('Failed to parse arguments. ' + str(e))
        return -1

    result = {}
    for item in ["Latency", "Throughput"]:
        data = [messages[pair][item] for pair in messages]
        golbalMin = numpy.amin(data)
        golbalMax = numpy.amax(data)
        histogram = [list(array) for array in numpy.histogram(data, bins=nodesNumber, range=(golbalMin, golbalMax))]

        result[item] = {}
        if item == "Latency":
            unit = "us"
            threshold = latencyThreshold
            badPairs = [{"Pair":pair, "Value":messages[pair][item]} for pair in messages if messages[pair][item] > latencyThreshold]
            bestPairs = {"Pairs":[pair for pair in messages if messages[pair][item] == golbalMin], "Value":golbalMin}
            worstPairs = {"Pairs":[pair for pair in messages if messages[pair][item] == golbalMax], "Value":golbalMax}
        else:
            unit = "MB/s"
            threshold = throughputThreshold
            badPairs = [{"Pair":pair, "Value":messages[pair][item]} for pair in messages if messages[pair][item] < throughputThreshold]
            bestPairs = {"Pairs":[pair for pair in messages if messages[pair][item] == golbalMax], "Value":golbalMax}
            worstPairs = {"Pairs":[pair for pair in messages if messages[pair][item] == golbalMin], "Value":golbalMin}
            if packetSize == 2**0:
                packetSize = 2**22

        result[item]["Unit"] = unit
        result[item]["Threshold"] = threshold
        result[item]["Packet_size"] = str(packetSize) + ' Bytes'
        result[item]["Result"] = {}
        result[item]["Result"]["Passed"] = len(badPairs) == 0
        result[item]["Result"]["Bad_pairs"] = badPairs
        result[item]["Result"]["Best_pairs"] = bestPairs
        result[item]["Result"]["Worst_pairs"] = worstPairs
        result[item]["Result"]["Histogram"] = renderHistogram(histogram)
        result[item]["Result"]["Average"] = numpy.average(data)
        result[item]["Result"]["Median"] = numpy.median(data)
        result[item]["Result"]["Standard_deviation"] = numpy.std(data)
        result[item]["Result"]["Variability"] = getVariability(data)

        result[item]["ResultByNode"] = {}
        for node in nodes:
            data = [messages[pair][item] for pair in messages if node in pair.split(',')]
            histogram = [list(array) for array in numpy.histogram(data, bins=nodesNumber, range=(golbalMin, golbalMax))]
            if item == "Latency":            
                badPairs = [{"Pair":pair, "Value":messages[pair][item]} for pair in messages if messages[pair][item] > latencyThreshold and node in pair.split(',')]
                bestPairs = {"Pairs":[pair for pair in messages if messages[pair][item] == numpy.amin(data) and node in pair.split(',')], "Value":numpy.amin(data)}
                worstPairs = {"Pairs":[pair for pair in messages if messages[pair][item] == numpy.amax(data) and node in pair.split(',')], "Value":numpy.amax(data)}
            else:
                badPairs = [{"Pair":pair, "Value":messages[pair][item]} for pair in messages if messages[pair][item] < throughputThreshold and node in pair.split(',')]
                bestPairs = {"Pairs":[pair for pair in messages if messages[pair][item] == numpy.amax(data) and node in pair.split(',')], "Value":numpy.amax(data)}
                worstPairs = {"Pairs":[pair for pair in messages if messages[pair][item] == numpy.amin(data) and node in pair.split(',')], "Value":numpy.amin(data)}           
            result[item]["ResultByNode"][node] = {}
            result[item]["ResultByNode"][node]["Bad_pairs"] = badPairs
            result[item]["ResultByNode"][node]["Passed"] = len(badPairs) == 0
            result[item]["ResultByNode"][node]["Best_pairs"] = bestPairs
            result[item]["ResultByNode"][node]["Worst_pairs"] = worstPairs
            result[item]["ResultByNode"][node]["Histogram"] = renderHistogram(histogram)
            result[item]["ResultByNode"][node]["Average"] = numpy.average(data)
            result[item]["ResultByNode"][node]["Median"] = numpy.median(data)
            result[item]["ResultByNode"][node]["Standard_deviation"] = numpy.std(data)
            result[item]["ResultByNode"][node]["Variability"] = getVariability(data)

    print(json.dumps(result))
    return 0

def renderHistogram(histogram):
    binEdges = histogram[1]
    return [histogram[0], ["{0:.2f}".format(binEdges[i-1]) + '-' + "{0:.2f}".format(binEdges[i]) for i in range(1, len(binEdges))]]

def getVariability(data):
    variability = numpy.std(data)/max(numpy.average(data), 10**-6)
    if variability < 0.05:
        return "Low"
    elif variability < 0.25:
        return "Moderate"
    else:
        return "High"

def printErrorAsJson(errormessage):
    print(json.dumps({"Error":errormessage}))

if __name__ == '__main__':
    sys.exit(main())
