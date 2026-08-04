@echo off
setlocal
pushd "%~dp0.."

echo === Building Admin API ===
dotnet build src\Config.Admin.Api\Config.Admin.Api.csproj
if errorlevel 1 ( echo Admin API build FAILED. & popd & exit /b 1 )

echo.
echo Build complete.
popd
