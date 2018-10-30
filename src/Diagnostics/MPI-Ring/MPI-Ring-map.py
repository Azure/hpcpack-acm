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

    intelMpiLocation = '/opt/intel/impi/2018.4.274'
    try:
        if 'DiagnosticTest' in job and 'Arguments' in job['DiagnosticTest']:
            arguments = job['DiagnosticTest']['Arguments']
            if arguments:
                for argument in arguments:                    
                    if argument['name'].lower() == 'Intel MPI location'.lower():
                        intelMpiLocation = argument['value']
                        continue
    except Exception as e:
        printErrorAsJson('Failed to parse arguments. ' + str(e))
        return -1

    rdmaVmSizes = set([size.lower() for size in ["Standard_H16r", "Standard_H16mr", "Standard_A8", "Standard_A9"]])
    normalNodes = set()
    rdmaNodes = set()
    for nodeInfo in nodesInfo:
        node = nodeInfo["Node"]
        try:
            if nodeInfo["Metadata"]["compute"]["vmSize"].lower() in rdmaVmSizes:
                rdmaNodes.add(node)
            else:
                normalNodes.add(node)
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
                rdmaNodes.add(node)
            else:
                normalNodes.add(node)
        except:
            raise Exception("Neither VM size from Metadata nor IsIB from NodeRegistrationInfo of node {0} could be found.".format(node))


    for node in nodelist:
        if node.lower() not in rdmaNodes | normalNodes:
            raise Exception("Missing node info: {0}".format(node))

    commandLine = "source {0}/intel64/bin/mpivars.sh && [mpicommand] >stdout 2>stderr && cat stdout | tail -n29 | head -n25 || (errorcode=$? && cat stdout stderr && exit $errorcode)".format(intelMpiLocation)
    privateKey = "-----BEGIN RSA PRIVATE KEY-----\nMIIEowIBAAKCAQEA06bdmM5tU/InWfakBnAltIA2WEvuZ/3qFwaT4EkmgJuEITxi+3NnXn7JfW+q\n6ezBc4lx6J0EuPggDIcslbczyz65QrB2NoH7De1PiRtWNWIonQDZHTYCbnaU3f/Nzsoj62lgfkSf\nUj4Osxd0yHCuGsfCtKMDES3d55RMUdVwbrXPL8jUqA9zh4miV9eX0dh/+6pCqPD7/dnOCy/rYtXs\nwgdjKG57O6eaT3XxiuozP00E5tZ7wF0fzzXBuA2Z21Sa2U42sOeeNuOvOuKQkrIzprhHhpDik31m\nHZK47F7eF2i7j/0ImedOFdgA1ETPPKFLSGspvf1xbHgEgGz1kjFq/QIEAAEAAQKCAQABHZ2IW741\n7RKWsq6J3eIBzKRJft4J7G3tvJxW8e3nOpVNuSXEbUssu/HUOoLdVVhuHPN/1TUgf69oXTtiRIVc\nLIPNwcrGRGHwaP0JJKdY4gallLFMCB9i5FkhnJXbaiJvq+ndoqnnAPLf9GfVDqhV5Jqc8nxeDZ2T\nks037GobtfMuO5WeCyTAMzc7tDIsn0HGyV0pSa7JFHAKorUuBMNnjEM+SBL37AqwcVFkstC4YD3I\n7j4miRE3loxPmBJs5HMTV4jpAGNbNmrPzfrmP4swHNoc9LR7YKpfzVpAzb24QY82fewvOxRZH6Hz\nBVhueJZAGV62JbBeaw9eeujDp+UBAoGBAN6IanPN/wqdqQ0i5dBwPK/0Mcq6bNtQt6n8rHD/C/xL\nFuhuRhLPI6q7sYPeSZu84EjyArLMR1o0GW90Ls4JzIxjxGCBgdUHG8YdmB9jNIjR5notYQcRNxaw\nwLuc5nurPt7QaxvqO3JcaDbw9c6q9c7xNE3Wlak4xxKeiXsWyHQdAoGBAPN7hpqISKIc+8dPc5kg\nuJDDPdFcJ8py0nbysEYtY+hUaDxfw7Cm8zrNj+M9BbQR9yM6EW16P0FQ+/0XBrLMVpRkyJZ0Y3Ij\n5Qol5IxJPyWzfj6e7cd9Rkvqs2sQcBehXCbQHjfpB12yu3excQBPT0Lr5gei7yfc+D21hGWDH1xh\nAoGAM2lm1qxf4O790HggiiB0FN6g5kpdvemPFSm4GT8DYN1kRHy9mbjbb6V/ZIzliqJ/Wrr23qIN\nVgy1V6eK7LUc2c5u3zDscu/6fbH2pEHCMF32FoIHaZ+Tj510WaPtJ+MvWkDijgd2hnxM42yWDZI3\nygC16cnKt9bTPzz7XEGuPA0CgYBco2gQTcAM5igpqiIaZeezNIXFrWF6VnubRDUrTkPP9qV+KxWC\nldK/UczoMaSE4bz9Cy/sTnHYwR5PKj6jMrnSVhI3pGrd16hiVw6BDbFX/9YNr1xa5WAkrFS9bJCp\nfPxZzB9jOGdUEBfhr4KGEqbemHB6AVUq/pj4qaKJGP2KoQKBgFt7cqr8t+0zU5ko+B2pxmMprsUx\nqZ3mBATMD21AshxWxsawpqoPJJ3NTScNrjISERQ6RG3lQNv+30z79k9Fy+5FUH4pvqU4sgmtYSVH\nM4xW+aJnEhzIbE7a4an2eTc0pAxc9GexwtCFwlBouSW6kfOcMgEysoy99wQoGNgRHY/D\n-----END RSA PRIVATE KEY-----"
    taskTemplateOrigin = {
        "Id":0,
        "CommandLine":commandLine,
        "Node":None,
        "UserName":"hpc_diagnostics",
        "Password":None,
        "PrivateKey":privateKey,
        "CustomizedData":None,
        "MaximumRuntimeSeconds":60
    }

    # Create task for every node to run Intel MPI Benchmark - Sendrecv among processors which forms a periodic communication chain within each node.
    # Ssh keys will also be created by these tasks for mutual trust which is necessary to run the following tasks
    mpicommand = "mpirun -env I_MPI_SHM_LMT=shm [rdmaOption] IMB-MPI1 sendrecv"
    rdmaOption = " -env I_MPI_FABRICS=shm:dapl -env I_MPI_DAPL_PROVIDER=ofa-v2-ib0 -env I_MPI_DYNAMIC_CONNECTION=0 -env I_MPI_FALLBACK_DEVICE=0"
    taskTemplate = copy.deepcopy(taskTemplateOrigin)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[mpicommand]", mpicommand)

    taskId = 1
    tasks = []
    for node in nodelist:
        task = copy.deepcopy(taskTemplate)
        task["Id"] = taskId
        taskId += 1
        task["Node"] = node
        task["CommandLine"] = task["CommandLine"].replace("[rdmaOption]", rdmaOption if node in rdmaNodes else "")
        task["CustomizedData"] = "[RDMA] {}".format(node) if node in rdmaNodes else node
        tasks.append(task)


    # Create task to run MPI Benchmark - Sendrecv among processors, one processor per node, which forms a periodic communication chain in selected nodes.
    if len(nodelist) > 1 and (len(rdmaNodes) == 0 or len(normalNodes) == 0):
        if len(rdmaNodes) == 0:
            rdmaOption = ""
        nodes = ','.join(nodelist)
        nodesCount = str(len(nodelist))
        mpicommand = "mpirun -hosts " + nodes + rdmaOption + " -ppn 1 IMB-MPI1 -npmin " + nodesCount + " sendrecv"
        taskTemplate = copy.deepcopy(taskTemplateOrigin)
        taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[mpicommand]", mpicommand)
        task = copy.deepcopy(taskTemplate)
        task["Id"] = taskId
        taskId += 1
        task["ParentIds"] = list(range(1, len(nodelist)+1))
        task["Node"] = nodelist[0]
        task["CustomizedData"] = "[RDMA] {}".format(nodes) if len(normalNodes) == 0 else nodes
        task["EnvironmentVariables"] = {"CCP_NODES":nodesCount + " " + " 1 ".join(nodelist) + " 1"}
        tasks.append(task)
    
    print(json.dumps(tasks))
    
def printErrorAsJson(errormessage):
    print(json.dumps({"Error":errormessage}))

if __name__ == '__main__':
    main()
