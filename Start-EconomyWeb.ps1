param([switch]$NoBrowser)
$ErrorActionPreference = 'Stop'
$gameUrl = 'http://127.0.0.1:18765/'
function Get-LabHealth {
    try { return Invoke-RestMethod -Uri ($gameUrl + 'api/health') -TimeoutSec 2 } catch { return $null }
}
$health = Get-LabHealth
if ($health -and $health.app -eq 'cityeconomylab-play') {
    Write-Host 'Reusing the running CityEconomyLab Play server.'
} else {
    $listener = Get-NetTCPConnection -LocalPort 18765 -State Listen -ErrorAction SilentlyContinue
    if ($listener) { throw 'Port 18765 belongs to another application. No process was stopped.' }
    $labPython = Join-Path $PSScriptRoot '.venv\Scripts\python.exe'
    if (-not (Test-Path -LiteralPath $labPython)) {
        $labPython = (Get-Command python.exe -ErrorAction Stop).Source
    }
    $outputDir = Join-Path $PSScriptRoot 'outputs\web'
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    $env:PYTHONUTF8 = '1'
    Start-Process -FilePath $labPython -ArgumentList @('-m','playweb','--port','18765','--state','outputs/web/session.json') -WorkingDirectory $PSScriptRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $outputDir 'server.log') -RedirectStandardError (Join-Path $outputDir 'server-error.log') | Out-Null
    for ($attempt=0; $attempt -lt 40; $attempt++) {
        $health = Get-LabHealth
        if ($health -and $health.app -eq 'cityeconomylab-play') { break }
        Start-Sleep -Milliseconds 250
    }
    if (-not $health -or $health.app -ne 'cityeconomylab-play') {
        throw ('The economy server did not start. See ' + $outputDir + '\server-error.log')
    }
}
Write-Host ('CityEconomyLab Play ready: ' + $gameUrl)
if (-not $NoBrowser) { Start-Process $gameUrl }
