rd/s/q BravoLights\bin\Debug\net5-windows\better-bravo-lights-lvar-module
rd/s/q BravoLights\bin\Release\net5-windows\better-bravo-lights-lvar-module

rem Builds the WASM module and installs it into the Debug output directory for the project.
"%MSFS_SDK%Tools\bin\fspackagetool.exe" MSFSWASMProject\BetterBravoLightsLVars.xml -outputdir BravoLights\bin\Debug\net5.0-windows
echo D | xcopy /c/d/e/h/k BravoLights\bin\Debug\net5.0-windows\better-bravo-lights-lvar-module BravoLights\bin\Release\net5.0-windows\better-bravo-lights-lvar-module
