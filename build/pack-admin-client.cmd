@echo off
setlocal
REM Packs the admin client static build output as pote.Config.Admin.Client.
REM Usage: pack-admin-client.cmd [version]   (default 0.1.0.0)
set VERSION=%1
if "%VERSION%"=="" set VERSION=0.1.0.0

call "%~dp0build-admin-client.cmd"
if errorlevel 1 exit /b 1

pushd "%~dp0..\src\Config.Admin.WebClient"
nuget pack Config.Admin.WebClient.nuspec -OutputDirectory "%~dp0artifacts" -BasePath build -Version %VERSION%
if errorlevel 1 ( echo NuGet pack FAILED. Is nuget.exe on PATH? & popd & exit /b 1 )
popd

echo.
echo Package written to build\artifacts
