import sys, requests, time, random, argparse, traceback, json, os

def main(cluster, category, command, result, name, cancel, timeout):
    REQUEST_TIMEOUT = 60
    
    # format uri string
    while cluster[-1] == '/':
        cluster = cluster[:-1]
    print "[Target Uri]: {0}".format(cluster)

    # check if the cluster is available for access
    try:
        api = "{0}/v1/nodes".format(cluster)
        response = requests.get(api, timeout = REQUEST_TIMEOUT)
    except Exception as e:
        print "[Fail]: Cluster is not available."
        print e
        time.sleep(60)
        return 'Fail'

    # get healthy nodes in the cluster
    nodes = response.json()
    healthynodes = [node["id"] for node in nodes if node["health"] == "OK"]
    if len(healthynodes) < 2:
        print "[Warn]: Healthy nodes count is less than 2."
        time.sleep(60)
        return 'Warn'

    # pick nodes randomly
    while True:
        randomNodes = [ node for node in healthynodes if random.random() < 0.5 ]
        if len(randomNodes) > 1:
            break
    print '[Allocated Nodes]: {}'.format(randomNodes)

    # check clusrun or diagnostics job
    nodesCount = len(randomNodes)
    if category == 'clusrun':
        postContent = {
            "name":name,
            "commandLine":command,
            "targetNodes":randomNodes
            }
        validateTaskCount = nodesCount
        print '[Command]: {}'.format(command)
    elif category == 'diagnostics':
        postContent = {
            "name":name,
            "type":"diagnostics",
            "diagnosticTest":{
                "name": "pingpong",
                "category": "mpi",
            },
            "targetNodes":randomNodes
        }
        validateTaskCount = nodesCount + nodesCount*(nodesCount-1)/2
    else:
        raise Exception('Invalid category: {0}'.format(category))

    jobCanceled = False
    # create the job
    try:
        api = "{0}/v1/{1}".format(cluster, category)
        response = requests.post(api, json=postContent, timeout = REQUEST_TIMEOUT)
        if response:
            jobUri = cluster + response.headers['Location']
            startTime = time.time()
            print "{}: Job created at {}".format(time.ctime(), jobUri)
            count = 1
            while True:
                api = jobUri
                response = requests.get(api, timeout = REQUEST_TIMEOUT)
                if response:
                    jobState = response.json()['state']
                    if jobState == 'Finished' or jobState == 'Failed' or jobState == 'Canceled':
                        break
                    if count%30 == 0:
                        print "{}: Job state is {}".format(time.ctime(), jobState)
                        api = '{}/tasks'.format(jobUri)
                        response = requests.get(api, timeout = REQUEST_TIMEOUT)
                        if response:
                            tasks = response.json()
                            taskStates = [task['state'] for task in tasks]
                            print '{}: Tasks states are {}'.format(time.ctime(), {state:taskStates.count(state) for state in taskStates})
                        
                    # cancel the job
                    if cancel and not jobCanceled and startTime+cancel < time.time():
                        api = jobUri
                        response = requests.patch(api, json={"Request":"cancel"}, timeout = REQUEST_TIMEOUT)
                        if response:
                            print '{}: Job canceled.'.format(time.ctime())
                            jobCanceled = True
                        else:
                            print 'Patch {}: {}'.format(api, response)
                            if response.content:
                                print response.content
                        api = jobUri
                        response = requests.get(api, timeout = REQUEST_TIMEOUT)
                        if response:
                            jobState = response.json()['state']
                            if jobState != 'Canceled' and jobState != 'Canceling':
                                if jobCanceled:
                                    print '[Fail]: job state "{}" is not correct, expecting "Canceling" or "Canceled".'.format(jobState)
                                    return 'Fail'
                                if jobState == 'Finished' or jobState == 'Finishing':
                                    print '[Warn]: failed to cancel the {} job.'.format(jobState)
                                    return 'Warn'
                                else:
                                    print '[Fail]: failed to cancel the {} job.'.format(jobState)
                                    return 'Fail'
                        else:
                            print 'Get {}: {}'.format(api, response)
                elif count%100 == 0:
                    print 'Get {}: {}'.format(api, response)
                time.sleep(1)
                count += 1

                # job time out
                if timeout and time.time() > timeout:
                    print '{}: Timeout.'.format(time.ctime())
                    api = jobUri
                    response = requests.patch(api, json={"Request":"cancel"}, timeout = REQUEST_TIMEOUT)
                    if response:
                        print '{}: Job canceled.'.format(time.ctime())
                    else:
                        print 'Patch {}: {}'.format(api, response)
                        if response.content:
                            print response.content
                    return 'Timeout'
            print "{}: Job end, state is {}, run time is {:.0f}s.".format(time.ctime(), jobState, time.time()-startTime)
            if jobState == 'Failed':
                print '[Warn]: job failed.'
                return 'Warn'
            if not cancel and jobState == 'Canceled':
                print '[Fail]: the job was canceled unexpectedly.'
                return 'Fail'
        else:
            print 'Post {}: {}'.format(api, response)
            print '[Fail]: failed to create job.'
            return 'Fail'
    except Exception as e:
        print '[Exception]: {0}'.format(e)
        return 'Exception'

    # validate result
    if jobState == 'Finished':
        print 'Validating job result...'
        try:
            api = "{0}/tasks".format(jobUri)
            response = requests.get(api, timeout = REQUEST_TIMEOUT)
            if response:
                tasks = response.json()
                if len(tasks) != validateTaskCount:
                    print '[Fail]: tasks count {0} is not correct, expecting {1}.'.format(len(tasks), validateTaskCount)
                    return 'Fail'
                nodes = set(randomNodes)
                for task in tasks:
                    taskId = task["id"]
                    taskState = task['state']
                    if taskState != 'Finished' and taskState != 'Failed':
                        print '[Fail]: task {0} state {1} is not correct.'.format(taskId, taskState)
                        return 'Fail'
                    if task['node'] not in nodes:
                        print '[Fail]: node {0} of task {1} is not in allocated nodes list.'.format(task['node'], taskId)
                        return 'Fail'
            else:
                print 'Get {}: {}'.format(api, response)
                print '[Fail]: failed to get task info of job.'
                return 'Fail'

            for taskId in [task["id"] for task in tasks]:
                api = "{}/tasks/{}".format(jobUri, taskId)
                response = requests.get(api, timeout = REQUEST_TIMEOUT)
                if response:
                    jobId = int(jobUri.split('/')[-1])
                    if category == 'clusrun':
                        taskValidation = [("jobId", jobId), ("id", taskId), ("jobType", "ClusRun"), ("state", "Finished"), ("commandLine", command)]
                    elif category == 'diagnostics':
                        taskValidation = [("jobId", jobId), ("id", taskId), ("jobType", "Diagnostics")]
                    task = response.json()
                    for key,value in taskValidation:
                        if key not in task:
                            print '[Fail]: no "{}" in {}.'.format(key, api)
                            return 'Fail'
                        if task[key] != value:
                            print '[Fail]: the value "{}" of "{}" in {} is not correct, expecting "{}".'.format(task[key], key, api, value)
                            return 'Fail'
                    if task['node'] not in nodes:
                        print '[Fail]: node {0} of task {1} is not in allocated nodes list.'.format(task['node'], taskId)
                        return 'Fail'                  
                else:
                    print 'Get {}: {}'.format(api, response)
                    print '[Fail]: failed to get task.'
                    return 'Fail'
                
                api = "{}/tasks/{}/result".format(jobUri, taskId)
                response = requests.get(api, timeout = REQUEST_TIMEOUT)
                if response:
                    jobId = int(jobUri.split('/')[-1])
                    task = response.json()
                    if category == 'clusrun':
                        if command == 'hostname':
                            result = task['nodeName'] + '\n'
                        taskValidation = [("jobId", jobId), ("taskId", taskId), ("commandLine", command), ("message", result), ("resultKey", None)]
                    elif category == 'diagnostics':
                        taskValidation = [("jobId", jobId), ("taskId", taskId), ]
                    for key,value in taskValidation:
                        if key not in task:
                            print '[Fail]: no "{}" in {}.'.format(key, api)
                            return 'Fail'                            
                        if value and task[key] != value:
                            print '[Fail]: the value "{}" of "{}" in {} is not correct, expecting "{}".'.format(task[key], key, api, value)
                            return 'Fail'
                    if task['nodeName'] not in nodes:
                        print '[Fail]: node {0} of task {1} is not in allocated nodes list.'.format(task['node'], taskId)
                        return 'Fail'
                    if category == 'clusrun':
                        resultKey = task['resultKey']
                        api = "{}/v1/output/clusrun/{}/raw".format(cluster, resultKey)
                        response = requests.get(api, timeout = REQUEST_TIMEOUT)
                        if response:
                            if result and response.content != result:
                                print '[Fail]: clusrun task result "{}" in {} is not correct, expecting "()"'.format(response.content, api, result)
                                return 'Fail'
                        else:
                            print 'Get {}: {}'.format(api, response)
                            print '[Fail]: failed to get clusrun task result.'
                            return 'Fail'                        
                        api = "{}/v1/output/clusrun/{}/page".format(cluster, resultKey)
                        response = requests.get(api, timeout = REQUEST_TIMEOUT)
                        if response:
                            if result and response.json()['content'] != result:
                                print '[Fail]: clusrun task result "{}" in {} is not correct, expecting "()"'.format(response.content, api, result)
                                return 'Fail'
                        else:
                            print 'Get {}: {}'.format(api, response)
                            print '[Fail]: failed to get clusrun task result.'
                            return 'Fail'                        
                else:
                    print 'Get {}: {}'.format(api, response)
                    print '[Fail]: failed to get task result.'
                    return 'Fail'
                                
            if category == 'diagnostics':
                api = "{0}".format(jobUri)
                response = requests.get(api, timeout = REQUEST_TIMEOUT)
                if response and 'aggregationResult' in response.json():
                    results = json.loads(response.json()['aggregationResult'])
                else:
                    api = "{0}/aggregationresult".format(jobUri)
                    response = requests.get(api, timeout = REQUEST_TIMEOUT)
                    if response:
                        results = response.json()
                    else:
                        print 'Get {}: {}'.format(api, response)
                        print '[Fail]: failed to get aggregation result.'
                        return 'Fail'                            
                if 'Error' in results:
                    print '[Warn]: aggregation result: {0}'.format(results['Error'])
                    return 'Warn'
                else:
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
                    for item in resultItems:
                        if item not in results:
                            print '[Fail]: no {0} in aggregation result.'.format(item)
                            return 'Fail'
                    if nodes != set(results['GoodNodes'] + results['BadNodes']):
                        print '[Fail]: nodes are not correct in aggregation result.'
                        return 'Fail'
        except Exception as e:
            print '[Exception]: {0}'.format(e)
            return 'Exception'
    print '[Pass]'
    return 'Pass'
    
