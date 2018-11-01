#v0.16

import sys, json, copy, numpy, time

def main():
    startTime = time.time()
    
    stdin = json.load(sys.stdin)
    job = stdin['Job']
    tasks = stdin['Tasks']
    taskResults = stdin['TaskResults']

    allNodes = job['TargetNodes']
    nodesNumber = len(allNodes)

    # for debug
    warnings = []
    if len(tasks) != len(taskResults):
        warnings.append('Task count {} is not equal to task result count {}.'.format(len(tasks), len(taskResults)))

    defaultPacketSize = 2**22
    packetSize = defaultPacketSize
    mode = 'Tournament'.lower()
    intelMpiLocation = '/opt/intel/impi/2018.4.274'
    debug = None
    try:
        if 'DiagnosticTest' in job and 'Arguments' in job['DiagnosticTest']:
            arguments = job['DiagnosticTest']['Arguments']
            if arguments:
                for argument in arguments:
                    if argument['name'].lower() == 'Packet size'.lower():
                        packetSize = 2**int(argument['value'])
                        continue
                    if argument['name'].lower() == 'Mode'.lower():
                        mode = argument['value'].lower()
                        continue                        
                    if argument['name'].lower() == 'Intel MPI location'.lower():
                        intelMpiLocation = argument['value']
                        continue
                    if argument['name'].lower() == 'Debug'.lower():
                        debug = True
                        continue
    except Exception as e:
        printErrorAsJson('Failed to parse arguments. ' + str(e))
        return -1

    taskStateFinished = 3
    taskStateFailed = 4
    taskStateCanceled = 5

    taskId2nodePair = {}
    tasksForStatistics = set()
    rdmaNodes = []
    canceledTasks = []
    canceledNodePairs = set()
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
            if state == taskStateCanceled:
                canceledTasks.append(taskId)
                canceledNodePairs.add(nodePair)
    except Exception as e:
        printErrorAsJson('Failed to parse tasks. ' + str(e))
        return -1

    messages = {}
    failedTasks = []
    taskRuntime = {}
    try:
        for taskResult in taskResults:
            taskId = taskResult['TaskId']
            nodeName = taskResult['NodeName']
            nodePair = taskId2nodePair[taskId]
            exitCode = taskResult['ExitCode']
            if 'Message' in taskResult and taskResult['Message']:
                try:
                    message = json.loads(taskResult['Message'])
                    hasResult = message['Latency'] > 0 and message['Throughput'] > 0
                    detail = message['Detail']
                    taskRuntime[taskId] = message['Time']
                except:
                    hasResult = False
                    detail = taskResult['Message']
                if taskId in tasksForStatistics and hasResult:
                    messages[taskId2nodePair[taskId]] = message
                if exitCode != 0 or not hasResult:
                    failedTask = {
                        'TaskId':taskId,
                        'NodeName':nodeName,
                        'NodeOrPair':nodePair,
                        'ExitCode':exitCode,
                        'Detail':detail
                        }
                    failedTasks.append(failedTask)
            else:
                raise Exception("No Message")
    except Exception as e:
        printErrorAsJson('Failed to parse task result. Task id: {}. {}'.format(taskId, e))
        return -1

    latencyThreshold = packetSize//50 if packetSize > 2**20 else 10000
    throughputThreshold = packetSize//1000 if 2**0 < packetSize < 2**20 else 50
    if len(rdmaNodes) == len(allNodes):
        latencyThreshold = 2**3 if packetSize < 2**13 else packetSize/2**10
        throughputThreshold = packetSize/2**3 if 2**0 < packetSize < 2**13 else 2**10
    if mode == 'Parallel'.lower():
        latencyThreshold = 1000000
        throughputThreshold = 0

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
    failedReasons, failedReasonsByNode = getFailedReasons(failedTasks, intelMpiLocation, canceledNodePairs)

    result = {
        'GoodNodesGroups':getLargestNonoverlappingGroups(goodNodesGroups),
        'GoodNodes':goodNodes,
        'FailedNodes':failedReasonsByNode,
        'BadNodes':badNodes,
        'RdmaNodes':rdmaNodes,
        'FailedReasons':failedReasons,
        'Latency':{},
        'Throughput':{},
        }

    if messages:
        nodesInMessages = [node for nodepair in messages.keys() for node in nodepair.split(',')]
        nodesInMessages = list(set(nodesInMessages))
        messagesByNode = {}
        for pair in messages:
            for node in pair.split(','):
                messagesByNode.setdefault(node, {})[pair] = messages[pair]
        histogramSize = min(nodesNumber, 10)

        statisticsItems = ["Latency", "Throughput"]
        for item in statisticsItems:
            data = [messages[pair][item] for pair in messages]
            globalMin = numpy.amin(data)
            globalMax = numpy.amax(data)
            histogram = [list(array) for array in numpy.histogram(data, bins=histogramSize, range=(globalMin, globalMax))]

            if item == "Latency":
                unit = "us"
                threshold = latencyThreshold
                badPairs = [{"Pair":pair, "Value":messages[pair][item]} for pair in messages if messages[pair][item] > latencyThreshold]
                badPairs.sort(key=lambda x:x["Value"], reverse=True)
                bestPairs = {"Pairs":[pair for pair in messages if messages[pair][item] == globalMin], "Value":globalMin}
                worstPairs = {"Pairs":[pair for pair in messages if messages[pair][item] == globalMax], "Value":globalMax}
            else:
                unit = "MB/s"
                threshold = throughputThreshold
                badPairs = [{"Pair":pair, "Value":messages[pair][item]} for pair in messages if messages[pair][item] < throughputThreshold]
                badPairs.sort(key=lambda x:x["Value"])
                bestPairs = {"Pairs":[pair for pair in messages if messages[pair][item] == globalMax], "Value":globalMax}
                worstPairs = {"Pairs":[pair for pair in messages if messages[pair][item] == globalMin], "Value":globalMin}
                if packetSize == 2**0:
                    packetSize = defaultPacketSize

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
            for node in nodesInMessages:
                data = [messagesByNode[node][pair][item] for pair in messagesByNode[node]]
                histogram = [list(array) for array in numpy.histogram(data, bins=histogramSize, range=(globalMin, globalMax))]
                if item == "Latency":
                    badPairs = [{"Pair":pair, "Value":messagesByNode[node][pair][item]} for pair in messagesByNode[node] if messagesByNode[node][pair][item] > latencyThreshold and node in pair.split(',')]
                    badPairs.sort(key=lambda x:x["Value"], reverse=True)
                    bestPairs = {"Pairs":[pair for pair in messagesByNode[node] if messagesByNode[node][pair][item] == numpy.amin(data) and node in pair.split(',')], "Value":numpy.amin(data)}
                    worstPairs = {"Pairs":[pair for pair in messagesByNode[node] if messagesByNode[node][pair][item] == numpy.amax(data) and node in pair.split(',')], "Value":numpy.amax(data)}
                else:
                    badPairs = [{"Pair":pair, "Value":messagesByNode[node][pair][item]} for pair in messagesByNode[node] if messagesByNode[node][pair][item] < throughputThreshold and node in pair.split(',')]
                    badPairs.sort(key=lambda x:x["Value"])
                    bestPairs = {"Pairs":[pair for pair in messagesByNode[node] if messagesByNode[node][pair][item] == numpy.amax(data) and node in pair.split(',')], "Value":numpy.amax(data)}
                    worstPairs = {"Pairs":[pair for pair in messagesByNode[node] if messagesByNode[node][pair][item] == numpy.amin(data) and node in pair.split(',')], "Value":numpy.amin(data)}
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

    endTime = time.time()
    
    if debug != None:
        taskRuntime = {
            'Max': numpy.amax(list(taskRuntime.values())),
            'Ave': numpy.average(list(taskRuntime.values())),
            'Sorted': sorted([{'runtime':taskRuntime[key], 'taskId':key, 'nodepair':taskId2nodePair[key]} for key in taskRuntime], key=lambda x:x["runtime"], reverse=True)
            }
        failedTasksByExitcode = {}
        for task in failedTasks:
            failedTasksByExitcode.setdefault(task['ExitCode'], []).append(task['TaskId'])
        result['DebugInfo'] = {
            'ReduceRuntime':endTime - startTime,
            'GoodNodesGroups':goodNodesGroups,
            'CanceledTasks':canceledTasks,
            'FailedTasksGroupByExitcode':failedTasksByExitcode,
            'Warnings':warnings,
            'TaskRuntime':taskRuntime,
            }
        
    print(json.dumps(result))
    return 0

