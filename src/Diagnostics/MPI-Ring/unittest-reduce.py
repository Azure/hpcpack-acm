import subprocess, json

def main():
    script = 'python mpi-ring-reduce.py'
    stdout = subprocess.check_output(script, shell=True, stdin=open('unittest-reduce-stdin', 'r'), stderr=subprocess.STDOUT)
        
    resultItems = [
        "Nodes",
        "Passed",
        "Latency",
        "Throughput"
        ]
        
    result = json.loads(stdout)
    for item in resultItems:
        if item not in result:
            print('Fail: no {0} in result.'.format(item))
            return

    print('Pass')
    return

if __name__ == '__main__':
    main()
    
