@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo The .NET 9 SDK was not found. Install it from https://dotnet.microsoft.com/download
  pause
  exit /b 1
)

echo Building Roblox Account Manager Reimagined...
dotnet publish src\RamReimagined\RamReimagined.csproj -c Release -o dist --nologo
if errorlevel 1 (
  echo.
  echo Build failed.
  pause
  exit /b 1
)

echo.
echo Built dist\RobloxAccountManagerReimagined.exe
echo Starting it now...
start "" "dist\RobloxAccountManagerReimagined.exe"