if __name__ == '__main__':
    def check_positive(value):
        ivalue = int(value)
        if ivalue < 0:
             raise argparse.ArgumentTypeError("{} is an invalid positive int value".format(ivalue))
        return ivalue

    parser = argparse.ArgumentParser(description='Functional test aganist clusrun, diagnostics or job cancellation')
    parser.add_argument('cluster_uri', help='Specify the cluster to test')
    parser.add_argument('-g', '--category', choices=['clusrun', 'diagnostics'], required=True, help='Choose the category of job to test')
    parser.add_argument('-m', '--command', help='Specify the command in clusrun job', default='hostname')
    parser.add_argument('-r', '--result', help='Specify the expected result of tasks in clusrun job', default=None)
    parser.add_argument('-n', '--name', help='Specify the job name', default='Functional test by chenling')
    parser.add_argument('-c', '--cancel', type=check_positive, default=0, help='Specify the time(seconds) to cancel the job')
    parser.add_argument('-t', '--continuous', type=check_positive, help='Specify the time(seconds) to run the test continuously until this time out')
    parser.add_argument('-o', '--timeout', type=check_positive, help='Specify the max time(seconds) to run the test for once')
    args = parser.parse_args()

    if args.result:
        args.result = args.result.replace('\\n', '\n')
        
    if args.continuous:
        startTime = time.time()
        endTime = startTime + args.continuous
        testResults = {
            'All':0,
            'Pass':0,
            'Fail':0,
            'Warn':0,
            'Timeout':0,
            'Exception':0
            }
        runTimes = []
        while endTime > time.time():
            startTime = time.time()
            print "[Time]: {0}".format(time.ctime())
            print "[Test Number]: {0}".format(testResults['All'])
            testResults['All'] += 1
            try:
                result = main(args.cluster_uri, args.category, args.command, args.result, args.name, args.cancel, args.timeout + time.time())
                testResults[result] += 1
            except:
                testResults['Exception'] += 1
                print(sys.exc_info()[0])
                time.sleep(60)
            runtime = time.time() - startTime
            runTimes.append(runtime)
            print '[Runtime]: {:.0f}s'.format(runtime)
            print '-'*60
        print '{}/{} {} Runtime:[Avg: {:.0f}, Min: {:.0f}, Max: {:.0f}]'.format(testResults['Pass'], testResults['All'], testResults, sum(runTimes)/len(runTimes), min(runTimes), max(runTimes))
    else:
        main(args.cluster_uri, args.category, args.command, args.result, args.name, args.cancel, 0)
