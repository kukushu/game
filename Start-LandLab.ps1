param([switch]$NoBrowser)
$ErrorActionPreference = 'Stop'
$gameUrl = 'http://127.0.0.1:18766/'
function Get-LabHealth {
    try { return Invoke-RestMethod -Uri ($gameUrl + 'api/health') -TimeoutSec 2 } catch { return $null }
}
$health = Get-LabHealth
if ($health -and $health.app -eq 'cityeconomylab-land') {
    Write-Host 'Reusing the running CityEconomyLab Land server.'
} else {
    $listener = Get-NetTCPConnection -LocalPort 18766 -State Listen -ErrorAction SilentlyContinue
    if ($listener) { throw 'Port 18766 belongs to another application. No process was stopped.' }
    $labPython = Join-Path $PSScriptRoot '.venv\Scripts\python.exe'
    if (-not (Test-Path -LiteralPath $labPython)) {
        $labPython = (Get-Command python.exe -ErrorAction Stop).Source
    }
    $outputDir = Join-Path $PSScriptRoot 'outputs\land'
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    $env:PYTHONUTF8 = '1'
    Start-Process -FilePath $labPython -ArgumentList @('-m','landlab','--port','18766','--state','outputs/land/session.json') -WorkingDirectory $PSScriptRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $outputDir 'server.log') -RedirectStandardError (Join-Path $outputDir 'server-error.log') | Out-Null
    for ($attempt=0; $attempt -lt 40; $attempt++) {
        $health = Get-LabHealth
        if ($health -and $health.app -eq 'cityeconomylab-land') { break }
        Start-Sleep -Milliseconds 250
    }
    if (-not $health -or $health.app -ne 'cityeconomylab-land') {
        throw ('The economy server did not start. See ' + $outputDir + '\server-error.log')
    }
}
Write-Host ('CityEconomyLab Land ready: ' + $gameUrl)
if (-not $NoBrowser) { Start-Process $gameUrl }