def getFailedReasons(failedTasks, intelMpiLocation, canceledNodePairs):
    reasonMpiNotInstalled = 'Intel MPI is not found in directory "{}".'.format(intelMpiLocation)
    solutionMpiNotInstalled = 'Please ensure Intel MPI is installed on the location specified by parameter "Intel MPI location" of diagnostics test MPI-PingPong. Run diagnostics test MPI-Installaion on the selected nodes if it is not installed on them.'

    reasonHostNotFound = 'The node pair may be not in the same network or there is issue when parsing host name.'
    solutionHostNotFound = 'Check DNS server and ensure the node pair could translate the host name to address of each other.'

    reasonFireWall = 'The connection was blocked by firewall.'
    reasonFireWallProbably = 'The connection may be blocked by firewall.'
    solutionFireWall = 'Check and configure the firewall properly.'

    reasonNodeSingleCore = 'MPI PingPong can not run inside a node with only 1 core.'
    solutionNodeSingleCore = 'Ignore this failure.'

    reasonTaskTimeout = 'Task timeout.'
    reasonPingpongTimeout = 'Pingpong test timeout.'
    reasonSampleTimeout = 'Pingpong test sample timeout.'
    reasonNoResult = 'No result.'

    reasonWgetFailed = 'Failed to download filter script.'
    solutionWgetFailed = 'Check accessibility of "$blobEndpoint/diagtestscripts/mpi-pingpong-filter.py" on nodes.'

    reasonAvSet = 'The nodes may not be in the same availability set.(CM ADDR ERROR)'
    solutionAvSet = 'Recreate the node(s) and ensure the nodes are in the same availability set.'
    
    failedReasons = {}
    for failedPair in failedTasks:
        reason = "Unknown"
        if "mpivars.sh: No such file or directory" in failedPair['Detail']:
            reason = reasonMpiNotInstalled
            failedNode = failedPair['NodeName']
            failedPair['NodeOrPair'] = failedNode
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionMpiNotInstalled, 'Nodes':set()})['Nodes'].add(failedNode)
        elif "linux/mpi/intel64/bin/pmi_proxy: No such file or directory" in failedPair['Detail']:
            reason = reasonMpiNotInstalled
            failedNode = failedPair['NodeOrPair'].split(',')[-1]
            failedPair['NodeOrPair'] = failedNode
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionMpiNotInstalled, 'Nodes':set()})['Nodes'].add(failedNode)            
        elif "Host {} not found:".format(failedPair['NodeOrPair'].split(',')[-1]) in failedPair['Detail']:
            reason = reasonHostNotFound
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionHostNotFound, 'NodePairs':[]})['NodePairs'].append(failedPair['NodeOrPair'])
        elif "check for firewalls!" in failedPair['Detail']:
            reason = reasonFireWall
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionFireWall, 'NodePairs':[]})['NodePairs'].append(failedPair['NodeOrPair'])
        elif "Assertion failed in file ../../src/mpid/ch3/channels/nemesis/netmod/tcp/socksm.c at line 2988: (it_plfd->revents & POLLERR) == 0" in failedPair['Detail']:
            reason = reasonFireWallProbably
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionFireWall, 'NodePairs':[]})['NodePairs'].append(failedPair['NodeOrPair'])
        elif "Benchmark PingPong invalid for 1 processes" in failedPair['Detail']:
            reason = reasonNodeSingleCore
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionNodeSingleCore, 'Nodes':[]})['Nodes'].append(failedPair['NodeName'])
        elif "wget" in failedPair['Detail'] and failedPair['ExitCode'] == 4 or 'mpi-pingpong-filter.py' in failedPair['Detail'] and failedPair['ExitCode'] == 8:
            reason = reasonWgetFailed
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionWgetFailed, 'Nodes':set()})['Nodes'].add(failedPair['NodeName'])
        elif "CM ADDR ERROR" in failedPair['Detail']:
            reason = reasonAvSet
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionAvSet, 'NodePairs':[]})['NodePairs'].append(failedPair['NodeOrPair'])
        else:
            if "Time limit (secs_per_sample * msg_sizes_list_len) is over;" in failedPair['Detail']:
                reason = reasonSampleTimeout
            elif failedPair['ExitCode'] == 124:
                reason = reasonPingpongTimeout
            elif failedPair['NodeOrPair'] in canceledNodePairs:
                reason = reasonTaskTimeout
            elif failedPair['Detail'].split('\n', 1)[0] == '[Message before filter]:':
                reason = reasonNoResult
            failedReasons.setdefault(reason, {'Reason':reason, 'NodePairs':[]})['NodePairs'].append(failedPair['NodeOrPair'])
        failedPair['Reason'] = reason
    if reasonMpiNotInstalled in failedReasons:
        failedReasons[reasonMpiNotInstalled]['Nodes'] = list(failedReasons[reasonMpiNotInstalled]['Nodes'])
    if reasonWgetFailed in failedReasons:
        failedReasons[reasonWgetFailed]['Nodes'] = list(failedReasons[reasonWgetFailed]['Nodes'])

    failedReasonsByNode = {}
    for failedPair in failedTasks:
        for node in failedPair['NodeOrPair'].split(','):
            failedReasonsByNode.setdefault(node, {}).setdefault(failedPair['Reason'], []).append(failedPair['NodeOrPair'])
    for value in failedReasonsByNode.values():
        nodesOrPairs = value.get(reasonMpiNotInstalled)
        if nodesOrPairs:
            value[reasonMpiNotInstalled] = list(set(nodesOrPairs))

    return (list(failedReasons.values()), failedReasonsByNode)

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

def getLargestNonoverlappingGroups(groups):
    largestGroups = []
    visitedNodes = set()
    while len(groups):
        maxLen = max([len(group) for group in groups])
        largestGroup = [group for group in groups if len(group) == maxLen][0]
        largestGroups.append(largestGroup)
        visitedNodes.update(largestGroup)
        groupsToRemove = []
        for group in groups:
            for node in group:
                if node in visitedNodes:
                    groupsToRemove.append(group)
                    break
        groups = [group for group in groups if group not in groupsToRemove]
    return largestGroups

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
    values = [int(value) for value in histogram[0]]
    binEdges = histogram[1]
    return [values, ["{0:.2f}".format(binEdges[i-1]) + '-' + "{0:.2f}".format(binEdges[i]) for i in range(1, len(binEdges))]]

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
