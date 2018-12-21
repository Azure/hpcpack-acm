#v1.0.0

import sys, json, copy, numpy, time

INTEL_MPI_URI = {
    '2019 Update 1'.lower(): {
        'Linux': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/14879/l_mpi_2019.1.144.tgz',
        'Windows': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/14881/w_mpi_p_2019.1.144.exe'
        },
    '2019'.lower(): {
        'Linux': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13584/l_mpi_2019.0.117.tgz',
        'Windows': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13586/w_mpi_p_2019.0.117.exe'
        },
    '2018 Update 4'.lower(): {
        'Linux': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13741/l_mpi_2018.4.274.tgz',
        'Windows': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13653/w_mpi_p_2018.4.274.exe'
        },
    '2018 Update 3'.lower(): {
        'Linux': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13112/l_mpi_2018.3.222.tgz',
        'Windows': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13065/w_mpi_p_2018.3.210.exe'
        },
    '2018 Update 2'.lower(): {
        'Linux': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12748/l_mpi_2018.2.199.tgz',
        'Windows': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12745/w_mpi_p_2018.2.185.exe'
        },
    '2018 Update 1'.lower(): {
        'Linux': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12414/l_mpi_2018.1.163.tgz',
        'Windows': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12443/w_mpi_p_2018.1.156.exe'
        },
    '2018'.lower(): {
        'Linux': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12120/l_mpi_2018.0.128.tgz',
        'Windows': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12114/w_mpi_p_2018.0.124.exe'
        }
    }

def main():
    diagName, diagArgs, targetNodes, windowsNodes, linuxNodes, rdmaNodes, tasks, taskResults = parseStdin()
    isMap = False if tasks and taskResults else True
    if diagName.lower() == 'MPI-Pingpong'.lower():
        arguments = {
            'Intel MPI Version': '2018 Update 4',
            'Packet size': -1,
            'Mode': 'Tournament',
            'Debug': False
        }
        parseArgs(diagArgs, arguments)
        if isMap:
            return mpiPingpongMap(arguments, windowsNodes, linuxNodes, rdmaNodes)
        else:
            return mpiPingpongReduce(arguments, targetNodes, tasks, taskResults)

def parseStdin():
    stdin = json.load(sys.stdin)

    job = stdin['Job']
    targetNodes = job['TargetNodes']
    if len(targetNodes) != len(set([node.lower() for node in targetNodes])):
        raise Exception('Duplicate name of nodes')
    diagName = '{}-{}'.format(job['DiagnosticTest']['Category'], job['DiagnosticTest']['Name'])
    diagArgs = job['DiagnosticTest']['Arguments']
    if diagArgs:
        diagArgs = [{key.lower():arg[key] for key in arg} for arg in diagArgs] # normalize the keys in arguments to lower case
        argNames = [arg['name'].lower() for arg in diagArgs]
        if len(argNames) != len(set(argNames)):
            raise Exception('Duplicate diagnostics arguments')

    nodes = stdin.get('Nodes')
    tasks = stdin.get('Tasks')
    taskResults = stdin.get('TaskResults')
    windowsNodes = linuxNodes = rdmaNodes = None
    if nodes:
        missingInfoNodes = [node['Node'] for node in nodes if not node['NodeRegistrationInfo'] or not node['Metadata']]
        if missingInfoNodes:
            raise Exception('Missing infomation for node(s): {}'.format(', '.join(missingInfoNodes)))

        rdmaVmSizes = set([size.lower() for size in ['Standard_H16r', 'Standard_H16mr', 'Standard_A8', 'Standard_A9']])
        metadataByNode = {node['Node']:json.loads(node['Metadata']) for node in nodes}

        windowsNodes = set()
        linuxNodes = set()
        unknownNodes = set()
        rdmaNodes = set()
        for node in targetNodes:
            osType = metadataByNode[node]['compute']['osType']
            if osType.lower() == 'Windows'.lower():
                windowsNodes.add(node)
            elif osType.lower() == 'Linux'.lower():
                linuxNodes.add(node)
            else:
                unknownNodes.add(node)
            vmSize = metadataByNode[node]['compute']['vmSize']
            if vmSize.lower() in rdmaVmSizes:
                rdmaNodes.add(node)
            
        if len(unknownNodes) != 0:
            raise Exception('Unknown OS type of node(s): {}'.format(', '.join(unknownNodes)))
    if tasks and taskResults:
        if len(tasks) != len(taskResults):
            raise Exception('Task count {} is not equal to task result count {}'.format(len(tasks), len(taskResults)))
        taskIdNodeNameSet = set(['{}:{}'.format(task['Id'], task['Node']) for task in tasks])
        if not all(['{}:{}'.format(task['TaskId'], task['NodeName']) in taskIdNodeNameSet for task in taskResults]):
            raise Exception('Task id and node name mismatch in "Tasks" and "TaskResults"')

    return diagName, diagArgs, targetNodes, windowsNodes, linuxNodes, rdmaNodes, tasks, taskResults

