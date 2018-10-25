#v0.1

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
    timeout = 1500
    if 'DiagnosticTest' in job and 'Arguments' in job['DiagnosticTest']:
        arguments = job['DiagnosticTest']['Arguments']
        if arguments:
            for argument in arguments:
                if argument['name'].lower() == 'Version'.lower():
                    version = argument['value'].lower()
                    continue
                if argument['name'].lower() == 'Timeout'.lower():
                    timeout = int(argument['value'])
                    continue

    nodeSize = {}
    for nodeInfo in nodesInfo:
        node = nodeInfo["Node"]
        try:
            nodeSize[node] = json.loads(nodeInfo["Metadata"])["compute"]["vmSize"]
        except Exception as e:
            nodeSize[node] = "Unknown"
            
    uris = {
        '2019'.lower(): ["http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13584/l_mpi_2019.0.117.tgz", "2019.0.117"],
        '2018 Update 4'.lower(): ["http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13741/l_mpi_2018.4.274.tgz", "2018.4.274"],
        '2018 Update 3'.lower(): ["http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13112/l_mpi_2018.3.222.tgz", "2018.3.222"],
        '2018 Update 2'.lower(): ["http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12748/l_mpi_2018.2.199.tgz", "2018.2.199"],
        '2018 Update 1'.lower(): ["http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12414/l_mpi_2018.1.163.tgz", "2018.1.163"],
        '2018'.lower(): ["http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12120/l_mpi_2018.0.128.tgz" "2018.0.128"]
        }

    installDirectory = "/opt/intel/impi/{}".format(uris[version][1])
    wgetOutput = "wget.output"
    commandCheckExist = "[ -d {0} ] && echo 'Already installed in {0}'".format(installDirectory)
    commandShowOutput = r"cat {} | sed 's/.*\r//'".format(wgetOutput)
    commandDownload = "timeout {0}s wget --progress=bar:force {1} >{2} 2>&1 && {3} || (errorcode=$? && {3} && exit $errorcode)".format(timeout, uris[version][0], wgetOutput, commandShowOutput)
    commandInstall = "tar -zxf l_mpi_{0}.tgz && cd l_mpi_{0} && sed -i -e 's/ACCEPT_EULA=decline/ACCEPT_EULA=accept/g' ./silent.cfg && ./install.sh --silent ./silent.cfg".format(uris[version][1])
    command = "{} || ({} && {}) ".format(commandCheckExist, commandDownload, commandInstall)
    
    taskTemplate = {
        "Id":0,
        "CommandLine":command,
        "Node":None,
        "CustomizedData":None,
        "MaximumRuntimeSeconds":timeout + 300,
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
