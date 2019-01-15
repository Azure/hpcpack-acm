#v1.3.0

import sys, json, copy, numpy, time, math, uuid
from datetime import datetime, timedelta

INTEL_PRODUCT_URL = {
    'MPI': {
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
        },
    'MKL': {
        '2019 Update 1'.lower(): {
            'Linux': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/14895/l_mkl_2019.1.144.tgz',
            'Windows': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/14893/w_mkl_2019.1.144.exe'
            },
        '2019'.lower(): {
            'Linux': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13575/l_mkl_2019.0.117.tgz',
            'Windows': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13558/w_mkl_2019.0.117.exe'
            },
        '2018 Update 4'.lower(): {
            'Linux': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13725/l_mkl_2018.4.274.tgz',
            'Windows': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13707/w_mkl_2018.4.274.exe'
            },
        '2018 Update 3'.lower(): {
            'Linux': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13005/l_mkl_2018.3.222.tgz',
            'Windows': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13037/w_mkl_2018.3.210.exe'
            },
        '2018 Update 2'.lower(): {
            'Linux': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12725/l_mkl_2018.2.199.tgz',
            'Windows': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12692/w_mkl_2018.2.185.exe'
            },
        '2018 Update 1'.lower(): {
            'Linux': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12414/l_mkl_2018.1.163.tgz',
            'Windows': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12394/w_mkl_2018.1.156.exe'
            },
        '2018'.lower(): {
            'Linux': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12070/l_mkl_2018.0.128.tgz',
            'Windows': 'http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12079/w_mkl_2018.0.124.exe'
            }
        }
    }

HPC_DIAG_USERNAME = 'hpc_diagnostics'
HPC_DIAG_PASSWORD = 'p@55word'
SSH_PRIVATE_KEY = '''-----BEGIN RSA PRIVATE KEY-----
MIIEowIBAAKCAQEA06bdmM5tU/InWfakBnAltIA2WEvuZ/3qFwaT4EkmgJuEITxi+3NnXn7JfW+q
6ezBc4lx6J0EuPggDIcslbczyz65QrB2NoH7De1PiRtWNWIonQDZHTYCbnaU3f/Nzsoj62lgfkSf
Uj4Osxd0yHCuGsfCtKMDES3d55RMUdVwbrXPL8jUqA9zh4miV9eX0dh/+6pCqPD7/dnOCy/rYtXs
wgdjKG57O6eaT3XxiuozP00E5tZ7wF0fzzXBuA2Z21Sa2U42sOeeNuOvOuKQkrIzprhHhpDik31m
HZK47F7eF2i7j/0ImedOFdgA1ETPPKFLSGspvf1xbHgEgGz1kjFq/QIEAAEAAQKCAQABHZ2IW741
7RKWsq6J3eIBzKRJft4J7G3tvJxW8e3nOpVNuSXEbUssu/HUOoLdVVhuHPN/1TUgf69oXTtiRIVc
LIPNwcrGRGHwaP0JJKdY4gallLFMCB9i5FkhnJXbaiJvq+ndoqnnAPLf9GfVDqhV5Jqc8nxeDZ2T
ks037GobtfMuO5WeCyTAMzc7tDIsn0HGyV0pSa7JFHAKorUuBMNnjEM+SBL37AqwcVFkstC4YD3I
7j4miRE3loxPmBJs5HMTV4jpAGNbNmrPzfrmP4swHNoc9LR7YKpfzVpAzb24QY82fewvOxRZH6Hz
BVhueJZAGV62JbBeaw9eeujDp+UBAoGBAN6IanPN/wqdqQ0i5dBwPK/0Mcq6bNtQt6n8rHD/C/xL
FuhuRhLPI6q7sYPeSZu84EjyArLMR1o0GW90Ls4JzIxjxGCBgdUHG8YdmB9jNIjR5notYQcRNxaw
wLuc5nurPt7QaxvqO3JcaDbw9c6q9c7xNE3Wlak4xxKeiXsWyHQdAoGBAPN7hpqISKIc+8dPc5kg
uJDDPdFcJ8py0nbysEYtY+hUaDxfw7Cm8zrNj+M9BbQR9yM6EW16P0FQ+/0XBrLMVpRkyJZ0Y3Ij
5Qol5IxJPyWzfj6e7cd9Rkvqs2sQcBehXCbQHjfpB12yu3excQBPT0Lr5gei7yfc+D21hGWDH1xh
AoGAM2lm1qxf4O790HggiiB0FN6g5kpdvemPFSm4GT8DYN1kRHy9mbjbb6V/ZIzliqJ/Wrr23qIN
Vgy1V6eK7LUc2c5u3zDscu/6fbH2pEHCMF32FoIHaZ+Tj510WaPtJ+MvWkDijgd2hnxM42yWDZI3
ygC16cnKt9bTPzz7XEGuPA0CgYBco2gQTcAM5igpqiIaZeezNIXFrWF6VnubRDUrTkPP9qV+KxWC
ldK/UczoMaSE4bz9Cy/sTnHYwR5PKj6jMrnSVhI3pGrd16hiVw6BDbFX/9YNr1xa5WAkrFS9bJCp
fPxZzB9jOGdUEBfhr4KGEqbemHB6AVUq/pj4qaKJGP2KoQKBgFt7cqr8t+0zU5ko+B2pxmMprsUx
qZ3mBATMD21AshxWxsawpqoPJJ3NTScNrjISERQ6RG3lQNv+30z79k9Fy+5FUH4pvqU4sgmtYSVH
M4xW+aJnEhzIbE7a4an2eTc0pAxc9GexwtCFwlBouSW6kfOcMgEysoy99wQoGNgRHY/D
-----END RSA PRIVATE KEY-----'''

def main():
    diagName, diagArgs, jobId, jobMaxRuntime, targetNodes, windowsNodes, linuxNodes, rdmaNodes, vmSizeByNode, nodeInfoByNode, distroByNode, tasks, taskResults = parseStdin()
    isMap = False if tasks and taskResults else True

    generatedTasks = []
    if diagName == 'MPI-Pingpong':
        arguments = {
            'Intel MPI version': '2018 Update 4',
            'Packet size': -1,
            'Mode': 'Tournament',
            'Debug': False
        }
        parseArgs(diagArgs, arguments)
        if isMap:
            generatedTasks = mpiPingpongMap(arguments, windowsNodes, linuxNodes, rdmaNodes)
        else:
            return mpiPingpongReduce(arguments, targetNodes, tasks, taskResults)

    if diagName == 'MPI-Ring':
        arguments = {
            'Intel MPI version': '2018 Update 4',
        }
        parseArgs(diagArgs, arguments)
        if isMap:
            generatedTasks = mpiRingMap(arguments, windowsNodes, linuxNodes, rdmaNodes)
        else:
            return mpiRingReduce(targetNodes, tasks, taskResults)

    if diagName == 'MPI-HPL':
        arguments = {
            'Intel MPI version': '2018 Update 4',
            'Intel MKL version': '2018 Update 4',
            'Memory limit': 50.0
        }
        parseArgs(diagArgs, arguments)
        if isMap:
            generatedTasks = mpiHplMap(arguments, jobId, windowsNodes, linuxNodes, rdmaNodes, vmSizeByNode, nodeInfoByNode)
        else:
            return mpiHplReduce(arguments, targetNodes, tasks, taskResults)

    if diagName.startswith('Prerequisite-Intel'):
        arguments = {
            'Version': '2018 Update 4',
            'Max runtime': 1800
        }
        parseArgs(diagArgs, arguments)
        product = 'MPI' if 'MPI' in diagName else 'MKL'
        if isMap:
            generatedTasks = installIntelProductMap(arguments, windowsNodes, linuxNodes, product)
        else:
            return installIntelProductReduce(arguments, targetNodes, tasks, taskResults, product)

    if diagName == 'Standalone Benchmark-Linpack':
        arguments = {
            'Intel MKL version': '2018 Update 4',
            'Size level': 10
        }
        parseArgs(diagArgs, arguments)
        if isMap:
            generatedTasks = benchmarkLinpackMap(arguments, windowsNodes, linuxNodes, vmSizeByNode)
        else:
            return benchmarkLinpackReduce(arguments, tasks, taskResults)

    if diagName == 'Standalone Benchmark-Sysbench CPU':
        if isMap:
            generatedTasks = benchmarkSysbenchCpuMap(windowsNodes, linuxNodes, vmSizeByNode, distroByNode)
        else:
            return benchmarkSysbenchCpuReduce(tasks, taskResults)

    if diagName == 'Standalone Benchmark-CIFS':
        arguments = {
            'Connect by': 'Connection string',
            'CIFS server': '',
            'Mode': 'Parallel'
        }
        parseArgs(diagArgs, arguments)
        if isMap:
            generatedTasks = benchmarkCifsMap(arguments, windowsNodes, linuxNodes, vmSizeByNode, distroByNode)
        else:
            return benchmarkCifsReduce(arguments, tasks, taskResults)

    arguments = {
        'Max runtime': jobMaxRuntime
    }
    parseArgs(diagArgs, arguments)
    job = {
        'MaximumRuntimeSeconds': arguments['Max runtime']
    }

    print(json.dumps({
        'ModifiedJob': job,
        'Tasks': generatedTasks
    }))

