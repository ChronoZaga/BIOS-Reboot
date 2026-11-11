@echo off
setlocal
rem Builds "BIOS Reboot.exe" (x64) using the C# compiler that ships with Windows (.NET Framework 4.x).

rem Work from this batch file's own folder, wherever it was downloaded to.
cd /d "%~dp0"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    echo 64-bit .NET Framework 4 compiler not found:
    echo   %CSC%
    goto :fail
)

if not exist "BIOS Reboot.cs" (
    echo "BIOS Reboot.cs" not found next to this batch file.
    goto :fail
)

set "ICON="
if exist "BIOS Reboot.ico" set "ICON=/win32icon:"BIOS Reboot.ico""

"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ ^
    /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll ^
    %ICON% /out:"BIOS Reboot.exe" "BIOS Reboot.cs"
if errorlevel 1 goto :fail

echo.
echo Build succeeded: "BIOS Reboot.exe"
pause
exit /b 0

:fail
echo.
echo Build failed.
pause
exit /b 1
