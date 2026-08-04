@echo off
setlocal
pushd "%~dp0..\src\Config.Admin.WebClient"

if not exist node_modules (
  echo Installing admin client dependencies...
  call npm install
  if errorlevel 1 ( echo npm install FAILED. & popd & exit /b 1 )
)

echo === Building admin client ===
call npm run build
if errorlevel 1 ( echo Admin client build FAILED. & popd & exit /b 1 )

echo.
echo Build complete. Output in src\Config.Admin.WebClient\build
popd
