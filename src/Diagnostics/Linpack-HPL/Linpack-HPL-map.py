#v0.2

import sys, json, copy, uuid, math

def main():
    stdin = json.load(sys.stdin)
    job = stdin["Job"]
    nodesInfo = stdin["Nodes"]
    nodelist = job["TargetNodes"]

    if len(nodelist) != len(set(nodelist)):
        # Duplicate nodes
        raise Exception('Duplicate nodes')

    memoryPercentage = 0.1
    intelMklLocation = '/opt/intel/compilers_and_libraries_2018/linux/mkl'
    intelMpiLocation = '/opt/intel/compilers_and_libraries_2018/linux/mpi'
    debug = 0
    if 'DiagnosticTest' in job and 'Arguments' in job['DiagnosticTest']:
        arguments = job['DiagnosticTest']['Arguments']
        if arguments:
            for argument in arguments:                    
                if argument['name'].lower() == 'Memory Limit'.lower():
                    memoryPercentage = float(argument['value'])
                    continue
                if argument['name'].lower() == 'Intel MKL location'.lower():
                    intelMklLocation = argument['value']
                    continue
                if argument['name'].lower() == 'Intel MPI location'.lower():
                    intelMpiLocation = argument['value']
                    continue
                if argument['name'].lower() == 'Debug'.lower():
                    debug = int(argument['value'])

    minCoreCount = float('inf')
    minMemoryMb = float('inf')
    nodeSize = {}
    for nodeInfo in nodesInfo:
        node = nodeInfo["Node"]
        try:
            nodeSize[node] = json.loads(nodeInfo["Metadata"])["compute"]["vmSize"]
        except:
            raise Exception("Failed to extract node info from Metadata of node {0}.".format(node))
        try:
            coreCount = int(nodeInfo["NodeRegistrationInfo"]["CoreCount"])
            if coreCount < minCoreCount:
                minCoreCount = coreCount
            memoryMb = int(nodeInfo["NodeRegistrationInfo"]["MemoryMegabytes"])
            if memoryMb < minMemoryMb:
                minMemoryMb = memoryMb
        except:
            raise Exception("Failed to parse node info from NodeRegistrationInfo of node {0}.".format(node))

    rdmaVmSizes = set([size.lower() for size in ["Standard_H16r", "Standard_H16mr", "Standard_A8", "Standard_A9"]])
    rdmaVmCount = sum(size.lower() in rdmaVmSizes for size in nodeSize.values())
    normalVmCount = len(nodeSize) - rdmaVmCount
    if 0 < rdmaVmCount < len(nodeSize):
        raise Exception("Can not run the test in the cluster consisting of {} RDMA node{} and {} normal node{}.".format(rdmaVmCount, 's' if rdmaVmCount > 1 else '', normalVmCount, 's' if normalVmCount > 1 else ''))

    rdmaOption = ""
    if rdmaVmCount:
        rdmaOption = "-env I_MPI_FABRICS=shm:dapl -env I_MPI_DAPL_PROVIDER=ofa-v2-ib0 -env I_MPI_DYNAMIC_CONNECTION=0 -env I_MPI_FALLBACK_DEVICE=0"
    
    privateKey = "-----BEGIN RSA PRIVATE KEY-----\nMIIEowIBAAKCAQEA06bdmM5tU/InWfakBnAltIA2WEvuZ/3qFwaT4EkmgJuEITxi+3NnXn7JfW+q\n6ezBc4lx6J0EuPggDIcslbczyz65QrB2NoH7De1PiRtWNWIonQDZHTYCbnaU3f/Nzsoj62lgfkSf\nUj4Osxd0yHCuGsfCtKMDES3d55RMUdVwbrXPL8jUqA9zh4miV9eX0dh/+6pCqPD7/dnOCy/rYtXs\nwgdjKG57O6eaT3XxiuozP00E5tZ7wF0fzzXBuA2Z21Sa2U42sOeeNuOvOuKQkrIzprhHhpDik31m\nHZK47F7eF2i7j/0ImedOFdgA1ETPPKFLSGspvf1xbHgEgGz1kjFq/QIEAAEAAQKCAQABHZ2IW741\n7RKWsq6J3eIBzKRJft4J7G3tvJxW8e3nOpVNuSXEbUssu/HUOoLdVVhuHPN/1TUgf69oXTtiRIVc\nLIPNwcrGRGHwaP0JJKdY4gallLFMCB9i5FkhnJXbaiJvq+ndoqnnAPLf9GfVDqhV5Jqc8nxeDZ2T\nks037GobtfMuO5WeCyTAMzc7tDIsn0HGyV0pSa7JFHAKorUuBMNnjEM+SBL37AqwcVFkstC4YD3I\n7j4miRE3loxPmBJs5HMTV4jpAGNbNmrPzfrmP4swHNoc9LR7YKpfzVpAzb24QY82fewvOxRZH6Hz\nBVhueJZAGV62JbBeaw9eeujDp+UBAoGBAN6IanPN/wqdqQ0i5dBwPK/0Mcq6bNtQt6n8rHD/C/xL\nFuhuRhLPI6q7sYPeSZu84EjyArLMR1o0GW90Ls4JzIxjxGCBgdUHG8YdmB9jNIjR5notYQcRNxaw\nwLuc5nurPt7QaxvqO3JcaDbw9c6q9c7xNE3Wlak4xxKeiXsWyHQdAoGBAPN7hpqISKIc+8dPc5kg\nuJDDPdFcJ8py0nbysEYtY+hUaDxfw7Cm8zrNj+M9BbQR9yM6EW16P0FQ+/0XBrLMVpRkyJZ0Y3Ij\n5Qol5IxJPyWzfj6e7cd9Rkvqs2sQcBehXCbQHjfpB12yu3excQBPT0Lr5gei7yfc+D21hGWDH1xh\nAoGAM2lm1qxf4O790HggiiB0FN6g5kpdvemPFSm4GT8DYN1kRHy9mbjbb6V/ZIzliqJ/Wrr23qIN\nVgy1V6eK7LUc2c5u3zDscu/6fbH2pEHCMF32FoIHaZ+Tj510WaPtJ+MvWkDijgd2hnxM42yWDZI3\nygC16cnKt9bTPzz7XEGuPA0CgYBco2gQTcAM5igpqiIaZeezNIXFrWF6VnubRDUrTkPP9qV+KxWC\nldK/UczoMaSE4bz9Cy/sTnHYwR5PKj6jMrnSVhI3pGrd16hiVw6BDbFX/9YNr1xa5WAkrFS9bJCp\nfPxZzB9jOGdUEBfhr4KGEqbemHB6AVUq/pj4qaKJGP2KoQKBgFt7cqr8t+0zU5ko+B2pxmMprsUx\nqZ3mBATMD21AshxWxsawpqoPJJ3NTScNrjISERQ6RG3lQNv+30z79k9Fy+5FUH4pvqU4sgmtYSVH\nM4xW+aJnEhzIbE7a4an2eTc0pAxc9GexwtCFwlBouSW6kfOcMgEysoy99wQoGNgRHY/D\n-----END RSA PRIVATE KEY-----"
    taskTemplateOrigin = {
        "Id":0,
        "CommandLine":None,
        "Node":None,
        "UserName":"hpc_diagnostics",
        "Password":None,
        "PrivateKey":privateKey,
        "CustomizedData":None,
    }
    
    taskId = 1
    tasks = []
    masterNode = nodelist[0]
    nodes = ','.join(nodelist)
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
    commandCreateHplDat = 'echo "{}" >HPL.dat'.format(hplDateFile)
    commandSourceMpiEnv = 'source {}/intel64/bin/mpivars.sh'.format(intelMpiLocation)
    commandRunHpl = "mpirun -hosts {} {} -ppn {} {}/benchmarks/mp_linpack/xhpl_intel64_dynamic -n [N] -b [NB] -p [P] -q [Q]".format(nodes, rdmaOption, minCoreCount, intelMklLocation)
    
    # Create task to run Intel HPL locally to ensure every node is ready
    # Ssh keys will also be created by these tasks for mutual trust which is necessary to run the following tasks
    commandCheckHpl = '{}/benchmarks/mp_linpack/xhpl_intel64_dynamic >/dev/null && echo MKL test succeed.'.format(intelMklLocation)
    commandCheckMpi = 'mpirun IMB-MPI1 pingpong >/dev/null && echo MPI test succeed.'
    taskTemplate = copy.deepcopy(taskTemplateOrigin)
    taskTemplate["CommandLine"] = "{}; {}; {} && {}".format(commandCreateHplDat, commandSourceMpiEnv, commandCheckHpl, commandCheckMpi)
    taskTemplate["MaximumRuntimeSeconds"] = 60
    for node in nodelist:
        task = copy.deepcopy(taskTemplate)
        task["Id"] = taskId
        taskId += 1
        task["Node"] = node
        task["CustomizedData"] = nodeSize[node]
        tasks.append(task)

    hplWorkingDir = "/tmp/hpc_diag_linpack_hpl"
    flagDir = "{}/{}.{}".format(hplWorkingDir, job["Id"], uuid.uuid4())
    nodesCount = len(nodelist)
    threadsCount = minCoreCount * nodesCount

    # Create task to run Intel HPL with Intel MPI among the nodes to ensure cluster integrity
    N = 10000
    NB = 192
    P = 1
    Q = threadsCount
    tmpOutputFile = "{}.failed".format(flagDir)
    commandCreateHplWorkingDir = "mkdir -p {}".format(hplWorkingDir)
    commandClearFlagDir = "rm -f {}".format(flagDir)
    commandRun = "{} >{} 2>&1".format(commandRunHpl.replace('[N]', str(N)).replace('[P]', str(P)).replace('[Q]', str(Q)).replace('[NB]', str(NB)), tmpOutputFile)
    commadnClearTmp = "rm -f {}".format(tmpOutputFile)
    commandCreateFlagDir = "mkdir -p {}".format(flagDir)
    commandSuccess = "echo Cluster is ready."
    commandFailure = "echo Cluster is not ready. && cat {} && exit -1".format(tmpOutputFile)
    task = copy.deepcopy(taskTemplateOrigin)
    task["Id"] = taskId
    taskId += 1
    task["CommandLine"] = "{} && {} && {} && {} && {} && {} && {} && {} || ({})".format(commandCreateHplWorkingDir,
                                                                                        commandClearFlagDir,
                                                                                        commandCreateHplDat,
                                                                                        commandSourceMpiEnv,
                                                                                        commandRun,
                                                                                        commadnClearTmp,
                                                                                        commandCreateFlagDir,
                                                                                        commandSuccess,
                                                                                        commandFailure)
    task["ParentIds"] = list(range(1, len(nodelist)+1))
    task["Node"] = masterNode
    task["CustomizedData"] = nodeSize[masterNode]
    task["EnvironmentVariables"] = {"CCP_NODES":"{} {}".format(nodesCount, " ".join("{} 1".format(node) for node in nodelist))} 
    task["MaximumRuntimeSeconds"] = 60
    tasks.append(task)

    # Create HPL tunning tasks
    PQs = [(p, threadsCount//p) for p in range(1, int(math.sqrt(threadsCount)) + 1) if p * (threadsCount//p) == threadsCount]
    NBs = [192, 256]
    memoryRange = [float(memory) / 10 for memory in list(range(int(memoryPercentage*10), 700, -10)) + list(range(int(memoryPercentage*10), 500, -20)) + list(range(int(memoryPercentage*10), 0, -50))]
    Ns = [int(math.sqrt(float(nodesCount) / 8 * 1024 * 1024 * minMemoryMb * percent / 100)) for percent in memoryRange]
    commandCheckFlag = '[ -d "{}" ]'.format(flagDir)
    for P, Q in PQs:
        for NB in NBs:
            outputPrefix = "{}/{}x{}.{}".format(flagDir, P, Q, NB)
            outputResult = "{}.result".format(outputPrefix)
            for N in Ns:
                # N = N // NB * NB # this would decrease perf instead of increases
                if debug:
                    N = debug
                tempOutputFile = "{}.{}".format(outputPrefix, N)
                commandCheckFinish = '[ ! -f "{}" ]'.format(outputResult)
                commandRun = "{} >{} 2>&1".format(commandRunHpl.replace('[N]', str(N)).replace('[P]', str(P)).replace('[Q]', str(Q)).replace('[NB]', str(NB)), tempOutputFile)
                commandSuccess = "mv {} {} && cat {} | tail -n20".format(tempOutputFile, outputResult, outputResult)
                commandFailure = "echo Test skiped. N={} NB={} P={} Q={}".format(N, NB, P, Q)
                task = copy.deepcopy(taskTemplateOrigin)
                task["Id"] = taskId
                taskId += 1
                task["CommandLine"] = "{} && {} && {} && {} && {} && {} || {}".format(commandCheckFlag,
                                                                                      commandCheckFinish,
                                                                                      commandCreateHplDat,
                                                                                      commandSourceMpiEnv,
                                                                                      commandRun,
                                                                                      commandSuccess,
                                                                                      commandFailure)
                task["ParentIds"] = [task["Id"] - 1]
                task["Node"] = masterNode
                task["CustomizedData"] = nodeSize[masterNode]
                task["EnvironmentVariables"] = {"CCP_NODES":"{} {}".format(nodesCount, " ".join("{} 1".format(node) for node in nodelist))} 
                task["MaximumRuntimeSeconds"] = 36000 if rdmaVmCount else 3600 # need to change in terms of cluster size?
                tasks.append(task)
    
    # Create result collecting task
    task = copy.deepcopy(taskTemplateOrigin)
    task["Id"] = taskId
    taskId += 1
    task["CommandLine"] = "{} && for file in $(ls {}/*.result); do cat $file | tail -n17 | head -n1; done".format(commandCheckFlag, flagDir)
    task["ParentIds"] = [task["Id"] - 1]
    task["Node"] = masterNode
    task["CustomizedData"] = nodeSize[masterNode]
    task["MaximumRuntimeSeconds"] = 60
    tasks.append(task)

    print(json.dumps(tasks))

if __name__ == '__main__':
    main()
