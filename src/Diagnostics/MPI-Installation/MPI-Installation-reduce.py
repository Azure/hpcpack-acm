#v0.1

import sys, json

def main():
    result = json.load(sys.stdin)
    job = result['Job']
    tasks = result['Tasks']
    taskResults = result['TaskResults']
    nodes = job['TargetNodes']
    
    results = {}
    try:
        for taskResult in taskResults:
            result = 'Installation Failed'
            if taskResult['ExitCode'] == 0:
                if 'Already installed' in taskResult['Message'].split('\n', 1)[0]:
                    result = 'Already installed'
                else:
                    result = 'Installation succeeded'
            elif taskResult['ExitCode'] == 124:
                result = 'Downloading timeout'
            results[taskResult['NodeName']] = result
    except Exception as e:
        printErrorAsJson('Failed to parse task result. ' + str(e))
        return -1

    htmlRows = []
    for node in results:
        htmlRows.append(
            '\n'.join([
                '  <tr>',
                '\n'.join(['    <td>{}</td>'.format(column) for column in [node, results[node]]]),
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
<h2>Intel MPI Installation</h2>
<table>
  <tr>
    <th>Node</th>
    <th>Result</th>
  </tr>
''' + '\n'.join(htmlRows) + '''
</table>
</body>
</html>
'''
    
    result = {
        "Html" : html,
        "Result" : results
        }
    
    print(json.dumps(result))
    return 0

def printErrorAsJson(errormessage):
    print(json.dumps({"Error":errormessage}))

if __name__ == '__main__':
    sys.exit(main())
