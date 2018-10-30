#v0.4

import sys, json, copy

def main():
    stdin = json.load(sys.stdin)
    job = stdin["Job"]
    nodesInfo = stdin["Nodes"]
    nodelist = job["TargetNodes"]

    if len(nodelist) != len(set(nodelist)):
        # Duplicate nodes
        raise Exception('Duplicate nodes')

    commandInstallSysbenchOnUbuntu = "apt install -y sysbench || apt update && apt install -y sysbench"
    commandInstallSysbenchOnSuse = "zypper install -y sysbench"
    commandInstallSysbenchOnCentos = "yum install -y epel-release && yum install -y sysbench"
    commandInstallSysbenchOnRedhat = "curl -s https://packagecloud.io/install/repositories/akopytov/sysbench/script.rpm.sh | bash && yum install -y sysbench"
    commandInstallQuiet = "({}) >/dev/null 2>&1"
    commandInstallSysbenchOnUbuntu = commandInstallQuiet.format(commandInstallSysbenchOnUbuntu)
    commandInstallSysbenchOnSuse = commandInstallQuiet.format(commandInstallSysbenchOnSuse)
    commandInstallSysbenchOnCentos = commandInstallQuiet.format(commandInstallSysbenchOnCentos)
    commandInstallSysbenchOnRedhat = commandInstallQuiet.format(commandInstallSysbenchOnRedhat)
    commandDetectDistroAndInstall = ("cat /etc/*release > distroInfo && "
                                 "if cat distroInfo | grep -Fiq 'Ubuntu'; then {};"
                                 "elif cat distroInfo | grep -Fiq 'Suse'; then {};"
                                 "elif cat distroInfo | grep -Fiq 'CentOS'; then {};"
                                 "elif cat distroInfo | grep -Fiq 'Redhat'; then {};"
                                 "elif cat distroInfo | grep -Fiq 'Red Hat'; then {};"
                                 "fi").format(commandInstallSysbenchOnUbuntu, 
                                              commandInstallSysbenchOnSuse,
                                              commandInstallSysbenchOnCentos,
                                              commandInstallSysbenchOnRedhat,
                                              commandInstallSysbenchOnRedhat)    
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
                commandInstallByNode[node] = commandInstallSysbenchOnUbuntu
            elif "CentOS".lower() in distroInfo.lower():
                commandInstallByNode[node] = commandInstallSysbenchOnCentos
            elif "Redhat".lower() in distroInfo.lower():
                commandInstallByNode[node] = commandInstallSysbenchOnRedhat
            else:
                commandInstallByNode[node] = commandDetectDistroAndInstall
        except Exception as e:
            raise Exception("Can not retrive distroInfo from NodeRegistrationInfo of node {0}. Exception: {1}".format(node, e))

    commandRunLscpu = "lscpu"
    commandRunSysbench = "sysbench --test=cpu --num-threads=`grep -c ^processor /proc/cpuinfo` run >output 2>&1 && cat output"

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
        id += 1
        task["Node"] = node
        task["CustomizedData"] = nodeSize[node]
        task["CommandLine"] = "{} && {} && {}".format(commandRunLscpu, commandInstallByNode[node], commandRunSysbench)
        tasks.append(task)

    print(json.dumps(tasks))

if __name__ == '__main__':
    main()
