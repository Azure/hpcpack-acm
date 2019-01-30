# python2, python3
# utf-8

import sys, requests, time, random, argparse, traceback, json, os, math, base64
from random import sample

import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

PY3 = sys.version_info[0] >= 3
REQUEST_TIMEOUT = 60
REQUEST_HEADER = {'Authorization':'Basic cm9vdDpQYXNzMXdvcmQ='}

def main(cluster, category, command, result, name, cancel, timeout, timeoutToCleanJob, pickRandom, platform):    
    # format uri string
    while cluster[-1] == '/':
        cluster = cluster[:-1]
    print("[Target Uri]: {0}".format(cluster))

    # check if the cluster is available for access
    nodes = None
    try:
        api = "{0}/v1/nodes?count=1000".format(cluster)
        response = restGet(api)
        nodes = response.json()
    except Exception as e:
        print("[Fail]: Cluster is not available.")
        print(e)
        time.sleep(60)
        return 'Fail'

    # filter healthy nodes in the cluster
    if platform == 'Mixed':
        platform = ''
    healthyNodes = [node["id"] for node in nodes if node["health"] == "OK" and platform in node['nodeRegistrationInfo']['distroInfo']]
    if not healthyNodes:
        print("[Warn]: No healthy {} nodes.".format(platform))
        time.sleep(60)
        return 'Warn'

    selectedNodes = None
    if pickRandom:
        # pick nodes randomly
        randomNodeCount = int(math.ceil(random.random()*len(healthyNodes)))
        print('[Random Node Count]: {}'.format(randomNodeCount))
        randomNodes = sample(healthyNodes, randomNodeCount)
        selectedNodes = randomNodes
    else:
        selectedNodes = healthyNodes
    print('[Allocated Nodes]: {}'.format(selectedNodes))

    # check clusrun or diagnostics job
    if category == 'clusrun':
        postContent = {
            "name":name,
            "commandLine":command,
            "targetNodes":selectedNodes
            }
        print('[Command]: {}'.format(command))
    elif category == 'diag-pingpong-tournament':
        postContent = {
            "name":name,
            "type":"diagnostics",
            "diagnosticTest":{
                "name": "Pingpong",
                "category": "MPI",
                "arguments": [{"name":"Mode", "value": "Tournament"}]
            },
            "targetNodes":selectedNodes
        }
    elif category == 'diag-pingpong-parallel':
        postContent = {
            "name":name,
            "type":"diagnostics",
            "diagnosticTest":{
                "name": "Pingpong",
                "category": "MPI",
                "arguments": [{"name":"Mode", "value": "Parallel"}]
            },
            "targetNodes":selectedNodes
        }
    elif category == 'diag-pingpong-debug':
        postContent = {
            "name":name,
            "type":"diagnostics",
            "diagnosticTest":{
                "name": "Pingpong",
                "category": "MPI",
                "arguments": [{"name":"Mode", "value": "Tournament"}, {"name":"Debug", "value": command}]
            },
            "targetNodes":selectedNodes
        }
    elif category == 'diag-cpu':
        postContent = {
            "name":name,
            "type":"diagnostics",
            "diagnosticTest":{
                "name": "CPU",
                "category": "Benchmark",
            },
            "targetNodes":selectedNodes
        }
    else:
        raise Exception('Invalid category: {0}'.format(category))

    jobCreated = False
    jobCanceled = False
    # create the job
    try:
        api = "{0}/v1/{1}".format(cluster, 'clusrun' if category == 'clusrun' else 'diagnostics')
        response = requests.post(api, json=postContent, timeout = REQUEST_TIMEOUT, headers = REQUEST_HEADER, verify = False)
        if response:
            jobUri = cluster + response.headers['Location']
            jobCreated = True
            startTime = time.time()
            print("{}: Job created at {}".format(time.ctime(), jobUri))
            count = 1
            while True:
                api = jobUri
                response = restGet(api)
                if response:
                    jobState = response.json()['state']
                    if jobState == 'Finished' or jobState == 'Failed' or jobState == 'Canceled':
                        break
                    if count%30 == 0:
                        print("{}: Job state is {}".format(time.ctime(), jobState))
                        api = '{}/tasks?count=100000'.format(jobUri)
                        response = restGet(api)
                        if response:
                            tasks = response.json()
                            taskStates = [task['state'] for task in tasks]
                            print('{}: Tasks states are {}'.format(time.ctime(), {state:taskStates.count(state) for state in taskStates}))
                        
                    # cancel the job
                    if cancel and not jobCanceled and startTime + cancel < time.time():
                        jobCanceled = cancelJob(jobUri)
                        api = jobUri
                        response = restGet(api)
                        if response:
                            jobState = response.json()['state']
                            if jobState != 'Canceled' and jobState != 'Canceling':
                                if jobCanceled:
                                    print('[Fail]: job state "{}" is not correct, expecting "Canceling" or "Canceled".'.format(jobState))
                                    return 'Fail'
                                if jobState == 'Finished' or jobState == 'Finishing':
                                    print('[Warn]: failed to cancel the {} job.'.format(jobState))
                                else:
                                    print('[Fail]: failed to cancel the {} job.'.format(jobState))
                                    return 'Fail'
                        else:
                            print('Get {}: {}'.format(api, response))
                elif count%100 == 0:
                    print('Get {}: {}'.format(api, response))
                time.sleep(1)
                count += 1

                # job time out
                if timeout and time.time() > timeout:
                    print('{}: Timeout.'.format(time.ctime()))
                    cancelJob(jobUri)
                    return 'Timeout'
            print("{}: Job end, state is {}, run time is {:.0f}s.".format(time.ctime(), jobState, time.time()-startTime))
            if jobState == 'Failed':
                print('[Warn]: job failed.')
                return 'Warn'
            if not cancel and jobState == 'Canceled':
                print('[Fail]: the job was canceled unexpectedly.')
                return 'Fail'
        else:
            print('Post {}: {}'.format(api, response))
            if response.content:
                print(response.content)
            print('[Fail]: failed to create job.')
            return 'Fail'
    except Exception as e:
        print('[Exception]: {0}'.format(e))
        while jobCreated and not jobCanceled and time.time() < timeoutToCleanJob:
            try:
                print('{}: Try to cancel job {}'.format(time.ctime(), jobUri))
                jobCanceled = cancelJob(jobUri)
                response = restGet(jobUri)
                if response:
                    jobState = response.json()['state']
                    if jobState != 'Running':
                        jobCanceled = True
                        break
            except Exception as ex:
                print('[Exception]: {0}'.format(ex))
            time.sleep(5)
        if jobCreated and not jobCanceled:
            print('{}: Cleaning up job timeout.'.format(time.ctime()))
        return 'Exception'

    # validate result
    if jobState == 'Finished':
        print('Validating job result...')
        try:
            api = "{0}/tasks".format(jobUri)
            response = restGet(api)
            if response:
                tasks = response.json()
                nodes = set(selectedNodes)
                if category == 'clusrun':
                    if len(tasks) != len(selectedNodes):
                        print('[Fail]: tasks count {0} is not correct, expecting {1}.'.format(len(tasks), validateTaskCount))
                        return 'Fail'
                    for task in tasks:
                        taskId = task["id"]
                        taskState = task['state']
                        if taskState == 'Canceled':
                            print('[Warn]: task {0} state is {1}.'.format(taskId, taskState))
                            return 'Warn'
                        if taskState != 'Finished' and taskState != 'Failed':
                            print('[Fail]: task {0} state {1} is not correct.'.format(taskId, taskState))
                            return 'Fail'
                        if task['node'] not in nodes:
                            print('[Fail]: node {0} of task {1} is not in allocated nodes list.'.format(task['node'], taskId))
                            return 'Fail'
            else:
                print('Get {}: {}'.format(api, response))
                print('[Fail]: failed to get task info of job.')
                return 'Fail'

            for taskId in [task["id"] for task in tasks]:
                api = "{}/tasks/{}".format(jobUri, taskId)
                response = restGet(api)
                if response:
                    jobId = int(jobUri.split('/')[-1])
                    if category == 'clusrun':
                        taskValidation = [("jobId", jobId), ("id", taskId), ("jobType", "ClusRun"), ("state", "Finished"), ("commandLine", command)]
                    else:
                        taskValidation = [("jobId", jobId), ("id", taskId), ("jobType", "Diagnostics")]
                    task = response.json()
                    for key,value in taskValidation:
                        if key not in task:
                            print('[Fail]: no "{}" in {}.'.format(key, api))
                            return 'Fail'
                        if task[key] != value:
                            print('[Fail]: the value "{}" of "{}" in {} is not correct, expecting "{}".'.format(task[key], key, api, value))
                            return 'Fail'
                    if task['node'] not in nodes:
                        print('[Fail]: node {0} of task {1} is not in allocated nodes list.'.format(task['node'], taskId))
                        return 'Fail'                  
                else:
                    print('Get {}: {}'.format(api, response))
                    print('[Fail]: failed to get task.')
                    return 'Fail'
                
                api = "{}/tasks/{}/result".format(jobUri, taskId)
                response = restGet(api)
                if response:
                    jobId = int(jobUri.split('/')[-1])
                    task = response.json()
                    if category == 'clusrun':
                        taskValidation = [("jobId", jobId), ("taskId", taskId), ("commandLine", command), ("message", result), ("resultKey", None)]
                    else:
                        taskValidation = [("jobId", jobId), ("taskId", taskId), ]
                    for key,value in taskValidation:
                        if key not in task:
                            print('[Fail]: no "{}" in {}.'.format(key, api))
                            return 'Fail'                            
                        if value and task[key] != value:
                            print('[Fail]: the value "{}" of "{}" in {} is not correct, expecting "{}".'.format(task[key], key, api, value))
                            return 'Fail'
                    if task['nodeName'] not in nodes:
                        print('[Fail]: node {0} of task {1} is not in allocated nodes list.'.format(task['node'], taskId))
                        return 'Fail'
                    if category == 'clusrun':
                        resultKey = task['resultKey']
                        api = "{}/v1/output/clusrun/{}/raw".format(cluster, resultKey)
                        response = restGet(api)
                        if response:
                            if result is not None and response.content.decode() != result:
                                print('[Fail]: clusrun task result "{}" in {} is not correct, expecting "{}"'.format(response.content.decode(), api, result))
                                return 'Fail'
                        else:
                            print('Get {}: {}'.format(api, response))
                            print('[Fail]: failed to get clusrun task result.')
                            return 'Fail'                        
                        api = "{}/v1/output/clusrun/{}/page".format(cluster, resultKey)
                        response = restGet(api)
                        if response:
                            if result is not None and response.json()['content'] != result:
                                print('[Fail]: clusrun task result "{}" in {} is not correct, expecting "{}"'.format(response.content, api, result))
                                return 'Fail'
                        else:
                            print('Get {}: {}'.format(api, response))
                            print('[Fail]: failed to get clusrun task result.')
                            return 'Fail'                        
                else:
                    print('Get {}: {}'.format(api, response))
                    print('[Fail]: failed to get task result.')
                    return 'Fail'
                                
            if category.startswith('diag-pingpong'):
                api = "{0}".format(jobUri)
                response = restGet(api)
                if response and 'aggregationResult' in response.json():
                    results = json.loads(response.json()['aggregationResult'])
                else:
                    api = "{0}/aggregationresult".format(jobUri)
                    response = restGet(api)
                    if response:
                        results = response.json()
                    else:
                        print('Get {}: {}'.format(api, response))
                        print('[Fail]: failed to get aggregation result.')
                        return 'Fail'                            
                if 'Error' in results:
                    print('[Warn]: aggregation result: {0}'.format(results['Error']))
                    return 'Warn'
                else:
                    resultItems = [
                        'GoodNodesGroups',
                        'GoodNodes',
                        'FailedNodes',
                        'BadNodes',
                        'RdmaNodes',
                        'FailedReasons'
                        ]
                    for item in resultItems:
                        if item not in results:
                            print('[Fail]: no {0} in aggregation result.'.format(item))
                            return 'Fail'
                    if nodes != set(results['GoodNodes'] + results['BadNodes']):
                        print('[Fail]: nodes are not correct in aggregation result.')
                        return 'Fail'
                    if len(results['GoodNodesGroups'][0]) != len(selectedNodes):
                        print('[Warn]: not all nodes are in same group.')
                        return 'Warn'
        except Exception as e:
            print('[Exception]: {0}'.format(e))
            return 'Exception'
    print('[Pass]')
    return 'Pass'

