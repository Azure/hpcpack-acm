# HPC Azure Cluster Management Service

## Introduction

The service enabled the diagnostics scenario of HPC clusters in Azure by providing the following features:

* Diagnostics Jobs

   With predefined diagnostic test definitions, the clsuter admin can easily validate the health of an HPC cluster.

* Clusrun Jobs

   By selecting a group of nodes and run clusrun, the commands will be dispatched to the selected nodes, and the outputs will be collected and shown interactively.

* Heatmap

   The heatmap is a real-time graphical view of a specific metric value of all nodes in the cluster. It provides a vivid way to view the cluster's metrics.

## How to deploy
There are two ways to deploy the service.
### Deploy from scratch

   <a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FEvanCui%2Fazure-hpc%2Fmaster%2FTemplates%2Fhpc-cluster%2Fazuredeploy.json" target="_blank">
      <img alt="Deploy to Azure" src="http://azuredeploy.net/deploybutton.png"/>
   </a>

   A cluster is deployed together with the diagnostic services, allowing the deployer to choose the scheduler, location, cluster size, the portal name, etc.
   This is the easiest way to create an HPC cluster with diagnostics functionalities enabled.
   For detailed usage of the deployment template, please refer: [Azure cluster deployment](https://github.com/EvanCui/azure-hpc/blob/master/Templates/hpc-cluster/README.md)
### Apply to an existing cluster
For an already deployed cluster, to enable the diagnostics functionalities, follow the steps below:
1. Create the service alone

   <a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FEvanCui%2Fhpc-acm%2Fmaster%2Fsrc%2Fdeployment%2Ftemplate%2Ftemplate.json" target="_blank">
  <img src="http://azuredeploy.net/deploybutton.png"/>
</a>

   For how to use the template, please refer: [Build HPC ACM Diagnostic service](https://github.com/EvanCui/hpc-acm/blob/master/src/deployment/template/README.md)
   
2. Register the cluster with the service (You can register multiple clusters with the same service by repeating this step for each of your cluster)

   Download the script from: [RegisterToAcm.ps1](https://raw.githubusercontent.com/EvanCui/hpc-acm/master/src/deployment/RegisterToACM.ps1)
   Run it in an elevated powershell window:
   ```
   .\RegisterToAcm.ps1 -resourceGroupName theResourceGroupOfYourCluster -acmRgName theResourceGroupOfAcmServices -subscriptionId theSubscriptionId
   ```

   After the configuration, the VMs will register themselves to the HPC ACM services, and you could check the resources section in the portal to see them.

## Known issues
* The service only support linux for now
* The service provide https portal with a self-signed cert, you need bypass the cert validation to visit the portal and use the rest api.
