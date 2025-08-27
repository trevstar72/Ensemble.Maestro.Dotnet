#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Stops the Maestro .NET development environment
.DESCRIPTION
    This script stops all Maestro development services and optionally cleans up containers and volumes.
.PARAMETER Clean
    Remove containers and volumes (full cleanup)
.EXAMPLE
    .\stop-dev-env.ps1
.EXAMPLE
    .\stop-dev-env.ps1 -Clean
#>

param(
    [switch]$Clean
)

Write-Host "🛑 Stopping Maestro .NET Development Environment..." -ForegroundColor Yellow

# Navigate to the Ensemble.Maestro directory where docker-compose.yml is located
$maestroDir = Join-Path $PSScriptRoot ".." "Ensemble.Maestro"
if (-not (Test-Path $maestroDir)) {
    Write-Host "❌ Ensemble.Maestro directory not found at: $maestroDir" -ForegroundColor Red
    exit 1
}

Push-Location $maestroDir

try {
    if ($Clean) {
        Write-Host "🧹 Stopping and removing containers, networks, and volumes..." -ForegroundColor Red
        docker-compose down -v --remove-orphans
        Write-Host "✅ Full cleanup completed" -ForegroundColor Green
    } else {
        Write-Host "📦 Stopping services..." -ForegroundColor Yellow
        docker-compose stop
        Write-Host "✅ Services stopped (containers preserved)" -ForegroundColor Green
        Write-Host "💡 Use '-Clean' parameter to remove containers and volumes" -ForegroundColor Cyan
    }
}
finally {
    Pop-Location
}