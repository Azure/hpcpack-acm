#v0.7

import sys, json

def main():
    result = json.load(sys.stdin)
    job = result['Job']
    tasks = result['Tasks']
    taskResults = result['TaskResults']
    nodes = job['TargetNodes']

    hasRdmaNodes = False
    hasNormalNodes = False
    try:
        for task in tasks:
            if task['CustomizedData'].startswith('[RDMA]'):
                hasRdmaNodes = True
            else:
                hasNormalNodes = True
    except Exception as e:
        printErrorAsJson('Failed to parse tasks. ' + str(e))
        return -1

    if hasRdmaNodes and hasNormalNodes:
        printErrorAsJson('Can not run MPI Ring among RDMA nodes and non-RDMA nodes.')
        return -1
    
    message = None
    try:
        for taskResult in taskResults:
            if taskResult['TaskId'] == len(nodes)+1:
                if taskResult['ExitCode'] == 0:
                    message = taskResult['Message']
                    break
                else:
                    noresult = "No result"
                    print(json.dumps({"Html" : noresult, "Result" : noresult}))
                    return -1
    except Exception as e:
        printErrorAsJson('Failed to parse task result. ' + str(e))
        return -1

    htmlRows = []
    if message:
        data = message.split('\n')
        title = '#bytes #repetitions  t_min[usec]  t_max[usec]  t_avg[usec]   Mbytes/sec'
        if data[0] and title in data[0]:
            maxThroughput = greenLineNumber = lineNumber = 0
            for line in data[1:]:
                row = line.split()
                if len(row) != len(title.split()):
                    break
                throughput = float(row[-1])
                if throughput > maxThroughput:
                    maxThroughput = throughput
                    greenLineNumber = lineNumber
                htmlRows.append(
                    '\n'.join([
                        '  <tr>',
                        '\n'.join(['    <td>{}</td>'.format(number) for number in row]),
                        '  </tr>'
                        ]))
                lineNumber += 1
            htmlRows[greenLineNumber] = htmlRows[greenLineNumber].replace('<tr>', '<tr bgcolor="#d8fcd4">')
            
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
<h2>MPI Ring</h2>
<table>
  <tr>
    <th>Package Size (Bytes)</th>
    <th>Repetition Count</th>
    <th>Min Time (usec)</th>
    <th>Max Time (usec)</th>
    <th>Average Time (usec)</th>
    <th>Throughput (Mbytes/sec)</th>
  </tr>
''' + '\n'.join(htmlRows) + '''
</table>
</body>
</html>
'''
    
    result = {
        "Html" : html,
        "Result" : message
        }
    
    print(json.dumps(result))
    return 0

def printErrorAsJson(errormessage):
    print(json.dumps({"Error":errormessage}))

if __name__ == '__main__':
    sys.exit(main())
