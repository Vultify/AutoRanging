@echo off
setlocal

set SPT_PATH=D:\SPT Test
set MOD_NAME=AutoRanging
set SRC_DIR=%~dp0AutoRanging\
set DEST_DIR=%SPT_PATH%\BepInEx\plugins\%MOD_NAME%

echo [AutoRanging] Building...
dotnet build "%SRC_DIR%AutoRanging.csproj" -c Release /p:SPT_PATH="%SPT_PATH%" -o "%SRC_DIR%bin\Release"

if %ERRORLEVEL% neq 0 (
    echo [AutoRanging] Build FAILED.
    pause
    exit /b 1
)

echo [AutoRanging] Installing to %DEST_DIR% ...
if not exist "%DEST_DIR%" mkdir "%DEST_DIR%"

copy /Y "%SRC_DIR%bin\Release\AutoRanging.dll" "%DEST_DIR%\AutoRanging.dll"

echo.
echo [AutoRanging] Done. Plugin installed to:
echo   %DEST_DIR%
echo.
pause
