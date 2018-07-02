import subprocess, io, json

def main():
    stdout = subprocess.check_output('mpi-pingpong-reduce.py', shell=True, stdin=open('unittest-reduce-stdin', 'r'), stderr=subprocess.STDOUT)
        
    resultItems = [
        'GoodPairs',
        'GoodNodesGroups',
        'GoodNodesGroupsWithMasters',
        'LargestGoodNodesGroups',
        'LargestGoodNodesGroupsWithMasters',
        'GoodNodes',
        'BadPairs',
        'FailedNodes',
        'BadNodes'
        ]
        
    result = json.loads(stdout)
    for item in resultItems:
        if item not in result:
            print 'Fail: no {0} in result.'.format(item)
            return

    print 'Pass'
    return

if __name__ == '__main__':
    main()
    
