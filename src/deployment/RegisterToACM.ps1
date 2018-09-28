param (
    [Parameter(Mandatory = $true)]
    [string] $resourceGroupName,

    [Parameter(Mandatory = $true)]
    [string] $acmRgName,

    [Parameter(Mandatory = $true)]
    [string] $subscriptionId
)

function Login {
    $needLogin = $true
    Try {
        $content = Get-AzureRmContext
        if ($content) {
            $needLogin = ([string]::IsNullOrEmpty($content.Account))
        }
    }
    Catch {
        if ($_ -like "*Login-AzureRmAccount to login*") {
            $needLogin = $true
        }
        else {
            throw
        }
    }

    if ($needLogin) {
        Login-AzureRmAccount
    }
}

# Log in
Login

# Select sub
Select-AzureRmSubscription -SubscriptionId $subscriptionId

# Get resources
Write-Host "Detecting resources under group $resourceGroupName"
$vms = Get-AzureRmVm -ResourceGroupName $resourceGroupName
foreach ($vm in $vms) {
    Write-Host "    Detected VM $($vm.Name)"
}

$vmssSet = Get-AzureRmVmss -ResourceGroupName $resourceGroupName
foreach ($vmss in $vmssSet) {
    Write-Host "    Detected VM Scale Set $($vmss.Name)"
}

# Confirm
$confirm = Read-Host "Register those detected resources to ACM Service? (y/n)"
if ($confirm -ne "y") { exit 1 }

# Enable MSI for those
Write-Host "Enabling MSI for resources under group $resourceGroupName"
foreach ($vm in $vms) {
    Write-Host "    Enable MSI for VM $($vm.Name)"
    if ($vm.Identity -eq $null -or !($vm.Identity.Type -contains "SystemAssigned")) {
        Write-Host "    Executing for VM $($vm.Name)"
        Update-AzureRmVM -ResourceGroupName $resourceGroupName -VM $vm -IdentityType "SystemAssigned"
    }
    else {
        Write-Host "    The VM $($vm.Name) already has an System Assigned Identity"
    }
}

foreach ($vmss in $vmssSet) {
    Write-Host "    Enable MSI for VM Scale Set $($vmss.Name)"
    if ($vmss.Identity -eq $null -or !($vmss.Identity.Type -contains "SystemAssigned")) {
        Write-Host "    Executing for VMSS $($vmss.Name)"
        Update-AzureRmVmss -ResourceGroupName $resourceGroupName -VMScaleSetName $vmss.Name -IdentityType "SystemAssigned"
    }
    else {
        Write-Host "    The VMSS $($vmss.Name) already has an System Assigned Identity"
    }
}

# Install the HpcAcmAgent
Write-Host "Installing the HpcAcmAgent for resources under group $resourceGroupName"
foreach ($vm in $vms) {
    Write-Host "    Instal HpcAcmAgent for VM $($vm.Name)"
    try {
        Remove-AzureRmVMExtension -ResourceGroupName $resourceGroupName -VMName $vm.Name -Name "HpcAcmAgent" -Force
    }
    catch {
    }
    Set-AzureRmVMExtension -Publisher "Microsoft.HpcPack" -ExtensionType "HpcAcmAgent" -ResourceGroupName $resourceGroupName -TypeHandlerVersion 1.0 -VMName $vm.Name -Location $vm.Location -Name "HpcAcmAgent"
}

foreach ($vmss in $vmssSet) {
    Write-Host "    Install HpcAcmAgentfor VM Scale Set $($vmss.Name)"
    try {
        Remove-AzureRmVmssExtension -VirtualMachineScaleSet $vmss -Name "HpcAcmAgent"
        Update-AzureRmVmss -ResourceGroupName $vmss.ResourceGroupName -VMScaleSetName $vmss.Name -VirtualMachineScaleSet $vmss
        Update-AzureRmVmssInstance -ResourceGroupName $vmss.ResourceGroupName -VMScaleSetName $vmss.Name -InstanceId "*"
    }
    catch {
    }

    Add-AzureRmVmssExtension -VirtualMachineScaleSet $vmss -Name "HpcAcmAgent" -Publisher "Microsoft.HpcPack" -Type "HpcAcmAgent" -TypeHandlerVersion 1.0
    Update-AzureRmVmss -ResourceGroupName $vmss.ResourceGroupName -VMScaleSetName $vmss.Name -VirtualMachineScaleSet $vmss
    Update-AzureRmVmssInstance -ResourceGroupName $vmss.ResourceGroupName -VMScaleSetName $vmss.Name -InstanceId "*"
}

# Add the tags
Write-Host "Configure storage information to the resource group $resourceGroupName"
$rg = Get-AzureRmResourceGroup -Name $resourceGroupName
$acmRg = Get-AzureRmResourceGroup -Name $acmRgName
$storageAccount = (Get-AzureRmStorageAccount -ResourceGroupName $acmRg.ResourceGroupName)[0]
$tags = $rg.Tags
$key = "StorageConfiguration"
# fetching the storage account
$value = "{ `"AccountName`": `"$($storageAccount.StorageAccountName)`", `"ResourceGroup`":`"$acmRgName`" }"

if ($tags -eq $null) {
    $tags = @{ "$key" = "$value" }
}
else {
    $tags[$key] = $value
}

Set-AzureRmResourceGroup -Tags $tags -Name $resourceGroupName

# Grant permissions
Write-Host "Grant proper permissions for the resources under resource group $resourceGroupName"

$vms = Get-AzureRmVm -ResourceGroupName $resourceGroupName
foreach ($vm in $vms) {
    Write-Host "    Grant for VM $($vm.Name)"
    try {
        New-AzureRmRoleAssignment -ObjectId $vm.Identity.PrincipalId -ResourceGroupName $vm.ResourceGroupName -RoleDefinitionName "Reader"
    }
    catch {
        if ($_ -contains 'already exists') {
            Write-Host "    Already exists"
        }
        else {
            throw
        }
    }
    try {
        New-AzureRmRoleAssignment -ObjectId $vm.Identity.PrincipalId -RoleDefinitionName "Storage Account Contributor" -ResourceName $storageAccount.StorageAccountName -ResourceType "Microsoft.Storage/storageAccounts" -ResourceGroupName $storageAccount.ResourceGroupName
    }
    catch {
        Write-Host $_
        if ($_ -contains 'already exists') {
            Write-Host "    Already exists"
        }
        else {
            throw
        }
    }
}

$vmssSet = Get-AzureRmVmss -ResourceGroupName $resourceGroupName
foreach ($vmss in $vmssSet) {
    Write-Host "    Grant for VMSS $($vmss.Name)"
    try {
        New-AzureRmRoleAssignment -ObjectId $vmss.Identity.PrincipalId -ResourceGroupName $vm.ResourceGroupName -RoleDefinitionName "Reader"
    }
    catch {
        if ($_ -contains 'already exists') {
            Write-Host "    Already exists"
        }
        else {
            throw
        }
    }

    try {
        New-AzureRmRoleAssignment -ObjectId $vmss.Identity.PrincipalId -RoleDefinitionName "Storage Account Contributor" -ResourceName $storageAccount.StorageAccountName -ResourceType "Microsoft.Storage/storageAccounts" -ResourceGroupName $storageAccount.ResourceGroupName
    }
    catch {
        if ($_ -contains 'already exists') {
            Write-Host "    Already exists"
        }
        else {
            throw
        }
    }
}

Write-Host "Success!"
