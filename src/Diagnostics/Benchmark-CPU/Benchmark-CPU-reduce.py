#v0.4

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
            output = taskResult['Message']
            taskDetail[taskId]['Output'] = output
    except Exception as e:
        printErrorAsJson('Failed to parse task results. ' + str(e))
        return -1

    htmlRows = []
    for task in taskDetail.values():
        totalNumber, totalTime, coreNumber, clock = parseTaskOutput(task['Output'])
        task['Result'] = '{:.2f}'.format(totalNumber/totalTime) if totalNumber and totalTime else None
        task['CoreNumber'] = coreNumber
        task['Clock'] = clock
        htmlRows.append(
            '\n'.join([
                '  <tr>',
                '\n'.join(['    <td>{}</td>'.format(task[column]) for column in ['Node', 'Size', 'CoreNumber', 'Clock', 'Result']]),
                '  </tr>'
                ]))
        del task['Output']

    description = "The result for each node is the number of times per second that calculating the prime number less than 10000."
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
<h2>Benchmark CPU</h2>
<table>
  <tr>
    <th>Node</th>
    <th>Size</th>
    <th>Cores</th>
    <th>Freq(Mhz)</th>
    <th>Result</th>
  </tr>
''' + '\n'.join(htmlRows) + '''
</table>
<p>''' + description + '''</p>
</body>
</html>
'''
    
    result = {
        'Title': 'Benchmark-CPU',
        'Description': description,
        'Results': list(taskDetail.values()),
        'Html': html
        }

    print(json.dumps(result, indent = 4))
    return 0

def parseTaskOutput(raw):
    totalTime = totalNumber = coreNumber = clock = None
    try:
        lines = raw.split('\n')
        for line in lines:
            if 'total time:' in line:
                totalTime = float(line.split(' ')[-1][:-1])
            if 'total number of events:' in line:
                totalNumber = int(line.split(' ')[-1])
            if 'Number of threads:' in line:
                coreNumber = int(line.split(' ')[-1])
            if 'CPU MHz:' in line:
                clock = float(line.split(' ')[-1])
    except:
        pass
    return (totalNumber, totalTime, coreNumber, clock)

def printErrorAsJson(errormessage):
    print(json.dumps({"Error":errormessage}))

if __name__ == '__main__':
    sys.exit(main())