def parseStdin():
    stdin = json.load(sys.stdin)

    job = stdin['Job']
    jobId = job['Id']
    jobMaxRuntime = job['MaximumRuntimeSeconds']
    targetNodes = job['TargetNodes']
    if not targetNodes:
        raise Exception('No node specified for running the job')
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
    windowsNodes = linuxNodes = rdmaNodes = None
    vmSizeByNode = {}
    nodeInfoByNode = {}
    distroByNode = {}
    if nodes:
        errorNodes = [node['Node'] for node in nodes if not node['LastHeartbeatTime'] or datetime.utcnow() - datetime.strptime(node['LastHeartbeatTime'].split('.')[0], '%Y-%m-%dT%H:%M:%S') > timedelta(minutes=10)] # nodes that lost heart beat for 10 minutes
        if errorNodes:
            raise Exception('Error node(s): {}'.format(', '.join(errorNodes)))
        missingInfoNodes = [node['Node'] for node in nodes if not node['NodeRegistrationInfo'] or not node['Metadata']]
        if missingInfoNodes:
            raise Exception('Missing infomation for node(s): {}'.format(', '.join(missingInfoNodes)))
        rdmaVmSizes = set([size.lower() for size in ['Standard_H16r', 'Standard_H16mr', 'Standard_A8', 'Standard_A9']])
        metadataByNode = {node['Node']:json.loads(node['Metadata']) for node in nodes}
        nodeInfoByNode = {node['Node']:node['NodeRegistrationInfo'] for node in nodes}
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
            vmSizeByNode[node] = vmSize
            if vmSize.lower() in rdmaVmSizes:
                rdmaNodes.add(node)
            distro = 'Unknown'
            distroInfo = ' '.join([metadataByNode[node]['compute']['offer'], metadataByNode[node]['compute']['publisher'], nodeInfoByNode[node]['DistroInfo']])
            if 'Ubuntu'.lower() in distroInfo:
                distro = 'Ubuntu'
            elif 'OpenSUSE'.lower() in distroInfo:
                distro = 'OpenSUSE'
            elif 'CentOS'.lower() in distroInfo:
                distro = 'CentOS'
            elif 'Redhat'.lower() in distroInfo or 'Red Hat'.lower() in distroInfo:
                distro = 'Redhat'
            elif 'Windows'.lower() in distroInfo:
                distro = 'Windows'
            distroByNode[node] = distro
        if unknownNodes:
            raise Exception('Unknown OS type of node(s): {}'.format(', '.join(unknownNodes)))

    tasks = stdin.get('Tasks')
    taskResults = stdin.get('TaskResults')
    if tasks and taskResults:
        taskIdNodeNameInTasks = set(['{}:{}'.format(task['Id'], task['Node']) for task in tasks])
        taskIdNodeNameInTaskResults = set(['{}:{}'.format(task['TaskId'], task['NodeName']) for task in taskResults])
        difference = taskIdNodeNameInTaskResults - taskIdNodeNameInTasks
        if difference:
            raise Exception('Task id and node name mismatch in "Tasks" and "TaskResults": {}'.format(', '.join(difference)))
        nodesInJob = set(targetNodes)
        tasksOnUnexpectedNodes = ['{}:{}'.format(task['Id'], task['Node']) for task in tasks if task['Node'] not in nodesInJob]
        if tasksOnUnexpectedNodes:
            raise Exception('Unexpected nodes in tasks: {}'.format(', '.join(tasksOnUnexpectedNodes)))

    return diagName, diagArgs, jobId, jobMaxRuntime, targetNodes, windowsNodes, linuxNodes, rdmaNodes, vmSizeByNode, nodeInfoByNode, distroByNode, tasks, taskResults

def parseArgs(diagArgsIn, diagArgsOut):
    if diagArgsIn:
        diagArgsMap = {key.lower():key for key in diagArgsOut}
        for arg in diagArgsIn:
            argName = arg['name'].lower()
            if argName in diagArgsMap:
                key = diagArgsMap[argName]
                argType = type(diagArgsOut[key])
                diagArgsOut[key] = argType(arg['value'])

def globalCheckIntelProductVersion(product, version):
    if product not in INTEL_PRODUCT_URL or version.lower() not in INTEL_PRODUCT_URL[product]:
        raise Exception('Intel {} {} is not supported'.format(product, version))

def globalGetDefaultInstallationLocationWindows(product, version):
    versionNumber = INTEL_PRODUCT_URL[product][version.lower()]['Windows'].split('_')[-1][:-len(".exe")]
    if versionNumber == '2018.4.274':
        versionNumber = '2018.5.274'        
    return 'C:\Program Files (x86)\IntelSWTools\compilers_and_libraries_{}\windows\{}'.format(versionNumber, product.lower())

def globalGetDefaultInstallationLocationLinux(product, version):
    versionNumber = INTEL_PRODUCT_URL[product][version.lower()]['Linux'].split('_')[-1][:-len(".tgz")]
    if versionNumber == '2018.4.274':
        versionNumber = '2018.5.274'        
    return '/opt/intel/compilers_and_libraries_{}/linux/{}'.format(versionNumber, product.lower())
    
def globalGetIntelProductAzureBlobUrl(originalUrl):
    fileName = originalUrl.split('/')[-1]
    return 'https://hpcacm.blob.core.windows.net/intel/{}'.format(fileName)

def globalGenerateHtmlResult(title, tableHeaders, tableRows, greenRowNumber, descriptions):
    table = ''
    if tableHeaders and tableRows:
        if len(set(map(len, tableRows))) > 1:
            return None
        if len(tableRows[0]) != len(tableHeaders):
            return None
        headers = '\n'.join(['<tr>', '\n'.join('<th>{}</th>'.format(header) for header in tableHeaders), '</tr>'])
        rows = ['\n'.join(['<tr>', '\n'.join('<td>{}</td>'.format(item) for item in row), '</tr>']) for row in tableRows]
        if greenRowNumber in range(len(rows)):
            rows[greenRowNumber] = rows[greenRowNumber].replace('<tr>', '<tr bgcolor="#d8fcd4">')
        table = '\n'.join(['<table>', headers, '\n'.join(rows), '</table>'])
    body = '\n'.join(['<h2>{}</h2>'.format(title), table, '\n'.join('<p>{}</p>'.format(description) for description in descriptions) if descriptions else ''])
    return '''
<!DOCTYPE html>
<html>
<head>
<style>
table {
    font-family: arial, sans-serif;
    border-collapse: collapse;
    width: 100%;
}
td, th {
    border: 1px solid #dddddd;
    text-align: left;
    padding: 8px;
}
</style>
</head>
<body>
''' + body + '''
</body>
</html>
'''


def mpiPingpongMap(arguments, windowsNodes, linuxNodes, rdmaNodes):
    mpiVersion = arguments['Intel MPI version']
    packetSize = arguments['Packet size']
    mode = arguments['Mode'].lower()
    globalCheckIntelProductVersion('MPI', mpiVersion)
    mpiInstallationLocationWindows = globalGetDefaultInstallationLocationWindows('MPI', mpiVersion)
    mpiInstallationLocationLinux = globalGetDefaultInstallationLocationLinux('MPI', mpiVersion)
    tasks = mpiPingpongCreateTasksWindows(list(windowsNodes & rdmaNodes), True, 1, mpiInstallationLocationWindows, packetSize)
    tasks += mpiPingpongCreateTasksWindows(list(windowsNodes - rdmaNodes), False, len(tasks) + 1, mpiInstallationLocationWindows, packetSize)
    tasks += mpiPingpongCreateTasksLinux(list(linuxNodes & rdmaNodes), True, len(tasks) + 1, mpiInstallationLocationLinux, mode, packetSize)
    tasks += mpiPingpongCreateTasksLinux(list(linuxNodes - rdmaNodes), False, len(tasks) + 1, mpiInstallationLocationLinux, mode, packetSize)
    return tasks

def mpiPingpongCreateTasksWindows(nodelist, isRdma, startId, mpiLocation, log):
    tasks = []
    if len(nodelist) == 0:
        return tasks

    mpiEnvFile = r'{}\intel64\bin\mpivars.bat'.format(mpiLocation)
    rdmaOption = ''
    taskLabel = '[Windows]'
    if isRdma:
        rdmaOption = '-env I_MPI_FABRICS=shm:dapl -env I_MPI_DAPL_PROVIDER=ofa-v2-ib0'
        taskLabel += '[RDMA]'

    taskTemplate = {
        'UserName': HPC_DIAG_USERNAME,
        'Password': HPC_DIAG_PASSWORD,
        'EnvironmentVariables': {'CCP_ISADMIN': 1}
    }

    sampleOption = '-msglog {}:{}'.format(log, log + 1) if -1 < log < 30 else '-iter 10'
    commandSetFirewall = r'netsh firewall add allowedprogram "{}\intel64\bin\mpiexec.exe" hpc_diagnostics_mpi'.format(mpiLocation) # this way would only add one row in firewall rules
    # commandSetFirewall = r'netsh advfirewall firewall add rule name="hpc_diagnostics_mpi" dir=in action=allow program="{}\intel64\bin\mpiexec.exe"'.format(mpiLocation) # this way would add multiple rows in firewall rules when it is executed multiple times
    commandRunIntra = r'\\"%USERDOMAIN%\%USERNAME%`n{}\\" | mpiexec {} -n 2 IMB-MPI1 pingpong'.format(HPC_DIAG_PASSWORD, rdmaOption)
    commandRunInter = r'\\"%USERDOMAIN%\%USERNAME%`n{}\\" | mpiexec {} -hosts [nodepair] -ppn 1 IMB-MPI1 -time 60 {} pingpong'.format(HPC_DIAG_PASSWORD, rdmaOption, sampleOption)
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
            command = commandMeasureTime.replace('[command]', commandRunInter)
            task = copy.deepcopy(taskTemplate)
            task['Id'] = id
            task['Node'] = nodepair[0]
            task['CommandLine'] = '"{}" && powershell "{}"'.format(mpiEnvFile, command).replace('[nodepair]', nodes)
            task['ParentIds'] = [idByNode[node] for node in nodepair if node in idByNode]
            task['CustomizedData'] = '{} {}'.format(taskLabel, nodes)
            task['MaximumRuntimeSeconds'] = 60
            tasks.append(task)
            idByNodeNext[nodepair[0]] = id
            idByNodeNext[nodepair[1]] = id
            id += 1
        idByNode = idByNodeNext
    return tasks

