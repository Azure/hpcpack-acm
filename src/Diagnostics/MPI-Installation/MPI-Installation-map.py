#v0.2

import sys, json, copy

def main():
    stdin = json.load(sys.stdin)
    job = stdin["Job"]
    nodes = stdin["Nodes"]
    
    nodelist = job["TargetNodes"]

    if len(nodelist) != len(set(nodelist)):
        # Duplicate nodes
        raise Exception('Duplicate nodes')

    metadataByNode = {node["Node"]:json.loads(node["Metadata"]) for node in nodes}

    windowsNodes = set()
    linuxNodes = set()
    unknownNodes = set()
    for node in nodelist:
        osType = metadataByNode[node]["compute"]["osType"]
        if osType.lower() == 'Windows'.lower():
            windowsNodes.add(node)
        elif osType.lower() == 'Linux'.lower():
            linuxNodes.add(node)
        else:
            unknownNodes.add(node)
        
    if len(unknownNodes) != 0:
        raise Exception('Unknown OS type of node(s): {}'.format(', '.join(unknownNodes)))

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
            
    IntelMpiUris = {
        '2019 Update 1'.lower(): {
            "Linux": "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/14879/l_mpi_2019.1.144.tgz",
            "Windows": "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/14881/w_mpi_p_2019.1.144.exe"
            },
        '2019'.lower(): {
            "Linux": "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13584/l_mpi_2019.0.117.tgz",
            "Windows": "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13586/w_mpi_p_2019.0.117.exe"
            },
        '2018 Update 4'.lower(): {
            "Linux": "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13741/l_mpi_2018.4.274.tgz",
            "Windows": "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13653/w_mpi_p_2018.4.274.exe"
            },
        '2018 Update 3'.lower(): {
            "Linux": "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13112/l_mpi_2018.3.222.tgz",
            "Windows": "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/13065/w_mpi_p_2018.3.210.exe"
            },
        '2018 Update 2'.lower(): {
            "Linux": "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12748/l_mpi_2018.2.199.tgz",
            "Windows": "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12745/w_mpi_p_2018.2.185.exe"
            },
        '2018 Update 1'.lower(): {
            "Linux": "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12414/l_mpi_2018.1.163.tgz",
            "Windows": "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12443/w_mpi_p_2018.1.156.exe"
            },
        '2018'.lower(): {
            "Linux": "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12120/l_mpi_2018.0.128.tgz",
            "Windows": "http://registrationcenter-download.intel.com/akdlm/irc_nas/tec/12114/w_mpi_p_2018.0.124.exe"
            }
        }
    if version not in IntelMpiUris:
        raise Exception("Version {} is not supported.".format(version))

    # command to install MPI on Linux node
    uri = IntelMpiUris[version]["Linux"]
    packageName = uri[-len("l_mpi_xxxx.x.xxx.tgz"):-len(".tgz")]
    versionNumber = packageName[-len("xxxx.x.xxx"):]
    installDirectory = "/opt/intel/impi/{}".format(versionNumber)
    wgetOutput = "wget.output"
    commandCheckExist = "[ -d {0} ] && echo 'Already installed in {0}'".format(installDirectory)
    commandShowOutput = r"cat {} | sed 's/.*\r//'".format(wgetOutput)
    commandDownload = "timeout {0}s wget --progress=bar:force {1} >{2} 2>&1 && {3} || (errorcode=$? && {3} && exit $errorcode)".format(timeout, uri, wgetOutput, commandShowOutput)
    commandInstall = "tar -zxf {0}.tgz && cd {0} && sed -i -e 's/ACCEPT_EULA=decline/ACCEPT_EULA=accept/g' ./silent.cfg && ./install.sh --silent ./silent.cfg".format(packageName)
    commandLinux = "{} || ({} && {}) ".format(commandCheckExist, commandDownload, commandInstall)

    # command to install MPI on Windows node
    uri = IntelMpiUris[version]["Windows"]
    versionNumber = uri.split('_')[-1][:-len(".exe")]
    installDirectory = "C:\Program Files (x86)\IntelSWTools\mpi\{}".format(versionNumber)
    commandWindows = """powershell "
if (Test-Path '[installDirectory]')
{
    'Already installed in [installDirectory]';
    exit
}
else
{
    date;
    $stopwatch = [system.diagnostics.stopwatch]::StartNew();
    'Start downloading';
    $client = new-object System.Net.WebClient;
    $client.DownloadFile('[uri]', 'mpi.exe');
    date;
    'End downloading';
    if ($stopwatch.Elapsed.TotalSeconds -gt [timeout])
    {
        'Not enough time to install before task timeout. Exit.';
        exit 124;
    }
    else
    {
        cmd /C '.\mpi.exe --silent --a install --eula=accept --output=%cd%\mpi.log & type mpi.log'
    }
}"
""".replace('[installDirectory]', installDirectory).replace('[uri]', uri).replace('[timeout]', str(timeout)).replace('\n', '')

    tasks = []
    id = 1
    for node in nodelist:
        task = {}
        task["Id"] = id
        id += 1
        task["Node"] = node
        task["CommandLine"] = commandLinux if node in linuxNodes else commandWindows
        task["CustomizedData"] = metadataByNode[node]["compute"]["osType"]
        task["MaximumRuntimeSeconds"] = 36000
        tasks.append(task)

    print(json.dumps(tasks))

if __name__ == '__main__':
    main()