def parseArgs(diagArgsIn, diagArgsOut):
    if diagArgsIn:
        diagArgsMap = {key.lower():key for key in diagArgsOut}
        for arg in diagArgsIn:
            argName = arg['name'].lower()
            if argName in diagArgsMap:
                key = diagArgsMap[argName]
                argType = type(diagArgsOut[key])
                diagArgsOut[key] = argType(arg['value'])

def globalGetMpiDefaultInstallationLocationWindows(mpiVersion):
    return 'C:\Program Files (x86)\IntelSWTools\mpi\{}'.format(INTEL_MPI_URI[mpiVersion.lower()]['Windows'].split('_')[-1][:-len(".exe")])

def globalGetMpiDefaultInstallationLocationLinux(mpiVersion):
    return '/opt/intel/impi/{}'.format(INTEL_MPI_URI[mpiVersion.lower()]['Linux'].split('_')[-1][:-len(".tgz")])

def mpiPingpongMap(arguments, windowsNodes, linuxNodes, rdmaNodes):
    tasks = None
    mpiVersion = arguments['Intel MPI Version']
    packetSize = arguments['Packet size']
    mode = arguments['Mode'].lower()
    mpiInstallationLocationWindows = globalGetMpiDefaultInstallationLocationWindows(mpiVersion)
    mpiInstallationLocationLinux = globalGetMpiDefaultInstallationLocationLinux(mpiVersion)
    tasks = mpiPingpongCreateTasksWindows(list(windowsNodes & rdmaNodes), True, 0, mpiInstallationLocationWindows, packetSize)
    tasks += mpiPingpongCreateTasksWindows(list(windowsNodes - rdmaNodes), False, len(tasks) + 1, mpiInstallationLocationWindows, packetSize)
    tasks += mpiPingpongCreateTasksLinux(list(linuxNodes & rdmaNodes), True, len(tasks) + 1, mpiInstallationLocationLinux, mode, packetSize, None)
    tasks += mpiPingpongCreateTasksLinux(list(linuxNodes - rdmaNodes), False, len(tasks) + 1, mpiInstallationLocationLinux, mode, packetSize, None)
    print(json.dumps(tasks))

def mpiPingpongCreateTasksWindows(nodelist, isRdma, startId, mpiLocation, log):
    tasks = []
    if len(nodelist) == 0:
        return tasks

    USERNAME = 'hpc_diagnostics'
    PASSWORD = 'p@55word'

    mpiEnvFile = r'{}\intel64\bin\mpivars.bat'.format(mpiLocation)
    rdmaOption = ''
    taskLabel = '[Windows]'
    if isRdma:
        rdmaOption = '-env I_MPI_FABRICS=shm:dapl -env I_MPI_DAPL_PROVIDER=ofa-v2-ib0'
        taskLabel += '[RDMA]'

    taskTemplate = {
        'UserName': USERNAME,
        'Password': PASSWORD,
        'EnvironmentVariables': {'CCP_ISADMIN': 1}
    }

    sampleOption = '-msglog {}:{}'.format(log, log + 1) if -1 < log < 30 else '-iter 10'
    commandSetFirewall = r'netsh firewall add allowedprogram "{}\intel64\bin\mpiexec.exe" hpc_diagnostics_mpi'.format(mpiLocation) # this way would only add one row in firewall rules
#    commandSetFirewall = r'netsh advfirewall firewall add rule name="hpc_diagnostics_mpi" dir=in action=allow program="{}\intel64\bin\mpiexec.exe"'.format(mpiLocation) # this way would add multiple rows in firewall rules when it is executed multiple times
    commandRunIntra = r'\\"%USERDOMAIN%\%USERNAME%`n{}\\" | mpiexec {} IMB-MPI1 pingpong'.format(PASSWORD, rdmaOption)
    commandRunInter = r'\\"%USERDOMAIN%\%USERNAME%`n{}\\" | mpiexec {} -hosts [nodepair] -ppn 1 IMB-MPI1 -time 60 {} pingpong'.format(PASSWORD, rdmaOption, sampleOption)
    commandMeasureTime = "$stopwatch = [system.diagnostics.stopwatch]::StartNew(); [command]; if($?) {'Run time: ' + $stopwatch.Elapsed.TotalSeconds}"

    idByNode = {}

    id = startId
    for node in nodelist:
        command = commandMeasureTime.replace('[command]', commandRunIntra)
        task = copy.deepcopy(taskTemplate)
        task['Id'] = id
        task['Node'] = node
        task['CommandLine'] = '{} && "{}" && powershell "{}"'.format(commandSetFirewall, mpiEnvFile, command)
        task['CustomizedData'] = '{} {}'.format(taskLabel, node)
        task['MaximumRuntimeSeconds'] = 30
        tasks.append(task)
        idByNode[node] = id
        id += 1

    if len(nodelist) < 2:
        return tasks

    taskgroups = mpiPingpongGetGroups(nodelist)
    for taskgroup in taskgroups:
        idByNodeNext = {}
        for nodepair in taskgroup:
            nodes = ','.join(nodepair)
            command = commandMeasureTime.replace('[command]', commandRunInter).replace('[nodepair]', nodes)
            task = copy.deepcopy(taskTemplate)
            task['Id'] = id
            task['Node'] = nodepair[0]
            task['CommandLine'] = '"{}" && powershell "{}"'.format(mpiEnvFile, command)
            task['ParentIds'] = [idByNode[node] for node in nodepair if node in idByNode]
            task['CustomizedData'] = '{} {}'.format(taskLabel, nodes)
            task['MaximumRuntimeSeconds'] = 60
            tasks.append(task)
            idByNodeNext[nodepair[0]] = id
            idByNodeNext[nodepair[1]] = id
            id += 1
        idByNode = idByNodeNext
    return tasks

