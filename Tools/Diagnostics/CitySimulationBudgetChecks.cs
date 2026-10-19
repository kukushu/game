using System;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;

static class CitySimulationBudgetChecks
{
    static int checks;
    static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    static bool Same(CityCoreIndustrySession a,CityCoreIndustrySession b)=>CityCoreSave.Write(a).SequenceEqual(CityCoreSave.Write(b));
    static void Main()
    {
        var a=CityCoreIndustrySession.Create();var b=CityCoreSave.Read(CityCoreSave.Write(a));
        Check(a.Advance(10)==200,"Baseline fixed-step count");Check(b.Simulation.Advance(10,3,double.PositiveInfinity)==3,"Max-step budget exceeded");
        Check(Math.Abs(b.Simulation.PendingSeconds-9.85)<1e-9&&!b.Simulation.IsStepping&&!b.Simulation.IsApplyingBoundary,"Pending seconds or boundary lost");
        var restored=CityCoreSave.Read(CityCoreSave.Write(b));Check(Same(b,restored)&&restored.Simulation.PendingSeconds>9,"In-flight whole-step debt not saved");
        int calls=0;while(b.Simulation.PendingSeconds>=.05-1e-10){int count=b.Simulation.Advance(0,3,double.PositiveInfinity);int other=restored.Simulation.Advance(0,3,double.PositiveInfinity);Check(count>0&&count<=3&&count==other,"Debt drain skipped or duplicated steps");if(++calls>100)throw new Exception("Unbounded debt drain");}
        Check(Same(a,b)&&Same(b,restored),"Budget grouping changed complete business/transport facts");a.Validate();b.Validate();restored.Validate();
        var timed=CityCoreIndustrySession.Create();var expected=CityCoreSave.Read(CityCoreSave.Write(timed));
        Check(timed.Simulation.Advance(1,200,double.Epsilon)==1,"Tiny budget must commit one whole step and return");expected.Advance(1);timed.Advance(0);Check(Same(timed,expected),"Wall budget changed complete fixed-step result");
        foreach(double invalid in new[]{0.0,-1,double.NaN,double.NegativeInfinity}){var before=CityCoreSave.Write(timed);bool rejected=false;try{timed.Simulation.Advance(1,4,invalid);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected&&before.SequenceEqual(CityCoreSave.Write(timed)),"Invalid budget changed requested time/state");}
        var prior=CityCoreSave.Write(timed);bool bad=false;try{timed.Simulation.Advance(1,0,8);}catch(ArgumentOutOfRangeException){bad=true;}Check(bad&&prior.SequenceEqual(CityCoreSave.Write(timed)),"Zero step allowance changed city");
        var edit=CityCoreIndustrySession.Create();edit.Simulation.Advance(2,3,double.PositiveInfinity);var car=edit.Transport.Vehicles.First();var position=edit.Transport.Position(car);var task=car.Task.id;
        edit.Transport.SplitRoadAt(1,12);Check(Point.Distance(position,edit.Transport.Position(car))<1e-7&&car.Task.id==task,"Dynamic edit at budget boundary moved vehicle/task");
        var clone=CityCoreSave.Read(CityCoreSave.Write(edit));edit.Advance(0);clone.Advance(0);edit.Validate();clone.Validate();Check(Same(edit,clone),"Edited in-flight debt recovery diverged");
        // 20x at 60 FPS requires more than the previous four-step allowance.
        // Unlimited wall budget isolates scheduling capacity from machine speed.
        foreach(int rate in new[]{20,50})
        {
            var fast=CityCoreIndustrySession.Create();var direct=CityCoreSave.Read(CityCoreSave.Write(fast));
            int advanced=0;for(int frame=0;frame<60;frame++){advanced+=fast.Simulation.Advance(rate/60.0,32,double.PositiveInfinity);direct.Advance(rate/60.0);}
            Check(advanced==rate*20,rate+"x scheduling cannot deliver all fixed steps per real second");
            Check(fast.Simulation.PendingSeconds<1e-9&&Same(fast,direct),rate+"x grouping changed complete city state or retained work");
        }
        var yielding=CityCoreIndustrySession.Create();Check(yielding.Simulation.Advance(20.0/60,32,double.Epsilon)==1&&yielding.Simulation.PendingSeconds>.28,"Higher step limit bypassed wall budget or lost work");
        Console.WriteLine("PASS "+checks+" budget checks: exact whole-city state, saved debt, finite batches, wall-time yield, invalid command atomicity, in-flight edit/recovery, 20x/50x capacity; no dropped fixed steps");
    }
}
