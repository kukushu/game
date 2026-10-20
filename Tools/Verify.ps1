$ErrorActionPreference='Stop'
# Only the supported CityCore + RoadCore suites run in the current checkout.
foreach($suite in @('VerifyRoadCore.ps1','VerifyCityCore.ps1','VerifySimulationBudget.ps1','VerifyCityScenario.ps1','VerifyCodeLayout.ps1')){
    & (Join-Path $PSScriptRoot $suite)
    if($LASTEXITCODE -ne 0){throw "Failed: $suite"}
}