def mpiPingpongCreateTasksLinux(nodelist, isRdma, startId, mpiLocation, mode, log, debugCommand):
    tasks = []
    if len(nodelist) == 0:
        return tasks

    rdmaOption = ''
    taskLabel = '[Linux]'
    if isRdma:
        rdmaOption = '-env I_MPI_FABRICS=shm:dapl -env I_MPI_DAPL_PROVIDER=ofa-v2-ib0'
        taskLabel += '[RDMA]'

    scriptLocation = 'diagtestscripts'
    filterScriptDir = '/tmp/hpc_{}'.format(scriptLocation)
    filterScriptName = 'MPI-Pingpong-filter.py'
    filterScriptVersion = '#v0.12'
    filterScriptPath = '{}/{}'.format(filterScriptDir, filterScriptName)
    commandDownloadScript = 'if [ ! -f {0} ] || [ "`head -n1 {0}`" != "{1}" ]; then wget -P {2} ${{blobEndpoint}}{3}/{4} >stdout 2>stderr; fi && '.format(filterScriptPath, filterScriptVersion, filterScriptDir, scriptLocation, filterScriptName)
    commandParseResult = " && cat stdout | [parseResult] >raw && cat raw | tail -n +2 | awk '{print [columns]}' | tr ' ' '\n' | [parseValue] >data"
    commandGenerateOutput = " && cat data | head -n1 >output && cat data | tail -n1 >>output && cat timeResult >>output && cat raw >>output && cat output | python {}".format(filterScriptPath)
    commandGenerateError = ' || (errorcode=$? && echo "MPI Pingpong task failed!" >error && cat stdout stderr >>error && cat error && exit $errorcode)'
    commandLine = commandDownloadScript + "TIMEFORMAT='%3R' && (time timeout [timeout]s bash -c '[sshcommand] && source {0}/intel64/bin/mpivars.sh && [mpicommand]' >stdout 2>stderr) 2>timeResult".format(mpiLocation) + commandParseResult + commandGenerateOutput + commandGenerateError
    privateKey = "-----BEGIN RSA PRIVATE KEY-----\nMIIEowIBAAKCAQEA06bdmM5tU/InWfakBnAltIA2WEvuZ/3qFwaT4EkmgJuEITxi+3NnXn7JfW+q\n6ezBc4lx6J0EuPggDIcslbczyz65QrB2NoH7De1PiRtWNWIonQDZHTYCbnaU3f/Nzsoj62lgfkSf\nUj4Osxd0yHCuGsfCtKMDES3d55RMUdVwbrXPL8jUqA9zh4miV9eX0dh/+6pCqPD7/dnOCy/rYtXs\nwgdjKG57O6eaT3XxiuozP00E5tZ7wF0fzzXBuA2Z21Sa2U42sOeeNuOvOuKQkrIzprhHhpDik31m\nHZK47F7eF2i7j/0ImedOFdgA1ETPPKFLSGspvf1xbHgEgGz1kjFq/QIEAAEAAQKCAQABHZ2IW741\n7RKWsq6J3eIBzKRJft4J7G3tvJxW8e3nOpVNuSXEbUssu/HUOoLdVVhuHPN/1TUgf69oXTtiRIVc\nLIPNwcrGRGHwaP0JJKdY4gallLFMCB9i5FkhnJXbaiJvq+ndoqnnAPLf9GfVDqhV5Jqc8nxeDZ2T\nks037GobtfMuO5WeCyTAMzc7tDIsn0HGyV0pSa7JFHAKorUuBMNnjEM+SBL37AqwcVFkstC4YD3I\n7j4miRE3loxPmBJs5HMTV4jpAGNbNmrPzfrmP4swHNoc9LR7YKpfzVpAzb24QY82fewvOxRZH6Hz\nBVhueJZAGV62JbBeaw9eeujDp+UBAoGBAN6IanPN/wqdqQ0i5dBwPK/0Mcq6bNtQt6n8rHD/C/xL\nFuhuRhLPI6q7sYPeSZu84EjyArLMR1o0GW90Ls4JzIxjxGCBgdUHG8YdmB9jNIjR5notYQcRNxaw\nwLuc5nurPt7QaxvqO3JcaDbw9c6q9c7xNE3Wlak4xxKeiXsWyHQdAoGBAPN7hpqISKIc+8dPc5kg\nuJDDPdFcJ8py0nbysEYtY+hUaDxfw7Cm8zrNj+M9BbQR9yM6EW16P0FQ+/0XBrLMVpRkyJZ0Y3Ij\n5Qol5IxJPyWzfj6e7cd9Rkvqs2sQcBehXCbQHjfpB12yu3excQBPT0Lr5gei7yfc+D21hGWDH1xh\nAoGAM2lm1qxf4O790HggiiB0FN6g5kpdvemPFSm4GT8DYN1kRHy9mbjbb6V/ZIzliqJ/Wrr23qIN\nVgy1V6eK7LUc2c5u3zDscu/6fbH2pEHCMF32FoIHaZ+Tj510WaPtJ+MvWkDijgd2hnxM42yWDZI3\nygC16cnKt9bTPzz7XEGuPA0CgYBco2gQTcAM5igpqiIaZeezNIXFrWF6VnubRDUrTkPP9qV+KxWC\nldK/UczoMaSE4bz9Cy/sTnHYwR5PKj6jMrnSVhI3pGrd16hiVw6BDbFX/9YNr1xa5WAkrFS9bJCp\nfPxZzB9jOGdUEBfhr4KGEqbemHB6AVUq/pj4qaKJGP2KoQKBgFt7cqr8t+0zU5ko+B2pxmMprsUx\nqZ3mBATMD21AshxWxsawpqoPJJ3NTScNrjISERQ6RG3lQNv+30z79k9Fy+5FUH4pvqU4sgmtYSVH\nM4xW+aJnEhzIbE7a4an2eTc0pAxc9GexwtCFwlBouSW6kfOcMgEysoy99wQoGNgRHY/D\n-----END RSA PRIVATE KEY-----"
    taskTemplateOrigin = {
        "Id":0,
        "CommandLine":commandLine,
        "Node":None,
        "UserName":"hpc_diagnostics",
        "Password":None,
        "PrivateKey":privateKey,
        "CustomizedData":None,
        "MaximumRuntimeSeconds":1000
    }

    headingStartId = startId
    headingNode2Id = {}
    taskStartId = headingStartId + len(nodelist)

    # Create task for every node to run Intel MPI Benchmark - PingPong between processors within each node.
    # Ssh keys will also be created by these tasks for mutual trust which is necessary to run the following tasks

    sshcommand = "rm -f ~/.ssh/known_hosts" # Clear ssh knownhosts
    checkcore = 'bash -c "if [ `grep -c ^processor /proc/cpuinfo` -eq 1 ]; then exit -10; fi"' # MPI Ping Pong can not get result but return 0 if core number is less than 2, so check core number and fail mpicommand if there is no result
    mpicommand = "mpirun -env I_MPI_SHM_LMT=shm {} IMB-MPI1 pingpong && {}".format(rdmaOption, checkcore)
    parseResult = "tail -n29 | head -n25"
    columns = "$3,$4"
    parseValue = "sed -n '1p;$p'"
    timeout = "600"
    taskTemplate = copy.deepcopy(taskTemplateOrigin)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[sshcommand]", sshcommand)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[mpicommand]", mpicommand)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[columns]", columns)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[parseResult]", parseResult)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[parseValue]", parseValue)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[timeout]", timeout)
    if debugCommand:
        taskTemplate["CommandLine"] = debugCommand

    id = headingStartId
    for node in nodelist:
        task = copy.deepcopy(taskTemplate)
        task["Id"] = id
        task["Node"] = node
        task["CustomizedData"] = '{} {}'.format(taskLabel, node)
        tasks.append(task)
        headingNode2Id[node] = id
        id += 1

    if len(nodelist) < 2:
        return tasks

    # Create tasks to run Intel MPI Benchmark - PingPong between all node pairs in selected nodes.

    if -1 < log < 30:
        sampleOption = "-msglog {}:{}".format(log, log + 1)
        parseResult = "tail -n 8 | head -n 3"
        parseValue = "tail -n2"
        timeout = 20
    else:
        sampleOption = "-iter 10"
        parseResult = "tail -n 29 | head -n 25"
        parseValue = "sed -n '1p;$p'"
        timeout = 20

    sshcommand = "host [pairednode] && ssh-keyscan [pairednode] >>~/.ssh/known_hosts" # Add ssh knownhosts
    mpicommand = "mpirun -hosts [dummynodes] {} -ppn 1 IMB-MPI1 -time 60 {} pingpong".format(rdmaOption, sampleOption)
    columns = "$3,$4"

    taskTemplate = copy.deepcopy(taskTemplateOrigin)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[sshcommand]", sshcommand)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[mpicommand]", mpicommand)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[columns]", columns)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[parseResult]", parseResult)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[parseValue]", parseValue)

    if mode == "Parallel".lower():
        timeout *= 10
        taskTemplate["MaximumRuntimeSeconds"] = 300

    if mode == 'Tournament'.lower():
        taskgroups = mpiPingpongGetGroups(nodelist)
        id = taskStartId
        firstGroup = True
        for taskgroup in taskgroups:
            nodeToIdNext = {}
            for nodepair in taskgroup:
                task = copy.deepcopy(taskTemplate)
                nodes = ','.join(nodepair)
                task["Id"] = id
                task["Node"] = nodepair[0]
                task["ParentIds"] = [headingNode2Id[node] for node in nodepair] if firstGroup else [nodeToId[node] for node in nodepair if node in nodeToId]
                task["CommandLine"] = task["CommandLine"].replace("[dummynodes]", nodes)
                task["CommandLine"] = task["CommandLine"].replace("[pairednode]", nodepair[1])
                task["CommandLine"] = task["CommandLine"].replace("[timeout]", str(timeout))
                if debugCommand:
                    task["CommandLine"] = debugCommand
                task["CustomizedData"] = '{} {}'.format(taskLabel, nodes)
                tasks.append(task)
                nodeToIdNext[nodepair[0]] = id
                nodeToIdNext[nodepair[1]] = id
                id += 1
            firstGroup = False
            nodeToId = nodeToIdNext
    else:
        id = taskStartId
        nodepairs = []
        for i in range(0, len(nodelist)):
            for j in range(i+1, len(nodelist)):
                nodepairs.append([nodelist[i], nodelist[j]])
        for nodepair in nodepairs:
            task = copy.deepcopy(taskTemplate)
            task["Id"] = id
            if mode == 'Parallel'.lower():
                task["ParentIds"] = [headingNode2Id[node] for node in nodepair]
            else:
                task["ParentIds"] = [id-1]
            id += 1
            nodes = ','.join(nodepair)
            task["CommandLine"] = task["CommandLine"].replace("[dummynodes]", nodes)
            task["CommandLine"] = task["CommandLine"].replace("[pairednode]", nodepair[1])
            task["CommandLine"] = task["CommandLine"].replace("[timeout]", str(timeout))
            if debugCommand:
                task["CommandLine"] = debugCommand
            task["Node"] = nodepair[0]
            task["CustomizedData"] = '{} {}'.format(taskLabel, nodes)
            tasks.append(task)
    return tasks

