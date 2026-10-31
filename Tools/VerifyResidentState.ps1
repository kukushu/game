param([string]$Results='Temp/MillionScaleExecution/resident-state',[switch]$TargetedOnly)
# The suite includes the original state checks plus the now-fixed cold-order case.
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'VerifyMemberOrder.ps1') -Results $Results -TargetedOnly:$TargetedOnly
