@echo off
echo Building AudioCaptureApp for Windows...

REM 清理之前的构建
echo Cleaning previous builds...
dotnet clean --configuration Release

REM 恢复包
echo Restoring packages...
dotnet restore

REM 发布为单个exe文件
echo Publishing as single executable...
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output ./publish

echo.
echo Build completed!
echo Executable location: ./publish/AudioCaptureApp.exe
echo.
pause