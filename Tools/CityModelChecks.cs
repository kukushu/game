using System;
using HarborCity;

public static class CityModelChecks
{
    static int checks;
    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks++;
    }
    public static void Main()
    {
        var city = CityModel.Create();
        Check(city.Valid(), "Initial city valid");
        Check(city.population == 180 && city.power == 160 && city.water == 160, "Initial economy");
        int before = city.money;
        Check(city.Place(10, 17, LandUse.Residential, out _), "Zone placement");
        Check(city.money == before - 60, "Cost deducted once");
        Check(!city.Place(10, 17, LandUse.Residential, out _) && city.money == before - 60, "No double charging");
        for (int i = 0; i < 10; i++) city.Tick();
        Check(city.levels[CityModel.Index(10,17)] > 0, "Connected zone grows");
        Check(city.Place(2,2,LandUse.Residential,out _), "Isolated zone placement");
        for (int i = 0; i < 10; i++) city.Tick();
        Check(city.levels[CityModel.Index(2,2)] == 0, "Isolated zone cannot grow");
        Check(!city.Place(0,18,LandUse.Bulldoze,out _), "Entry protected");
        city.Place(1,18,LandUse.Bulldoze,out _);
        Check(city.power == 0 && city.water == 0 && city.population == 0, "Road disconnection removes service");
        city.Place(1,18,LandUse.Road,out _);
        Check(city.power == 160 && city.population > 0, "Road reconnection restores service");
        city.money = 0;
        Check(!city.Place(1,1,LandUse.Power,out _) && city.Get(1,1) == LandUse.Empty, "Insufficient funds preserve map");
        city.levels[0] = 4;
        Check(!city.Valid(), "Malformed levels rejected");
        city = CityModel.Create();
        city.Place(5,19,LandUse.Bulldoze,out _);
        int sum = 0;
        foreach (int level in city.levels) sum += level;
        for (int i = 0; i < 20; i++) city.Tick();
        int after = 0;
        foreach (int level in city.levels) after += level;
        Check(after == sum, "No development without power");
        Console.WriteLine("PASS: " + checks + " simulation checks");
        TrafficChecks.Run();
    }
}
