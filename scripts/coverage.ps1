#requires -Version 5.1
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
    Write-Host "Instalando ReportGenerator..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-reportgenerator-globaltool | Out-Null
}

Write-Host "Limpando TestResults..." -ForegroundColor Cyan
Remove-Item -Recurse -Force "$root\TestResults" -ErrorAction SilentlyContinue

Write-Host "Executando testes com cobertura..." -ForegroundColor Cyan
dotnet test "$root\DiarioX.Server.Tests\DiarioX.Server.Tests.csproj" `
    --collect:"XPlat Code Coverage" `
    --settings "$root\coverlet.runsettings" `
    --results-directory "$root\TestResults"

Write-Host "Gerando relatorio HTML..." -ForegroundColor Cyan
reportgenerator `
    -reports:"$root\TestResults\**\coverage.cobertura.xml" `
    -targetdir:"$root\TestResults\CoverageReport" `
    -reporttypes:"Html;TextSummary;MarkdownSummaryGithub"

$index = "$root\TestResults\CoverageReport\index.html"
Write-Host "Relatorio: $index" -ForegroundColor Green
Start-Process $index
