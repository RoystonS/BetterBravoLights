if (Test-Path -Path .\BravoLights\bin\Release) {
	Remove-Item -Path .\BravoLights\bin\Release -Recurse
}

dotnet publish -p:PublishProfile=FolderProfile
$path = Resolve-Path "BravoLights\bin\Release\net5.0-windows\publish\BetterBravoLights.exe"

$o = [system.diagnostics.fileversioninfo]::GetVersionInfo($path)
Write-Output "Better Bravo Lights $($o.ProductVersion)" | Out-File -Encoding UTF8 BravoLights\bin\Release\net5.0-windows\publish\VERSION.txt

Start-Process -Wait -FilePath "$env:MSFS_SDK\Tools\bin\fspackagetool.exe" -ArgumentList "MSFSWASMProject\BetterBravoLightsLVars.xml -outputdir BravoLights\bin\Release\net5.0-windows\publish"

Compress-Archive -Path BravoLights\bin\Release\net5.0-windows\publish\* -Force -DestinationPath BetterBravoLights.zip