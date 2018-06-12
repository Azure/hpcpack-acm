import sys, json, copy

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
        if nodeInfo["Metadata"]["compute"]["vmSize"].lower() in rdmaVmSizes:
            rdmaNodes.append(nodeInfo["Node"])
        else:
            normalNodes.append(nodeInfo["Node"])
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

    taskTemplateOrigin = {
        "Id":0,
        "CommandLine":"source /opt/intel/impi/`ls /opt/intel/impi`/bin64/mpivars.sh && [mpicommand] >stdout 2>stderr && cat stdout | [parseResult] > raw && cat raw | tail -n +2 | awk '{print [columns]}' | tr ' ' '\n' | sed -n '1p;$p' > data && echo -n '{\"Latency\":' > json && cat data | head -n1 | tr -d '\n' >> json && echo -n ',\"Throughput\":' >> json && cat data | tail -n1 | tr -d '\n' >> json && echo -n ',\"Detail\":\"' >> json && cat raw | awk '{printf \"%s\\\\n\", $0}' >> json && echo -n '\"}' >> json && cat json || (errorcode=$? && echo -n '{\"Latency\":-1,\"Throughput\":-1,\"Detail\":\"' > json && cat stderr | awk '{printf \"%s\\\\n\", $0}' >> json && echo -n '\"}' >> json && cat json && exit $errorcode)",
        "Node":None,
        "UserName":"hpc_diagnostics",
        "Password":None,
        "PrivateKey":"-----BEGIN RSA PRIVATE KEY-----\nMIIEowIBAAKCAQEA06bdmM5tU/InWfakBnAltIA2WEvuZ/3qFwaT4EkmgJuEITxi+3NnXn7JfW+q\n6ezBc4lx6J0EuPggDIcslbczyz65QrB2NoH7De1PiRtWNWIonQDZHTYCbnaU3f/Nzsoj62lgfkSf\nUj4Osxd0yHCuGsfCtKMDES3d55RMUdVwbrXPL8jUqA9zh4miV9eX0dh/+6pCqPD7/dnOCy/rYtXs\nwgdjKG57O6eaT3XxiuozP00E5tZ7wF0fzzXBuA2Z21Sa2U42sOeeNuOvOuKQkrIzprhHhpDik31m\nHZK47F7eF2i7j/0ImedOFdgA1ETPPKFLSGspvf1xbHgEgGz1kjFq/QIEAAEAAQKCAQABHZ2IW741\n7RKWsq6J3eIBzKRJft4J7G3tvJxW8e3nOpVNuSXEbUssu/HUOoLdVVhuHPN/1TUgf69oXTtiRIVc\nLIPNwcrGRGHwaP0JJKdY4gallLFMCB9i5FkhnJXbaiJvq+ndoqnnAPLf9GfVDqhV5Jqc8nxeDZ2T\nks037GobtfMuO5WeCyTAMzc7tDIsn0HGyV0pSa7JFHAKorUuBMNnjEM+SBL37AqwcVFkstC4YD3I\n7j4miRE3loxPmBJs5HMTV4jpAGNbNmrPzfrmP4swHNoc9LR7YKpfzVpAzb24QY82fewvOxRZH6Hz\nBVhueJZAGV62JbBeaw9eeujDp+UBAoGBAN6IanPN/wqdqQ0i5dBwPK/0Mcq6bNtQt6n8rHD/C/xL\nFuhuRhLPI6q7sYPeSZu84EjyArLMR1o0GW90Ls4JzIxjxGCBgdUHG8YdmB9jNIjR5notYQcRNxaw\nwLuc5nurPt7QaxvqO3JcaDbw9c6q9c7xNE3Wlak4xxKeiXsWyHQdAoGBAPN7hpqISKIc+8dPc5kg\nuJDDPdFcJ8py0nbysEYtY+hUaDxfw7Cm8zrNj+M9BbQR9yM6EW16P0FQ+/0XBrLMVpRkyJZ0Y3Ij\n5Qol5IxJPyWzfj6e7cd9Rkvqs2sQcBehXCbQHjfpB12yu3excQBPT0Lr5gei7yfc+D21hGWDH1xh\nAoGAM2lm1qxf4O790HggiiB0FN6g5kpdvemPFSm4GT8DYN1kRHy9mbjbb6V/ZIzliqJ/Wrr23qIN\nVgy1V6eK7LUc2c5u3zDscu/6fbH2pEHCMF32FoIHaZ+Tj510WaPtJ+MvWkDijgd2hnxM42yWDZI3\nygC16cnKt9bTPzz7XEGuPA0CgYBco2gQTcAM5igpqiIaZeezNIXFrWF6VnubRDUrTkPP9qV+KxWC\nldK/UczoMaSE4bz9Cy/sTnHYwR5PKj6jMrnSVhI3pGrd16hiVw6BDbFX/9YNr1xa5WAkrFS9bJCp\nfPxZzB9jOGdUEBfhr4KGEqbemHB6AVUq/pj4qaKJGP2KoQKBgFt7cqr8t+0zU5ko+B2pxmMprsUx\nqZ3mBATMD21AshxWxsawpqoPJJ3NTScNrjISERQ6RG3lQNv+30z79k9Fy+5FUH4pvqU4sgmtYSVH\nM4xW+aJnEhzIbE7a4an2eTc0pAxc9GexwtCFwlBouSW6kfOcMgEysoy99wQoGNgRHY/D\n-----END RSA PRIVATE KEY-----",
        "CustomizedData":None,
    }

    rdmaOption = ""
    if isRdma:
        rdmaOption = " -env I_MPI_FABRICS=shm:dapl -env I_MPI_DAPL_PROVIDER=ofa-v2-ib0 -env I_MPI_DYNAMIC_CONNECTION=0 -env I_MPI_FALLBACK_DEVICE=0"

    # Create task for every node to run Intel MPI Benchmark - Sendrecv among processors which forms a periodic communication chain within each node.
    # Mutual trust will also be set by these tasks which is necessary to run the following tasks
    mpicommand = "mpirun -env I_MPI_SHM_LMT=shm" + rdmaOption + " IMB-MPI1 sendrecv"
    parseResult = "tail -n29 | head -n25"
    columns = "$5,$6"
    taskTemplate = copy.deepcopy(taskTemplateOrigin)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[mpicommand]", mpicommand)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[columns]", columns)
    taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[parseResult]", parseResult)

    startIndex = 100000000
    taskId = startIndex
    tasks = []
    for node in nodelist:
        task = copy.deepcopy(taskTemplate)
        task["Id"] = taskId
        taskId += 1
        task["Node"] = node
        task["CustomizedData"] = node
        tasks.append(task)

    if len(nodelist) > 1:
        # Create task to run MPI Benchmark - Sendrecv among processors, one processor per node, which forms a periodic communication chain in selected nodes.
        nodes = ','.join(nodelist)
        nodesCount = str(len(nodelist))
        mpicommand = "mpirun -hosts " + nodes + rdmaOption + " -ppn 1 IMB-MPI1 -npmin " + nodesCount + " sendrecv"
        parseResult = "tail -n29 | head -n25"
        columns = "$5,$6"
        taskTemplate = copy.deepcopy(taskTemplateOrigin)
        taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[mpicommand]", mpicommand)
        taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[parseResult]", parseResult)
        taskTemplate["CommandLine"] = taskTemplate["CommandLine"].replace("[columns]", columns)
        task = copy.deepcopy(taskTemplate)
        task["Id"] = 1
        task["ParentIds"] = list(range(startIndex, startIndex + len(nodelist)))
        task["Node"] = nodelist[0]
        task["CustomizedData"] = nodes
        task["EnvironmentVariables"] = {"CCP_NODES":nodesCount + " " + " 1 ".join(nodelist) + " 1"}
        tasks.append(task)
    
    print(json.dumps(tasks))

if __name__ == '__main__':
    main()
