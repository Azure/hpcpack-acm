#v0.5

import sys, json

def main():
    stdin = json.load(sys.stdin)
    job = stdin['Job']
    nodelist = job["TargetNodes"]
    tasks = stdin['Tasks']
    taskResults = stdin['TaskResults']
    
    intelMklLocation = '/opt/intel/compilers_and_libraries_2018/linux/mkl'
    try:
        if 'DiagnosticTest' in job and 'Arguments' in job['DiagnosticTest']:
            arguments = job['DiagnosticTest']['Arguments']
            if arguments:
                for argument in arguments:                    
                    if argument['name'].lower() == 'Intel MKL location'.lower():
                        intelMklLocation = argument['value']
                        continue
    except Exception as e:
        printErrorAsJson('Failed to parse arguments. ' + str(e))
        return -1
                
    taskDetail = {}
    try:
        for task in tasks:
            taskId = task['Id']
            if taskId % 2 == 0:
                node = task['Node']
                size = task['CustomizedData']
                taskDetail[taskId] = {
                    'Node': node,
                    'Size': size
                    }
    except Exception as e:
        printErrorAsJson('Failed to parse tasks. ' + str(e))
        return -1

    nodesWithoutIntelMklInstalled = []
    try:
        for taskResult in taskResults:
            taskId = taskResult['TaskId']
            nodeName = taskResult['NodeName']
            output = taskResult['Message']
            if taskId % 2 == 0:
                taskDetail[taskId]['Output'] = output
            elif 'benchmarks/linpack: No such file or directory' in output:
                nodesWithoutIntelMklInstalled.append(nodeName)
    except Exception as e:
        printErrorAsJson('Failed to parse task results. ' + str(e))
        return -1

    defaultFlopsPerCycle = 16 # Use this default value because currently it seems that the Intel microarchitectures used in Azure VM are in "Intel Haswell/Broadwell/Skylake/Kaby Lake". Consider getting this value from test parameter in case new Azure VM sizes are introduced.

    results = list(taskDetail.values())
    htmlRows = []
    for task in results:
        perf, N, coreCount, coreFreq = parseTaskOutput(task['Output'])
        theoreticalPerfExpr = "{} * {} * {}".format(coreCount, defaultFlopsPerCycle, coreFreq) if coreCount and coreFreq else None
        theoreticalPerf = eval(theoreticalPerfExpr) if theoreticalPerfExpr else None
        efficiency = perf / theoreticalPerf if perf and theoreticalPerf else None
        task['TheoreticalPerf'] = theoreticalPerf
        task['Perf'] = perf
        task['N'] = N
        task['Efficiency'] = efficiency
        theoreticalPerfInHtml = "{} = {}".format(theoreticalPerfExpr, theoreticalPerf) if theoreticalPerfExpr else None
        perfInHtml = "{:.1f}".format(perf) if perf else None
        efficiencyInHtml = "{:.1%}".format(efficiency) if efficiency else None
        htmlRows.append(
            '\n'.join([
                '  <tr>',
                '\n'.join(['    <td>{}</td>'.format(item) for item in [task['Node'], task['Size'], theoreticalPerfInHtml, perfInHtml, N, efficiencyInHtml]]),
                '  </tr>'
                ]))
        del task['Output']
    
    description = 'This is the result of running {} on each node.'
    intelLinpack = 'Intel Optimized LINPACK Benchmark'
    intelLinpackWithLink = '<a href="https://software.intel.com/en-us/mkl-linux-developer-guide-intel-optimized-linpack-benchmark-for-linux">{}</a>'.format(intelLinpack)
    descriptionInHtml = '<p>{}</p>'.format(description.format(intelLinpackWithLink))
    theoreticalPerfDescription = "The theoretical peak performance of each node is calculated by: [core count of node] * [(double-precision) floating-point operations per cycle] * [average frequency of core]" if any([task['TheoreticalPerf'] for task in results]) else ''
    intelMklNotFound = 'Intel MKL is not found in <b>{}</b> on node{}: {}'.format(intelMklLocation, '' if len(nodesWithoutIntelMklInstalled) == 1 else 's', ', '.join(nodesWithoutIntelMklInstalled)) if nodesWithoutIntelMklInstalled else ''
    installIntelMkl = 'Diagnostics test <b>Linpack-Installation</b> can be used to install Intel MKL.' if nodesWithoutIntelMklInstalled else ''
    specifyIntelMklLocation = 'Set the parameter <b>Intel MKL location</b> to specify the location of Intel MKL if it is already installed.' if len(nodesWithoutIntelMklInstalled) == len(nodelist) else ''
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
<h2>Linpack standalone</h2>
<table>
  <tr>
    <th>Node</th>
    <th>Node size</th>
    <th>Theoretical peak performance(GFlop/s)</th>
    <th>Best performance(GFlop/s)</th>
    <th>Problem size</th>
    <th>Efficiency</th>
  </tr>
''' + '\n'.join(htmlRows) + '''
</table>
<p>''' + descriptionInHtml + '''</p>
<p>''' + theoreticalPerfDescription + '''</p>
<p>''' + intelMklNotFound + '''</p>
<p>''' + installIntelMkl + '''</p>
<p>''' + specifyIntelMklLocation + '''</p>
</body>
</html>
'''
    description = description.format(intelLinpack)
    result = {
        'Title': 'Linpack-Standalone',
        'Description': description,
        'Results': results,
        'Html': html
        }

    print(json.dumps(result, indent = 4))
    return 0

def parseTaskOutput(raw):
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
        cpuInfo = raw.split('\n', 2)[:2]
        if len(cpuInfo) == 2:
            firstLine = cpuInfo[0]
            secondLine = cpuInfo[1]
            if firstLine.startswith('CPU') and secondLine.startswith('Model name'):
                coreCount = int(firstLine.split()[-1])
                coreFreq = float([word for word in secondLine.split() if word.endswith('GHz')][0][:-3])
    except:
        pass
    return (bestPerf, n, coreCount, coreFreq)

def printErrorAsJson(errormessage):
    print(json.dumps({"Error":errormessage}))

if __name__ == '__main__':
    sys.exit(main())
