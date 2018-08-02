#v0.1

import sys, json, copy

def main():
    stdin = json.load(sys.stdin)
    job = stdin["Job"]
    nodesInfo = stdin["Nodes"]
    nodelist = job["TargetNodes"]

    if len(nodelist) != len(set(nodelist)):
        # Duplicate nodes
        raise Exception('Duplicate nodes')

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
            else:
                nodeOS[node] = "other"
        except Exception as e:
            raise Exception("Can not retrive distroInfo from NodeRegistrationInfo of node {0}. Exception: {1}".format(node, e))

    commandInstallSysbenchOnUbuntu = "apt install -y sysbench >/dev/null 2>&1 && "
    commandInstallSysbenchOnCentos = "yum install -y epel-release >/dev/null 2>&1 && yum install -y sysbench >/dev/null 2>&1 && "
    commandLine = "sysbench --test=cpu --num-threads=`grep -c ^processor /proc/cpuinfo` run >output 2>&1 && cat output"
    #commandSysbenchV0 = "sysbench --test=cpu --num-threads=`grep -c ^processor /proc/cpuinfo` run"
    #commandSysbenchV1 = "sysbench cpu --threads=`grep -c ^processor /proc/cpuinfo` run"
    #commandLine = "if [[ $(sysbench --version) = *'sysbench 0.'* ]]; then {}; else {}; fi".format(commandSysbenchV0, commandSysbenchV1)
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
        if nodeOS[node] == "ubuntu":
            task["CommandLine"] = commandInstallSysbenchOnUbuntu + commandLine
        elif nodeOS[node] == "centos":
            task["CommandLine"] = commandInstallSysbenchOnCentos + commandLine
        else:
            raise Exception("The OS distribution version of node {0} is neither ubuntu nor centos.".format(node))
        tasks.append(task)

    print(json.dumps(tasks))

if __name__ == '__main__':
    main()
