#v0.2

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

    nodeOS = {}
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
                nodeOS[node] = "ubuntu"
            elif "CentOS".lower() in distroInfo.lower():
                nodeOS[node] = "centos"
            elif "Redhat".lower() in distroInfo.lower():
                nodeOS[node] = "redhat"
            else:
                nodeOS[node] = "other"
        except Exception as e:
            raise Exception("Can not retrive distroInfo from NodeRegistrationInfo of node {0}. Exception: {1}".format(node, e))

    tempMountPoint = "/mnt/hpc_diag_benchmark_cifs_share"
    tempDir = "hpc_diag_benchmark_cifs_tmp"
    tempFileName = "hpc_diag_benchmark_cifs_`hostname`"
    commandGetLocation = "(curl --header 'metadata: true' --connect-timeout 5 http://169.254.169.254/metadata/instance?api-version=2017-08-01 2>/dev/null | tr ',' '\n' | grep location || :) && "
    commandInstallCifsUtilsOnUbuntu = "(apt install -y cifs-utils || apt update && apt install -y cifs-utils) >/dev/null 2>&1 && "
    commandInstallCifsUtilsOnSuse = "zypper install -y >/dev/null 2>&1 && "
    commandInstallCifsUtilsOnOthers = "yum install -y cifs-utils >/dev/null 2>&1 && "
    commandMountShare = "mkdir -p {} && ".format(tempMountPoint) + connectionString.replace("[mount point]", tempMountPoint)
    commandUnmountShare = "(umount -l {} >/dev/null 2>&1 && rm -rf {} || :)".format(tempMountPoint, tempMountPoint)
    commandCopyLocal = "dd if=/dev/zero of=/tmp/{} bs=1M count=100".format(tempFileName)
    mountDir = mountPoint if mountPoint else tempMountPoint
    commandCopyToCifs = "mkdir -p {0}/{1} && dd if=/tmp/{2} of={0}/{1}/{2}".format(mountDir, tempDir, tempFileName)
    commandCopyFromCifs = "dd if={0}/{1}/{2} of=/tmp/{2}".format(mountDir, tempDir, tempFileName)
    commandCleanup = "rm -f /tmp/{} && rm -f {}/{}/{}".format(tempFileName, mountDir, tempDir, tempFileName)
    commandRun = "{} && {} && {} && {} || {}".format(commandCopyLocal, commandCopyToCifs, commandCopyFromCifs, commandCleanup, commandCleanup)
    if not mountPoint:
        commandRun = "{} && {} && {} && {} || {}".format(commandUnmountShare, commandMountShare, commandRun, commandUnmountShare, commandUnmountShare)
    commandDetectDistroAndRun = ("cat /etc/*release >distroInfo && "
                                 "if cat distroInfo | grep -Fiq 'Ubuntu'; then ({});"
                                 "elif cat distroInfo | grep -Fiq 'Suse'; then ({});"
                                 "elif cat distroInfo | grep -Fiq 'CentOS'; then ({});"
                                 "elif cat distroInfo | grep -Fiq 'Redhat'; then ({});"
                                 "elif cat distroInfo | grep -Fiq 'Red Hat'; then ({});"
                                 "fi").format(commandInstallCifsUtilsOnUbuntu + commandRun, 
                                              commandInstallCifsUtilsOnSuse + commandRun,
                                              commandInstallCifsUtilsOnOthers + commandRun,
                                              commandInstallCifsUtilsOnOthers + commandRun,
                                              commandInstallCifsUtilsOnOthers + commandRun)
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
        task["CommandLine"] = commandGetLocation
        if nodeOS[node] == "ubuntu":
            task["CommandLine"] += commandInstallCifsUtilsOnUbuntu + commandRun
        elif nodeOS[node] == "centos":
            task["CommandLine"] += commandInstallCifsUtilsOnOthers + commandRun
        elif nodeOS[node] == "redhat":
            task["CommandLine"] += commandInstallCifsUtilsOnOthers + commandRun
        else:
            task["CommandLine"] += commandDetectDistroAndRun
        tasks.append(task)

    print(json.dumps(tasks))

if __name__ == '__main__':
    main()
