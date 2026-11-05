$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $root
$base=Join-Path $root 'Temp/TwoGoods-20261104'
New-Item -ItemType Directory -Force -Path $base|Out-Null
$original=Join-Path $root 'Assets/Scripts/Runtime/Simulation/Core/CitySimulation.cs'
$text=[IO.File]::ReadAllText($original)
$needle='transport.RestartDemolishedDepartures();'
if(($text.Split(@($needle),[StringSplitOptions]::None).Count-1) -ne 1){throw 'Restart negative control anchor changed'}
# Deliberately charge active paid imports again at the restart boundary. This
# mutation exists only in a disposable compilation, never in the production tree.
$mutation='if(transport.RestartDemolishedDepartures()>0)foreach(var paidImport in transport.Vehicles)if(paidImport.status==VehicleStatus.Active&&paidImport.Task.Shipment?.kind==CargoKind.RawMaterial)business.Depart(paidImport.Task);'
$mutated=Join-Path $base 'CitySimulation.restart-negative.cs'
[IO.File]::WriteAllText($mutated,$text.Replace($needle,$mutation))
$harness=Join-Path $base 'RestartAccountingNegative.cs'
[IO.File]::WriteAllText($harness,@'
using System;
using System.Reflection;
static class RestartAccountingNegative {
    static void Main() {
        try { typeof(CityCoreIndustryChecks).GetMethod("TradeTests",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null); }
        catch(TargetInvocationException e) {
            if(e.InnerException.Message.Contains("restart charged twice / new import ledger mismatch")) { Console.WriteLine("PASS deliberate duplicate import debit rejected: "+e.InnerException.Message); return; }
            throw;
        }
        throw new Exception("Negative control did not detect a duplicate import debit");
    }
}
'@)
$sources=@(Get-CityModelSources $root|Where-Object {[IO.Path]::GetFileName($_) -ne 'CitySimulation.cs'})
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$compiler='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$exe=Join-Path $base 'RestartAccountingNegative.exe'
& $mono $compiler /nologo /langversion:latest /main:RestartAccountingNegative ('/out:'+$exe) $sources $mutated $harness Tools/Diagnostics/CityCoreIndustryChecks.cs
if($LASTEXITCODE -ne 0){throw 'Negative control compilation failed'}
& $mono $exe
if($LASTEXITCODE -ne 0){throw 'Import restart negative control failed'}
