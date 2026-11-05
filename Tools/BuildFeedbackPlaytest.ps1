param([string]$OutputDirectory='',[int]$TimeoutSeconds=900)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
if(-not $OutputDirectory){$OutputDirectory=Join-Path $root ('Temp/FeedbackPlaytest-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))}
$output=[IO.Path]::GetFullPath($OutputDirectory)
$temporary=[IO.Path]::GetFullPath((Join-Path $root 'Temp'))+[IO.Path]::DirectorySeparatorChar
if(-not $output.StartsWith($temporary,[StringComparison]::OrdinalIgnoreCase)){throw 'Build output must be under this checkout Temp'}
if(Test-Path -LiteralPath $output){throw 'Choose a new output directory; existing artifacts are retained'}
New-Item -ItemType Directory -Path $output|Out-Null
$project=Join-Path $root 'Temp/FeedbackUnity'
Sync-CityWorkspaceProject $root $project
$commit=(& git -C $root rev-parse HEAD).Trim();if($LASTEXITCODE -ne 0){throw 'Cannot read source commit'}
$branch=(& git -C $root branch --show-current).Trim()
$dirty=(@(& git -C $root status --porcelain --untracked-files=normal | Where-Object {$_ -notlike '?? Docs/Performance/Current100k-20261103/*'}).Count -gt 0)
$files=@(foreach($folder in @('Assets','Packages','ProjectSettings')){Get-ChildItem -LiteralPath (Join-Path $root $folder) -File -Recurse|Sort-Object FullName|ForEach-Object {@{path=$_.FullName.Substring($root.Length+1).Replace('\','/');sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}}})
$hashInput=($files|ForEach-Object {$_.path+' '+$_.sha256}) -join "`n"
$sha=[Security.Cryptography.SHA256]::Create()
try{$fingerprint=[Convert]::ToHexString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($hashInput))).ToLowerInvariant()}finally{$sha.Dispose()}
$build=@{playtest=$true;dirty=$dirty;label=('反馈试玩 v1 · '+$commit.Substring(0,8)+$(if($dirty){' + local'}else{''}));commit=$commit;branch=$branch;sourceFingerprint=$fingerprint;builtUtc=[DateTime]::UtcNow.ToString('o')}
[IO.File]::WriteAllText((Join-Path $project 'Assets/Resources/FeedbackBuildInfo.json'),($build|ConvertTo-Json))
$cases=Join-Path $project 'Assets/StreamingAssets/FeedbackCases'
New-Item -ItemType Directory -Force -Path $cases|Out-Null
$caseManifest=@()
foreach($case in @(@{source='zero-14.hcity';target='shop-cash.hcity';description='Existing first shop cash exhausted snapshot'},@{source='final.hcity';target='supply-stopped.hcity';description='Existing city supply stalled snapshot'})){
    $source=Join-Path $root ('Temp/TwoGoodsDiagnosis-20261105/wrapper-verified/'+$case.source)
    $target=Join-Path $cases $case.target
    Copy-Item -LiteralPath $source -Destination $target
    $sourceHash=(Get-FileHash -LiteralPath $source).Hash
    if((Get-FileHash -LiteralPath $target).Hash -ne $sourceHash){throw 'Case copy differs'}
    $caseManifest+=@{file=$case.target;source=$source;sha256=$sourceHash;description=$case.description;terrain='diagnostic flat y=2'}
}
@{build=$build;sources=$files;cases=$caseManifest;validation='Compilation and artifact inspection only; no Player launch, simulation, regression, performance or automated interaction'}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $output 'build-manifest.json') -Encoding utf8
$env:HARBOR_TRAFFIC_VALIDATION_PLAYER=Join-Path $output 'Player/HarborCity.exe'
$env:HARBOR_PLAYER_RELEASE='1'
foreach($flag in @('HARBOR_CORE_ATTRIBUTION','HARBOR_RESIDENT_TIME_SCHEDULE','HARBOR_RESIDENT_GUARD_SCHEDULE','HARBOR_RESIDENT_FUNDING_SCHEDULE','HARBOR_RESIDENT_POLLING_REFERENCE','HARBOR_HOUSEHOLD_SHOPPING_SCHEDULE')){[Environment]::SetEnvironmentVariable($flag,'0','Process')}
$unity='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe'
$log=Join-Path $output 'unity-build.log'
$arguments=@('-batchmode','-nographics','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','HarborCity.CityPlayerBuild.BuildFeedbackPlaytest','-logFile',('"'+$log+'"'))
$process=Start-Process -FilePath $unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
if(-not $process.WaitForExit($TimeoutSeconds*1000)){$process.Kill();throw 'Feedback build timed out; only this build process was stopped'}
if($process.ExitCode -ne 0){throw "Unity compilation/build failed: $log"}
if(-not(Test-Path -LiteralPath $env:HARBOR_TRAFFIC_VALIDATION_PLAYER)){throw 'Player executable missing'}
$player=Join-Path $output 'Player'
foreach($case in $caseManifest){$packaged=Join-Path $player ('HarborCity_Data/StreamingAssets/FeedbackCases/'+$case.file);if((Get-FileHash -LiteralPath $packaged).Hash -ne $case.sha256){throw 'Packaged case differs'}}
Copy-Item -LiteralPath (Join-Path $output 'build-manifest.json') -Destination (Join-Path $player 'build-manifest.json')
Copy-Item -LiteralPath (Join-Path $root 'Docs/Gameplay/FeedbackPlaytest-20261105.md') -Destination (Join-Path $player '试玩说明.md')
Get-ChildItem -LiteralPath $player -Recurse -File|ForEach-Object {[PSCustomObject]@{path=$_.FullName.Substring($player.Length+1);bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}}|ConvertTo-Json -Depth 3|Set-Content -LiteralPath (Join-Path $output 'artifact-files.json') -Encoding utf8
Write-Output "Built only; Player not launched: $env:HARBOR_TRAFFIC_VALIDATION_PLAYER"
