Write-Host "Building AudioCaptureApp for Windows..." -ForegroundColor Green

# 清理之前的构建
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean --configuration Release

# 恢复包
Write-Host "Restoring packages..." -ForegroundColor Yellow
dotnet restore

# 发布为单个exe文件
Write-Host "Publishing as single executable..." -ForegroundColor Yellow
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output ./publish

Write-Host ""
Write-Host "Build completed!" -ForegroundColor Green
Write-Host "Executable location: ./publish/AudioCaptureApp.exe" -ForegroundColor Cyan
Write-Host ""
Read-Host "Press Enter to continue"