def restGet(api):
    return requests.get(api, timeout = REQUEST_TIMEOUT, headers = REQUEST_HEADER, verify = False)

def cancelJob(jobUri):
    response = requests.patch(jobUri, json={"Request":"cancel"}, timeout = REQUEST_TIMEOUT, headers = REQUEST_HEADER, verify = False)
    if response:
        print('{}: Job canceled.'.format(time.ctime()))
        return True
    else:
        print('Patch {}: {}'.format(jobUri, response))
        if response.content:
            print(response.content)
        return False

if __name__ == '__main__':
    def check_positive(value):
        ivalue = int(value)
        if ivalue < 0:
             raise argparse.ArgumentTypeError("{} is an invalid positive int value".format(ivalue))
        return ivalue

    parser = argparse.ArgumentParser(description='Functional test against clusrun, diagnostics or job cancellation')
    parser.add_argument('cluster_uri', help='Specify the cluster to test')
    parser.add_argument('-g', '--category', choices=['clusrun', 'diag-pingpong-parallel', 'diag-pingpong-tournament', 'diag-pingpong-debug', 'diag-cpu'], required=True, help='Choose the category of job to test')
    parser.add_argument('-m', '--command', help='Specify the command in clusrun or diag-pingpong-debug job', default='')
    parser.add_argument('-r', '--result', help='Specify the expected result of tasks in clusrun job', default=None)
    parser.add_argument('-n', '--name', help='Specify the job name', default='Functional test by chenling')
    parser.add_argument('-c', '--cancel', type=check_positive, default=0, help='Specify the time(seconds) to cancel the job')
    parser.add_argument('-t', '--continuous', type=check_positive, help='Specify the time(seconds) to run the test continuously until this time out')
    parser.add_argument('-o', '--timeout', type=check_positive, default=60*60*24*365, help='Specify the max time(seconds) to wait for the job until canceling it in one test')
    parser.add_argument('-u', '--username', default='root', help='Specify the username of cluster admin')
    parser.add_argument('-p', '--password', default='Pass1word', help='Specify the password of cluster admin')
    parser.add_argument('-d', '--random', action="store_true", help='Specify if pick random nodes to run the test')
    parser.add_argument('-l', '--platform', choices=['Windows', 'Linux', 'Mixed'], default='Mixed', help='Specify the platform type of nodes that will be selected to run the test')
    args = parser.parse_args()
    
    credential = '{}:{}'.format(args.username, args.password)
