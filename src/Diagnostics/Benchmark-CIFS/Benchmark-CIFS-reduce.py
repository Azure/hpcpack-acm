#v0.3

import sys, json

def main():
    stdin = json.load(sys.stdin)
    job = stdin['Job']
    tasks = stdin['Tasks']
    taskResults = stdin['TaskResults']

    connectWay = 'Connection string'.lower()
    cifsServer = None
    try:
        if 'DiagnosticTest' in job and 'Arguments' in job['DiagnosticTest']:
            arguments = job['DiagnosticTest']['Arguments']
            if arguments:
                for argument in arguments:
                    if argument['name'].lower() == 'Connect by'.lower():
                        connectWay = argument['value'].lower()
                        continue
                    if argument['name'].lower() == 'CIFS server'.lower():
                        cifsServer = argument['value']
                        continue
    except Exception as e:
        printErrorAsJson('Failed to parse arguments. ' + str(e))
        return -1

    if connectWay == 'Connection string'.lower() and cifsServer:
        connectionString = cifsServer
        precursor = 'mount -t cifs '
        successor = ' [mount point]'
        begin = connectionString.find(precursor) + len(precursor)
        end = connectionString.find(successor)
        if 0 < begin < end:
            cifsServer = connectionString[begin:end]

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
        task['Location'], task['Local'], task['ToCifs'], task['FromCifs'] = parseTaskOutput(task['Output'])
        htmlRows.append(
            '\n'.join([
                '  <tr>',
                '\n'.join(['    <td>{}</td>'.format(task[column]) for column in ['Node', 'Size', 'Location', 'Local', 'ToCifs', 'FromCifs']]),
                '  </tr>'
                ]))
        del task['Output']

    description = "The benchmark shows the speed of copying file between local disk and CIFS server: {}.".format(cifsServer)
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
<p>''' + description + '''</p>
<p>If result is None, please check it manually following <a href="https://docs.microsoft.com/en-us/azure/storage/files/storage-how-to-use-files-linux#install-cifs-utils">Use Azure Files with Linux</a>.</p>
</body>
</html>
'''
    
    result = {
        'Title': 'Benchmark-CIFS',
        'Description': description,
        'Results': list(taskDetail.values()),
        'Html': html
        }

    print(json.dumps(result, indent = 4))
    return 0

def parseTaskOutput(raw):
    location = local = toCifs = fromCifs = None
    try:
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
    except:
        pass
    return (location, local, toCifs, fromCifs)

def printErrorAsJson(errormessage):
    print(json.dumps({"Error":errormessage}))

if __name__ == '__main__':
    sys.exit(main())
