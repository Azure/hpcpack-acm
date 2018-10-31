#v0.2

import sys, json

def main():
    stdin = json.load(sys.stdin)
    job = stdin['Job']
    tasks = stdin['Tasks']
    taskResults = stdin['TaskResults']

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
        'Standard_F72s_v2': '72 * 16 * 2.7'
        }
    
    htmlRows = []
    for task in taskDetail.values():
        perf, n = parseTaskOutput(task['Output'])
        theoreticalPerf = theoreticalPerfBySize.get(task['Size'])
        task['Perf'] = perf
        task['N'] = n
        task['TheoreticalPerf'] = "{} = {}".format(theoreticalPerf, eval(theoreticalPerf)) if theoreticalPerf else None
        task['Efficiency'] = "{:.1%}".format(perf/eval(theoreticalPerf)) if perf and theoreticalPerf else None
        htmlRows.append(
            '\n'.join([
                '  <tr>',
                '\n'.join(['    <td>{}</td>'.format(task[column]) for column in ['Node', 'Size', 'TheoreticalPerf', 'Perf', 'N', 'Efficiency']]),
                '  </tr>'
                ]))
    
    description = 'The table shows the result of running <a href="https://software.intel.com/en-us/mkl-linux-developer-guide-intel-optimized-linpack-benchmark-for-linux">Intel Optimized LINPACK Benchmark</a> on each node.'
    nodesWithoutIntelMklInstalled = 'Intel MKL are not installed on node{}: {}'.format('' if len(nodesWithoutIntelMklInstalled) == 1 else 's', ', '.join(nodesWithoutIntelMklInstalled)) if nodesWithoutIntelMklInstalled else ''
    installIntelMkl = 'Diagnostics test "Linpack-Installation" can be used to install Intel MKL.' if nodesWithoutIntelMklInstalled else ''
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
<p>''' + description + '''</p>
<p>''' + nodesWithoutIntelMklInstalled + '''</p>
<p>''' + installIntelMkl + '''</p>
</body>
</html>
'''
    
    result = {
        'Title': 'Linpack-Standalone',
        'Description': description,
        'Results': list(taskDetail.values()),
        'Html': html
        }

    print(json.dumps(result, indent = 4))
    return 0

def parseTaskOutput(raw):
    bestPerf = n = None
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
    except:
        pass
    return (bestPerf, n)

def printErrorAsJson(errormessage):
    print(json.dumps({"Error":errormessage}))

if __name__ == '__main__':
    sys.exit(main())
