#v0.4

import sys, json

def main():
    stdin = json.load(sys.stdin)
    job = stdin['Job']
    tasks = stdin['Tasks']
    taskResults = stdin['TaskResults']
    nodes = job['TargetNodes']

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
        theoreticalPerfBySize = {
            # Size : Cores * DP FLOPs/cycle * Freq
            
            # Dsv3/Ev3/Esv3: 2.3(3.5) E5-2673 v4 Broadwell
            'Standard_D2s_v3': '2 * 16 * 2.3',
            'Standard_D4s_v3': '4 * 16 * 2.3',
            'Standard_D8s_v3': '8 * 16 * 2.3',
            'Standard_D16s_v3': '16 * 16 * 2.3',
            'Standard_D32s_v3': '32 * 16 * 2.3',
            'Standard_D64s_v3': '64 * 16 * 2.3',
            'Standard_E2s_v3': '2 * 16 * 2.3',
            'Standard_E4s_v3': '4 * 16 * 2.3',
            'Standard_E8s_v3': '8 * 16 * 2.3',
            'Standard_E16s_v3': '16 * 16 * 2.3',
            'Standard_E20s_v3': '20 * 16 * 2.3',
            'Standard_E32s_v3': '32 * 16 * 2.3',
            'Standard_E64s_v3': '64 * 16 * 2.3',
            'Standard_E64is_v3': '64 * 16 * 2.3',
            'Standard_E2_v3': '2 * 16 * 2.3',
            'Standard_E4_v3': '4 * 16 * 2.3',
            'Standard_E8_v3': '8 * 16 * 2.3',
            'Standard_E16_v3': '16 * 16 * 2.3',
            'Standard_E20_v3': '20 * 16 * 2.3',
            'Standard_E32_v3': '32 * 16 * 2.3',
            'Standard_E64_v3': '64 * 16 * 2.3',
            'Standard_E64i_v3': '64 * 16 * 2.3',
            
            # Dv2/Dv3/F/Fs: 2.4(3.1) E5-2673 v3 Haswell
            'Standard_D2_v3': '2 * 16 * 2.4',
            'Standard_D4_v3': '4 * 16 * 2.4',
            'Standard_D8_v3': '8 * 16 * 2.4',
            'Standard_D16_v3': '16 * 16 * 2.4',
            'Standard_D32_v3': '32 * 16 * 2.4',
            'Standard_D64_v3': '64 * 16 * 2.4',
            'Standard_DS1_v2': '1 * 16 * 2.4',
            'Standard_DS2_v2': '2 * 16 * 2.4',
            'Standard_DS3_v2': '4 * 16 * 2.4',
            'Standard_DS4_v2': '8 * 16 * 2.4',
            'Standard_DS5_v2': '16 * 16 * 2.4',
            'Standard_F1s': '1 *16 * 2.4',
            'Standard_F2s': '2 *16 * 2.4',
            'Standard_F4s': '4 *16 * 2.4',
            'Standard_F8s': '8 *16 * 2.4',
            'Standard_F16s': '16 *16 * 2.4',
            'Standard_F1': '1 *16 * 2.4',
            'Standard_F2': '2 *16 * 2.4',
            'Standard_F4': '4 *16 * 2.4',
            'Standard_F8': '8 *16 * 2.4',
            'Standard_F16': '16 *16 * 2.4',
            
            # H: 3.2(3.6) E5-2667 V3 Haswell
            'Standard_H8': '8 * 16 * 3.2',
            'Standard_H16': '16 * 16 * 3.2',
            'Standard_H8m': '8 * 16 * 3.2',
            'Standard_H16m': '16 * 16 * 3.2',
            'Standard_H16r': '16 * 16 * 3.2',
            'Standard_H16mr': '16 * 16 * 3.2',

            # Fsv2: 2.7(3.7) Platinum 8168
            'Standard_F2s_v2': '2 * 16 * 2.7',
            'Standard_F4s_v2': '4 * 16 * 2.7',
            'Standard_F8s_v2': '8 * 16 * 2.7',
            'Standard_F16s_v2': '16 * 16 * 2.7',
            'Standard_F32s_v2': '32 * 16 * 2.7',
            'Standard_F64s_v2': '64 * 16 * 2.7',
            'Standard_F72s_v2': '72 * 16 * 2.7',

            # Above VM sizes info(vCPUs, microarchitecture, frequency) were extracted from "Sizes for Linux virtual machines in Azure"(https://docs.microsoft.com/en-us/azure/virtual-machines/linux/sizes) at 2018.11.8
            # Below VM sizes info(vCPUs, microarchitecture, frequency) were extracted from "CPU(s)" and "Model name" in result of running command "lscpu" on the VMs
            
            # G/Gs: 2.0 E5-2698B v3 Haswell
            'Standard_G1': '2 * 16 * 2.0',
            'Standard_G2': '4 * 16 * 2.0',
            'Standard_G3': '8 * 16 * 2.0',
            'Standard_G4': '16 * 16 * 2.0',
            'Standard_G5': '32 * 16 * 2.0',
            'Standard_GS1': '2 * 16 * 2.0',
            'Standard_GS2': '4 * 16 * 2.0',
            'Standard_GS3': '8 * 16 * 2.0',
            'Standard_GS4': '16 * 16 * 2.0',
            'Standard_GS5': '32 * 16 * 2.0'
            }

        sizeByNode = {}
        try:
            for task in tasks:
                node = task['Node']
                size = task['CustomizedData']
                if size:
                    sizeByNode[node] = size
        except Exception as e:
            printErrorAsJson('Failed to parse tasks. ' + str(e))
            return -1

        theoreticalPerf = None
        theoreticalPerfDescription = ''
        nodeSizes = set(sizeByNode.values())
        if len(nodeSizes) > 1:
            vmSizeDescription = 'The cluster is heterogeneous with node sizes: {}. Optimal perfermance may not be achieved in this test.'.format(', '.join(nodeSizes))
            missingSizeInfo = [size for size in nodeSizes if not theoreticalPerfBySize.get(size)]
            if missingSizeInfo:
                theoreticalPerfDescription = "The theoretical peak performance of the cluster can not be calculated because no info is found for node size{}: {}".format('s' if len(missingSizeInfo) > 1 else '', ', '.join(missingSizeInfo))
            else:
                sizes = [sizeByNode[node] for node in nodes]
                theoreticalPerf = " + ".join(["{} * {}".format(sizes.count(size), theoreticalPerfBySize[size]) for size in nodeSizes])
                theoreticalPerfDescription = "The theoretical peak performance of the cluster is <b>{}</b> GFlop/s, which is calculated as the sum of FLOPs of each node: SUM([core count per node] * [(double-precision) floating-point operations per cycle] * [average frequency of core]) = {}".format(eval(theoreticalPerf), theoreticalPerf)
        elif len(nodeSizes) == 1:
            size = list(nodeSizes)[0]
            vmSizeDescription = 'The cluster is homogeneous with node size: {}.'.format(size)
            theoreticalPerf = theoreticalPerfBySize.get(size)
            if theoreticalPerf:
                theoreticalPerf = "{} * {}".format(len(nodes), theoreticalPerf)
                theoreticalPerfDescription = "The theoretical peak performance of the cluster is <b>{}</b> GFlop/s, which is calculated by: [node count] * [core count per node] * [(double-precision) floating-point operations per cycle] * [average frequency of core] = {}".format(eval(theoreticalPerf), theoreticalPerf)
            else:
                theoreticalPerfDescription = "The theoretical peak performance of the cluster can not be calculated because no info is found for node size: {}".format(size)
        else:
            vmSizeDescription = 'The node size in the cluster is unknown.'

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
                        efficiency = perf/eval(theoreticalPerf) if theoreticalPerf else None
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
'''.format('\n'.join(htmlRows), descriptionInHtml, vmSizeDescription, theoreticalPerfDescription)
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
                    state = 'Unknown state'
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
