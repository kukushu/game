$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
& python (Join-Path $PSScriptRoot 'VerifySingleRuntime.py') $root
if($LASTEXITCODE -ne 0){throw 'Single-runtime source checks failed'}
foreach($script in Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.ps1'){
    $errors=$null
    [Management.Automation.Language.Parser]::ParseFile($script.FullName,[ref]$null,[ref]$errors)|Out-Null
    if($errors){throw "$($script.Name): $errors"}
}
'PASS active PowerShell tools parse correctly; historical CodeLayout.json is retained as evidence'
