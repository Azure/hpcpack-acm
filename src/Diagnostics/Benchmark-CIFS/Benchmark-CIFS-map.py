#v0.3

import sys, json, copy

def main():
    stdin = json.load(sys.stdin)
    job = stdin["Job"]
    nodesInfo = stdin["Nodes"]
    nodelist = job["TargetNodes"]

    if len(nodelist) != len(set(nodelist)):
        # Duplicate nodes
        raise Exception('Duplicate nodes')

    connectWay = 'Connection string'.lower()
    cifsServer = None
    mode = 'Parallel'.lower()
    if 'DiagnosticTest' in job and 'Arguments' in job['DiagnosticTest']:
        arguments = job['DiagnosticTest']['Arguments']
        if arguments:
            for argument in arguments:
                if argument['name'].lower() == 'Connect by'.lower():
                    connectWay = argument['value'].lower()
                    continue
                if argument['name'].lower() == 'CIFS server'.lower():
                    cifsServer = argument['value']
                    continue
                if argument['name'].lower() == 'Mode'.lower():
                    mode = argument['value'].lower()
                    continue

    mountPoint = None
    connectionString = None
    if connectWay == 'Mount point'.lower():
        mountPoint = cifsServer
    else:
        connectionString = cifsServer

    if not mountPoint and not connectionString:
        raise Exception('Neither mount point nor connection string of CIFS server is specified.')

    commandInstallCifsUtilsOnUbuntu = "apt install -y cifs-utils || apt update && apt install -y cifs-utils"
    commandInstallCifsUtilsOnSuse = "zypper install -y"
    commandInstallCifsUtilsOnOthers = "yum install -y cifs-utils"
    commandInstallQuiet = "({}) >/dev/null 2>&1"
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

    commandInstallByNode = {}
    nodeSize = {}
    for nodeInfo in nodesInfo:
        node = nodeInfo["Node"]
        try:
            nodeSize[node] = json.loads(nodeInfo["Metadata"])["compute"]["vmSize"]
        except Exception as e:
            nodeSize[node] = "Unknown"
        try:
            distroInfo = nodeInfo["NodeRegistrationInfo"]["DistroInfo"]
            if "Ubuntu".lower() in distroInfo.lower():
                commandInstallByNode[node] = commandInstallCifsUtilsOnUbuntu
            elif "CentOS".lower() in distroInfo.lower():
                commandInstallByNode[node] = commandInstallCifsUtilsOnOthers
            elif "Redhat".lower() in distroInfo.lower():
                commandInstallByNode[node] = commandInstallCifsUtilsOnOthers
            else:
                commandInstallByNode[node] = commandDetectDistroAndInstall
        except Exception as e:
            raise Exception("Can not retrive distroInfo from NodeRegistrationInfo of node {0}. Exception: {1}".format(node, e))

    tempMountPoint = "/mnt/hpc_diag_benchmark_cifs_share"
    tempDir = "hpc_diag_benchmark_cifs_tmp"
    tempFileName = "hpc_diag_benchmark_cifs_`hostname`"
    commandMountShare = "mkdir -p {} && {}".format(tempMountPoint, connectionString.replace("[mount point]", tempMountPoint))
    commandUnmountShare = "(umount -l {} >/dev/null 2>&1 && rm -rf {} || :)".format(tempMountPoint, tempMountPoint)
    commandCopyLocal = "dd if=/dev/zero of=/tmp/{} bs=1M count=100".format(tempFileName)
    mountDir = mountPoint if mountPoint else tempMountPoint
    commandCopyToCifs = "mkdir -p {0}/{1} && dd if=/tmp/{2} of={0}/{1}/{2}".format(mountDir, tempDir, tempFileName)
    commandCopyFromCifs = "dd if={0}/{1}/{2} of=/tmp/{2}".format(mountDir, tempDir, tempFileName)
    commandCleanup = "rm -f /tmp/{} && rm -f {}/{}/{}".format(tempFileName, mountDir, tempDir, tempFileName)
    
    commandGetLocation = "(curl --header 'metadata: true' --connect-timeout 5 http://169.254.169.254/metadata/instance?api-version=2017-08-01 2>/dev/null | tr ',' '\n' | grep location || :)"
    commandRun = "({} && {} && {} || :) && {}".format(commandCopyLocal, commandCopyToCifs, commandCopyFromCifs, commandCleanup)
    if not mountPoint:
        commandRun = "({} && {} && {} || :) && {}".format(commandUnmountShare, commandMountShare, commandRun, commandUnmountShare)
    
    taskTemplate = {
        "Id":0,
        "CommandLine":None,
        "Node":None,
        "CustomizedData":None,
    }

    tasks = []
    id = 1
    for node in nodelist:
        task = copy.deepcopy(taskTemplate)
        task["Id"] = id
        if mode != 'Parallel'.lower() and id != 1:
            task["ParentIds"] = [id-1]
        id += 1
        task["Node"] = node
        task["CustomizedData"] = nodeSize[node]
        task["CommandLine"] = "{} && {} && {}".format(commandGetLocation, commandInstallByNode[node], commandRun)
        tasks.append(task)

    print(json.dumps(tasks))

if __name__ == '__main__':
    main()
