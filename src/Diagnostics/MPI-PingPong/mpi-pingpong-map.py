import sys, json, copy, random

def main():
    stdin = json.load(sys.stdin)
    job = stdin["Job"]
    nodesInfo = stdin["Nodes"]
    nodelist = job["TargetNodes"]

    if len(nodelist) != len(set(nodelist)):
        # Duplicate nodes
        raise Exception('Duplicate nodes')

    rdmaVmSizes = set([size.lower() for size in ["Standard_H16r", "Standard_H16mr", "Standard_A8", "Standard_A9"]])
    normalNodes = []
    rdmaNodes = []
    for nodeInfo in nodesInfo:
        node = nodeInfo["Node"]
        try:
            if nodeInfo["Metadata"]["compute"]["vmSize"].lower() in rdmaVmSizes:
                rdmaNodes.append(node)
            else:
                normalNodes.append(node)
            continue
        except:
            pass
        try:
            networksInfos = nodeInfo["NodeRegistrationInfo"]["NetworksInfo"]
            isIB = False
            for networksInfo in networksInfos:
                if networksInfo["IsIB"]:
                    isIB = True
            if isIB:
                rdmaNodes.append(node)
            else:
                normalNodes.append(node)
        except:
            raise Exception("Neither VM size from Metadata nor IsIB from NodeRegistrationInfo of node {0} could be found.".format(node))

    if len(normalNodes) != 0 and len(rdmaNodes) != 0:
        # Mixed normal nodes and rdma nodes
        raise Exception("Mixed nodes. Normal nodes: {0}. RDMA nodes: {1}.".format(','.join(normalNodes), ','.join(rdmaNodes)))

    if len(rdmaNodes) != 0:
        isRdma = True
        allNodes = set([node.lower() for node in rdmaNodes])
    else:
        isRdma = False
        allNodes = set([node.lower() for node in normalNodes])

    for node in nodelist:
        if node.lower() not in allNodes:
            raise Exception("Missing node info: {0}".format(node))

    mode = 'Tournament'.lower()
    level = 22
    if 'DiagnosticTest' in job and 'Arguments' in job['DiagnosticTest']:
        arguments = job['DiagnosticTest']['Arguments']
        if arguments:
            for argument in arguments:
                if argument['name'].lower() == 'Mode'.lower():
                    mode = argument['value'].lower()
                    continue
                if argument['name'].lower() == 'Packet size'.lower():
                    level = int(argument['value'])
                    continue

    commandClearSshKnowhosts = "rm -f ~/.ssh/known_hosts && "
    commandAddSshKnowhosts = "ssh-keyscan [pairednode] >> ~/.ssh/known_hosts 2>stderr && "
    commandParseResult = " && cat stdout | [parseResult] > raw && cat raw | tail -n +2 | awk '{print [columns]}' | tr ' ' '\n' | [parseValue] > data"
    commandGenerateOutputAsJson = (" && echo -n '{\"Latency\":' > json && cat data | head -n1 | tr -d '\n' >> json"
                                   " && echo -n ',\"Throughput\":' >> json && cat data | tail -n1 | tr -d '\n' >> json"
                                   " && echo -n ',\"Detail\":\"' >> json && cat raw | awk '{printf \"%s\\\\n\", $0}' >> json"
                                   " && echo -n '\",\"Time\":' >> json && cat timeResult | tr -d '\n' >> json && echo -n '}' >> json"
                                   " && cat json")
    commandGenerateErrorAsJson = (" || (errorcode=$?"
                                  " && echo -n '{\"Latency\":-1,\"Throughput\":-1' > json"
                                  " && echo -n ',\"Time\":' >> json && cat timeResult | tr -d '\n' >> json"
                                  " && echo -n ',\"Detail\":\"' >> json && cat stdout stderr | awk '{printf \"%s\\\\n\", $0}' >> json && echo -n '\"}' >> json"
                                  " && cat json"
                                  " && exit $errorcode)")
    commandLine = "TIMEFORMAT='%3R' && (time timeout [timeout]s bash -c 'source /opt/intel/impi/`ls /opt/intel/impi`/bin64/mpivars.sh && [mpicommand]' >stdout 2>stderr) 2>timeResult" + commandParseResult + commandGenerateOutputAsJson + commandGenerateErrorAsJson
    privateKey = "-----BEGIN RSA PRIVATE KEY-----\nMIIEowIBAAKCAQEA06bdmM5tU/InWfakBnAltIA2WEvuZ/3qFwaT4EkmgJuEITxi+3NnXn7JfW+q\n6ezBc4lx6J0EuPggDIcslbczyz65QrB2NoH7De1PiRtWNWIonQDZHTYCbnaU3f/Nzsoj62lgfkSf\nUj4Osxd0yHCuGsfCtKMDES3d55RMUdVwbrXPL8jUqA9zh4miV9eX0dh/+6pCqPD7/dnOCy/rYtXs\nwgdjKG57O6eaT3XxiuozP00E5tZ7wF0fzzXBuA2Z21Sa2U42sOeeNuOvOuKQkrIzprhHhpDik31m\nHZK47F7eF2i7j/0ImedOFdgA1ETPPKFLSGspvf1xbHgEgGz1kjFq/QIEAAEAAQKCAQABHZ2IW741\n7RKWsq6J3eIBzKRJft4J7G3tvJxW8e3nOpVNuSXEbUssu/HUOoLdVVhuHPN/1TUgf69oXTtiRIVc\nLIPNwcrGRGHwaP0JJKdY4gallLFMCB9i5FkhnJXbaiJvq+ndoqnnAPLf9GfVDqhV5Jqc8nxeDZ2T\nks037GobtfMuO5WeCyTAMzc7tDIsn0HGyV0pSa7JFHAKorUuBMNnjEM+SBL37AqwcVFkstC4YD3I\n7j4miRE3loxPmBJs5HMTV4jpAGNbNmrPzfrmP4swHNoc9LR7YKpfzVpAzb24QY82fewvOxRZH6Hz\nBVhueJZAGV62JbBeaw9eeujDp+UBAoGBAN6IanPN/wqdqQ0i5dBwPK/0Mcq6bNtQt6n8rHD/C/xL\nFuhuRhLPI6q7sYPeSZu84EjyArLMR1o0GW90Ls4JzIxjxGCBgdUHG8YdmB9jNIjR5notYQcRNxaw\nwLuc5nurPt7QaxvqO3JcaDbw9c6q9c7xNE3Wlak4xxKeiXsWyHQdAoGBAPN7hpqISKIc+8dPc5kg\nuJDDPdFcJ8py0nbysEYtY+hUaDxfw7Cm8zrNj+M9BbQR9yM6EW16P0FQ+/0XBrLMVpRkyJZ0Y3Ij\n5Qol5IxJPyWzfj6e7cd9Rkvqs2sQcBehXCbQHjfpB12yu3excQBPT0Lr5gei7yfc+D21hGWDH1xh\nAoGAM2lm1qxf4O790HggiiB0FN6g5kpdvemPFSm4GT8DYN1kRHy9mbjbb6V/ZIzliqJ/Wrr23qIN\nVgy1V6eK7LUc2c5u3zDscu/6fbH2pEHCMF32FoIHaZ+Tj510WaPtJ+MvWkDijgd2hnxM42yWDZI3\nygC16cnKt9bTPzz7XEGuPA0CgYBco2gQTcAM5igpqiIaZeezNIXFrWF6VnubRDUrTkPP9qV+KxWC\nldK/UczoMaSE4bz9Cy/sTnHYwR5PKj6jMrnSVhI3pGrd16hiVw6BDbFX/9YNr1xa5WAkrFS9bJCp\nfPxZzB9jOGdUEBfhr4KGEqbemHB6AVUq/pj4qaKJGP2KoQKBgFt7cqr8t+0zU5ko+B2pxmMprsUx\nqZ3mBATMD21AshxWxsawpqoPJJ3NTScNrjISERQ6RG3lQNv+30z79k9Fy+5FUH4pvqU4sgmtYSVH\nM4xW+aJnEhzIbE7a4an2eTc0pAxc9GexwtCFwlBouSW6kfOcMgEysoy99wQoGNgRHY/D\n-----END RSA PRIVATE KEY-----"
    taskTemplateOrigin = {
        "Id":0,
        "CommandLine":commandLine,
        "Node":None,
        "UserName":"hpc_diagnostics",
        "Password":None,
        "PrivateKey":privateKey,
        "CustomizedData":None,
    }

    rdmaOption = ""
    if isRdma:
        rdmaOption = " -env I_MPI_FABRICS=shm:dapl -env I_MPI_DAPL_PROVIDER=ofa-v2-ib0 -env I_MPI_DYNAMIC_CONNECTION=0 -env I_MPI_FALLBACK_DEVICE=0"

    headingStartId = 100000000
    headingIdGroup = list(range(headingStartId, headingStartId + len(nodelist)))

    tasks = []

    # Create task for every node to run Intel MPI Benchmark - PingPong between processors within each node.
    # Ssh keys will also be created by these tasks for mutual trust which is necessary to run the following tasks

    mpicommand = "mpirun -env I_MPI_SHM_LMT=shm" + rdmaOption + " IMB-MPI1 pingpong"
    parseResult = "tail -n29 | head -n25"
    columns = "$3,$4"
    parseValue = "sed -n '3p;$p'"
    timeout = "3"
    taskTemplate = copy.deepcopy(taskTemplateOrigin)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[mpicommand]", mpicommand)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[columns]", columns)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[parseResult]", parseResult)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[parseValue]", parseValue)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[timeout]", timeout)
    taskTemplate["CommandLine"] = commandClearSshKnowhosts + taskTemplate["CommandLine"]

    id = headingStartId
    for node in nodelist:
        task = copy.deepcopy(taskTemplate)
        task["Id"] = id
        id += 1
        task["Node"] = node
        task["CustomizedData"] = node
        tasks.append(task)

    if len(nodelist) < 2:
        print(json.dumps(tasks))
        return

    # Create tasks to run Intel MPI Benchmark - PingPong between all node pairs in selected nodes.

    if 0 < level < 30:
        msglog = " -msglog " + str(level-1) + ":" + str(level)
        linesCount = 3
        parseValue = "tail -n2"
        timeout = 10
    else:
        msglog = ""
        linesCount = 24
        parseValue = "sed -n '3p;$p'"
        timeout = 60

    mpicommand = "mpirun -hosts [dummynodes]" + rdmaOption+ " -ppn 1 IMB-MPI1" + msglog + " pingpong"
    parseResult = "tail -n" + str(linesCount+5) + " | head -n" + str(linesCount+1)
    columns = "$3,$4"

    if mode == "Jumble".lower():
        timeout *= 3
        mpicommand = "sleep [delay] && " + mpicommand

    taskTemplate = copy.deepcopy(taskTemplateOrigin)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[mpicommand]", mpicommand)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[parseResult]", parseResult)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[columns]", columns)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[parseValue]", parseValue)
    taskTemplate["CommandLine"] = commandAddSshKnowhosts + taskTemplate["CommandLine"]

    if mode == 'Tournament'.lower():
        taskgroups = getGroupsOptimal(nodelist)
        idGroups = []
        id = 1
        for taskgroup in taskgroups:
            ids = list(range(id, id + len(taskgroup)))
            idGroups.append(ids)
            id += len(ids)
        id = 1
        for i in range(0, len(taskgroups)):
            for nodepair in taskgroups[i]:
                task = copy.deepcopy(taskTemplate)
                task["Id"] = id
                id += 1
                if i == 0:
                    task["ParentIds"] = headingIdGroup
                else:
                    task["ParentIds"] = idGroups[i-1]
                nodes = ','.join(nodepair)
                task["CommandLine"] = task["CommandLine"].replace("[dummynodes]", nodes)
                task["CommandLine"] = task["CommandLine"].replace("[pairednode]", nodepair[1])
                task["CommandLine"] = task["CommandLine"].replace("[timeout]", str(timeout))
                task["Node"] = nodepair[0]
                task["CustomizedData"] = nodes
                #task["EnvironmentVariables"] = {"CCP_NODES":"2 "+" 1 ".join(nodepair)+" 1"}
                tasks.append(task)
    else:
        id = 1
        delay = 0
        nodepairs = []
        for i in range(0, len(nodelist)):
            for j in range(i+1, len(nodelist)):
                nodepairs.append([nodelist[i], nodelist[j]])
        for nodepair in nodepairs:
            task = copy.deepcopy(taskTemplate)
            task["Id"] = id
            if id == 1 or mode == 'Jumble'.lower():
                task["ParentIds"] = headingIdGroup
                delay = random.random()*len(nodelist)
            else:
                task["ParentIds"] = [id-1]
            id += 1
            nodes = ','.join(nodepair)
            task["CommandLine"] = task["CommandLine"].replace("[dummynodes]", nodes)
            task["CommandLine"] = task["CommandLine"].replace("[pairednode]", nodepair[1])
            task["CommandLine"] = task["CommandLine"].replace("[timeout]", str(timeout+delay))
            task["CommandLine"] = task["CommandLine"].replace("[delay]", str(delay))
            task["Node"] = nodepair[0]
            #task["EnvironmentVariables"] = {"CCP_NODES":"2 "+" 1 ".join(nodepair)+" 1"}
            task["CustomizedData"] = nodes
            tasks.append(task)

    print(json.dumps(tasks))

