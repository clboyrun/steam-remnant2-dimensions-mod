@echo off
rem Installs the optional tray dependencies and registers the tool to start at login.
cd /d "%~dp0"
python -m pip install -r requirements.txt
python -m clickup_sync --install-startup
echo Done. Start now with:  pythonw -m clickup_sync
