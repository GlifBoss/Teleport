@echo off
SetLocal EnableExtensions EnableDelayedExpansion
cd /D "%~dp0"

dotnet nuget locals all --clear

pause
exit /B
