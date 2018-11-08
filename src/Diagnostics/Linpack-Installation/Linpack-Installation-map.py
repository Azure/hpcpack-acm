#v0.2

import sys, json, copy

def main():
    stdin = json.load(sys.stdin)
    job = stdin["Job"]
    nodesInfo = stdin["Nodes"]
    nodelist = job["TargetNodes"]

    if len(nodelist) != len(set(nodelist)):
        # Duplicate nodes
        raise Exception('Duplicate nodes')
    
    version = '2018 Update 4'.lower()
    timeout = 3600
    if 'DiagnosticTest' in job and 'Arguments' in job['DiagnosticTest']:
        arguments = job['DiagnosticTest']['Arguments']
        if arguments:
            for argument in arguments:
                if argument['name'].lower() == 'Version'.lower():
                    version = argument['value'].lower()
                    continue
                if argument['name'].lower() == 'Max runtime'.lower():
                    timeout = int(argument['value'])
                    continue

    timeout -= 179
    if timeout <= 0:
        raise Exception("The Max runtime parameter should be equal or larger than 180.")

    nodeSize = {}
    for nodeInfo in nodesInfo:
        node = nodeInfo["Node"]
        try:
            nodeSize[node] = json.loads(nodeInfo["Metadata"])["compute"]["vmSize"]
        except Exception as e:
            nodeSize[node] = "Unknown"
            
    uris = {
        '2019'.lower(): "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13575/l_mkl_2019.0.117.tgz",
        '2018 Update 4'.lower(): "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13725/l_mkl_2018.4.274.tgz",
        '2018 Update 3'.lower(): "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13005/l_mkl_2018.3.222.tgz",
        '2018 Update 2'.lower(): "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12725/l_mkl_2018.2.199.tgz",
        '2018 Update 1'.lower(): "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12414/l_mkl_2018.1.163.tgz",
        '2018'.lower(): "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12070/l_mkl_2018.0.128.tgz"
        }

    uri = uris[version]
    packageName = uri[-len("l_mpi_xxxx.x.xxx.tgz"):-len(".tgz")]
    installDirectory = "/opt/intel/mkl"
    wgetOutput = "wget.output"
    commandCheckExist = "[ -d {0} ] && echo 'Already installed in {0}'".format(installDirectory)
    commandShowOutput = r"cat {} | sed 's/.*\r//'".format(wgetOutput)
    commandDownload = "timeout {0}s wget --progress=bar:force {1} >{2} 2>&1 && {3} || (errorcode=$? && {3} && exit $errorcode)".format(timeout, uri, wgetOutput, commandShowOutput)
    commandInstall = "tar -zxf {0}.tgz && cd {0} && sed -i -e 's/ACCEPT_EULA=decline/ACCEPT_EULA=accept/g' ./silent.cfg && ./install.sh --silent ./silent.cfg".format(packageName)
    command = "{} || ({} && {}) ".format(commandCheckExist, commandDownload, commandInstall)
    
    taskTemplate = {
        "Id":0,
        "CommandLine":command,
        "Node":None,
        "CustomizedData":None,
        "MaximumRuntimeSeconds":36000
    }

    tasks = []
    id = 1
    for node in nodelist:
        task = copy.deepcopy(taskTemplate)
        task["Id"] = id
        id += 1
        task["Node"] = node
        task["CustomizedData"] = nodeSize[node]
        tasks.append(task)

    print(json.dumps(tasks))

if __name__ == '__main__':
    main()