def getGroupsOptimal(nodelist):
    n = len(nodelist)
    if n <= 2:
        return [[[nodelist[0], nodelist[-1]]]]
    groups = []
    if n%2 == 1:
        for j in range(0, n):
            group = []
            for i in range(1, n//2+1):
                group.append([nodelist[(j+i)%n], nodelist[j-i]])
            groups.append(group)
    else:
        groups = getGroupsOptimal(nodelist[1:])
        for i in range(0, len(groups)):
            groups[i].append([nodelist[0], nodelist[i+1]])
    return groups

#use for test
def checkGroupsOptimal(nodelist, groups):
    n = len(nodelist)
    minN = n
    if n%2 == 0:
        minN = n-1
    if len(groups) != minN:
        return False
    allPairs = set()
    for group in groups:
        if len(group) != n//2:
            return False
        nodesInGroup = set()
        for pair in group:
            if len(pair) != 2:
                return False
            if pair[0] in nodesInGroup:
                return False
            else:
                nodesInGroup.add(pair[0])
            if pair[1] in nodesInGroup:
                return False
            else:
                nodesInGroup.add(pair[1])
            pair = tuple(pair)
            if pair in allPairs:
                return False
            else:
                allPairs.add(pair)
            pairReverse = tuple([pair[1], pair[0]])
            if pairReverse in allPairs:
                return False
            else:
                allPairs.add(pairReverse)
    if len(allPairs) != n*(n-1):
        return False
    for ping in nodelist:
        for pong in nodelist:
            if ping != pong and not tuple([ping, pong]) in allPairs:
                return False
    return True

if __name__ == '__main__':
    main()
