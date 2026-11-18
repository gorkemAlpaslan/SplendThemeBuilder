@echo off
setlocal
set CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe

echo [1/3] Compiling DoomControlCenter.exe...
"%CSC%" /nologo /optimize+ /target:winexe /out:DoomControlCenter.exe /r:System.dll,System.Windows.Forms.dll,System.Drawing.dll,System.Core.dll DoomThemeSuite\*.cs
if errorlevel 1 exit /b 1

echo [2/3] Compiling ExplorerGlass.exe...
"%CSC%" /nologo /optimize+ /target:winexe /out:ExplorerGlass.exe /r:System.dll,System.Windows.Forms.dll,System.Drawing.dll ExplorerGlass.cs
if errorlevel 1 exit /b 1

echo [3/3] Running self-test...
DoomControlCenter.exe --self-test
if errorlevel 1 exit /b 1

echo Build & self-test succeeded!
endlocal
