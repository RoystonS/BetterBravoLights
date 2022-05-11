rd/s/q BravoLights\bin\Debug\net5-windows\better-bravo-lights-lvar-module
rd/s/q BravoLights\bin\Release\net5-windows\better-bravo-lights-lvar-module

rem Builds the WASM module and installs it into the Debug output directory for the project.
"%MSFS_SDK%Tools\bin\fspackagetool.exe" MSFSWASMProject\BetterBravoLightsLVars.xml -outputdir BravoLights\bin\Debug\net5.0-windows

rem And another copy for the Release dir
echo D | xcopy /c/d/e/h/k BravoLights\bin\Debug\net5.0-windows\Packages BravoLights\bin\Release\net5.0-windows\Packages

rem And another copy for unit tests
echo D | xcopy /c/d/e/h/k BravoLights\bin\Debug\net5.0-windows\Packages BravoLights.Tests\bin\Debug\net5.0-windows\Packages
