# NavPlat Test Coverage Script
# Run this before committing to ensure coverage meets 80% threshold

Write-Host "Running NavPlat Test Coverage Checks..." -ForegroundColor Cyan
Write-Host ""

# Clean up old coverage data
if (Test-Path "./coverage") {
    Write-Host "Cleaning up old coverage data..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force ./coverage
}

# Backend Coverage
Write-Host "Backend Coverage (Unit Tests)" -ForegroundColor Yellow
Write-Host "=================================" -ForegroundColor Yellow

dotnet test NavPlat.sln `
    --configuration Release `
    --filter "Category=Unit|FullyQualifiedName~UnitTests" `
    --collect:"XPlat Code Coverage" `
    --results-directory ./coverage `
    --settings coverlet.runsettings `
    --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "Backend unit tests failed!" -ForegroundColor Red
    exit 1
}

# Generate coverage report
Write-Host ""
Write-Host "Generating Backend Coverage Report..." -ForegroundColor Yellow

dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.2.0 2>$null | Out-Null

reportgenerator `
    -reports:./coverage/**/coverage.cobertura.xml `
    -targetdir:./coverage/report `
    -reporttypes:"Html;Cobertura;TextSummary"

# Extract coverage percentage
if (Test-Path "./coverage/report/Summary.txt") {
    $summary = Get-Content "./coverage/report/Summary.txt" -Raw
    if ($summary -match 'Line coverage:\s+(\d+\.?\d*)%') {
        $coverage = [double]$matches[1]
        Write-Host ""
        $coverageStr = "$coverage%"
        Write-Host "Backend Code Coverage: $coverageStr" -ForegroundColor $(if ($coverage -ge 80) { "Green" } else { "Red" })
        
        if ($coverage -lt 80) {
            Write-Host "Backend coverage ($coverageStr) is below 80% threshold!" -ForegroundColor Red
            Write-Host "Please improve test coverage before committing." -ForegroundColor Yellow
            exit 1
        } else {
            Write-Host "Backend coverage meets 80% threshold!" -ForegroundColor Green
        }
    }
} else {
    Write-Host "Could not find coverage summary" -ForegroundColor Yellow
}

# Frontend Coverage
Write-Host ""
Write-Host "Frontend Coverage" -ForegroundColor Yellow
Write-Host "====================" -ForegroundColor Yellow

Set-Location frontend

# Check if node_modules exists
if (-not (Test-Path "node_modules")) {
    Write-Host "Installing frontend dependencies..." -ForegroundColor Yellow
    npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to install frontend dependencies!" -ForegroundColor Red
        Set-Location ..
        exit 1
    }
}

npm run test:coverage

if ($LASTEXITCODE -ne 0) {
    Write-Host "Frontend tests failed!" -ForegroundColor Red
    Set-Location ..
    exit 1
}

# Check frontend coverage
if (Test-Path "./coverage/coverage-summary.json") {
    $coverageJson = Get-Content "./coverage/coverage-summary.json" | ConvertFrom-Json
    $coverage = $coverageJson.total.lines.pct
    
    Write-Host ""
    $coverageStr = "$coverage%"
    Write-Host "Frontend Code Coverage: $coverageStr" -ForegroundColor $(if ($coverage -ge 80) { "Green" } else { "Red" })
    
    if ($coverage -lt 80) {
        Write-Host "Frontend coverage ($coverageStr) is below 80% threshold!" -ForegroundColor Red
        Write-Host "Please improve test coverage before committing." -ForegroundColor Yellow
        Set-Location ..
        exit 1
    } else {
        Write-Host "Frontend coverage meets 80% threshold!" -ForegroundColor Green
    }
} else {
    Write-Host "Could not find frontend coverage summary" -ForegroundColor Yellow
}

Set-Location ..

Write-Host ""
Write-Host "All coverage checks passed! Ready to commit." -ForegroundColor Green
Write-Host ""
Write-Host "Coverage reports generated:" -ForegroundColor Cyan
Write-Host "  - Backend: ./coverage/report/index.html" -ForegroundColor White
Write-Host "  - Frontend: ./frontend/coverage/index.html" -ForegroundColor White
