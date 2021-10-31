if (Test-Path -Path .\BravoLights\bin\Release) {
	Remove-Item -Path .\BravoLights\bin\Release -Recurse
}

if ($null -eq $env:VCTargetsPath) {
	$env:VCTargetsPath = 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Microsoft\VC\v160\'
}

dotnet publish -p:PublishProfile=FolderProfile -p:Configuration=Release
$path = Resolve-Path "BravoLights\bin\Release\net5.0-windows\publish\BetterBravoLights.exe"

$o = [system.diagnostics.fileversioninfo]::GetVersionInfo($path)
Write-Output "Better Bravo Lights $($o.ProductVersion)" | Out-File -Encoding UTF8 BravoLights\bin\Release\net5.0-windows\publish\VERSION.txt

Start-Process -Wait -FilePath "$env:MSFS_SDK\Tools\bin\fspackagetool.exe" -ArgumentList "MSFSWASMProject\BetterBravoLightsLVars.xml -outputdir BravoLights\bin\Release\net5.0-windows\publish"

Compress-Archive -Path BravoLights\bin\Release\net5.0-windows\publish\* -Force -DestinationPath BetterBravoLights.zip