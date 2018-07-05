import sys, requests, argparse, time

def main(uri):
    while uri[-1] == '/':
        uri = uri[:-1]
    print "Target Uri: {0}\n".format(uri)
    
    result = [0, 0]

    try:
        api = "{0}/v1/dashboard/nodes".format(uri)
        testAPI(api, result).json()
    except Exception as e:
        print "Cluster is not available."
        print str(e)
        time.sleep(60)
        return [0, 1]

    api = "{0}/v1/dashboard/clusrun".format(uri)
    testAPI(api, result)

    api = "{0}/v1/dashboard/diagnostics".format(uri)
    testAPI(api, result)

    api = "{0}/v1/nodes".format(uri)
    nodes = testAPI(api, result).json()
    nodes = [ node["id"] for node in nodes if node["health"] == "OK" ]

    if nodes:
        api = "{0}/v1/nodes/{1}".format(uri, nodes[0])
        testAPI(api, result)

        api = "{0}/v1/nodes/{1}/events".format(uri, nodes[0])
        testAPI(api, result)

        api = "{0}/v1/nodes/{1}/jobs".format(uri, nodes[0])
        testAPI(api, result)

        api = "{0}/v1/nodes/{1}/metrichistory".format(uri, nodes[0])
        testAPI(api, result)

        api = "{0}/v1/nodes/{1}/metadata".format(uri, nodes[0])
        testAPI(api, result)
    else:
        print "No available nodes, skip 5 APIs."

    api = "{0}/v1/metrics/categories".format(uri)
    metrics = testAPI(api, result).json()

    if metrics:
        api = "{0}/v1/metrics/{1}".format(uri, metrics[0])
        testAPI(api, result)
    else:
        print "No available metrics, skip 1 API."

    api = "{0}/v1/diagnostics/tests".format(uri)
    testAPI(api, result)

    testJobs("diagnostics", uri, result)
    testJobs("clusrun", uri, result)    

    return result

def testJobs(category, uri, result):
    api = "{0}/v1/{1}".format(uri, category)
    jobs = testAPI(api, result).json()
    finishedJobIds = [ job["id"] for job in jobs if job["state"] == "Finished" ]

    if jobs:
        api = "{0}/v1/{1}/{2}".format(uri, category, jobs[-1]["id"])
        testAPI(api, result)

        api = "{0}/v1/{1}/{2}/tasks".format(uri, category, jobs[-1]["id"])
        testAPI(api, result)

        if finishedJobIds:
            api = "{0}/v1/{1}/{2}/tasks".format(uri, category, finishedJobIds[-1])
            response = requests.get(api)
            tasks = response.json()
            
            api = "{0}/v1/{1}/{2}/tasks/{3}".format(uri, category, finishedJobIds[-1], tasks[0]["id"])
            testAPI(api, result)

            api = "{0}/v1/{1}/{2}/tasks/{3}/result".format(uri, category, finishedJobIds[-1], tasks[0]["id"])
            taskResult = testAPI(api, result).json()

            resultKey = taskResult["resultKey"]
            api = "{0}/v1/output/{1}/{2}/raw".format(uri, category, resultKey)
            testAPI(api, result)

            api = "{0}/v1/output/{1}/{2}/page".format(uri, category, resultKey)
            testAPI(api, result)
        else:
            print "No finished {0} jobs, skip {1} APIs.".format(category, 4)
    else:
        print "No {0} jobs, skip {1} APIs.".format(category, 6)
    
def testAPI(api, result):
    result[1] += 1
    print "Test API: {0}".format(api)
    try:
        response = requests.get(api)
        if response:
            result[0] += 1
        print "Result: {0}\n".format(response)
        return response
    except Exception as e:
        print e
    
if __name__ == '__main__':
    def check_positive(value):
        ivalue = int(value)
        if ivalue < 0:
             raise argparse.ArgumentTypeError("{} is an invalid positive int value".format(ivalue))
        return ivalue

    parser = argparse.ArgumentParser(description='Test the availibility of rest APIs against a cluster')
    parser.add_argument('cluster_uri', help='Specify the cluster to test')
    parser.add_argument('-t', '--continuous', type=check_positive, help='Specify the time(seconds) to run the test continuously until this time out')
    args = parser.parse_args()

    if args.continuous:
        startTime = time.time()
        endTime = startTime + args.continuous
        count = 0
        testResult = [0, 0, 0]
        while endTime > time.time():
            print "[Time]: {0}".format(time.ctime())
            print "[Test Number]: {0}".format(count)
            try:
                result = main(args.cluster_uri)
                testResult[0] += result[0]
                testResult[1] += result[1]                
            except:
                testResult[2] += 1
                print(sys.exc_info()[0])
                time.sleep(60)
            print '-'*60
            count += 1
        print '{}/{} Exceptions: {}'.format(*testResult)
    else:
        main(args.cluster_uri)
