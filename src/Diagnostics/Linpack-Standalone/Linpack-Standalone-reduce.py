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

    results = list(taskDetail.values())
    htmlRows = []
    for task in results:
        perf, N = parseTaskOutput(task['Output'])
        theoreticalPerfExpr = theoreticalPerfBySize.get(task['Size'])
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
    theoreticalPerfDescription = "The theoretical peak performance of each node is calculated by: [core count of node] * [(double-precision) floating-point operations per cycle] * [average frequency of core]" if any([task['TheoreticalPerf'] for task in results]) else None
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
