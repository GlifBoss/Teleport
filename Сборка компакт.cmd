@echo off
SetLocal EnableExtensions EnableDelayedExpansion
cd /D "%~dp0"

dotnet publish -c Release -r win-x64 --self-contained false

pause
exit /B
