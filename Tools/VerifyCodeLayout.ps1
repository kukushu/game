$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$manifest=Get-Content (Join-Path $PSScriptRoot 'CodeLayout.json') -Raw | ConvertFrom-Json
foreach($entry in $manifest.files){
    $path=Join-Path $root $entry.newPath
    if((Get-FileHash -LiteralPath $path).Hash -ne $entry.sha256){throw "Source changed: $path"}
    $guid=[regex]::Match((Get-Content -LiteralPath ($path+'.meta') -Raw),'(?m)^guid: (\w+)').Groups[1].Value
    if($guid -ne $entry.guid){throw "GUID changed: $path"}
    if(Test-Path -LiteralPath (Join-Path $root $entry.oldPath)){throw "Old duplicate: $($entry.oldPath)"}
}
foreach($script in Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.ps1'){
    $errors=$null
    [Management.Automation.Language.Parser]::ParseFile($script.FullName,[ref]$null,[ref]$errors)|Out-Null
    if($errors){throw "$($script.Name): $errors"}
}
"PASS: $($manifest.files.Count) source files unchanged, original GUIDs retained, no old duplicates, tools parse correctly."
