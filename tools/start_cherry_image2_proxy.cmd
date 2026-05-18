@echo off
setlocal
cd /d "%~dp0.."
start "Cherry Image2 Proxy" powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%CD%\tools\cherry_image2_proxy.ps1"
endlocal
