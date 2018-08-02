#v0.3

import sys, json, copy, numpy

def main():
    stdin = json.load(sys.stdin)
    job = stdin['Job']
    tasks = stdin['Tasks']
    taskResults = stdin['TaskResults']

    allNodes = job['TargetNodes']
    nodesNumber = len(allNodes)

    if len(tasks) != len(taskResults):
        printErrorAsJson('Task count {} is not equal to task result count {}.'.format(len(tasks), len(taskResults)))
        return -1

    latencyThreshold = 1000
    throughputThreshold = 100
    packetSize = 2**22
    mode = 'Tournament'.lower()
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
                    if argument['name'].lower() == 'Mode'.lower():
                        mode = argument['value'].lower()
                        continue
    except Exception as e:
        printErrorAsJson('Failed to parse arguments. ' + str(e))
        return -1
    if mode == 'Jumble'.lower():
        throughputThreshold = 0

    taskStateFinished = 3
    taskStateFailed = 4

    taskId2nodePair = {}
    tasksForStatistics = set()
    rdmaNodes = []
    try:
        for task in tasks:
            taskId = task['Id']
            state = task['State']
            nodePair = task['CustomizedData']
            if len(nodePair) > 6 and nodePair[:6] == '[RDMA]':
                nodePair = nodePair[7:]
                if ',' not in nodePair:
                    rdmaNodes.append(nodePair)
            taskId2nodePair[taskId] = nodePair
            if ',' in nodePair and state == taskStateFinished:
                tasksForStatistics.add(taskId)
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
        printErrorAsJson('Failed to parse task result. Task id: {}. {}'.format(taskId, e))
        return -1

    goodPairs = [pair for pair in messages if messages[pair]["Throughput"] > throughputThreshold]
    goodNodesGroups = getGroupsOfFullConnectedNodes(goodPairs)
    goodNodes = set([node for group in goodNodesGroups for node in group])
    if goodNodes != set([node for pair in goodPairs for node in pair.split(',')]):
        printErrorAsJson('Should not get here!')
        return -1
    if nodesNumber == 1 or nodesNumber == 2 and len(rdmaNodes) == 1:
        goodNodes = [task['Node'] for task in tasks if task['State'] == taskStateFinished]
    badNodes = [node for node in allNodes if node not in goodNodes]
    goodNodes = list(goodNodes)
    failedReasons = getFailedReasons(failedNodes)

    result = {
        'GoodNodesGroups':goodNodesGroups,
        'GoodNodes':goodNodes,
        'FailedNodes':failedNodes,
        'BadNodes':badNodes,
        'RdmaNodes':rdmaNodes,
        'FailedReasons':failedReasons
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

def getFailedReasons(failedNodes):
    INTEL_MPI_INSTALL_CLUSRUN_HINT = "wget [intel_mpi_binary_location(eg. https://your_storage_account.blob.core.windows.net/your_container_name/[l_mpi_version].tgz)] && tar -zxvf [l_mpi_version].tgz && sed -i -e 's/ACCEPT_EULA=decline/ACCEPT_EULA=accept/g' ./[l_mpi_version]/silent.cfg && ./[l_mpi_version]/install.sh --silent ./[l_mpi_version]/silent.cfg"
    reasonMpiNotInstalled = 'Intel MPI is not found in default directory "/opt/intel".'
    solutionMpiNotInstalled = 'If Intel MPI is installed on other location, specify the directory in parameter "MPI install directory" of diagnostics test MPI-PingPong. Or download from https://software.intel.com/en-us/intel-mpi-library and install with clusrun command: "{}".'.format(INTEL_MPI_INSTALL_CLUSRUN_HINT)
    reasonHostNotFound = 'The node pair may be not in the same network or there is issue when parsing host name.'
    solutionHostNotFound = 'Check DNS server and ensure the node pair could translate the host name to address of each other.'
    failedReasons = {}
    for failedNode in failedNodes:
        reason = "Unknown"
        if "mpivars.sh: No such file or directory" in failedNode['Detail']:
            reason = reasonMpiNotInstalled
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionMpiNotInstalled, 'Nodes':set()})['Nodes'].add(failedNode['NodeName'])
        elif "Host {} not found:".format(failedNode['NodePair'].split(',')[1]) in failedNode['Detail']:
            reason = reasonHostNotFound
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionHostNotFound, 'NodePairs':[]})['NodePairs'].append(failedNode['NodePair'])
        failedNode['Reason'] = reason
    if reasonMpiNotInstalled in failedReasons:
        failedReasons[reasonMpiNotInstalled]['Nodes'] = list(failedReasons[reasonMpiNotInstalled]['Nodes'])
    return failedReasons.values()

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