#    if PY3:
#        base64Str = base64.b64encode(credential.encode()).decode()
#    else:
#        base64Str = base64.b64encode(credential)
    base64Str = base64.b64encode(credential.encode()).decode()
    REQUEST_HEADER = {'Authorization':'Basic {}'.format(base64Str)}
    
    if args.result:
        args.result = args.result.replace('\\n', '\n').replace('\\r', '\r')
        
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
            print("[Time]: {0}".format(time.ctime()))
            print("[Test Number]: {0}".format(testResults['All']))
            testResults['All'] += 1
            try:
                result = main(args.cluster_uri, args.category, args.command, args.result, args.name, args.cancel, args.timeout + time.time(), endTime, args.random, args.platform)
                testResults[result] += 1
            except Exception as e:
                testResults['Exception'] += 1
                print('Line {}: {}'.format(sys.exc_info()[2].tb_lineno, e))
                time.sleep(60)
            runtime = time.time() - startTime
            runTimes.append(runtime)
            print('[Runtime]: {:.0f}s'.format(runtime))
            print('-'*60)
        print('{}/{} {} Runtime:[Avg: {:.0f}, Min: {:.0f}, Max: {:.0f}]'.format(testResults['Pass'], testResults['All'], testResults, sum(runTimes)/len(runTimes), min(runTimes), max(runTimes)))
    else:
        main(args.cluster_uri, args.category, args.command, args.result, args.name, args.cancel, 0, 0, args.random, args.platform)