def mpiPingpongCreateTasksLinux(nodelist, isRdma, startId, mpiLocation, mode, log):
    tasks = []
    if len(nodelist) == 0:
        return tasks

    taskTemplate = {
        'UserName':HPC_DIAG_USERNAME,
        'Password':None,
        'PrivateKey':SSH_PRIVATE_KEY,
    }

    rdmaOption = ''
    taskLabel = '[Linux]'
    interVmTimeout = 20
    if isRdma:
        rdmaOption = '-env I_MPI_FABRICS=shm:dapl -env I_MPI_DAPL_PROVIDER=ofa-v2-ib0'
        taskLabel += '[RDMA]'
        interVmTimeout = 2

    sampleOption = '-msglog {}:{}'.format(log, log + 1) if -1 < log < 30 else '-iter 10'

    commandAddHost = 'host [pairednode] && ssh-keyscan [pairednode] >>~/.ssh/known_hosts'
    commandClearHosts = 'rm -f ~/.ssh/known_hosts'
    commandRunIntra = 'source {}/intel64/bin/mpivars.sh && mpirun -env I_MPI_SHM_LMT=shm {} -n 2 IMB-MPI1 pingpong'.format(mpiLocation, rdmaOption)    
    commandRunInter = 'source {}/intel64/bin/mpivars.sh && mpirun -hosts [nodepair] {} -ppn 1 IMB-MPI1 -time 60 {} pingpong'.format(mpiLocation, rdmaOption, sampleOption)
    commandMeasureTime = "TIMEFORMAT='Run time: %3R' && time timeout [timeout]s bash -c '[command]'"

    idByNode = {}

    id = startId
    for node in sorted(nodelist):
        command = commandMeasureTime.replace('[timeout]', '600').replace('[command]', commandRunIntra)
        task = copy.deepcopy(taskTemplate)
        task['Id'] = id
        task['Node'] = node
        task['CommandLine'] = '{} && {}'.format(commandClearHosts, command)
        task['CustomizedData'] = '{} {}'.format(taskLabel, node)
        task['MaximumRuntimeSeconds'] = 650
        tasks.append(task)
        idByNode[node] = id
        id += 1

    if len(nodelist) < 2:
        return tasks

    if mode == 'Tournament'.lower():
        taskgroups = mpiPingpongGetGroups(nodelist)
        for taskgroup in taskgroups:
            idByNodeNext = {}
            for nodepair in taskgroup:
                nodes = ','.join(nodepair)
                command = commandMeasureTime.replace('[timeout]', str(interVmTimeout)).replace('[command]', commandRunInter)
                task = copy.deepcopy(taskTemplate)
                task['Id'] = id
                task['Node'] = nodepair[0]
                task['ParentIds'] = [idByNode[node] for node in nodepair if node in idByNode]
                task['CommandLine'] = '{} && {}'.format(commandAddHost, command).replace('[pairednode]', nodepair[1]).replace('[nodepair]', nodes)
                task['CustomizedData'] = '{} {}'.format(taskLabel, nodes)
                task['MaximumRuntimeSeconds'] = 30
                tasks.append(task)
                idByNodeNext[nodepair[0]] = id
                idByNodeNext[nodepair[1]] = id
                id += 1
            idByNode = idByNodeNext
    else:
        nodepairs = []
        for i in range(0, len(nodelist)):
            for j in range(i+1, len(nodelist)):
                nodepairs.append([nodelist[i], nodelist[j]])
        for nodepair in nodepairs:
            nodes = ','.join(nodepair)
            task = copy.deepcopy(taskTemplate)
            task['Id'] = id
            task['Node'] = nodepair[0]
            task['CustomizedData'] = '{} {}'.format(taskLabel, nodes)
            if mode == 'Parallel'.lower():
                command = commandMeasureTime.replace('[timeout]', '200').replace('[command]', commandRunInter)
                task['ParentIds'] = [idByNode[node] for node in nodepair]
                task['MaximumRuntimeSeconds'] = 230
            else:
                command = commandMeasureTime.replace('[timeout]', str(interVmTimeout)).replace('[command]', commandRunInter)
                task['ParentIds'] = [id-1]
                task['MaximumRuntimeSeconds'] = 30
            task['CommandLine'] = '{} && {}'.format(commandAddHost, command).replace('[nodepair]', nodes).replace('[pairednode]', nodepair[1])
            tasks.append(task)
            id += 1
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

    mpiVersion = arguments['Intel MPI version']    
    packetSize = 2**arguments['Packet size']
    mode = arguments['Mode'].lower()
    debug = arguments['Debug']

    TASK_STATE_FINISHED = 3
    TASK_STATE_CANCELED = 5

    defaultPacketSize = 2**22
    isDefaultSize = not 2**-1 < packetSize < 2**30

    taskId2nodePair = {}
    tasksForStatistics = set()
    windowsTaskIds = set()
    linuxTaskIds = set()
    rdmaNodes = []
    canceledTasks = []
    canceledNodePairs = set()
    hasInterVmTask = False
    messages = {}
    failedTasks = []
    taskRuntime = {}
    try:
        taskId = None
        resultByTaskId = {taskResult['TaskId']: taskResult for taskResult in taskResults}
        for task in tasks:
            taskId = task['Id']
            nodeName = task['Node']
            state = task['State']
            taskLabel = task['CustomizedData']
            nodeOrPair = taskLabel.split()[-1]
            taskId2nodePair[taskId] = nodeOrPair
            if '[Windows]' in taskLabel:
                windowsTaskIds.add(taskId)
            if '[Linux]' in taskLabel:
                linuxTaskIds.add(taskId)
            if '[RDMA]' in taskLabel and ',' not in taskLabel:
                rdmaNodes.append(nodeOrPair)
            isInterVmTask = False
            if ',' in nodeOrPair:
                isInterVmTask = True
                hasInterVmTask = True
            if state == TASK_STATE_CANCELED:
                canceledTasks.append(taskId)
                canceledNodePairs.add(nodeOrPair)
            exitCode = output = message = None
            taskResult = resultByTaskId.get(taskId)
            if taskResult:
                exitCode = taskResult['ExitCode']
                output = taskResult.get('Message')
                if exitCode == 0:
                    message = mpiPingpongParseOutput(output, isDefaultSize)
                    if message:
                        taskRuntime[taskId] = message['Time']
                        if isInterVmTask and state == TASK_STATE_FINISHED:
                            messages[nodeOrPair] = message
            if not message:
                failedTasks.append({
                    'TaskId':taskId,
                    'NodeName':nodeName,
                    'NodeOrPair':nodeOrPair,
                    'ExitCode':exitCode,
                    'Output':output
                    })
    except Exception as e:
        printErrorAsJson('Failed to parse task {}. {}'.format(taskId, e))
        return -1

    if len(windowsTaskIds) + len(linuxTaskIds) != len(tasks):
        printErrorAsJson('Lost OS type information.')
        return -1

    if not hasInterVmTask:
        printErrorAsJson('No inter VM test was executed. Please select more nodes.')
        return 0

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
        histogramSize = 8

        statisticsItems = ['Latency', 'Throughput']
        for item in statisticsItems:
            data = [messages[pair][item] for pair in messages]
            globalMin = numpy.amin(data)
            globalMax = numpy.amax(data)
            factor = math.ceil(globalMax / histogramSize)
            histogramBins = [factor * n for n in range(histogramSize + 1)]
            histogram = [list(array) for array in numpy.histogram(data, bins=histogramBins)]

            if item == 'Latency':
                unit = 'us'
                threshold = latencyThreshold
                badPairs = [{'Pair':pair, 'Value':messages[pair][item]} for pair in messages if messages[pair][item] > latencyThreshold]
                badPairs.sort(key=lambda x:x['Value'], reverse=True)
                bestPairs = {'Pairs':[pair for pair in messages if messages[pair][item] == globalMin], 'Value':globalMin}
                worstPairs = {'Pairs':[pair for pair in messages if messages[pair][item] == globalMax], 'Value':globalMax}
                packet_size = 0 if isDefaultSize else packetSize
            else:
                unit = 'MB/s'
                threshold = throughputThreshold
                badPairs = [{'Pair':pair, 'Value':messages[pair][item]} for pair in messages if messages[pair][item] < throughputThreshold]
                badPairs.sort(key=lambda x:x['Value'])
                bestPairs = {'Pairs':[pair for pair in messages if messages[pair][item] == globalMax], 'Value':globalMax}
                worstPairs = {'Pairs':[pair for pair in messages if messages[pair][item] == globalMin], 'Value':globalMin}
                packet_size = defaultPacketSize if isDefaultSize else packetSize

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
                histogram = [list(array) for array in numpy.histogram(data, bins=histogramBins)]
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
            'TaskRuntime':taskRuntime,
            }
        
    print(json.dumps(result))
    return 0

def mpiPingpongParseOutput(output, isDefaultSize):
    try:
        lines = [line for line in output.splitlines() if line]
        latency = throughput = time = None
        if lines[-1].startswith('Run time'):
            return {
                'Latency': float(lines[-26 if isDefaultSize else -4].split()[2]),
                'Throughput': float(lines[-3 if isDefaultSize else -4].split()[3]),
                'Time': float(lines[-1].split()[-1])
            }
        else:
            return None
    except:
        return None

def mpiPingpongGetFailedReasons(failedTasks, mpiVersion, canceledNodePairs):
    reasonMpiNotInstalled = 'Intel MPI is not found.'
    solutionMpiNotInstalled = 'Please ensure Intel MPI {} is installed on the default location {} or {}. Run "Prerequisite-Intel MPI Installation" on the nodes if it is not installed on them.'.format(mpiVersion, globalGetDefaultInstallationLocationLinux('MPI', mpiVersion), globalGetDefaultInstallationLocationWindows('MPI', mpiVersion))

    reasonHostNotFound = 'The node pair may be not in the same network or there is issue when parsing host name.'
    solutionHostNotFound = 'Check DNS server and ensure the node pair could translate the host name to address of each other.'

    reasonFireWall = 'The connection was blocked by firewall.'
    reasonFireWallProbably = 'The connection may be blocked by firewall.'
    solutionFireWall = 'Check and configure the firewall properly.'

    reasonNodeSingleCore = 'MPI PingPong can not run with only 1 process.'
    solutionNodeSingleCore = 'Ignore this failure.'

    reasonTaskTimeout = 'Task timeout.'
    reasonPingpongTimeout = 'Pingpong test timeout.'
    reasonSampleTimeout = 'Pingpong test sample timeout.'
    reasonNoResult = 'No result.'

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
            elif exitcode is None or output is None:
                reason = reasonNoResult
            failedReasons.setdefault(reason, {'Reason':reason, 'NodePairs':[]})['NodePairs'].append(nodeOrPair)
        failedPair['Reason'] = reason
    if reasonMpiNotInstalled in failedReasons:
        failedReasons[reasonMpiNotInstalled]['Nodes'] = list(failedReasons[reasonMpiNotInstalled]['Nodes'])
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
        largestGroups.append(sorted(largestGroup))
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

