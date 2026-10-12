@echo off
set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.3.7f1\Editor\Unity.exe"
if not exist "%UNITY_EXE%" (
    echo Unity 6000.3.7f1 was not found at the expected path.
    pause
    exit /b 1
)
powershell -NoProfile -Command "if (Get-Process Unity -ErrorAction SilentlyContinue ^| Where-Object { $_.MainWindowTitle -like '*DebtRunner*' }) { exit 1 }"
if errorlevel 1 (
    echo DebtRunner is already open in Unity. Save your work and close it before launching DX11.
    pause
    exit /b 2
)
start "" "%UNITY_EXE%" -projectPath "%~dp0.." -force-d3d11
