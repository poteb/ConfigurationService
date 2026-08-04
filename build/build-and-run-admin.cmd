@echo off
setlocal
pushd "%~dp0"

call build-admin-api.cmd
if errorlevel 1 (
    echo === Admin API build failed - not running ===
    popd
    exit /b 1
)

echo Starting Admin API in a new window...
start "Config.Admin.Api" cmd /c run-admin-api.cmd

call run-admin-client.cmd
popd