def mpiRingMap(arguments, windowsNodes, linuxNodes, rdmaNodes):
    mpiVersion = arguments['Intel MPI version']
    globalCheckIntelProductVersion('MPI', mpiVersion)
    mpiInstallationLocationWindows = globalGetDefaultInstallationLocationWindows('MPI', mpiVersion)
    mpiInstallationLocationLinux = globalGetDefaultInstallationLocationLinux('MPI', mpiVersion)

    if windowsNodes and linuxNodes:
        raise Exception('Can not run this test among Linux nodes and Windows nodes')

    nodeset = windowsNodes if windowsNodes else linuxNodes
    if 0 < len(rdmaNodes) < len(nodeset):
        raise Exception('Can not run this test among RDMA nodes and non-RDMA nodes')
    
    taskLabel = '{}{}'.format('[Windows]' if windowsNodes else '[Linux]', '[RDMA]' if rdmaNodes else '')
    rdmaOption = '-env I_MPI_FABRICS=shm:dapl -env I_MPI_DAPL_PROVIDER=ofa-v2-ib0' if rdmaNodes else ''
    nodes = ','.join(nodeset)
    nodesCount = len(nodeset)

    taskTemplateWindows = {
        'UserName': HPC_DIAG_USERNAME,
        'Password': HPC_DIAG_PASSWORD,
        'EnvironmentVariables': {'CCP_ISADMIN': 1}
    }

    taskTemplateLinux = {
        'UserName':HPC_DIAG_USERNAME,
        'Password':None,
        'PrivateKey':SSH_PRIVATE_KEY,
    }

    taskTemplate = taskTemplateWindows if windowsNodes else taskTemplateLinux

    mpiEnvFile = r'{}\intel64\bin\mpivars.bat'.format(mpiInstallationLocationWindows)
    commandSetFirewall = r'netsh firewall add allowedprogram "{}\intel64\bin\mpiexec.exe" hpc_diagnostics_mpi'.format(mpiInstallationLocationWindows)
    commandRunIntra = r'\\"%USERDOMAIN%\%USERNAME%`n{}\\" | mpiexec {} -n %NUMBER_OF_PROCESSORS% IMB-MPI1 sendrecv'.format(HPC_DIAG_PASSWORD, rdmaOption)
    commandRunInter = r'\\"%USERDOMAIN%\%USERNAME%`n{}\\" | mpiexec {} -hosts {} -ppn 1 IMB-MPI1 -npmin {} sendrecv'.format(HPC_DIAG_PASSWORD, rdmaOption, nodes, nodesCount)
    commandMeasureTime = "$stopwatch = [system.diagnostics.stopwatch]::StartNew(); [command]; if($?) {'Run time: ' + $stopwatch.Elapsed.TotalSeconds}"
    commandRunWindows = '{} && "{}" && powershell "{}"'.format(commandSetFirewall, mpiEnvFile, commandMeasureTime)
    commandRunIntraWindows = commandRunWindows.replace('[command]', commandRunIntra)
    commandRunInterWindows = commandRunWindows.replace('[command]', commandRunInter)

    commandSource = 'source {0}/intel64/bin/mpivars.sh'.format(mpiInstallationLocationLinux)
    commandRunIntra = '{} && time mpirun -env I_MPI_SHM_LMT=shm {} -n `grep -c ^processor /proc/cpuinfo` IMB-MPI1 sendrecv'.format(commandSource, rdmaOption)
    commandRunInter = '{} && time mpirun -hosts {} {} -ppn 1 IMB-MPI1 -npmin {} sendrecv 2>/dev/null'.format(commandSource, nodes, rdmaOption, nodesCount)
    commandMeasureTime = "TIMEFORMAT='Run time: %3R' && time timeout 30s bash -c '[command]'"
    commandRunIntraLinux = commandMeasureTime.replace('[command]', commandRunIntra)
    commandRunInterLinux = commandMeasureTime.replace('[command]', commandRunInter)

    taskId = 1
    tasks = []
    for node in sorted(nodeset):
        task = copy.deepcopy(taskTemplate)
        task['Id'] = taskId
        taskId += 1
        task['Node'] = node
        task['CommandLine'] = commandRunIntraWindows if windowsNodes else commandRunIntraLinux
        task['CustomizedData'] = '{} {}'.format(taskLabel, node)
        task['MaximumRuntimeSeconds'] = 60
        tasks.append(task)

    masterNode = next(iter(nodeset)) # pick a node
    if nodesCount > 1:
        task = copy.deepcopy(taskTemplate)
        task['Id'] = taskId
        taskId += 1
        task['Node'] = masterNode
        task['ParentIds'] = list(range(1, nodesCount + 1))
        task['CommandLine'] = commandRunInterWindows if windowsNodes else commandRunInterLinux
        task['CustomizedData'] = '{} {}'.format(taskLabel, nodes)
        if linuxNodes:
            task['EnvironmentVariables'] = {'CCP_NODES': '{} {}'.format(nodesCount, ' '.join(['{} 1'.format(node) for node in nodeset]))}
        task['MaximumRuntimeSeconds'] = 120
        tasks.append(task)
    
    return tasks

def mpiRingReduce(nodes, tasks, taskResults):
    output = None
    try:
        for taskResult in taskResults:
            taskId = taskResult['TaskId']
            if taskId == 1 and len(nodes) == 1 or taskId == len(nodes) + 1:
                if taskResult['ExitCode'] == 0:
                    output = taskResult['Message']
                    break
    except Exception as e:
        printErrorAsJson('Failed to parse task result. ' + str(e))
        return -1
    
    rows = []
    if output:
        data = output.splitlines()
        title = '#bytes #repetitions  t_min[usec]  t_max[usec]  t_avg[usec]   Mbytes/sec'
        hasResult = False
        for line in data:
            if hasResult:
                row = line.split()
                if len(row) != len(title.split()):
                    break
                rows.append({
                    'Message_Size':{
                        'value':row[0],
                        'unit':'Bytes'
                    },
                    'Latency':{
                        'value':row[-2],
                        'unit':'usec'
                    },
                    'Throughput':{
                        'value':row[-1],
                        'unit':'Mbytes/sec'
                    }
                })
            elif title in line:
                hasResult = True
    
    result = {
        'Description': 'This data shows the {} communication performance as latencies and throughputs for various MPI message sizes, which are extracted from the result of running Intel MPI-1 Benchmark Sendrecv, more refer to https://software.intel.com/en-us/imb-user-guide-sendrecv'.format('intra-VM' if len(nodes) == 1 else 'inter-VM'),
        'Result': rows
        }
    
    print(json.dumps(result))
    return 0

