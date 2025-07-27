#!/bin/bash

echo "Building AudioCaptureApp for Windows..."

# 清理之前的构建
echo "Cleaning previous builds..."
dotnet clean --configuration Release

# 恢复包
echo "Restoring packages..."
dotnet restore

# 发布为单个exe文件
echo "Publishing as single executable..."
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output ./publish

echo ""
echo "Build completed!"
echo "Executable location: ./publish/AudioCaptureApp.exe"
echo ""