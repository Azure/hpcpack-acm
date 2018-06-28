import subprocess, io, json

def main():
    stdout = subprocess.check_output('mpi-pingpong-map.py', shell=True, stdin=open('unittest-map-stdin', 'r'), stderr=subprocess.STDOUT)
        
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
    
