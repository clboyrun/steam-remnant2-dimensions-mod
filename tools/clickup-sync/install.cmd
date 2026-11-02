@echo off
rem Installs the optional tray dependencies and registers the tool to start at login.
cd /d "%~dp0"
py -m pip install -r requirements.txt
py -m clickup_sync --install-startup
echo Done. Start now with:  pyw -m clickup_sync
