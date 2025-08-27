#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Starts the Maestro .NET development environment
.DESCRIPTION
    This script starts all required services for Maestro .NET development:
    - Neo4j (relationships and graph data)
    - Qdrant (vector embeddings)  
    - Redis (caching)
    - SQL Server (primary data storage)
    - Adminer (database management UI)
.EXAMPLE
    .\start-dev-env.ps1
#>

Write-Host "🚀 Starting Maestro .NET Development Environment..." -ForegroundColor Green

# Check if Docker is running
try {
    docker info | Out-Null
    Write-Host "✅ Docker is running" -ForegroundColor Green
}
catch {
    Write-Host "❌ Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
    exit 1
}

# Navigate to the Ensemble.Maestro directory where docker-compose.yml is located
$maestroDir = Join-Path $PSScriptRoot ".." "Ensemble.Maestro"
if (-not (Test-Path $maestroDir)) {
    Write-Host "❌ Ensemble.Maestro directory not found at: $maestroDir" -ForegroundColor Red
    exit 1
}

Push-Location $maestroDir

try {
    Write-Host "📦 Starting all services with docker-compose..." -ForegroundColor Yellow
    docker-compose up -d

    Write-Host "⏳ Waiting for services to be healthy..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10

    # Check service health
    Write-Host "`n🏥 Service Health Status:" -ForegroundColor Cyan
    
    $services = @(
        @{Name="Neo4j"; Container="maestro_neo4j"; Port="34747"; Url="http://localhost:34747"; Description="Graph Database (neo4j/ensemble123)"}
        @{Name="Qdrant"; Container="maestro_qdrant"; Port="36333"; Url="http://localhost:36333"; Description="Vector Database"}
        @{Name="Redis"; Container="maestro_redis"; Port="36379"; Url="redis://localhost:36379"; Description="Cache"}
        @{Name="SQL Server"; Container="maestro_sqlserver"; Port="1434"; Url="localhost,1434"; Description="Primary Database (sa/Maestro123!)"}
        @{Name="Adminer"; Container="maestro_adminer"; Port="38080"; Url="http://localhost:38080"; Description="Database Management UI"}
    )

    foreach ($service in $services) {
        $status = docker inspect $service.Container --format='{{.State.Health.Status}}' 2>$null
        if (-not $status) {
            $status = docker inspect $service.Container --format='{{.State.Status}}' 2>$null
        }
        
        $statusIcon = switch ($status) {
            "healthy" { "✅" }
            "running" { "✅" }
            "unhealthy" { "⚠️" }
            "starting" { "🔄" }
            default { "❌" }
        }
        
        Write-Host "  $statusIcon $($service.Name): $status - $($service.Description)" -ForegroundColor White
    }

    Write-Host "`n🌐 Service URLs:" -ForegroundColor Cyan
    Write-Host "  • Neo4j Browser: http://localhost:34747 (neo4j/ensemble123)" -ForegroundColor White
    Write-Host "  • Qdrant Dashboard: http://localhost:36333/dashboard" -ForegroundColor White  
    Write-Host "  • Adminer: http://localhost:38080" -ForegroundColor White
    Write-Host "  • SQL Server: localhost,1434 (sa/Maestro123!)" -ForegroundColor White

    Write-Host "`n✅ Development environment is ready!" -ForegroundColor Green
    Write-Host "💡 Run 'dotnet run' in the Ensemble.Maestro.Dotnet directory to start the application" -ForegroundColor Yellow
}
finally {
    Pop-Location
}