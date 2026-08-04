# KeyValues test & coverage script
[CmdletBinding()]
param(
    [double]$Threshold = 70.0
)

Write-Host "--- KeyValues Unit Test & Coverage Script ---" -ForegroundColor Cyan

# 1. Run Unit Tests & Collect Code Coverage
Write-Host "`n1. Running Unit Tests and Code Coverage..." -ForegroundColor Yellow
dotnet test --configuration Release --collect:"XPlat Code Coverage" --settings coverlet.runsettings

if ($LASTEXITCODE -ne 0) {
    Write-Error "`n[Error] Unit tests failed."
    exit $LASTEXITCODE
}

# 2. Check & Install ReportGenerator
Write-Host "`n2. Checking ReportGenerator tool..."
$rgInstalled = Get-Command reportgenerator -ErrorAction SilentlyContinue
if (-not $rgInstalled) {
    Write-Host "  Installing ReportGenerator tool..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-reportgenerator-globaltool
}

# 3. Generate Coverage Report
Write-Host "`n3. Generating coverage report..." -ForegroundColor Yellow
reportgenerator "-reports:**/coverage.cobertura.xml" "-targetdir:coveragereport" "-reporttypes:TextSummary;JsonSummary"

if (Test-Path coveragereport/Summary.txt) {
    Write-Host "`n--- Coverage Summary ---" -ForegroundColor Green
    Get-Content coveragereport/Summary.txt | Write-Host
}

# 4. Check Coverage Threshold
if (Test-Path coveragereport/Summary.json) {
    $json = Get-Content coveragereport/Summary.json | ConvertFrom-Json
    $coverage = [double]$json.summary.linecoverage
    $coverageStr = "$coverage%"
    $thresholdStr = "$Threshold%"

    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host " Total Line Coverage: $coverageStr" -ForegroundColor Cyan
    Write-Host " Required Threshold:  $thresholdStr" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan

    if ($coverage -lt $Threshold) {
        Write-Error "`n[Error] Code coverage ($coverageStr) is below threshold ($thresholdStr)."
        exit 1
    } else {
        Write-Host "`n[Success] Code coverage meets requirements ($coverageStr >= $thresholdStr)." -ForegroundColor Green
    }
}
