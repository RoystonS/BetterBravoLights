if (Test-Path -Path .\BravoLights\bin\Release) {
	Remove-Item -Path .\BravoLights\bin\Release -Recurse
}

if ($null -eq $env:VCTargetsPath) {
	$env:VCTargetsPath = 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Microsoft\VC\v160\'
}

dotnet publish -p:PublishProfile=FolderProfile -p:Configuration=Release
$path = Resolve-Path "BravoLights\bin\Release\net5.0-windows\publish\BetterBravoLights.exe"

if (Test-Path -Path BetterBravoLights) {
	Remove-Item -Path BetterBravoLights -Recurse
}

mkdir BetterBravoLights
mkdir BetterBravoLights\Program
Copy-Item -Path "BravoLights\bin\Release\net5.0-windows\publish\*" -Destination "BetterBravoLights\Program" -Recurse
Copy-Item -Path "BravoLights\install.bat" -Destination "BetterBravoLights"
Copy-Item -Path "BravoLights\uninstall.bat" -Destination "BetterBravoLights"
Copy-Item -Path "BravoLights\LICENSES.md" -Destination "BetterBravoLights"
Move-Item -Path "BetterBravoLights\Program\Config.ini" -Destination "BetterBravoLights"

$o = [system.diagnostics.fileversioninfo]::GetVersionInfo($path)
Write-Output "Better Bravo Lights $($o.ProductVersion)" | Out-File -Encoding UTF8 BetterBravoLights\VERSION.txt

Start-Process -Wait -FilePath "$env:MSFS_SDK\Tools\bin\fspackagetool.exe" -ArgumentList "MSFSWASMProject\BetterBravoLightsLVars.xml -outputdir BetterBravoLights\Program"

Compress-Archive -Path "BetterBravoLights\*" -Force -DestinationPath BetterBravoLights.zip
