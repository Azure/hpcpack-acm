param (
  [Parameter(Mandatory=$true)]
  [string]$SubscriptionId,

  [Parameter(Mandatory=$true)]
  [string]$ResourceGroupName,

  [Parameter(Mandatory=$true)]
  [string]$WebAppName
)

$ErrorActionPreference = 'Stop'

function CreateAADApp() {
  param (
    [Parameter(Mandatory=$true)]
    [System.Uri]$SiteUri,

    [Parameter(Mandatory=$false)]
    [String]$Password,

    [Parameter(Mandatory=$true)]
    [String]$TenantId,

    [Parameter(Mandatory=$true)]
    [String]$AccountId
  )

  # Connect-AzureAD is required to use Active Directory cmdlets.
  $aadConnection = Connect-AzureAD -TenantId $TenantId -AccountId $AccountId

  if ([string]::IsNullOrEmpty($Password)) {
    $Password = [System.Convert]::ToBase64String($([guid]::NewGuid()).ToByteArray())
  }

  $startDate = Get-Date
  $passwordCredential = New-Object -TypeName Microsoft.Open.AzureAD.Model.PasswordCredential
  $passwordCredential.StartDate = $startDate
  $passwordCredential.EndDate = $startDate.AddYears(2)
  $passwordCredential.Value = $Password

  $displayName = $SiteUri.Host
  [string[]]$replyUrl = $SiteUri.AbsoluteUri + ".auth/login/aad/callback"

  $reqAAD = New-Object -TypeName "Microsoft.Open.AzureAD.Model.RequiredResourceAccess"

  # The Resource is "Windows Azure Active Directory".
  $reqAAD.ResourceAppId = "00000002-0000-0000-c000-000000000000"

  # The permission is "Sign you in and read your profile"
  $permission = New-Object -TypeName "Microsoft.Open.AzureAD.Model.ResourceAccess" -ArgumentList "311a71cc-e848-46a1-bdf8-97ff7156d8e6","Scope"

  $reqAAD.ResourceAccess = $permission

  $appReg = New-AzureADApplication -DisplayName $displayName -IdentifierUris $SiteUri -Homepage $SiteUri -ReplyUrls $replyUrl -PasswordCredential $passwordCredential -RequiredResourceAccess $reqAAD

  $loginBaseUrl = $(Get-AzureRmEnvironment -Name $aadConnection.Environment.Name).ActiveDirectoryAuthority

  $issuerUrl = $loginBaseUrl +  $aadConnection.Tenant.Id.Guid + "/"

  return @{
    'IssuerUrl' = $issuerUrl
    'ClientId' = $appReg.AppId
    'ClientSecret' = $Password
  }
}

function ConfigAADForAppService() {
  param (
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory=$true)]
    [string]$WebAppName,

    [Parameter(Mandatory=$true)]
    [string]$ClientId,

    [Parameter(Mandatory=$true)]
    [string]$ClientSecret,

    [Parameter(Mandatory=$true)]
    [string]$IssuerUrl
  )

  $authResourceName = $WebAppName + "/authsettings"
  $auth = Invoke-AzureRmResourceAction -ResourceGroupName $ResourceGroupName -ResourceType Microsoft.Web/sites/config -ResourceName $authResourceName -Action list -ApiVersion 2016-08-01 -Force

  $auth.properties.enabled = "True"
  $auth.properties.unauthenticatedClientAction = "RedirectToLoginPage"
  $auth.properties.tokenStoreEnabled = "True"
  $auth.properties.defaultProvider = "AzureActiveDirectory"
  $auth.properties.isAadAutoProvisioned = "False"
  $auth.properties.clientId = $ClientId
  $auth.properties.clientSecret = $ClientSecret
  $auth.properties.issuer = $IssuerUrl

  New-AzureRmResource -PropertyObject $auth.properties -ResourceGroupName $ResourceGroupName -ResourceType Microsoft.Web/sites/config -ResourceName $authResourceName -ApiVersion 2016-08-01 -Force
}

function Login {
  $needLogin = $true
  try {
    $context = Get-AzureRmContext
    if ($context) {
      $needLogin = ([string]::IsNullOrEmpty($context.Account))
    }
  }
  catch {
    if ($_ -like "*Login-AzureRmAccount to login*") {
      $needLogin = $true
    }
    else {
      throw
    }
  }

  if ($needLogin) {
    $context = (Login-AzureRmAccount).Context
  }
  return $context;
}

Login

$context = Select-AzureRmSubscription -SubscriptionId $SubscriptionId

[System.Uri]$SiteUri = 'https://' + $WebAppName + '.azurewebsites.net'

Write-Host 'Creating AAD App...'
$aadApp = CreateAADApp -SiteUri $SiteUri -TenantId $context.Tenant.Id -AccountId $context.Account.Id

Write-Host 'Configurating App Service...'
ConfigAADForAppService -ResourceGroupName $ResourceGroupName -WebAppName $WebAppName `
  -ClientId $aadApp['ClientId'] -ClientSecret $aadApp['ClientSecret'] -IssuerUrl $aadApp['IssuerUrl']

Write-Host 'Success!'

Write-Host 'Please save the following information properly for use of the HPC ACM CLI tools:'

return $aadApp
