@echo off
setlocal
pushd "%~dp0..\src\Config.Admin.WebClient"

if not exist node_modules (
  echo Installing admin client dependencies...
  call npm install
  if errorlevel 1 ( echo npm install FAILED. & popd & exit /b 1 )
)

echo Starting admin client dev server on http://localhost:5071 ...
call npm run dev

popd
