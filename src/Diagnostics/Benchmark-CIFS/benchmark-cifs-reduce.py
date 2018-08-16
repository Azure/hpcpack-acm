#v0.1

import sys, json

def main():
    stdin = json.load(sys.stdin)
    job = stdin['Job']
    tasks = stdin['Tasks']
    taskResults = stdin['TaskResults']

    TASK_STATE_FINISHED = 3

    taskDetail = {}
    try:
        for task in tasks:
            taskId = task['Id']
            state = task['State']
            node = task['Node']
            size = task['CustomizedData']
            taskDetail[taskId] = {
                'State': state,
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
            exitcode = taskResult['ExitCode']
            taskDetail[taskId]['ExitCode'] = exitcode
    except Exception as e:
        printErrorAsJson('Failed to parse task results. ' + str(e))
        return -1

    htmlRows = []
    for task in taskDetail.values():
        if task['State'] == TASK_STATE_FINISHED:
            try:
                location, local, toCifs, fromCifs = parseTaskOutput(task['Output'])
            except Exception as e:
                printErrorAsJson('Failed to parse task output. ' + str(e))
                return -1
            task['Location'] = location
            task['Local'] = local
            task['ToCifs'] = toCifs
            task['FromCifs'] = fromCifs
            del task['State']
            del task['Output']
            del task['ExitCode']
            htmlRows.append(
                '\n'.join([
                    '  <tr>',
                    '\n'.join(['    <td>{}</td>'.format(task[column]) for column in ['Node', 'Size', 'Location', 'Local', 'ToCifs', 'FromCifs']]),
                    '  </tr>'
                    ]))

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
<h2>Benchmark CIFS</h2>
<table>
  <tr>
    <th>Node</th>
    <th>Size</th>
    <th>Location</th>
    <th>Local Disk</th>
    <th>To CIFS</th>
    <th>From CIFS</th>
  </tr>
''' + '\n'.join(htmlRows) + '''
</table>
<p>The benchmark shows the speed of copying file between local disk and CIFS server.</p>
</body>
</html>
'''
    
    result = {
        'Title': 'Benchmark-CIFS',
        'Description': 'The benchmark shows the speed of copying file between local disk and CIFS server.',
        'Results': list(taskDetail.values()),
        'Html': html
        }

    print(json.dumps(result, indent = 4))
    return 0

def parseTaskOutput(raw):
    location = local = toCifs = fromCifs = None
    lines = raw.split('\n')
    for line in lines:
        if 'location' in line:
            location = line.split(':')[-1][1:-1]
        if 'copied' in line:
            speed = line.split(',')[-1]
            if not local:
                local = speed
                continue
            if not toCifs:
                toCifs = speed
                continue
            if not fromCifs:
                fromCifs = speed
                continue
    return (location, local, toCifs, fromCifs)

def printErrorAsJson(errormessage):
    print(json.dumps({"Error":errormessage}))

if __name__ == '__main__':
    sys.exit(main())
