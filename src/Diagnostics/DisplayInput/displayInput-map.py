import sys, json, copy

def main():
    stdin = json.load(sys.stdin)
    job = stdin["Job"]
    nodesInfo = stdin["Nodes"]
    nodelist = job["TargetNodes"]
      
    if len(nodelist) != len(set(nodelist)):
        # Duplicate nodes
        raise Exception('Duplicate nodes')

    taskTemplate = {
        "Id":0,
        "CommandLine":"echo '{0}'".format(json.dumps(stdin)),
        "Node":None,
        "UserName":"hpc_diagnostics",
        "Password":None,
        "CustomizedData":None,
    }

    startIndex = 100000000
    taskId = startIndex
    tasks = []
    for node in nodelist:
        task = copy.deepcopy(taskTemplate)
        task["Id"] = taskId
        taskId += 1
        task["Node"] = node
        task["CustomizedData"] = node
        tasks.append(task)

    print(json.dumps(tasks))

if __name__ == '__main__':
    main()
