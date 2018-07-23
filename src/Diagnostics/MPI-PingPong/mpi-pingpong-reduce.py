import sys, json, copy, numpy

def main():
    stdin = json.load(sys.stdin)
    job = stdin['Job']
    tasks = stdin['Tasks']
    taskResults = stdin['TaskResults']

    allNodes = job['TargetNodes']
    nodesNumber = len(allNodes)
    if nodesNumber < 2:
        printErrorAsJson('The number of nodes is less than 2.')
        return -1

    if len(tasks) != len(taskResults):
        printErrorAsJson('Task count {} is not equal to task result count {}.'.format(len(tasks), len(taskResults)))
        return -1

    latencyThreshold = 1000
    throughputThreshold = 100
    packetSize = 2**22
    try:
        if 'DiagnosticTest' in job and 'Arguments' in job['DiagnosticTest']:
            arguments = job['DiagnosticTest']['Arguments']
            if arguments:
                for argument in arguments:
                    if argument['name'].lower() == 'Latency threshold'.lower():
                        latencyThreshold = int(argument['value'])
                        continue
                    if argument['name'].lower() == 'Throughput threshold'.lower():
                        throughputThreshold = int(argument['value'])
                        continue
                    if argument['name'].lower() == 'Packet size'.lower():
                        packetSize = 2**int(argument['value'])
                        # if packetSize > somevalue, the Latency is invalid
                        continue
    except Exception as e:
        printErrorAsJson('Failed to parse arguments. ' + str(e))
        return -1

    taskStateFinished = 3
    taskStateFailed = 4

    taskId2nodePair = {}
    failedPairs = []
    tasksForStatistics = set()
    rdmaNodes = []
    try:
        for task in tasks:
            taskId = task['Id']
            state = task['State']
            nodePair = task['CustomizedData']
            isRdma = False
            if len(nodePair) > 6 and nodePair[:6] == '[RDMA]':
                nodePair = nodePair[7:]
                isRdma = True
            taskId2nodePair[taskId] = nodePair
            if ',' in nodePair:
                if state == taskStateFailed:
                    failedPairs.append(nodePair)
                if state == taskStateFinished:
                    tasksForStatistics.add(taskId)
            elif isRdma:
                rdmaNodes.append(nodePair)
    except Exception as e:
        printErrorAsJson('Failed to parse tasks. ' + str(e))
        return -1

    messages = {}
    failedNodes = []
    try:
        for taskResult in taskResults:
            taskId = taskResult['TaskId']
            nodeName = taskResult['NodeName']
            nodePair = taskId2nodePair[taskId]
            exitCode = taskResult['ExitCode']
            message = json.loads(taskResult['Message'])
            detail = message['Detail']
            if taskId in tasksForStatistics:
                messages[taskId2nodePair[taskId]] = message
            if exitCode != 0:
                failedNode = {
                    'NodeName':nodeName,
                    'NodePair':nodePair,
                    'ExitCode':exitCode,
                    'Detail':detail
                    }
                failedNodes.append(failedNode)
    except Exception as e:
        printErrorAsJson('Failed to parse task results. ' + str(e))
        return -1

    goodPairs = list(messages.keys())
    goodNodesGroups = getGroupsOfFullConnectedNodes(goodPairs)
    largestGoodNodesGroups = [group for group in goodNodesGroups if len(group) == max([len(groupInner) for groupInner in goodNodesGroups])]
    goodNodesGroupsWithMasters = getGroupsWithMasters(goodPairs)
    largestGoodNodesGroupsWithMasters = [group for group in goodNodesGroupsWithMasters if len(group["Nodes"]) == max([len(groupInner["Nodes"]) for groupInner in goodNodesGroupsWithMasters])]
    goodNodes = set([node for group in goodNodesGroups for node in group])
    if goodNodes != set([node for pair in goodPairs for node in pair.split(',')]):
        printErrorAsJson('Should not get here!')
        return -1
    badNodes = [node for node in allNodes if node not in goodNodes]
    goodNodes = list(goodNodes)

    result = {
        'GoodPairs':goodPairs,
        'GoodNodesGroups':goodNodesGroups,
        'GoodNodesGroupsWithMasters':goodNodesGroupsWithMasters,
        'LargestGoodNodesGroups':largestGoodNodesGroups,
        'LargestGoodNodesGroupsWithMasters':largestGoodNodesGroupsWithMasters,
        'GoodNodes':goodNodes,
        'FailedPairs':failedPairs,
        'FailedNodes':failedNodes,
        'BadNodes':badNodes,
        'RdmaNodes':rdmaNodes
        }

    if messages:
        histogramSize = min(nodesNumber, 10)
        statisticsItems = ["Throughput"]
        if packetSize < 65536:
            statisticsItems.append("Latency")
        for item in statisticsItems:
            data = [messages[pair][item] for pair in messages]
            golbalMin = numpy.amin(data)
            golbalMax = numpy.amax(data)
            histogram = [list(array) for array in numpy.histogram(data, bins=histogramSize, range=(golbalMin, golbalMax))]

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
            for node in goodNodes:
                data = [messages[pair][item] for pair in messages if node in pair.split(',')]
                histogram = [list(array) for array in numpy.histogram(data, bins=histogramSize, range=(golbalMin, golbalMax))]
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

def getNodeMap(pairs):
    connectedNodesOfNode = {}
    for pair in pairs:
        nodes = pair.split(',')
        connectedNodesOfNode.setdefault(nodes[0], set()).add(nodes[1])
        connectedNodesOfNode.setdefault(nodes[1], set()).add(nodes[0])
    return connectedNodesOfNode

def getGroupsWithMasters(pairs):
    connectedNodesOfNode = getNodeMap(pairs)
    for node in connectedNodesOfNode:
        connectedNodesOfNode[node].add(node)
    groupsWithMasters = []
    while connectedNodesOfNode:
        master, nodes = connectedNodesOfNode.popitem()
        masters = [node for node in connectedNodesOfNode if connectedNodesOfNode[node] == nodes]
        for node in masters:
            del connectedNodesOfNode[node]
        masters.append(master)
        groupsWithMasters.append({
            "Nodes":list(nodes),
            "Masters":masters
            })
    return groupsWithMasters

def getGroupsOfFullConnectedNodes(pairs):
    connectedNodesOfNode = getNodeMap(pairs)
    nodes = list(connectedNodesOfNode.keys())
    if not nodes:
        return []
    groups = [set([nodes[0]])]
    for node in nodes[1:]:
        newGroups = []
        for group in groups:
            newGroup = group & connectedNodesOfNode[node]
            newGroup.add(node)
            addToGroups(newGroups, newGroup)
        for group in newGroups:
            addToGroups(groups, group)
    return [list(group) for group in groups]

def addToGroups(groups, new):
    for old in groups:
        if old <= new or old >= new:
            old |= new
            return
    groups.append(new)

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
