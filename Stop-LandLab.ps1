$ErrorActionPreference = 'Stop'
$gameUrl = 'http://127.0.0.1:18766/'
try { $health = Invoke-RestMethod -Uri ($gameUrl + 'api/health') -TimeoutSec 2 } catch { Write-Host 'CityEconomyLab Land is not running.'; exit 0 }
if ($health.app -ne 'cityeconomylab-land') { throw 'This port does not belong to CityEconomyLab Land. Nothing was stopped.' }
Invoke-RestMethod -Method Post -Uri ($gameUrl + 'api/stop') -ContentType 'application/json' -Body '{}' -TimeoutSec 5 | Out-Null
Write-Host 'CityEconomyLab Land stopped. Completed steps have been saved.'
