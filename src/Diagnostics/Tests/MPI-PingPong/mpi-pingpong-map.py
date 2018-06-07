import sys, json, copy

def main():
    job = json.load(sys.stdin)
    nodelist = job["TargetNodes"]
      
    if len(nodelist) != len(set(nodelist)):
        # Duplicate nodes
        raise Exception('Duplicate nodes')

    isRdma = False
    parallel = True
    level = 0
    if 'DiagnosticTest' in job and 'Arguments' in job['DiagnosticTest']:
        arguments = json.loads(job['DiagnosticTest']['Arguments'])
        for argument in arguments:
            if argument['name'].lower() == 'Run with RDMA'.lower():
                isRdma = argument['value'].lower() == 'YES'.lower()
                continue
            if argument['name'].lower() == 'Mode'.lower():
                parallel = argument['value'].lower() == 'Tournament'.lower()
                continue
            if argument['name'].lower() == 'Packet size'.lower():
                level = int(argument['value'])
                continue          
        
    taskTemplateOrigin = {
        "Id":0,
        "CommandLine":"source /opt/intel/impi/`ls /opt/intel/impi`/bin64/mpivars.sh && [mpicommand] 2>stderr | [parseResult] > raw && cat raw | tail -n +2 | awk '{print [columns]}' | tr ' ' '\n' | [parseValue] > data && echo -n '{\"Latency\":' > json && cat data | head -n1 | tr -d '\n' >> json && echo -n ',\"Throughput\":' >> json && cat data | tail -n1 | tr -d '\n' >> json && echo -n ',\"Detail\":\"' >> json && cat raw | awk '{printf \"%s\\\\n\", $0}' >> json && echo -n '\"}' >> json && cat json",
        "Node":None,
        "UserName":"hpc_diagnostics",
        "Password":None,
        "PrivateKey":"-----BEGIN RSA PRIVATE KEY-----\nMIIEowIBAAKCAQEA06bdmM5tU/InWfakBnAltIA2WEvuZ/3qFwaT4EkmgJuEITxi+3NnXn7JfW+q\n6ezBc4lx6J0EuPggDIcslbczyz65QrB2NoH7De1PiRtWNWIonQDZHTYCbnaU3f/Nzsoj62lgfkSf\nUj4Osxd0yHCuGsfCtKMDES3d55RMUdVwbrXPL8jUqA9zh4miV9eX0dh/+6pCqPD7/dnOCy/rYtXs\nwgdjKG57O6eaT3XxiuozP00E5tZ7wF0fzzXBuA2Z21Sa2U42sOeeNuOvOuKQkrIzprhHhpDik31m\nHZK47F7eF2i7j/0ImedOFdgA1ETPPKFLSGspvf1xbHgEgGz1kjFq/QIEAAEAAQKCAQABHZ2IW741\n7RKWsq6J3eIBzKRJft4J7G3tvJxW8e3nOpVNuSXEbUssu/HUOoLdVVhuHPN/1TUgf69oXTtiRIVc\nLIPNwcrGRGHwaP0JJKdY4gallLFMCB9i5FkhnJXbaiJvq+ndoqnnAPLf9GfVDqhV5Jqc8nxeDZ2T\nks037GobtfMuO5WeCyTAMzc7tDIsn0HGyV0pSa7JFHAKorUuBMNnjEM+SBL37AqwcVFkstC4YD3I\n7j4miRE3loxPmBJs5HMTV4jpAGNbNmrPzfrmP4swHNoc9LR7YKpfzVpAzb24QY82fewvOxRZH6Hz\nBVhueJZAGV62JbBeaw9eeujDp+UBAoGBAN6IanPN/wqdqQ0i5dBwPK/0Mcq6bNtQt6n8rHD/C/xL\nFuhuRhLPI6q7sYPeSZu84EjyArLMR1o0GW90Ls4JzIxjxGCBgdUHG8YdmB9jNIjR5notYQcRNxaw\nwLuc5nurPt7QaxvqO3JcaDbw9c6q9c7xNE3Wlak4xxKeiXsWyHQdAoGBAPN7hpqISKIc+8dPc5kg\nuJDDPdFcJ8py0nbysEYtY+hUaDxfw7Cm8zrNj+M9BbQR9yM6EW16P0FQ+/0XBrLMVpRkyJZ0Y3Ij\n5Qol5IxJPyWzfj6e7cd9Rkvqs2sQcBehXCbQHjfpB12yu3excQBPT0Lr5gei7yfc+D21hGWDH1xh\nAoGAM2lm1qxf4O790HggiiB0FN6g5kpdvemPFSm4GT8DYN1kRHy9mbjbb6V/ZIzliqJ/Wrr23qIN\nVgy1V6eK7LUc2c5u3zDscu/6fbH2pEHCMF32FoIHaZ+Tj510WaPtJ+MvWkDijgd2hnxM42yWDZI3\nygC16cnKt9bTPzz7XEGuPA0CgYBco2gQTcAM5igpqiIaZeezNIXFrWF6VnubRDUrTkPP9qV+KxWC\nldK/UczoMaSE4bz9Cy/sTnHYwR5PKj6jMrnSVhI3pGrd16hiVw6BDbFX/9YNr1xa5WAkrFS9bJCp\nfPxZzB9jOGdUEBfhr4KGEqbemHB6AVUq/pj4qaKJGP2KoQKBgFt7cqr8t+0zU5ko+B2pxmMprsUx\nqZ3mBATMD21AshxWxsawpqoPJJ3NTScNrjISERQ6RG3lQNv+30z79k9Fy+5FUH4pvqU4sgmtYSVH\nM4xW+aJnEhzIbE7a4an2eTc0pAxc9GexwtCFwlBouSW6kfOcMgEysoy99wQoGNgRHY/D\n-----END RSA PRIVATE KEY-----",
        "CustomizedData":None
    }
    
    rdmaOption = ""
    if isRdma:
        rdmaOption = " -env I_MPI_FABRICS=shm:dapl -env I_MPI_DAPL_PROVIDER=ofa-v2-ib0 -env I_MPI_DYNAMIC_CONNECTION=0 -env I_MPI_FALLBACK_DEVICE=0"

    headingStartId = 100000000
    headingIdGroup = list(range(headingStartId, headingStartId + len(nodelist)))

    tasks = []

    # Create task for every node to run Intel MPI Benchmark - PingPong between processors within each node.
    # Mutual trust will also be set by these tasks which is necessary to run the following tasks
    mpicommand = "mpirun -env I_MPI_SHM_LMT=shm" + rdmaOption + " IMB-MPI1 pingpong"
    parseResult = "tail -n29 | head -n25"
    columns = "$3,$4"
    parseValue = "sed -n '3p;$p'"
    taskTemplate = copy.deepcopy(taskTemplateOrigin)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[mpicommand]", mpicommand)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[columns]", columns)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[parseResult]", parseResult)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[parseValue]", parseValue)

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
    msglog = ""
    linesCount = 24
    parseValue = "sed -n '3p;$p'"
    if 0 < level < 30:
        msglog = " -msglog " + str(level-1) + ":" + str(level)
        linesCount = 3
        parseValue = "tail -n2"
    
    mpicommand = "mpirun -hosts [dummynodes]" + rdmaOption+ " -ppn 1 IMB-MPI1" + msglog + " pingpong"
    parseResult = "tail -n" + str(linesCount+5) + " | head -n" + str(linesCount+1)
    columns = "$3,$4"
    
    taskTemplate = copy.deepcopy(taskTemplateOrigin)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[mpicommand]", mpicommand)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[parseResult]", parseResult)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[columns]", columns)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[parseValue]", parseValue)

    if parallel and len(nodelist) > 2:
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
                task["Node"] = nodepair[0]
                task["CustomizedData"] = nodes
                task["EnvironmentVariables"] = {"CCP_NODES":"2 "+" 1 ".join(nodepair)+" 1"}
                tasks.append(task)                
    else:
        id = 1
        nodepairs = []
        for i in range(0, len(nodelist)):
            for j in range(i+1, len(nodelist)):
                nodepairs.append([nodelist[i], nodelist[j]])
        for nodepair in nodepairs:
            task = copy.deepcopy(taskTemplate)
            task["Id"] = id
            if id == 1:
                task["ParentIds"] = headingIdGroup
            else:
                task["ParentIds"] = [id-1]
            id += 1
            nodes = ','.join(nodepair)
            task["CommandLine"] = task["CommandLine"].replace("[dummynodes]", nodes)
            task["Node"] = nodepair[0]
            task["EnvironmentVariables"] = {"CCP_NODES":"2 "+" 1 ".join(nodepair)+" 1"}
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
