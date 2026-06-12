# MediAlert Publish and Package Script for MonsterASP.NET
# Run this script in PowerShell to package the application.

$ErrorActionPreference = "Stop"

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "   MediAlert Build & Publish Packager" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan

# 1. Clean previous build & publish outputs
Write-Host "Cleaning previous build outputs..." -ForegroundColor Yellow
if (Test-Path -Path ".\publish") {
    Remove-Item -Path ".\publish" -Recurse -Force
}
if (Test-Path -Path ".\publish.zip") {
    Remove-Item -Path ".\publish.zip" -Force
}

# 2. Build and Publish the application
Write-Host "Running dotnet publish in Release mode..." -ForegroundColor Yellow
dotnet publish -c Release -o .\publish

# 3. Create the deployment zip file (Zipping the *contents* of publish folder)
Write-Host "Creating publish.zip archive..." -ForegroundColor Yellow
Compress-Archive -Path ".\publish\*" -DestinationPath ".\publish.zip" -Force

Write-Host "==============================================" -ForegroundColor Green
Write-Host " SUCCESS: publish.zip is ready for upload!" -ForegroundColor Green
Write-Host " File Size: $((Get-Item .\publish.zip).Length / 1MB -as [int]) MB" -ForegroundColor Green
Write-Host " Path: $(Resolve-Path .\publish.zip)" -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green
