Remove-Item -Path .\BravoLights\bin\Release -Recurse

dotnet publish -p:PublishProfile=FolderProfile
$path = Resolve-Path "BravoLights\bin\Release\net5.0-windows\publish\BetterBravoLights.exe"

$o = [system.diagnostics.fileversioninfo]::GetVersionInfo($path)
echo $o.ProductVersion | Out-File -Encoding UTF8 BravoLights\bin\Release\net5.0-windows\publish\VERSION.txt
