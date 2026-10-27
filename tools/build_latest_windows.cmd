@echo off
setlocal
cd /d "%~dp0.."
python "%~dp0build_windows.py" --launch
if errorlevel 1 pause