def mpiHplMap(arguments, jobId, windowsNodes, linuxNodes, rdmaNodes, vmSizeByNode, nodeInfoByNode):
    if windowsNodes:
        raise Exception('The test is not supported on Windows nodes currently')

    if 0 < len(rdmaNodes) < len(linuxNodes):
        raise Exception('Can not run this test among RDMA nodes and non-RDMA nodes')

    mpiVersion = arguments['Intel MPI version']
    mklVersion = arguments['Intel MKL version']
    memoryPercentage = arguments['Memory limit']
    globalCheckIntelProductVersion('MPI', mpiVersion)
    globalCheckIntelProductVersion('MKL', mklVersion)
    mpiInstallationLocationLinux = globalGetDefaultInstallationLocationLinux('MPI', mpiVersion)
    mklInstallationLocationLinux = globalGetDefaultInstallationLocationLinux('MKL', mklVersion)

    minCoreCount = min([nodeInfoByNode[node]['CoreCount'] for node in linuxNodes])
    if not minCoreCount:
        raise Exception('Node core count info is not correct')

    minMemoryMb = min([nodeInfoByNode[node]['MemoryMegabytes'] for node in linuxNodes])
    if not minMemoryMb:
        raise Exception('Node memory info is not correct')

    rdmaOption = '-env I_MPI_FABRICS=shm:dapl -env I_MPI_DAPL_PROVIDER=ofa-v2-ib0 -env I_MPI_DYNAMIC_CONNECTION=0 -env I_MPI_FALLBACK_DEVICE=0' if rdmaNodes else ''

    taskTemplateLinux = {
        'UserName': HPC_DIAG_USERNAME,
        'Password': None,
        'PrivateKey': SSH_PRIVATE_KEY
    }
    
    taskId = 1
    tasks = []
    masterNode = next(iter(linuxNodes)) # pick a node
    nodes = ','.join(linuxNodes)
    hplDateFile = '''HPLinpack benchmark input file
Innovative Computing Laboratory, University of Tennessee
HPL.out      output file name (if any)
6            device out (6=stdout,7=stderr,file)
1            # of problems sizes (N)
1000         Ns
1            # of NBs
192 256      NBs
0            PMAP process mapping (0=Row-,1=Column-major)
1            # of process grids (P x Q)
1 2          Ps
1 2          Qs
16.0         threshold
1            # of panel fact
2 1 0        PFACTs (0=left, 1=Crout, 2=Right)
1            # of recursive stopping criterium
2            NBMINs (>= 1)
1            # of panels in recursion
2            NDIVs
1            # of recursive panel fact.
1 0 2        RFACTs (0=left, 1=Crout, 2=Right)
1            # of broadcast
0            BCASTs (0=1rg,1=1rM,2=2rg,3=2rM,4=Lng,5=LnM)
1            # of lookahead depth
0            DEPTHs (>=0)
0            SWAP (0=bin-exch,1=long,2=mix)
1            swapping threshold
1            L1 in (0=transposed,1=no-transposed) form
1            U  in (0=transposed,1=no-transposed) form
0            Equilibration (0=no,1=yes)
8            memory alignment in double (> 0)'''
    nodesCount = len(linuxNodes)
    threadsCount = minCoreCount * nodesCount
    commandCreateHplDat = 'echo "{}" >HPL.dat'.format(hplDateFile)
    commandSourceMpiEnv = 'source {}/intel64/bin/mpivars.sh'.format(mpiInstallationLocationLinux)
    commandRunHpl = 'mpirun -env I_MPI_SHM_LMT=shm -hosts {} {} -np {} -ppn {} {}/benchmarks/mp_linpack/xhpl_intel64_dynamic -n [N] -b [NB] -p [P] -q [Q]'.format(nodes, rdmaOption, threadsCount, minCoreCount, mklInstallationLocationLinux)
    
    # Create task to run Intel HPL locally to ensure every node is ready
    # Ssh keys will also be created by these tasks for mutual trust which is necessary to run the following tasks
    commandCheckCpu = "lscpu | egrep '^CPU\(s\)|^Model name|^Thread\(s\) per core'"
    commandCheckHpl = '{}/benchmarks/mp_linpack/xhpl_intel64_dynamic >/dev/null && echo MKL test succeed.'.format(mklInstallationLocationLinux)
    commandCheckMpi = 'mpirun -env I_MPI_SHM_LMT=shm -n 1 IMB-MPI1 sendrecv >/dev/null && echo MPI test succeed.'
    taskTemplate = copy.deepcopy(taskTemplateLinux)
    taskTemplate['CommandLine'] = '{}; {}; {}; {} && {}'.format(commandCheckCpu, commandCreateHplDat, commandSourceMpiEnv, commandCheckHpl, commandCheckMpi)
    taskTemplate['MaximumRuntimeSeconds'] = 60
    for node in sorted(linuxNodes):
        task = copy.deepcopy(taskTemplate)
        task['Id'] = taskId
        taskId += 1
        task['Node'] = node
        task['CustomizedData'] = vmSizeByNode[node]
        tasks.append(task)

    hplWorkingDir = '/tmp/hpc_diag_linpack_hpl'
    flagDir = '{}/{}.{}'.format(hplWorkingDir, jobId, uuid.uuid4())

    # Create task to run Intel HPL with Intel MPI among the nodes to ensure cluster integrity
    N = 10000
    NB = 192
    P = 1
    Q = threadsCount
    tmpOutputFile = '{}.failed'.format(flagDir)
    commandCreateHplWorkingDir = 'mkdir -p {}'.format(hplWorkingDir)
    commandClearFlagDir = 'rm -f {}'.format(flagDir)
    commandRun = '{} >{} 2>&1'.format(commandRunHpl.replace('[N]', str(N)).replace('[P]', str(P)).replace('[Q]', str(Q)).replace('[NB]', str(NB)), tmpOutputFile)
    commadnClearTmp = 'rm -f {}'.format(tmpOutputFile)
    commandCreateFlagDir = 'mkdir -p {}'.format(flagDir)
    commandSuccess = 'echo Cluster is ready.'
    commandFailure = 'echo Cluster is not ready. && cat {} && exit -1'.format(tmpOutputFile)
    task = copy.deepcopy(taskTemplateLinux)
    task['Id'] = taskId
    taskId += 1
    task['CommandLine'] = '{} && {} && {} && {} && {} && {} && {} && {} || ({})'.format(commandCreateHplWorkingDir,
                                                                                        commandClearFlagDir,
                                                                                        commandCreateHplDat,
                                                                                        commandSourceMpiEnv,
                                                                                        commandRun,
                                                                                        commadnClearTmp,
                                                                                        commandCreateFlagDir,
                                                                                        commandSuccess,
                                                                                        commandFailure)
    task['ParentIds'] = list(range(1, len(linuxNodes)+1))
    task['Node'] = masterNode
    task['CustomizedData'] = vmSizeByNode[masterNode]
    task['EnvironmentVariables'] = {'CCP_NODES': '{} {}'.format(nodesCount, ' '.join('{} 1'.format(node) for node in linuxNodes))} 
    task['MaximumRuntimeSeconds'] = 60
    tasks.append(task)

    # Create HPL tunning tasks
    PQs = [(p, threadsCount//p) for p in range(1, int(math.sqrt(threadsCount)) + 1) if p * (threadsCount//p) == threadsCount]
    NBs = [192, 256]
    memoryRange = []
    while memoryPercentage > 70:
        memoryRange.append(memoryPercentage)
        memoryPercentage -= 1
    while memoryPercentage > 50:
        memoryRange.append(memoryPercentage)
        memoryPercentage -= 2
    while memoryPercentage > 0:
        memoryRange.append(memoryPercentage)
        memoryPercentage -= 5
    Ns = [int(math.sqrt(minMemoryMb * 1024 * 1024 * nodesCount / 8 * percent / 100)) for percent in memoryRange]
    commandCheckFlag = '[ -d "{}" ]'.format(flagDir)
    for P, Q in PQs:
        for NB in NBs:
            outputPrefix = '{}/{}x{}.{}'.format(flagDir, P, Q, NB)
            outputResult = '{}.result'.format(outputPrefix)
            for N in Ns:
                # N = N // NB * NB # this would decrease perf instead of increases
                tempOutputFile = '{}.{}'.format(outputPrefix, N)
                commandCheckFinish = '[ ! -f "{}" ]'.format(outputResult)
                commandRun = '{} >{} 2>&1'.format(commandRunHpl.replace('[N]', str(N)).replace('[P]', str(P)).replace('[Q]', str(Q)).replace('[NB]', str(NB)), tempOutputFile)
                commandSuccess = 'mv {} {} && cat {} | tail -n20'.format(tempOutputFile, outputResult, outputResult)
                commandFailure = 'echo Test skiped. N={} NB={} P={} Q={}'.format(N, NB, P, Q)
                task = copy.deepcopy(taskTemplateLinux)
                task['Id'] = taskId
                taskId += 1
                task['CommandLine'] = '{} && {} && {} && {} && {} && {} || {}'.format(commandCheckFlag,
                                                                                      commandCheckFinish,
                                                                                      commandCreateHplDat,
                                                                                      commandSourceMpiEnv,
                                                                                      commandRun,
                                                                                      commandSuccess,
                                                                                      commandFailure)
                task['ParentIds'] = [task['Id'] - 1]
                task['Node'] = masterNode
                task['CustomizedData'] = vmSizeByNode[masterNode]
                task['MaximumRuntimeSeconds'] = N / 10
                tasks.append(task)
    
    # Create result collecting task
    task = copy.deepcopy(taskTemplateLinux)
    task['Id'] = taskId
    taskId += 1
    task['CommandLine'] = '{} && for file in $(ls {}/*.result); do cat $file | tail -n17 | head -n1; done'.format(commandCheckFlag, flagDir)
    task['ParentIds'] = [task['Id'] - 1]
    task['Node'] = masterNode
    task['CustomizedData'] = vmSizeByNode[masterNode]
    task['MaximumRuntimeSeconds'] = 60
    tasks.append(task)

    return tasks

def mpiHplReduce(arguments, nodes, tasks, taskResults):
    mpiVersion = arguments['Intel MPI version']
    mklVersion = arguments['Intel MKL version']
    mpiInstallationLocationLinux = globalGetDefaultInstallationLocationLinux('MPI', mpiVersion)
    mklInstallationLocationLinux = globalGetDefaultInstallationLocationLinux('MKL', mklVersion)

    taskDetail = {}
    try:
        for taskResult in taskResults:
            taskId = taskResult['TaskId']
            node = taskResult['NodeName']
            output = taskResult['Message']
            exitcode = taskResult['ExitCode']
            taskDetail[taskId] = {
                'Node':node,
                'Output':output,
                'ExitCode':exitcode
            }
    except Exception as e:
        printErrorAsJson('Failed to parse task results. ' + str(e))
        return -1

    nodeCheckingTasks = [taskDetail[taskId] for taskId in range(1, len(nodes) + 1)]
    flagTask = taskDetail[len(nodes) + 1]
    intelHpl = 'Intel Distribution for LINPACK Benchmark'
    description = 'This is the result of running {} in the cluster.'.format(intelHpl)
    result = tableHeaders = tableRows = greenRowNumber = descriptions = None
    resultCode = -1
    if flagTask['ExitCode'] == 0:
        # get CPU info from node checking tasks
        cpuFreqByNode = {}
        cpuCoreByNode = {}
        hyperThreadingNodes = []
        for task in nodeCheckingTasks:
            node = task['Node']
            output = task['Output']
            try:
                for line in output.splitlines():
                    if line.startswith('CPU'):
                        coreCount = int(line.split()[-1])
                        cpuCoreByNode[node] = coreCount
                    elif line.startswith('Model name'):
                        coreFreq = float([word for word in line.split() if word.endswith('GHz')][0][:-3])
                        cpuFreqByNode[node] = coreFreq
                    elif line.startswith('Thread(s) per core'):
                        threads = int(line.split()[-1])
                        if threads > 1:
                            hyperThreadingNodes.append(node)
            except:
                pass

        # get VM size info from task CustomizedData which is set in map script
        sizeByNode = {}
        cpuCoreBySize = {}
        cpuFreqBySize = {}
        try:
            for task in tasks:
                node = task['Node']
                size = task['CustomizedData']
                if node not in sizeByNode and size:
                    freq = cpuFreqByNode.get(node)
                    if freq:
                        size += '({}GHz)'.format(freq)
                        cpuFreqBySize[size] = freq
                    coreCount = cpuCoreByNode.get(node)
                    if coreCount:
                        cpuCoreBySize[size] = coreCount
                    sizeByNode[node] = size
        except Exception as e:
            printErrorAsJson('Failed to parse tasks. ' + str(e))
            return -1

        if len(sizeByNode) != len(nodes):
            printErrorAsJson('VM size info is missing.')
            return -1

        # get theoretical peak performance of cluster
        defaultFlopsPerCycle = 16 # Use this default value because currently it seems that the Intel microarchitectures used in Azure VM are in "Intel Haswell/Broadwell/Skylake/Kaby Lake". Consider getting this value from test parameter in case new Azure VM sizes are introduced.
        theoreticalPerf = None
        theoreticalPerfExpr = None
        theoreticalPerfDescription = "The theoretical peak performance of the cluster can not be calculated because CPU info is missing."
        nodeSizes = set(sizeByNode.values())
        if len(nodeSizes) > 1:
            vmSizeDescription = 'The cluster is heterogeneous with VM sizes: {}. Optimal perfermance may not be achieved in this test.'.format(', '.join(nodeSizes))
            sizes = [sizeByNode[node] for node in nodes]
            theoreticalPerfExpr = " + ".join(["{} * {} * {} * {}".format(sizes.count(size), cpuCoreBySize.get(size), defaultFlopsPerCycle, cpuFreqBySize.get(size)) for size in nodeSizes])
            if 'None' not in theoreticalPerfExpr:
                theoreticalPerf = eval(theoreticalPerfExpr)
                theoreticalPerfDescription = "The theoretical peak performance of the cluster is <b>{}</b> GFlop/s, which is calculated by summing the FLOPs of each node: SUM([core count per node] * [(double-precision) floating-point operations per cycle] * [average frequency of core]) = {}".format(theoreticalPerf, theoreticalPerfExpr)
        elif len(nodeSizes) == 1:
            size = list(nodeSizes)[0]
            vmSizeDescription = 'The cluster is homogeneous with VM size: {}.'.format(size)
            theoreticalPerfExpr = "{} * {} * {} * {}".format(len(nodes), cpuCoreBySize.get(size), defaultFlopsPerCycle, cpuFreqBySize.get(size))
            if 'None' not in theoreticalPerfExpr:
                theoreticalPerf = eval(theoreticalPerfExpr)
                theoreticalPerfDescription = "The theoretical peak performance of the cluster is <b>{}</b> GFlop/s, which is calculated by: [node count] * [core count per node] * [(double-precision) floating-point operations per cycle] * [average frequency of core] = {}".format(theoreticalPerf, theoreticalPerfExpr)
        else:
            vmSizeDescription = 'The VM size in the cluster is unknown.'

        # warning for Hyper-Threading
        hyperThreadingDescription = 'Optimal perfermance may not be achieved in this test because Hyper-Threading is enabled on the node(s): {}'.format(', '.join(hyperThreadingNodes)) if hyperThreadingNodes else ''

        # get result from result task and generate output
        resultTask = taskDetail[max(taskDetail.keys())]
        output = resultTask['Output']
        if '*.result: No such file or directory' in output:
            keyWord = '*.result:'
            logDir = [word for word in output.split() if word.endswith(keyWord)][0][:-(len(keyWord))]
            node = resultTask['Node']
            result = 'No result. The cluster may be too busy or has broken node(s). Check log in {} on {}'.format(logDir, node)
            descriptions = [result]
        else:
            result = []
            output = [line.split() for line in output.splitlines()]
            tableHeaders = ['Problem size(N)', 'Block size(NB)', 'P', 'Q', 'Time(s)', 'Performance(GFlop/s)', 'Efficiency']
            tableRows = []
            maxPerf = -1
            lineNumber = 0
            for row in output:
                if len(row) == 7:
                    try:
                        perf = float(row[6])
                        efficiency = perf/theoreticalPerf if theoreticalPerf else None
                        result.append({
                            "N":int(row[1]),
                            "NB":int(row[2]),
                            "P":int(row[3]),
                            "Q":int(row[4]),
                            "Time":float(row[5]),
                            "Perf":perf,
                            "Efficiency":efficiency
                        })
                        if perf > maxPerf:
                            maxPerf = perf
                            greenRowNumber = lineNumber
                        perfInHtml = "{:.2f}".format(perf)
                        efficiencyInHtml = "{:.2%}".format(efficiency) if efficiency else None
                        tableRows.append(row[1:-1] + [perfInHtml, efficiencyInHtml])
                        lineNumber += 1
                    except:
                        pass
            intelHplWithLink = '<a target="_blank" rel="noopener noreferrer" href="https://software.intel.com/en-us/mkl-linux-developer-guide-overview-of-the-intel-distribution-for-linpack-benchmark">{}</a>'.format(intelHpl)
            descriptionInHtml = description.replace(intelHpl, intelHplWithLink)
            descriptions = [descriptionInHtml, vmSizeDescription, theoreticalPerfDescription, hyperThreadingDescription]
        resultCode = 0
    else:
        result = 'Cluster is not ready to run Intel HPL. Diagnostics test MPI-Pingpong may help to diagnose the cluster.'
        descriptions = [result.replace('MPI-Pingpong', '<b>MPI-Pingpong</b>')]
        resultCode = -1
        if any(task['ExitCode'] != 0 for task in nodeCheckingTasks):
            tableHeaders = ['Node', 'State']
            tableRows = []
            for task in nodeCheckingTasks:
                taskStates = []
                output = task['Output']
                if 'MKL test succeed.' in output and 'MPI test succeed.' in output:
                    taskStates.append('Ready')
                if 'mpivars.sh: No such file or directory' in output:
                    taskStates.append('Intel MPI {} is not found in directory: {}'.format(mpiVersion, mpiInstallationLocationLinux))
                if 'xhpl_intel64_dynamic: No such file or directory' in output:
                    taskStates.append('Intel MKL {} is not found in directory: {}'.format(mklVersion, mklInstallationLocationLinux))
                if 'dapl fabric is not available' in output:
                    taskStates.append('Error when running MPI test')
                state = '<br>'.join(taskStates)
                if not state:
                    state = 'Unknown'
                tableRows.append([task['Node'], state])

    title = 'High Performance Linpack Benchmark'
    html = globalGenerateHtmlResult(title, tableHeaders, tableRows, greenRowNumber, descriptions)

    result = {
        'Description': description,
        'Results': result,
        'Html': html
        }

    print(json.dumps(result, indent = 4))
    return resultCode


def installIntelProductMap(arguments, windowsNodes, linuxNodes, product):
    version = arguments['Version'].lower()
    timeout = arguments['Max runtime']
    globalCheckIntelProductVersion(product, version)

    leastTime = 180 if product == 'MPI' else 600
    timeout -= leastTime - 1
    if timeout <= 0:
        raise Exception('The Max runtime parameter should be equal or larger than {}'.format(leastTime))
            
    # command to install MPI/MKL on Linux node
    url = globalGetIntelProductAzureBlobUrl(INTEL_PRODUCT_URL[product][version]['Linux'])
    installDirectory = globalGetDefaultInstallationLocationLinux(product, version)
    wgetOutput = 'wget.output'
    commandCheckExist = "[ -d {0} ] && echo 'Already installed in {0}'".format(installDirectory)
    commandShowOutput = r"cat {} | sed 's/.*\r//'".format(wgetOutput)
    commandDownload = 'timeout {0}s wget --retry-connrefused --waitretry=1 --read-timeout=20 --timeout=15 -t 0 --progress=bar:force -O intel.tgz {1} 1>{2} 2>&1 && {3} || (errorcode=$? && {3} && exit $errorcode)'.format(timeout, url, wgetOutput, commandShowOutput)
    commandInstall = "tar -zxf intel.tgz && cd l_{}_* && sed -i -e 's/ACCEPT_EULA=decline/ACCEPT_EULA=accept/g' ./silent.cfg && ./install.sh --silent ./silent.cfg".format(product.lower())
    commandLinux = '{} || ({} && {})'.format(commandCheckExist, commandDownload, commandInstall)

    # command to install MPI/MKL on Windows node
    url = globalGetIntelProductAzureBlobUrl(INTEL_PRODUCT_URL[product][version]['Windows'])
    installDirectory = globalGetDefaultInstallationLocationWindows(product, version)
    commandWindows = """powershell "
if (Test-Path '[installDirectory]')
{
    'Already installed in [installDirectory]';
    exit
}
else
{
    rm -Force -ErrorAction SilentlyContinue [product].exe;
    rm -Force -ErrorAction SilentlyContinue [product].log;
    date;
    $stopwatch = [system.diagnostics.stopwatch]::StartNew();
    'Start downloading';
    $client = new-object System.Net.WebClient;
    $client.DownloadFile('[url]', '[product].exe');
    date;
    'End downloading';
    if ($stopwatch.Elapsed.TotalSeconds -gt [timeout])
    {
        'Not enough time to install before task timeout. Exit.';
        exit 124;
    }
    else
    {
        cmd /C '.\[product].exe --silent --a install --eula=accept --output=%cd%\[product].log & type [product].log'
    }
}"
""".replace('[installDirectory]', installDirectory).replace('[url]', url).replace('[timeout]', str(timeout)).replace('[product]', product).replace('\n', '')

    tasks = []
    id = 1
    for node in sorted(windowsNodes):
        task = {}
        task['Id'] = id
        id += 1
        task['Node'] = node
        task['CommandLine'] = commandWindows
        task['CustomizedData'] = 'Windows'
        task['MaximumRuntimeSeconds'] = 36000
        tasks.append(task)
    for node in sorted(linuxNodes):
        task = {}
        task['Id'] = id
        id += 1
        task['Node'] = node
        task['CommandLine'] = commandLinux
        task['CustomizedData'] = 'Linux'
        task['MaximumRuntimeSeconds'] = 36000
        tasks.append(task)

    return tasks

def installIntelProductReduce(arguments, targetNodes, tasks, taskResults, product):
    version = arguments['Version']

    TASK_STATE_CANCELED = 5
    canceledTasks = set()
    osTypeByNode = {}
    try:
        for task in tasks:
            osTypeByNode[task['Node']] = task['CustomizedData']
            if task['State'] == TASK_STATE_CANCELED:
                canceledTasks.add(task['Id'])
    except Exception as e:
        printErrorAsJson('Failed to parse tasks. ' + str(e))
        return -1

    results = {}
    try:
        for taskResult in taskResults:
            node = taskResult['NodeName']
            message = taskResult['Message']
            result = 'Installation Failed'
            if taskResult['ExitCode'] == 0:
                if 'Already installed' in message.split('\n', 1)[0]:
                    result = 'Already installed'
                elif osTypeByNode[node].lower() == 'Linux'.lower() or 'installation was completed successfully' in message:
                    result = 'Installation succeeded'
            elif taskResult['ExitCode'] == 124 or taskResult['TaskId'] in canceledTasks:
                result = 'Timeout'
            results[node] = result
    except Exception as e:
        printErrorAsJson('Failed to parse task result. ' + str(e))
        return -1

    lostNodes = set(targetNodes) - set(results.keys())

    description = 'This is the result of installing Intel {} {} on each node.'.format(product, version)
    lostNodesDescription = 'The task result is lost for node(s): {}'.format(', '.join(lostNodes)) if lostNodes else ''
    mpiLink = '<a target="_blank" rel="noopener noreferrer" href="https://software.intel.com/en-us/mpi-library">Intel MPI</a>'
    mklLink = '<a target="_blank" rel="noopener noreferrer" href="https://software.intel.com/en-us/mkl">Intel MKL</a>'

    title = 'Intel {} Installation'.format(product)
    tableHeaders = ['Node', 'OS', 'Result']
    tableRows = [[node, osTypeByNode[node], results[node]] for node in sorted(results)]
    descriptions = [description.replace('Intel MPI', mpiLink).replace('Intel MKL', mklLink), lostNodesDescription]
    html = globalGenerateHtmlResult(title, tableHeaders, tableRows, None, descriptions)

    result = {
        'Description': description,
        'Results': results,
        'Html': html
        }

    print(json.dumps(result))
    return 0


def benchmarkLinpackMap(arguments, windowsNodes, linuxNodes, vmSizeByNode):
    intelMklVersion = arguments['Intel MKL version'].lower()
    sizeLevel = arguments['Size level']
    globalCheckIntelProductVersion('MKL', intelMklVersion)
    mklInstallationLocationWindows = globalGetDefaultInstallationLocationWindows('MKL', intelMklVersion)
    mklInstallationLocationLinux = globalGetDefaultInstallationLocationLinux('MKL', intelMklVersion)

    if not 1 <= sizeLevel <= 15:
        raise Exception('Parameter "Size level" should be in range 1 - 15')

    commandInstallNumactlOnUbuntu = 'apt install -y numactl'
    commandInstallNumactlOnSuse = 'zypper install -y numactl'
    commandInstallNumactlOnOthers = 'yum install -y numactl'
    commandInstallNumactlQuietOrWarn = "({}) >/dev/null 2>&1 || echo 'Failed to install numactl.'"
    commandInstallNumactlOnUbuntu = commandInstallNumactlQuietOrWarn.format(commandInstallNumactlOnUbuntu)
    commandInstallNumactlOnSuse = commandInstallNumactlQuietOrWarn.format(commandInstallNumactlOnSuse)
    commandInstallNumactlOnOthers = commandInstallNumactlQuietOrWarn.format(commandInstallNumactlOnOthers)
    commandDetectDistroAndInstall = ("cat /etc/*release > distroInfo && "
                                 "if cat distroInfo | grep -Fiq 'Ubuntu'; then {};"
                                 "elif cat distroInfo | grep -Fiq 'Suse'; then {};"
                                 "elif cat distroInfo | grep -Fiq 'CentOS'; then {};"
                                 "elif cat distroInfo | grep -Fiq 'Redhat'; then {};"
                                 "elif cat distroInfo | grep -Fiq 'Red Hat'; then {};"
                                 "fi").format(commandInstallNumactlOnUbuntu, 
                                              commandInstallNumactlOnSuse,
                                              commandInstallNumactlOnOthers,
                                              commandInstallNumactlOnOthers,
                                              commandInstallNumactlOnOthers)

    commandModify = "sed -i 's/.*# number of tests/{} # number of tests/' lininput_xeon64".format(sizeLevel)
    commandRunLinpack = 'cd {}/benchmarks/linpack && {} && ./runme_xeon64'.format(mklInstallationLocationLinux, commandModify)
    commandCheckCpu = "lscpu | egrep '^CPU\(s\)|^CPU MHz'"

    # Bug: the output of task is empty when it runs Linpack with large problem size, thus start another task to collect the output
    tempOutputDir = '/tmp/hpc_diag_linpack_standalone'
    commandCreateTempOutputDir = 'mkdir -p {}'.format(tempOutputDir)

    tasks = []
    id = 1
    for node in sorted(linuxNodes):
        outputFile = '{}/{}'.format(tempOutputDir, uuid.uuid4())
        commandClearFile = 'rm -f {}'.format(outputFile)
        task = {}
        task['Id'] = id
        task['Node'] = node
        task['CustomizedData'] = '[Linux] {}'.format(vmSizeByNode[node])
        task['CommandLine'] = '{} && {} && ({} && {}) 2>&1 | tee {}'.format(commandClearFile, commandCreateTempOutputDir, commandDetectDistroAndInstall, commandRunLinpack, outputFile)
        task['MaximumRuntimeSeconds'] = 36000
        tasks.append(task)
        task = copy.deepcopy(task)
        task['ParentIds'] = [id]
        task['Id'] += 1
        task['CommandLine'] = '{}; cat {} && {}'.format(commandCheckCpu, outputFile, commandClearFile)
        tasks.append(task)
        id += 2

    commandModify = "(gc lininput_xeon64) -replace '.*# number of tests', '{} # number of tests' | out-file -encoding ascii lininput_xeon64".format(sizeLevel)
    commandCheckCpu = 'wmic cpu get NumberOfCores,MaxClockSpeed /format:list'
    commandWindows = r'{} && cd {}\benchmarks\linpack && del win_xeon64.txt 2>nul && powershell "{}" && runme_xeon64 >nul && type win_xeon64.txt'.format(commandCheckCpu, mklInstallationLocationWindows, commandModify)
    for node in sorted(windowsNodes):
        task = {}
        task['Id'] = id
        id += 1
        task['Node'] = node
        task['CustomizedData'] = '[Windows] {}'.format(vmSizeByNode[node])
        task['CommandLine'] = commandWindows
        task['MaximumRuntimeSeconds'] = 36000
        tasks.append(task)

    return tasks

def benchmarkLinpackReduce(arguments, tasks, taskResults):
    intelMklVersion = arguments['Intel MKL version'].lower()
    intelMklLocationLinux = globalGetDefaultInstallationLocationLinux('MKL', intelMklVersion)
    intelMklLocationWindows = globalGetDefaultInstallationLocationWindows('MKL', intelMklVersion)
        
    taskDetail = {}
    try:
        for task in tasks:
            taskId = task['Id']
            node = task['Node']
            taskLabels = task['CustomizedData'].split()
            osType = taskLabels[0][1:-1]
            vmSize = taskLabels[1]
            if osType == 'Linux' and taskId % 2 == 0 or osType == 'Windows':
                taskDetail[taskId] = {
                    'Node': node,
                    'OS': osType,
                    'Size': vmSize,
                    'Output': None
                    }
    except Exception as e:
        printErrorAsJson('Failed to parse tasks. ' + str(e))
        return -1

    nodesWithoutIntelMklInstalledLinux = []
    nodesWithoutIntelMklInstalledWindows = []
    nodesFailedToInstallNumactl = []
    try:
        for taskResult in taskResults:
            taskId = taskResult['TaskId']
            nodeName = taskResult['NodeName']
            output = taskResult['Message']
            detail = taskDetail.get(taskId)
            if detail:
                detail['Output'] = output
                if 'Failed to install numactl' in output:
                    nodesFailedToInstallNumactl.append(nodeName)
                if 'benchmarks/linpack: No such file or directory' in output:
                    nodesWithoutIntelMklInstalledLinux.append(nodeName)
                elif 'The system cannot find the path specified' in output:
                    nodesWithoutIntelMklInstalledWindows.append(nodeName)
    except Exception as e:
        printErrorAsJson('Failed to parse task results. ' + str(e))
        return -1

    defaultFlopsPerCycle = 16 # Use this default value because currently it seems that the Intel microarchitectures used in Azure VM are in "Intel Haswell/Broadwell/Skylake/Kaby Lake". Consider getting this value from test parameter in case new Azure VM sizes are introduced.

    results = sorted(list(taskDetail.values()), key = lambda k:k['Node'])
    tableRows = []
    for task in results:
        perf, N, coreCount, coreFreq = benchmarkLinpackParseTaskOutput(task['Output'])
        theoreticalPerfExpr = "{} * {} * {:.1f}".format(coreCount, defaultFlopsPerCycle, coreFreq) if coreCount and coreFreq else None
        theoreticalPerf = eval(theoreticalPerfExpr) if theoreticalPerfExpr else None
        efficiency = perf / theoreticalPerf if perf and theoreticalPerf else None
        task['TheoreticalPerf'] = theoreticalPerf
        task['Perf'] = perf
        task['N'] = N
        task['Efficiency'] = efficiency
        theoreticalPerfInHtml = "{} = {}".format(theoreticalPerfExpr, theoreticalPerf) if theoreticalPerfExpr else None
        perfInHtml = "{:.1f}".format(perf) if perf else None
        efficiencyInHtml = "{:.1%}".format(efficiency) if efficiency else None
        tableRows.append([task['Node'], task['OS'], task['Size'], theoreticalPerfInHtml, perfInHtml, N, efficiencyInHtml])
        del task['Output']

    intelLinpack = 'Intel Optimized LINPACK Benchmark'
    description = 'This is the result of running {} on each node.'.format(intelLinpack)
    intelLinpackLink = '<a target="_blank" rel="noopener noreferrer" href="https://software.intel.com/en-us/mkl-linux-developer-guide-intel-optimized-linpack-benchmark-for-linux">{}</a>'.format(intelLinpack)
    theoreticalPerfDescription = "The theoretical peak performance of each node is calculated by: [core count of node] * [(double-precision) floating-point operations per cycle] * [average frequency of core]" if any([task['TheoreticalPerf'] for task in results]) else ''
    intelMklNotFoundLinux = 'Intel MKL {} is not found in <b>{}</b> on node(s): {}'.format(intelMklVersion, intelMklLocationLinux, ', '.join(nodesWithoutIntelMklInstalledLinux)) if nodesWithoutIntelMklInstalledLinux else ''
    intelMklNotFoundWindows = 'Intel MKL {} is not found in <b>{}</b> on node(s): {}'.format(intelMklVersion, intelMklLocationWindows, ', '.join(nodesWithoutIntelMklInstalledWindows)) if nodesWithoutIntelMklInstalledWindows else ''
    installIntelMkl = '<b>Prerequisite-Intel MKL Installation</b> can be used to install Intel MKL.' if nodesWithoutIntelMklInstalledLinux or nodesWithoutIntelMklInstalledWindows else ''
    installNumactl = 'Please install <b>numactl</b> manually, if necessary, on node(s): {}'.format(', '.join(nodesFailedToInstallNumactl)) if nodesFailedToInstallNumactl else ''

    title = 'Standalone Linpack Benchmark'
    tableHeaders = ['Node', 'OS', 'VM size', 'Theoretical peak performance(GFlop/s)', 'Best performance(GFlop/s)', 'Problem size', 'Efficiency']
    descriptions = [description.replace(intelLinpack, intelLinpackLink), theoreticalPerfDescription, intelMklNotFoundLinux, intelMklNotFoundWindows, installIntelMkl, installNumactl]
    html = globalGenerateHtmlResult(title, tableHeaders, tableRows, None, descriptions)

    result = {
        'Description': description,
        'Results': results,
        'Html': html
        }

    print(json.dumps(result, indent = 4))
    return 0

def benchmarkLinpackParseTaskOutput(raw):
    bestPerf = n = coreCount = coreFreq = None
    try:
        start = raw.find('Performance Summary (GFlops)')
        end = raw.find('Residual checks PASSED')
        if -1 < start < end:
            table = [line for line in raw[start:end].splitlines() if line.strip()][2:]
            bestPerf = 0
            for line in table:
                numbers = line.split()
                perf = float(numbers[3])
                if perf > bestPerf:
                    bestPerf = perf
                    n = int(numbers[0])
        lines = [line for line in raw.splitlines() if line]
        firstLine = lines[0]
        secondLine = lines[1]
        if firstLine.startswith('CPU') and secondLine.startswith('CPU MHz'):
            coreCount = int(firstLine.split()[-1])
            coreFreq = float(secondLine.split()[-1]) / 1000
        elif firstLine.startswith('MaxClockSpeed'):
            coreFreq = float(firstLine.split('=')[1]) / 1000
            coreCount = sum(int(line.split('=')[1]) for line in lines if line.startswith('NumberOfCores'))
    except:
        pass
    return (bestPerf, n, coreCount, coreFreq)

def benchmarkSysbenchCpuMap(windowsNodes, linuxNodes, vmSizeByNode, distroByNode):
    if windowsNodes and not linuxNodes:
        raise Exception('The test is not supported on Windows node')

    commandInstallSysbenchOnUbuntu = 'apt install -y sysbench'
    commandInstallSysbenchOnOpensuse = 'zypper install -y sysbench' # support in openSUSE but not in Suse
    commandInstallSysbenchOnCentos = 'yum install -y sysbench'
    commandInstallSysbenchOnRedhat = 'curl -s https://packagecloud.io/install/repositories/akopytov/sysbench/script.rpm.sh | bash && yum install -y sysbench'
    commandInstallQuiet = '({}) >/dev/null 2>&1'
    commandInstallSysbenchOnUbuntu = commandInstallQuiet.format(commandInstallSysbenchOnUbuntu)
    commandInstallSysbenchOnOpensuse = commandInstallQuiet.format(commandInstallSysbenchOnOpensuse)
    commandInstallSysbenchOnCentos = commandInstallQuiet.format(commandInstallSysbenchOnCentos)
    commandInstallSysbenchOnRedhat = commandInstallQuiet.format(commandInstallSysbenchOnRedhat)
    commandDetectDistroAndInstall = ("cat /etc/*release > distroInfo && "
                                 "if cat distroInfo | grep -Fiq 'Ubuntu'; then {};"
                                 "elif cat distroInfo | grep -Fiq 'Opensuse'; then {};"
                                 "elif cat distroInfo | grep -Fiq 'CentOS'; then {};"
                                 "elif cat distroInfo | grep -Fiq 'Redhat'; then {};"
                                 "elif cat distroInfo | grep -Fiq 'Red Hat'; then {};"
                                 "fi").format(commandInstallSysbenchOnUbuntu, 
                                              commandInstallSysbenchOnOpensuse,
                                              commandInstallSysbenchOnCentos,
                                              commandInstallSysbenchOnRedhat,
                                              commandInstallSysbenchOnRedhat)    
    commandInstallByDistro = {
        'Ubuntu': commandInstallSysbenchOnUbuntu,
        'OpenSUSE': commandInstallSysbenchOnOpensuse,
        'CentOS': commandInstallSysbenchOnCentos,
        'Redhat': commandInstallSysbenchOnRedhat,
        'Unknown': commandDetectDistroAndInstall
    }

    commandRunLscpu = 'lscpu'
    commandRunSysbench = 'sysbench --test=cpu --num-threads=`grep -c ^processor /proc/cpuinfo` run >output 2>&1 && cat output'

    tasks = []
    id = 1
    for node in sorted(linuxNodes):
        task = {}
        task['Id'] = id
        id += 1
        task['Node'] = node
        task['CustomizedData'] = '[{}] {}'.format(distroByNode[node], vmSizeByNode[node])
        task['CommandLine'] = '{} && {} && {}'.format(commandRunLscpu, commandInstallByDistro[distroByNode[node]], commandRunSysbench)
        tasks.append(task)

    for node in sorted(windowsNodes):
        task = {}
        task['Id'] = id
        id += 1
        task['Node'] = node
        task['CustomizedData'] = '[Windows] {}'.format(vmSizeByNode[node])
        task['CommandLine'] = 'echo This test is not supported on Windows node'
        tasks.append(task)

    return tasks

def benchmarkSysbenchCpuReduce(tasks, taskResults):
    taskDetail = {}
    try:
        for task in tasks:
            taskId = task['Id']
            node = task['Node']
            taskLabels = task['CustomizedData'].split()
            osDistro = taskLabels[0][1:-1]
            vmSize = taskLabels[1]
            if osDistro != 'Windows':
                taskDetail[taskId] = {
                    'Node': node,
                    'Distro': osDistro,
                    'Size': vmSize
                    }
    except Exception as e:
        printErrorAsJson('Failed to parse tasks. ' + str(e))
        return -1

    try:
        for taskResult in taskResults:
            taskId = taskResult['TaskId']
            output = taskResult['Message']
            detail = taskDetail.get(taskId)
            if detail:
                detail['Output'] = output
    except Exception as e:
        printErrorAsJson('Failed to parse task results. ' + str(e))
        return -1

    results = sorted(list(taskDetail.values()), key = lambda k:k['Node'])
    for task in results:
        totalNumber, totalTime, coreNumber, threadNumber, model = benchmarkSysbenchCpuParseTaskOutput(task['Output'])
        task['Result'] = '{:.2f}'.format(totalNumber/totalTime) if totalNumber and totalTime else None
        task['CoreNumber'] = coreNumber
        task['ThreadNumber'] = threadNumber
        task['Model'] = model
        del task['Output']

    description = "This is the result of running sysbench on each node, the value in which is the times of calculating the prime number less than 10000 per second."
    notSupportWindows = 'The test is not supported on Windows node.' if len(results) < len(tasks) else ''

    title = 'Sysbench CPU Benchmark'
    tableHeaders = ['Node', 'Distro', 'VM size', 'Model', 'Cores', 'Threads', 'Result']
    tableRows = [[result[item] for item in ['Node', 'Distro', 'Size', 'Model', 'CoreNumber', 'ThreadNumber', 'Result']] for result in results]
    descriptions = [description, notSupportWindows]
    html = globalGenerateHtmlResult(title, tableHeaders, tableRows, None, descriptions)

    result = {
        'Description': description,
        'Results': results,
        'Html': html
        }

    print(json.dumps(result, indent = 4))
    return 0

def benchmarkSysbenchCpuParseTaskOutput(raw):
    totalTime = totalNumber = threadNumber = coreNumber = model = None
    try:
        lines = raw.splitlines()
        for line in lines:
            if line.startswith('CPU(s):'):
                coreNumber = line.split()[-1]
                continue
            if line.startswith('Model name:'):
                model = line[len('Model name:'):].strip()
                continue
            if line.startswith('Number of threads:'):
                threadNumber = int(line.split()[-1])
                continue
            if line.strip().startswith('total time:'):
                totalTime = float(line.split()[-1][:-1])
                continue
            if line.strip().startswith('total number of events:'):
                totalNumber = int(line.split()[-1])
                continue
    except:
        pass
    return totalNumber, totalTime, coreNumber, threadNumber, model

def benchmarkCifsMap(arguments, windowsNodes, linuxNodes, vmSizeByNode, distroByNode):
    if windowsNodes and not linuxNodes:
        raise Exception('The test is not supported on Windows node')

    connectWay = arguments['Connect by'].lower()
    cifsServer = arguments['CIFS server']
    mode = arguments['Mode'].lower()

    mountPoint = None
    connectionString = None
    if connectWay == 'Mount point'.lower():
        mountPoint = cifsServer
    else:
        connectionString = cifsServer

    if not mountPoint and not connectionString:
        raise Exception('Neither mount point nor connection string of CIFS server is specified.')

    commandInstallCifsUtilsOnUbuntu = 'apt install -y cifs-utils'
    commandInstallCifsUtilsOnSuse = 'zypper install -y cifs-utils'
    commandInstallCifsUtilsOnOthers = 'yum install -y cifs-utils'
    commandInstallQuiet = '({}) >/dev/null 2>&1'
    commandInstallCifsUtilsOnUbuntu = commandInstallQuiet.format(commandInstallCifsUtilsOnUbuntu)
    commandInstallCifsUtilsOnSuse = commandInstallQuiet.format(commandInstallCifsUtilsOnSuse)
    commandInstallCifsUtilsOnOthers = commandInstallQuiet.format(commandInstallCifsUtilsOnOthers)
    commandDetectDistroAndInstall = ("cat /etc/*release >distroInfo && "
                                 "if cat distroInfo | grep -Fiq 'Ubuntu'; then {};"
                                 "elif cat distroInfo | grep -Fiq 'Suse'; then {};"
                                 "elif cat distroInfo | grep -Fiq 'CentOS'; then {};"
                                 "elif cat distroInfo | grep -Fiq 'Redhat'; then {};"
                                 "elif cat distroInfo | grep -Fiq 'Red Hat'; then {};"
                                 "fi").format(commandInstallCifsUtilsOnUbuntu, 
                                              commandInstallCifsUtilsOnSuse,
                                              commandInstallCifsUtilsOnOthers,
                                              commandInstallCifsUtilsOnOthers,
                                              commandInstallCifsUtilsOnOthers)

    commandInstallByDistro = {
        'Ubuntu': commandInstallCifsUtilsOnUbuntu,
        'OpenSUSE': commandInstallCifsUtilsOnSuse,
        'CentOS': commandInstallCifsUtilsOnOthers,
        'Redhat': commandInstallCifsUtilsOnOthers,
        'Unknown': commandDetectDistroAndInstall
    }

    tempMountPoint = '/mnt/hpc_diag_benchmark_cifs_share'
    tempDir = 'hpc_diag_benchmark_cifs_tmp'
    tempFileName = 'hpc_diag_benchmark_cifs_`hostname`'
    commandMountShare = 'mkdir -p {} && {}'.format(tempMountPoint, connectionString.replace('[mount point]', tempMountPoint))
    commandUnmountShare = '(umount -l {} >/dev/null 2>&1 && rm -rf {} || :)'.format(tempMountPoint, tempMountPoint)
    commandCopyLocal = 'dd if=/dev/zero of=/tmp/{} bs=1M count=100'.format(tempFileName)
    mountDir = mountPoint if mountPoint else tempMountPoint
    commandCopyToCifs = 'mkdir -p {0}/{1} && dd if=/tmp/{2} of={0}/{1}/{2}'.format(mountDir, tempDir, tempFileName)
    commandCopyFromCifs = 'dd if={0}/{1}/{2} of=/tmp/{2}'.format(mountDir, tempDir, tempFileName)
    commandCleanup = 'rm -f /tmp/{} && rm -f {}/{}/{}'.format(tempFileName, mountDir, tempDir, tempFileName)
    
    commandGetLocation = "curl --header 'metadata: true' --connect-timeout 5 http://169.254.169.254/metadata/instance?api-version=2017-08-01 2>/dev/null | tr ',' '\n' | grep location"
    commandRun = '({} && {} && {} || :) && {}'.format(commandCopyLocal, commandCopyToCifs, commandCopyFromCifs, commandCleanup)
    if not mountPoint:
        commandRun = '({} && {} && {} || :) && {}'.format(commandUnmountShare, commandMountShare, commandRun, commandUnmountShare)

    tasks = []
    id = 1
    for node in sorted(linuxNodes):
        task = {}
        task['Id'] = id
        if mode != 'Parallel'.lower() and id != 1:
            task['ParentIds'] = [id-1]
        id += 1
        task['Node'] = node
        task['CustomizedData'] = '[{}] {}'.format(distroByNode[node], vmSizeByNode[node])
        task['CommandLine'] = '{} ; {} && {}'.format(commandGetLocation, commandInstallByDistro[distroByNode[node]], commandRun)
        tasks.append(task)

    for node in sorted(windowsNodes):
        task = {}
        task['Id'] = id
        id += 1
        task['Node'] = node
        task['CustomizedData'] = '[Windows] {}'.format(vmSizeByNode[node])
        task['CommandLine'] = 'echo This test is not supported on Windows node'
        tasks.append(task)

    return tasks

def benchmarkCifsReduce(arguments, tasks, taskResults):
    connectWay = arguments['Connect by'].lower()
    cifsServer = arguments['CIFS server']

    if connectWay == 'Connection string'.lower() and cifsServer:
        connectionString = cifsServer
        precursor = 'mount -t cifs '
        successor = ' [mount point]'
        begin = connectionString.find(precursor) + len(precursor)
        end = connectionString.find(successor)
        if 0 < begin < end:
            cifsServer = connectionString[begin:end]

    taskDetail = {}
    try:
        for task in tasks:
            taskId = task['Id']
            node = task['Node']
            taskLabels = task['CustomizedData'].split()
            osDistro = taskLabels[0][1:-1]
            vmSize = taskLabels[1]
            if osDistro != 'Windows':
                taskDetail[taskId] = {
                    'Node': node,
                    'Distro': osDistro,
                    'Size': vmSize
                    }
    except Exception as e:
        printErrorAsJson('Failed to parse tasks. ' + str(e))
        return -1

    try:
        for taskResult in taskResults:
            taskId = taskResult['TaskId']
            output = taskResult['Message']
            detail = taskDetail.get(taskId)
            if detail:
                detail['Output'] = output
    except Exception as e:
        printErrorAsJson('Failed to parse task results. ' + str(e))
        return -1

    results = sorted(list(taskDetail.values()), key = lambda k:k['Node'])
    htmlRows = []
    for task in results:
        task['Location'], task['Local'], task['ToCifs'], task['FromCifs'] = benchmarkCifsParseTaskOutput(task['Output'])
        del task['Output']

    description = 'The benchmark shows the speed of copying file between local disk and CIFS server: {}.'.format(cifsServer)
    checkManually = 'If result is None, please check it manually following <a target="_blank" rel="noopener noreferrer" href="https://docs.microsoft.com/en-us/azure/storage/files/storage-how-to-use-files-linux#install-cifs-utils">Use Azure Files with Linux</a>.'
    notSupportWindows = 'The test is not supported on Windows node.' if len(results) < len(tasks) else ''

    title = 'CIFS Benchmark'
    tableHeaders = ['Node', 'Distro', 'VM size', 'Location', 'Local Disk', 'To CIFS', 'From CIFS']
    tableRows = [[result[item] for item in ['Node', 'Distro', 'Size', 'Location', 'Local', 'ToCifs', 'FromCifs']] for result in results]
    descriptions = [description.replace(cifsServer, '<b>{}</b>'.format(cifsServer)), checkManually, notSupportWindows]
    html = globalGenerateHtmlResult(title, tableHeaders, tableRows, None, descriptions)

    result = {
        'Description': description,
        'Results': results,
        'Html': html
        }

    print(json.dumps(result, indent = 4))
    return 0

def benchmarkCifsParseTaskOutput(raw):
    location = local = toCifs = fromCifs = None
    try:
        lines = raw.split('\n')
        for line in lines:
            if 'location' in line:
                location = line.split(':')[-1][1:-1]
            if 'copied' in line:
                speed = line.split(',')[-1]
                if not local:
                    local = speed
                    continue
                if not toCifs:
                    toCifs = speed
                    continue
                if not fromCifs:
                    fromCifs = speed
                    continue
    except:
        pass
    return (location, local, toCifs, fromCifs)


def printErrorAsJson(errormessage):
    print(json.dumps({"Error":errormessage}))

if __name__ == '__main__':
    main()
