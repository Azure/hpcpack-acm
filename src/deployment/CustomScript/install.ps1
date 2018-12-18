add-type -AssemblyName System.IO.Compression.FileSystem

if (Test-Path -Path pack)
{
    cd pack
    .\handler.ps1 uninstall 2>&1 >> ..\handler.log
    cd ..
    Remove-Item -Path pack -Recurse -Force -ErrorAction Ignore
}

curl -o pack.zip https://evanc.blob.core.windows.net/linuxnm/HpcAcmAgentWin-1.0.2.0.zip

[System.IO.Compression.ZipFile]::ExtractToDirectory("pack.zip", ".\\pack")
cd pack

.\handler.ps1 uninstall 2>&1 >> ..\handler.log
.\handler.ps1 install 2>&1 >> ..\handler.log
.\handler.ps1 enable 2>&1 >> ..\handler.log
