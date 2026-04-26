@echo off
echo Cleaning up processes and old artifacts...
taskkill /F /IM DeveloperTool.exe /T 2>nul
taskkill /F /IM InstallerGUI.exe /T 2>nul

echo Building SNEK_Iluvatar Solution (Publishing Self-Contained)...

echo Publishing Developer Tool...
dotnet publish src\developer-tool\gui\DeveloperTool.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true -o src\developer-tool\gui\bin\Release\publish

echo Publishing Installer Template...
dotnet publish src\end-user-installer\gui\InstallerGUI.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=false -p:IncludeNativeLibrariesForSelfExtract=true -o src\end-user-installer\gui\bin\Release\publish

if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    exit /b %ERRORLEVEL%
)

echo Build successful!
echo.
echo Output binaries:
echo Developer Tool: src\developer-tool\gui\bin\Release\publish\DeveloperTool.exe
echo Installer Template: src\end-user-installer\gui\bin\Release\publish\InstallerGUI.exe
