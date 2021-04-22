@echo off
cd /d %~dp0
set /p store=Install to Windows Store version of paint.net? (Y/N)

if %store% == y set store=Y
if %store% == yes set store=Y

if %store% == Y (
  set folder="%USERPROFILE%\Documents\paint.net App Files\Effects\"
  echo Installing to Windows Store version...
) else (
  set folder="%ProgramFiles%\paint.net\Effects\"
  echo Installing to standard version...
)

if not exist %folder% mkdir %folder%
copy *.dll %folder%

pause
