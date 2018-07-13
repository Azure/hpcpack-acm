import subprocess, io, json, os

def main():
    runOnWindows = ''
    runOnLinux = 'python '
    if os.name == 'nt':
        prefix = runOnWindows
    else:
        prefix = runOnLinux

    script = 'mpi-ring-map.py'
    stdout = subprocess.check_output(prefix + script, shell=True, stdin=open('unittest-map-stdin', 'r'), stderr=subprocess.STDOUT)
        
    taskTemplateItems = [
        "Id",
        "CommandLine",
        "Node",
        "UserName",
        "PrivateKey",
        "CustomizedData",
    ]
        
    tasks = json.loads(stdout)
    if not tasks:
        print 'Fail: no task.'
        return
    for task in tasks:
        for item in taskTemplateItems:
            if item not in task:
                print 'Fail: no {0} in task.'.format(item)
                return

    print 'Pass'
    return

if __name__ == '__main__':
    main()
    
