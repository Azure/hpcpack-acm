#v0.5

import sys, json

def main():
    stdin = json.load(sys.stdin)
    job = stdin['Job']
    tasks = stdin['Tasks']
    taskResults = stdin['TaskResults']
    nodes = job['TargetNodes']

    if len(tasks) > len(taskResults):
        resultMissingTasks = set([task['Id'] for task in tasks]) - set([task['TaskId'] for task in taskResults])
        printErrorAsJson('No result of tasks: {}'.format(resultMissingTasks))
        return -1

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

    intelMklLocation = '/opt/intel/compilers_and_libraries_2018/linux/mkl'
    intelMpiLocation = '/opt/intel/compilers_and_libraries_2018/linux/mpi'
    try:
        if 'DiagnosticTest' in job and 'Arguments' in job['DiagnosticTest']:
            arguments = job['DiagnosticTest']['Arguments']
            if arguments:
                for argument in arguments:                    
                    if argument['name'].lower() == 'Intel MKL location'.lower():
                        intelMklLocation = argument['value']
                        continue
                    if argument['name'].lower() == 'Intel MPI location'.lower():
                        intelMpiLocation = argument['value']
                        continue
    except Exception as e:
        printErrorAsJson('Failed to parse arguments. ' + str(e))
        return -1

    nodeCheckingTasks = [taskDetail[taskId] for taskId in range(1, len(nodes) + 1)]
    flagTask = taskDetail[len(nodes) + 1]
    description = 'This is the result of running {} in the cluster.'
    intelHpl = 'Intel Distribution for LINPACK Benchmark'
    result = None
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
                        size += "({}GHz)".format(freq)
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
            vmSizeDescription = 'The cluster is heterogeneous with node sizes: {}. Optimal perfermance may not be achieved in this test.'.format(', '.join(nodeSizes))
            sizes = [sizeByNode[node] for node in nodes]
            theoreticalPerfExpr = " + ".join(["{} * {} * {} * {}".format(sizes.count(size), cpuCoreBySize.get(size), defaultFlopsPerCycle, cpuFreqBySize.get(size)) for size in nodeSizes])
            if 'None' not in theoreticalPerfExpr:
                theoreticalPerf = eval(theoreticalPerfExpr)
                theoreticalPerfDescription = "The theoretical peak performance of the cluster is <b>{}</b> GFlop/s, which is calculated by summing the FLOPs of each node: SUM([core count per node] * [(double-precision) floating-point operations per cycle] * [average frequency of core]) = {}".format(theoreticalPerf, theoreticalPerfExpr)
        elif len(nodeSizes) == 1:
            size = list(nodeSizes)[0]
            vmSizeDescription = 'The cluster is homogeneous with node size: {}.'.format(size)
            theoreticalPerfExpr = "{} * {} * {} * {}".format(len(nodes), cpuCoreBySize.get(size), defaultFlopsPerCycle, cpuFreqBySize.get(size))
            if 'None' not in theoreticalPerfExpr:
                theoreticalPerf = eval(theoreticalPerfExpr)
                theoreticalPerfDescription = "The theoretical peak performance of the cluster is <b>{}</b> GFlop/s, which is calculated by: [node count] * [core count per node] * [(double-precision) floating-point operations per cycle] * [average frequency of core] = {}".format(theoreticalPerf, theoreticalPerfExpr)
        else:
            vmSizeDescription = 'The node size in the cluster is unknown.'

        # warning for Hyper-Threading
        hyperThreadingDescription = 'Optimal perfermance may not be achieved in this test because Hyper-Threading is enabled on the node(s): {}'.format(', '.join(hyperThreadingNodes)) if hyperThreadingNodes else ''

        # get result from result task and generate output
        resultTask = taskDetail[len(taskDetail)]
        output = resultTask['Output']
        if '*.result: No such file or directory' in output:
            keyWord = '*.result:'
            logDir = [word for word in output.split() if word.endswith(keyWord)][0][:-(len(keyWord))]
            node = resultTask['Node']
            result = 'No result. The cluster may be too busy or has broken node(s). Check log in {} on {}'.format(logDir, node)
            htmlContent = '<p>{}</p>'.format(result)
        else:
            result = []
            output = [line.split() for line in output.splitlines()]
            htmlRows = []
            maxPerf = greenLineNumber = -1
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
                            greenLineNumber = lineNumber
                        perfInHtml = "{:.2f}".format(perf)
                        efficiencyInHtml = "{:.2%}".format(efficiency) if efficiency else None
                        htmlRows.append(
                            '\n'.join([
                                '  <tr>',
                                '\n'.join(['    <td>{}</td>'.format(item) for item in row[1:-1] + [perfInHtml, efficiencyInHtml]]),
                                '  </tr>'
                                ]))
                        lineNumber += 1
                    except:
                        pass
            if greenLineNumber >= 0:
                htmlRows[greenLineNumber] = htmlRows[greenLineNumber].replace('<tr>', '<tr bgcolor="#d8fcd4">')
            intelHplWithLink = '<a href="https://software.intel.com/en-us/mkl-linux-developer-guide-overview-of-the-intel-distribution-for-linpack-benchmark">{}</a>'.format(intelHpl)
            descriptionInHtml = '<p>{}</p>'.format(description.format(intelHplWithLink))
            htmlContent = '''
<table>
  <tr>
    <th>Problem size(N)</th>
    <th>Block size(NB)</th>
    <th>P</th>
    <th>Q</th>
    <th>Time(s)</th>
    <th>Performance(GFlop/s)</th>
    <th>Efficiency</th>
  </tr>
{}
</table>
<p>{}</p>
<p>{}</p>
<p>{}</p>
<p>{}</p>
'''.format('\n'.join(htmlRows), descriptionInHtml, vmSizeDescription, theoreticalPerfDescription, hyperThreadingDescription)
        resultCode = 0
    else:
        result = 'Cluster is not ready to run Intel HPL. Diagnostics test MPI-Pingpong may help to diagnose the cluster.'
        htmlContent = '<p>{}</p>'.format(result).replace('MPI-Pingpong', '<b>MPI-Pingpong</b>')
        resultCode = -1
        if any(task['ExitCode'] != 0 for task in nodeCheckingTasks):
            htmlRows = []
            for task in nodeCheckingTasks:
                taskStates = []
                output = task['Output']
                if 'MKL test succeed.' in output and 'MPI test succeed.' in output:
                    taskStates.append('Ready')
                if 'mpivars.sh: No such file or directory' in output:
                    taskStates.append('Intel MPI is not found in directory: {}'.format(intelMpiLocation))
                if 'xhpl_intel64_dynamic: No such file or directory' in output:
                    taskStates.append('Intel MKL is not found in directory: {}'.format(intelMklLocation))
                if 'dapl fabric is not available' in output:
                    taskStates.append('Error when running MPI test')
                state = '<br>'.join(taskStates)
                if not state:
                    state = 'Unknown'
                htmlRows.append(
                    '\n'.join([
                        '  <tr>',
                        '\n'.join(['    <td>{}</td>'.format(item) for item in [task['Node'], state]]),
                        '  </tr>'
                        ]))
            htmlContent += '''
<table>
  <tr>
    <th>Node</th>
    <th>State</th>
  </tr>
{}
</table>
'''.format('\n'.join(htmlRows))

    html = '''
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
<h2>Linpack HPL</h2>
''' + htmlContent + '''
</body>
</html>
'''
    description = description.format(intelHpl)
    result = {
        'Title': 'Linpack-HPL',
        'Description': description,
        'Results': result,
        'Html': html
        }

    print(json.dumps(result, indent = 4))
    return resultCode

def printErrorAsJson(errormessage):
    print(json.dumps({"Error":errormessage}))

if __name__ == '__main__':
    sys.exit(main())
