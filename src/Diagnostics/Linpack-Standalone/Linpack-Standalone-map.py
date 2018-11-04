#v0.3

import sys, json, copy, uuid

def main():
    stdin = json.load(sys.stdin)
    job = stdin["Job"]
    nodesInfo = stdin["Nodes"]
    nodelist = job["TargetNodes"]

    if len(nodelist) != len(set(nodelist)):
        # Duplicate nodes
        raise Exception('Duplicate nodes')

    intelMklLocation = '/opt/intel/compilers_and_libraries_2018/linux/mkl'
    sizeLevel = 10
    if 'DiagnosticTest' in job and 'Arguments' in job['DiagnosticTest']:
        arguments = job['DiagnosticTest']['Arguments']
        if arguments:
            for argument in arguments:                    
                if argument['name'].lower() == 'Intel MKL location'.lower():
                    intelMklLocation = argument['value']
                    continue
                if argument['name'].lower() == 'Size level'.lower():
                    sizeLevel = int(argument['value'])
                    continue

    if not 1 <= sizeLevel <= 15:
        raise Exception("Parameter Size level should be in 1 ~ 15.")

    commandInstallNumactlOnUbuntu = "apt install -y numactl"
    commandInstallNumactlOnSuse = "zypper install -y numactl"
    commandInstallNumactlOnOthers = "yum install -y numactl"
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
                commandInstallByNode[node] = commandInstallNumactlOnUbuntu
            elif "CentOS".lower() in distroInfo.lower():
                commandInstallByNode[node] = commandInstallNumactlOnOthers
            elif "Redhat".lower() in distroInfo.lower():
                commandInstallByNode[node] = commandInstallNumactlOnOthers
            else:
                commandInstallByNode[node] = commandDetectDistroAndInstall
        except Exception as e:
            raise Exception("Can not retrive distroInfo from NodeRegistrationInfo of node {0}. Exception: {1}".format(node, e))

    commandModify = "sed -i 's/.*# number of tests/{} # number of tests/' lininput_xeon64".format(sizeLevel)
    commandRunLinpack = "cd {}/benchmarks/linpack && {} && ./runme_xeon64".format(intelMklLocation, commandModify)

    taskTemplate = {
        "Id":0,
        "CommandLine":None, # r"printf '\n\n1\n40000\n40000\n1\n1' | /opt/intel/mkl/benchmarks/linpack/xlinpack_xeon64",
        "Node":None,
        "CustomizedData":None,
        "MaximumRuntimeSeconds":999999        
    }

    # Bug: the output of task is empty when it runs Linpack with large problem size, thus start another task to collect the output
    tempOutputDir = "/tmp/hpc_diag_linpack_standalone"
    commandCreateTempOutputDir = "mkdir -p {}".format(tempOutputDir)

    tasks = []
    id = 1
    for node in nodelist:
        outputFile = "{}/{}".format(tempOutputDir, uuid.uuid4())
        commandClearFile = "rm -f {}".format(outputFile)
        task = copy.deepcopy(taskTemplate)
        task["Id"] = id
        task["Node"] = node
        task["CustomizedData"] = nodeSize[node]
        task["CommandLine"] = "{} && {} && {} && {} 2>&1 | tee {}".format(commandClearFile, commandCreateTempOutputDir, commandInstallByNode[node], commandRunLinpack, outputFile)
        tasks.append(task)
        task = copy.deepcopy(task)
        task["ParentIds"] = [id]
        task["Id"] += 1
        task["CommandLine"] = "cat {} && {}".format(outputFile, commandClearFile)
        tasks.append(task)
        id += 2

    print(json.dumps(tasks))

if __name__ == '__main__':
    main()
