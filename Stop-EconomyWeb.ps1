$ErrorActionPreference = 'Stop'
$gameUrl = 'http://127.0.0.1:18765/'
try { $health = Invoke-RestMethod -Uri ($gameUrl + 'api/health') -TimeoutSec 2 } catch { Write-Host 'CityEconomyLab Play is not running.'; exit 0 }
if ($health.app -ne 'cityeconomylab-play') { throw 'This port does not belong to CityEconomyLab Play. Nothing was stopped.' }
Invoke-RestMethod -Method Post -Uri ($gameUrl + 'api/stop') -ContentType 'application/json' -Body '{}' -TimeoutSec 5 | Out-Null
Write-Host 'CityEconomyLab Play stopped. Completed steps have been saved.'
