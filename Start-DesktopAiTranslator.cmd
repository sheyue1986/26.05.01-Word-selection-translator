@echo off
setlocal
set ROOT=%~dp0
set EXE=%ROOT%publish\DesktopAiTranslator\DesktopAiTranslator.exe
if not exist "%EXE%" (
  powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%scripts\Publish-Local.ps1"
)
start "" "%EXE%"
