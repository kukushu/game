param([switch]$NoBrowser)
# Compatibility alias: use the original game address and save.
& (Join-Path $PSScriptRoot 'Start-LandLab.ps1') -NoBrowser:$NoBrowser