def mpiPingpongGetGroups(nodelist):
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
        groups = mpiPingpongGetGroups(nodelist[1:])
        for i in range(0, len(groups)):
            groups[i].append([nodelist[0], nodelist[i+1]])
    return groups

def mpiPingpongReduce(arguments, allNodes, tasks, taskResults):
    startTime = time.time()
    nodesNumber = len(allNodes)

    # for debug
    warnings = []
    if len(tasks) != len(taskResults):
        warnings.append('Task count {} is not equal to task result count {}.'.format(len(tasks), len(taskResults)))

    defaultPacketSize = 2**22
    mpiVersion = arguments['Intel MPI Version']    
    packetSize = 2**arguments['Packet size']
    mode = arguments['Mode'].lower()
    debug = arguments['Debug']

    isDefaultSize = not 2**-1 < packetSize < 2**30

    taskStateFinished = 3
    taskStateFailed = 4
    taskStateCanceled = 5

    taskId2nodePair = {}
    tasksForStatistics = set()
    windowsTaskIds = set()
    linuxTaskIds = set()
    rdmaNodes = []
    canceledTasks = []
    canceledNodePairs = set()
    hasInterVmTask = False
    try:
        for task in tasks:
            taskId = task['Id']
            state = task['State']
            taskLabel = task['CustomizedData']
            nodeOrPair = taskLabel.split()[-1]
            if '[Windows]' in taskLabel:
                windowsTaskIds.add(taskId)
            if '[Linux]' in taskLabel:
                linuxTaskIds.add(taskId)
            if '[RDMA]' in taskLabel and ',' not in taskLabel:
                rdmaNodes.append(nodeOrPair)
            taskId2nodePair[taskId] = nodeOrPair
            if ',' in nodeOrPair:
                hasInterVmTask = True
                if state == taskStateFinished:
                    tasksForStatistics.add(taskId)
            if state == taskStateCanceled:
                canceledTasks.append(taskId)
                canceledNodePairs.add(nodeOrPair)
    except Exception as e:
        printErrorAsJson('Failed to parse tasks. ' + str(e))
        return -1

    if len(windowsTaskIds) + len(linuxTaskIds) != len(tasks):
        printErrorAsJson('Lost OS type information.')
        return -1

    if not hasInterVmTask:
        printErrorAsJson('No inter VM test was executed. Please select more nodes.')
        return 0

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
                output = taskResult['Message']
                hasResult = False
                try:
                    if taskId in linuxTaskIds:
                        message = json.loads(output)
                        hasResult = message['Latency'] > 0 and message['Throughput'] > 0
                        taskRuntime[taskId] = message['Time']
                    elif exitCode == 0:
                        message = mpiPingpongParseOutput(output, isDefaultSize)
                        hasResult = True if message else False
                except:
                    pass
                if taskId in tasksForStatistics and hasResult:
                    messages[taskId2nodePair[taskId]] = message
                if exitCode != 0 or not hasResult:
                    failedTask = {
                        'TaskId':taskId,
                        'NodeName':nodeName,
                        'NodeOrPair':nodePair,
                        'ExitCode':exitCode,
                        'Output':output
                        }
                    failedTasks.append(failedTask)
            else:
                raise Exception('No Message')
    except Exception as e:
        printErrorAsJson('Failed to parse task result. Task id: {}. {}'.format(taskId, e))
        return -1

    latencyThreshold = packetSize//50 if packetSize > 2**20 else 10000
    throughputThreshold = packetSize//1000 if 2**-1 < packetSize < 2**20 else 50
    if len(rdmaNodes) == len(allNodes):
        latencyThreshold = 2**3 if packetSize < 2**13 else packetSize/2**10
        throughputThreshold = packetSize/2**3 if 2**-1 < packetSize < 2**13 else 2**10
    if mode == 'Parallel'.lower():
        latencyThreshold = 1000000
        throughputThreshold = 0

    goodPairs = [pair for pair in messages if messages[pair]['Throughput'] > throughputThreshold]
    goodNodesGroups = mpiPingpongGetGroupsOfFullConnectedNodes(goodPairs)
    goodNodes = set([node for group in goodNodesGroups for node in group])
    if goodNodes != set([node for pair in goodPairs for node in pair.split(',')]):
        printErrorAsJson('Should not get here!')
        return -1
    if nodesNumber == 1 or nodesNumber == 2 and len(rdmaNodes) == 1:
        goodNodes = [task['Node'] for task in tasks if task['State'] == taskStateFinished]
    badNodes = [node for node in allNodes if node not in goodNodes]
    goodNodes = list(goodNodes)
    failedReasons, failedReasonsByNode = mpiPingpongGetFailedReasons(failedTasks, mpiVersion, canceledNodePairs)

    result = {
        'GoodNodesGroups':mpiPingpongGetLargestNonoverlappingGroups(goodNodesGroups),
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

        statisticsItems = ['Latency', 'Throughput']
        for item in statisticsItems:
            data = [messages[pair][item] for pair in messages]
            globalMin = numpy.amin(data)
            globalMax = numpy.amax(data)
            histogram = [list(array) for array in numpy.histogram(data, bins=histogramSize, range=(globalMin, globalMax))]

            if item == 'Latency':
                unit = 'us'
                threshold = latencyThreshold
                badPairs = [{'Pair':pair, 'Value':messages[pair][item]} for pair in messages if messages[pair][item] > latencyThreshold]
                badPairs.sort(key=lambda x:x['Value'], reverse=True)
                bestPairs = {'Pairs':[pair for pair in messages if messages[pair][item] == globalMin], 'Value':globalMin}
                worstPairs = {'Pairs':[pair for pair in messages if messages[pair][item] == globalMax], 'Value':globalMax}
                packet_size = 0 if packetSize == 2**-1 else packetSize
            else:
                unit = 'MB/s'
                threshold = throughputThreshold
                badPairs = [{'Pair':pair, 'Value':messages[pair][item]} for pair in messages if messages[pair][item] < throughputThreshold]
                badPairs.sort(key=lambda x:x['Value'])
                bestPairs = {'Pairs':[pair for pair in messages if messages[pair][item] == globalMax], 'Value':globalMax}
                worstPairs = {'Pairs':[pair for pair in messages if messages[pair][item] == globalMin], 'Value':globalMin}
                packet_size = defaultPacketSize if packetSize == 2**-1 else packetSize

            result[item]['Unit'] = unit
            result[item]['Threshold'] = threshold
            result[item]['Packet_size'] = str(packet_size) + ' Bytes'
            result[item]['Result'] = {}
            result[item]['Result']['Passed'] = len(badPairs) == 0
            result[item]['Result']['Bad_pairs'] = badPairs
            result[item]['Result']['Best_pairs'] = bestPairs
            result[item]['Result']['Worst_pairs'] = worstPairs
            result[item]['Result']['Histogram'] = mpiPingpongRenderHistogram(histogram)
            result[item]['Result']['Average'] = numpy.average(data)
            result[item]['Result']['Median'] = numpy.median(data)
            result[item]['Result']['Standard_deviation'] = numpy.std(data)
            result[item]['Result']['Variability'] = mpiPingpongGetVariability(data)
            
            result[item]['ResultByNode'] = {}
            for node in nodesInMessages:
                data = [messagesByNode[node][pair][item] for pair in messagesByNode[node]]
                histogram = [list(array) for array in numpy.histogram(data, bins=histogramSize, range=(globalMin, globalMax))]
                if item == 'Latency':
                    badPairs = [{'Pair':pair, 'Value':messagesByNode[node][pair][item]} for pair in messagesByNode[node] if messagesByNode[node][pair][item] > latencyThreshold and node in pair.split(',')]
                    badPairs.sort(key=lambda x:x['Value'], reverse=True)
                    bestPairs = {'Pairs':[pair for pair in messagesByNode[node] if messagesByNode[node][pair][item] == numpy.amin(data) and node in pair.split(',')], 'Value':numpy.amin(data)}
                    worstPairs = {'Pairs':[pair for pair in messagesByNode[node] if messagesByNode[node][pair][item] == numpy.amax(data) and node in pair.split(',')], 'Value':numpy.amax(data)}
                else:
                    badPairs = [{'Pair':pair, 'Value':messagesByNode[node][pair][item]} for pair in messagesByNode[node] if messagesByNode[node][pair][item] < throughputThreshold and node in pair.split(',')]
                    badPairs.sort(key=lambda x:x['Value'])
                    bestPairs = {'Pairs':[pair for pair in messagesByNode[node] if messagesByNode[node][pair][item] == numpy.amax(data) and node in pair.split(',')], 'Value':numpy.amax(data)}
                    worstPairs = {'Pairs':[pair for pair in messagesByNode[node] if messagesByNode[node][pair][item] == numpy.amin(data) and node in pair.split(',')], 'Value':numpy.amin(data)}
                result[item]['ResultByNode'][node] = {}
                result[item]['ResultByNode'][node]['Bad_pairs'] = badPairs
                result[item]['ResultByNode'][node]['Passed'] = len(badPairs) == 0
                result[item]['ResultByNode'][node]['Best_pairs'] = bestPairs
                result[item]['ResultByNode'][node]['Worst_pairs'] = worstPairs
                result[item]['ResultByNode'][node]['Histogram'] = mpiPingpongRenderHistogram(histogram)
                result[item]['ResultByNode'][node]['Average'] = numpy.average(data)
                result[item]['ResultByNode'][node]['Median'] = numpy.median(data)
                result[item]['ResultByNode'][node]['Standard_deviation'] = numpy.std(data)
                result[item]['ResultByNode'][node]['Variability'] = mpiPingpongGetVariability(data)

    endTime = time.time()
    
    if debug:
        taskRuntime = {
            'Max': numpy.amax(list(taskRuntime.values())),
            'Ave': numpy.average(list(taskRuntime.values())),
            'Sorted': sorted([{'runtime':taskRuntime[key], 'taskId':key, 'nodepair':taskId2nodePair[key]} for key in taskRuntime], key=lambda x:x['runtime'], reverse=True)
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

def mpiPingpongParseOutput(output, isDefaultSize):
    lines = output.splitlines()
    title = '#bytes #repetitions      t[usec]   Mbytes/sec'
    hasResult = False
    data = []
    for line in lines:
        if hasResult:
            numbers = line.split()
            if len(numbers) == 4:
                data.append(numbers)
            else:
                break
        elif title in line:
            hasResult = True
    if isDefaultSize and len(data) == 24:
        return {
            'Latency': float(data[0][2]),
            'Throughput': float(data[-1][3])
        }
    if not isDefaultSize and len(data) == 3:
        return {
            'Latency': float(data[1][2]),
            'Throughput': float(data[1][3])
        }

def mpiPingpongGetFailedReasons(failedTasks, mpiVersion, canceledNodePairs):
    reasonMpiNotInstalled = 'Intel MPI is not found.'
    solutionMpiNotInstalled = 'Please ensure Intel MPI {} is installed on the default location. Run diagnostics test MPI-Installaion on the nodes if it is not installed on them.'.format(mpiVersion)

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
    
    reasonDapl = 'MPI issue: "dapl fabric is not available and fallback fabric is not enabled"'
    solutionDapl = 'Please re-create the VM.'

    failedReasons = {}
    for failedPair in failedTasks:
        reason = "Unknown"
        nodeName = failedPair['NodeName']
        nodeOrPair = failedPair['NodeOrPair']
        output = failedPair['Output']
        exitCode = failedPair['ExitCode']
        pairedNode = nodeOrPair.split(',')[-1]
        if "mpivars.sh: No such file or directory" in output or 'The system cannot find the path specified' in output:
            reason = reasonMpiNotInstalled
            failedNode = nodeName
            failedPair['NodeOrPair'] = failedNode
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionMpiNotInstalled, 'Nodes':set()})['Nodes'].add(failedNode)
        elif "linux/mpi/intel64/bin/pmi_proxy: No such file or directory" in output or 'pmi_proxy not found on {}. Set Intel MPI environment variables'.format(pairedNode) in output:
            reason = reasonMpiNotInstalled
            failedNode = pairedNode
            failedPair['NodeOrPair'] = failedNode
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionMpiNotInstalled, 'Nodes':set()})['Nodes'].add(failedNode)            
        elif "Host {} not found:".format(pairedNode) in output:
            reason = reasonHostNotFound
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionHostNotFound, 'NodePairs':[]})['NodePairs'].append(nodeOrPair)
        elif "check for firewalls!" in output:
            reason = reasonFireWall
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionFireWall, 'NodePairs':[]})['NodePairs'].append(nodeOrPair)
        elif "Assertion failed in file ../../src/mpid/ch3/channels/nemesis/netmod/tcp/socksm.c at line 2988: (it_plfd->revents & POLLERR) == 0" in output:
            reason = reasonFireWallProbably
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionFireWall, 'NodePairs':[]})['NodePairs'].append(nodeOrPair)
        elif "Benchmark PingPong invalid for 1 processes" in output:
            reason = reasonNodeSingleCore
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionNodeSingleCore, 'Nodes':[]})['Nodes'].append(nodeName)
        elif "wget" in output and exitCode == 4 or 'mpi-pingpong-filter.py' in output and exitCode == 8:
            reason = reasonWgetFailed
            failedPair['NodeOrPair'] = nodeName
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionWgetFailed, 'Nodes':set()})['Nodes'].add(nodeName)
        elif "CM ADDR ERROR" in output:
            reason = reasonAvSet
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionAvSet, 'NodePairs':[]})['NodePairs'].append(nodeOrPair)
        elif "dapl fabric is not available and fallback fabric is not enabled" in output:
            reason = reasonDapl
            failedNode = nodeName
            if '[1] MPI startup(): dapl fabric is not available and fallback fabric is not enabled' in output:
                failedNode = pairedNode
            failedPair['NodeOrPair'] = failedNode
            failedReasons.setdefault(reason, {'Reason':reason, 'Solution':solutionDapl, 'Nodes':set()})['Nodes'].add(failedNode)            
        else:
            if "Time limit (secs_per_sample * msg_sizes_list_len) is over;" in output:
                reason = reasonSampleTimeout
            elif exitCode == 124:
                reason = reasonPingpongTimeout
            elif nodeOrPair in canceledNodePairs:
                reason = reasonTaskTimeout
            elif output.split('\n', 1)[0] == '[Message before filter]:':
                reason = reasonNoResult
            failedReasons.setdefault(reason, {'Reason':reason, 'NodePairs':[]})['NodePairs'].append(nodeOrPair)
        failedPair['Reason'] = reason
    if reasonMpiNotInstalled in failedReasons:
        failedReasons[reasonMpiNotInstalled]['Nodes'] = list(failedReasons[reasonMpiNotInstalled]['Nodes'])
    if reasonWgetFailed in failedReasons:
        failedReasons[reasonWgetFailed]['Nodes'] = list(failedReasons[reasonWgetFailed]['Nodes'])
    if reasonDapl in failedReasons:
        failedReasons[reasonDapl]['Nodes'] = list(failedReasons[reasonDapl]['Nodes'])

    failedReasonsByNode = {}
    for failedTask in failedTasks:
        nodeOrPair = failedTask['NodeOrPair']
        for node in nodeOrPair.split(','):
            failedReasonsByNode.setdefault(node, {}).setdefault(failedTask['Reason'], []).append(nodeOrPair)
            severity = failedReasonsByNode[node].setdefault('Severity', 0)
            failedReasonsByNode[node]['Severity'] = severity + 1
    for value in failedReasonsByNode.values():
        nodesOrPairs = value.get(reasonMpiNotInstalled)
        if nodesOrPairs:
            value[reasonMpiNotInstalled] = list(set(nodesOrPairs))
    for key in failedReasonsByNode.keys():
        severity = failedReasonsByNode[key].pop('Severity')
        failedReasonsByNode["{} ({})".format(key, severity)] = failedReasonsByNode.pop(key)

    return (list(failedReasons.values()), failedReasonsByNode)

