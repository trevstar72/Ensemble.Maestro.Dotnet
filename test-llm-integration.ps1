# Test LLM Integration
# Simple PowerShell script to trigger a test execution via HTTP

$testConfig = @{
    projectName = "LLM Integration Test"
    description = "Testing real OpenAI integration with gpt-4o-mini"
    targetLanguage = "C#"
    deploymentTarget = "Azure"
    agentPoolSize = 1
} | ConvertTo-Json

Write-Host "Testing LLM Integration..." -ForegroundColor Green
Write-Host "Making request to testbench..." -ForegroundColor Yellow

try {
    # Try to trigger a test execution by hitting the testbench endpoint
    $response = Invoke-WebRequest -Uri "https://localhost:5001/testbench" -SkipCertificateCheck -TimeoutSec 30
    Write-Host "Testbench page loaded successfully: $($response.StatusCode)" -ForegroundColor Green
    
    # Check if we can see any existing executions
    if ($response.Content -match "No active executions|active executions") {
        Write-Host "Testbench is ready for testing!" -ForegroundColor Green
        Write-Host "You can now manually trigger a test execution from the UI at https://localhost:5001/testbench" -ForegroundColor Cyan
    }
    
} catch {
    Write-Host "Error accessing testbench: $($_.Exception.Message)" -ForegroundColor Red
}

# Check if the app is responsive
try {
    $healthResponse = Invoke-WebRequest -Uri "https://localhost:5001/health" -SkipCertificateCheck
    Write-Host "Health check: $($healthResponse.Content)" -ForegroundColor $(if($healthResponse.Content -eq "Healthy") {"Green"} else {"Yellow"})
} catch {
    Write-Host "Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}