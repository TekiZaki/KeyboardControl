@echo off
setlocal
cd /d "%~dp0"

echo Building KeyboardControl.exe (Zero Visual Studio / Zero SDK dependencies)...

if not exist "bin\Release" mkdir "bin\Release"

set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if not exist "%CSC%" (
    set "CSC=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

if not exist "%CSC%" (
    echo [ERROR] Windows native C# compiler not found in %SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\
    pause
    exit /b 1
)

"%CSC%" /nologo /target:winexe /optimize+ /unsafe /out:bin\Release\KeyboardControl.exe /r:System.dll,System.Core.dll,System.Drawing.dll,System.Management.dll,System.Windows.Forms.dll Program.cs Controls\BrightnessControl.cs Controls\HotkeyManager.cs Controls\VolumeControl.cs UI\FlyoutOsd.cs UI\MainForm.cs

if %ERRORLEVEL% equ 0 (
    echo [SUCCESS] Built executable at bin\Release\KeyboardControl.exe
) else (
    echo [FAILED] Compilation failed with exit code %ERRORLEVEL%.
)

endlocal
