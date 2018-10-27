#v0.1

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

    try:
        for taskResult in taskResults:
            taskId = taskResult['TaskId']
            if taskId % 2 == 0:
                output = taskResult['Message']
                taskDetail[taskId]['Output'] = output
    except Exception as e:
        printErrorAsJson('Failed to parse task results. ' + str(e))
        return -1

    htmlRows = []
    for task in taskDetail.values():
        task['Perf'], task['N'] = parseTaskOutput(task['Output'])
        htmlRows.append(
            '\n'.join([
                '  <tr>',
                '\n'.join(['    <td>{}</td>'.format(task[column]) for column in ['Node', 'Size', 'Perf', 'N']]),
                '  </tr>'
                ]))
    
    description = 'The table shows the result of running <a href="https://software.intel.com/en-us/mkl-linux-developer-guide-intel-optimized-linpack-benchmark-for-linux">Intel Optimized LINPACK Benchmark</a> on each node.'
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
    <th>Best performance(GFlop/s)</th>
    <th>Problem size</th>
  </tr>
''' + '\n'.join(htmlRows) + '''
</table>
<p>''' + description + '''</p>
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
