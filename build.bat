@echo off
setlocal EnableDelayedExpansion

set PROJECT_DIR=%~dp0
for /f "usebackq delims=" %%a in (`powershell -NoProfile -Command "(Get-Content '%PROJECT_DIR%manifest.json' -Raw | ConvertFrom-Json).version_number"`) do set VERSION=%%a

set PACKAGE_DIR=%PROJECT_DIR%thunderstore_package
set PLUGINS_DIR=%PACKAGE_DIR%\plugins
set ZIP_NAME=MikuBongFix-%VERSION%.zip
set GAME_PACKAGE_DIR=D:\Steam\steamapps\common\PEAK\BepInEx\plugins\MikuBongFix-%VERSION%
set GAME_PLUGINS_DIR=%GAME_PACKAGE_DIR%\plugins
set MSBUILD_EXE=C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin\MSBuild.exe

if not exist "%MSBUILD_EXE%" set MSBUILD_EXE=msbuild

echo ============================================
echo MikuBongFix Release Build
echo Version: %VERSION%
echo ============================================
echo.

echo [1/4] Building release DLL...
"%MSBUILD_EXE%" "%PROJECT_DIR%MikuBongFix.csproj" /p:Configuration=Release /p:Platform=AnyCPU /p:RunPostBuildEvent=Never /t:Rebuild /v:minimal
if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] Build failed.
    exit /b 1
)

echo.
echo [2/4] Preparing Thunderstore package layout...
if exist "%PACKAGE_DIR%" rmdir /S /Q "%PACKAGE_DIR%"
mkdir "%PLUGINS_DIR%" >nul

copy /Y "%PROJECT_DIR%bin\Release\MikuBongFix.dll" "%PLUGINS_DIR%\" >nul
copy /Y "%PROJECT_DIR%mikupeak" "%PLUGINS_DIR%\" >nul
for %%f in ("%PROJECT_DIR%response_*.wav") do copy /Y "%%~f" "%PLUGINS_DIR%\" >nul

copy /Y "%PROJECT_DIR%manifest.json" "%PACKAGE_DIR%\manifest.json" >nul
copy /Y "%PROJECT_DIR%README.md" "%PACKAGE_DIR%\README.md" >nul
copy /Y "%PROJECT_DIR%CHANGELOG.md" "%PACKAGE_DIR%\CHANGELOG.md" >nul
copy /Y "%PROJECT_DIR%icon.png" "%PACKAGE_DIR%\icon.png" >nul

echo.
echo [3/4] Creating upload zip...
if exist "%PROJECT_DIR%%ZIP_NAME%" del "%PROJECT_DIR%%ZIP_NAME%"
powershell -NoProfile -Command "Compress-Archive -Path '%PACKAGE_DIR%\*' -DestinationPath '%PROJECT_DIR%%ZIP_NAME%' -CompressionLevel Optimal -Force"
if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] Failed to create zip package.
    exit /b 1
)

echo.
echo [4/4] Optional local deployment...
if /I "%DEPLOY_TO_GAME%"=="1" (
    if not exist "%GAME_PLUGINS_DIR%" mkdir "%GAME_PLUGINS_DIR%"
    copy /Y "%PROJECT_DIR%bin\Release\MikuBongFix.dll" "%GAME_PLUGINS_DIR%\" >nul
    copy /Y "%PROJECT_DIR%mikupeak" "%GAME_PLUGINS_DIR%\" >nul
    for %%f in ("%PROJECT_DIR%response_*.wav") do copy /Y "%%~f" "%GAME_PLUGINS_DIR%\" >nul
    copy /Y "%PROJECT_DIR%manifest.json" "%GAME_PACKAGE_DIR%\manifest.json" >nul
    copy /Y "%PROJECT_DIR%README.md" "%GAME_PACKAGE_DIR%\README.md" >nul
    copy /Y "%PROJECT_DIR%CHANGELOG.md" "%GAME_PACKAGE_DIR%\CHANGELOG.md" >nul
    copy /Y "%PROJECT_DIR%icon.png" "%GAME_PACKAGE_DIR%\icon.png" >nul
    echo Local deployment completed: %GAME_PACKAGE_DIR%
) else (
    echo Skipped local deployment. Set DEPLOY_TO_GAME=1 to enable it.
)

echo.
echo ============================================
echo Release package ready: %ZIP_NAME%
echo Package directory: %PACKAGE_DIR%
echo ============================================
echo.
endlocal
