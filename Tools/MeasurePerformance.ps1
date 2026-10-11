param(
    [Parameter(Mandatory=$true)][string]$SavePath,
    [string]$UnityData = 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data'
)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path $PSScriptRoot -Parent
$diagnosticPath = Join-Path $projectPath 'Temp/PerformanceDiagnostic'
New-Item -ItemType Directory -Path $diagnosticPath -Force | Out-Null
$monoPath = Join-Path $UnityData 'MonoBleedingEdge/bin/mono.exe'
$compilerPath = Join-Path $UnityData 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$modelPath = Join-Path $diagnosticPath 'HarborPerformanceModel.dll'
$probePath = Join-Path $diagnosticPath 'PerformanceProbe.exe'
$modelSources = Get-ChildItem -LiteralPath (Join-Path $projectPath 'Assets') -Filter 'City*.cs' | Where-Object { $_.Name -notin @('CityLandscape.cs','CityRoadView.cs') } | ForEach-Object FullName
& $monoPath $compilerPath /nologo /langversion:latest /target:library "/out:$modelPath" $modelSources
if ($LASTEXITCODE -ne 0) { throw 'Performance model compilation failed.' }
& $monoPath $compilerPath /nologo /langversion:latest /r:System.Web.Extensions.dll "/r:$modelPath" "/out:$probePath" (Join-Path $PSScriptRoot 'Diagnostics/PerformanceProbe.cs')
if ($LASTEXITCODE -ne 0) { throw 'Performance probe compilation failed.' }
& $monoPath $probePath (Resolve-Path -LiteralPath $SavePath).Path
if ($LASTEXITCODE -ne 0) { throw 'Performance diagnostic failed.' }
