add-type -AssemblyName System.IO.Compression.FileSystem
curl -o pack.zip https://evanc.blob.core.windows.net/linuxnm/HpcAcmAgentWin-1.0.2.0.zip

Remove-Item -Path pack -Recurse -Force -ErrorAction Ignore
[System.IO.Compression.ZipFile]::ExtractToDirectory("pack.zip", ".\\pack")
cd pack
.\handler.ps1 install
.\handler.ps1 enable