def mpiPingpongGetNodeMap(pairs):
    connectedNodesOfNode = {}
    for pair in pairs:
        nodes = pair.split(',')
        connectedNodesOfNode.setdefault(nodes[0], set()).add(nodes[1])
        connectedNodesOfNode.setdefault(nodes[1], set()).add(nodes[0])
    return connectedNodesOfNode

def mpiPingpongGetLargestNonoverlappingGroups(groups):
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

def mpiPingpongGetGroupsOfFullConnectedNodes(pairs):
    connectedNodesOfNode = mpiPingpongGetNodeMap(pairs)
    nodes = list(connectedNodesOfNode.keys())
    if not nodes:
        return []
    groups = [set([nodes[0]])]
    for node in nodes[1:]:
        newGroups = []
        for group in groups:
            newGroup = group & connectedNodesOfNode[node]
            newGroup.add(node)
            mpiPingpongAddToGroups(newGroups, newGroup)
        for group in newGroups:
            mpiPingpongAddToGroups(groups, group)
    return [list(group) for group in groups]

def mpiPingpongAddToGroups(groups, new):
    for old in groups:
        if old <= new or old >= new:
            old |= new
            return
    groups.append(new)

def mpiPingpongRenderHistogram(histogram):
    values = [int(value) for value in histogram[0]]
    binEdges = histogram[1]
    return [values, ["{0:.2f}".format(binEdges[i-1]) + '-' + "{0:.2f}".format(binEdges[i]) for i in range(1, len(binEdges))]]

def mpiPingpongGetVariability(data):
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
    main()
