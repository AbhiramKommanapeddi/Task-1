@echo off
REM Resilient Form Automation Runner
set DOTNET_CLI_HOME=%~dp0.dotnet_home
set NUGET_PACKAGES=%~dp0.nuget\packages

REM Check if dotnet is in PATH, otherwise use local user profile
where dotnet >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    dotnet run -- %*
) else (
    "%USERPROFILE%\.dotnet\dotnet.exe" run -- %*
)

