# Transaction Dispute Portal - Local Development Startup Script (PowerShell)

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Transaction Dispute Portal - Local Dev Start" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

# Check if .NET SDK is installed
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
	Write-Host "❌ .NET SDK is not installed. Please install .NET 10 SDK first." -ForegroundColor Red
	exit 1
}

# Check if Node.js is installed
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
	Write-Host "❌ Node.js is not installed. Please install Node.js 18+ first." -ForegroundColor Red
	exit 1
}

Write-Host "✓ Prerequisites validated" -ForegroundColor Green

# Start Backend in new process
Write-Host "Starting Backend API..." -ForegroundColor Yellow
$backendPath = Join-Path $PSScriptRoot "backend\TransactionDisputePortal.Api"
$backendProcess = Start-Process -NoNewWindow -PassThru -WorkingDirectory $backendPath -FileName "dotnet" -ArgumentList "run"
Write-Host "Backend started (PID: $($backendProcess.Id))" -ForegroundColor Green
Start-Sleep -Seconds 5

# Start Frontend in new process
Write-Host "Starting Frontend..." -ForegroundColor Yellow
$frontendPath = Join-Path $PSScriptRoot "frontend\TransactionDisputePortal.Web"
Push-Location $frontendPath
npm install 2>&1 | Out-Null
$frontendProcess = Start-Process -NoNewWindow -PassThru -FileName "npm" -ArgumentList "run", "dev"
Write-Host "Frontend started (PID: $($frontendProcess.Id))" -ForegroundColor Green
Pop-Location

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "Application Started!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host "Frontend:  http://localhost:5173" -ForegroundColor Cyan
Write-Host "Backend:   http://localhost:5115" -ForegroundColor Cyan
Write-Host "API Docs:  http://localhost:5115/openapi/v1.json" -ForegroundColor Cyan
Write-Host ""
Write-Host "To stop services, close both terminal windows or press Ctrl+C"
Write-Host "================================================" -ForegroundColor Green

# Keep script running
$backendProcess.WaitForExit()
$frontendProcess.WaitForExit()
