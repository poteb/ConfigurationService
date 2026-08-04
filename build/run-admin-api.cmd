@echo off
setlocal
pushd "%~dp0.."

echo Starting Admin API on http://localhost:34246 ...
dotnet run --project src\Config.Admin.Api\Config.Admin.Api.csproj

popd
