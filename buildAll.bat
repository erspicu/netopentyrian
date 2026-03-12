@echo off
setlocal

set "ROOT=%~dp0"
set "CONFIG=%~1"
set "MSBUILD_PATH=%~2"

if "%CONFIG%"=="" set "CONFIG=Debug"

if /I not "%CONFIG%"=="Debug" if /I not "%CONFIG%"=="Release" (
    echo Invalid configuration "%CONFIG%". Use Debug or Release.
    exit /b 1
)

echo Building OpenTyrian (%CONFIG%)...
if defined MSBUILD_PATH (
    powershell -ExecutionPolicy Bypass -File "%ROOT%build.ps1" -Configuration "%CONFIG%" -MSBuildPath "%MSBUILD_PATH%"
) else (
    powershell -ExecutionPolicy Bypass -File "%ROOT%build.ps1" -Configuration "%CONFIG%"
)

if errorlevel 1 (
    echo Build failed.
    exit /b 1
)

set "SOURCE_DIR=%ROOT%src\OpenTyrian.WinForms\bin\%CONFIG%\net40"
set "OUTPUT_DIR=%ROOT%build"

if not exist "%SOURCE_DIR%\OpenTyrian.WinForms.exe" (
    echo Build output not found: "%SOURCE_DIR%"
    exit /b 1
)

if not exist "%OUTPUT_DIR%" (
    mkdir "%OUTPUT_DIR%"
)

for %%F in (
    OpenTyrian.WinForms.exe
    OpenTyrian.WinForms.exe.config
    OpenTyrian.WinForms.pdb
    OpenTyrian.Core.dll
    OpenTyrian.Core.pdb
    OpenTyrian.Platform.dll
    OpenTyrian.Platform.pdb
) do (
    if exist "%SOURCE_DIR%\%%F" (
        copy /Y "%SOURCE_DIR%\%%F" "%OUTPUT_DIR%\%%F" >nul
    )
)

if exist "%ROOT%tyrian21" (
    robocopy "%ROOT%tyrian21" "%OUTPUT_DIR%\tyrian21" /E /NFL /NDL /NJH /NJS /NP >nul
    if errorlevel 8 (
        echo Failed to copy tyrian21 to build.
        exit /b 1
    )
)

echo Build output copied to "%OUTPUT_DIR%".
exit /b 0
