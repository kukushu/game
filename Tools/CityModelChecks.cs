using System;
public static class CityModelChecks
{
    public static void Main()
    {
        EntityChecks.Run();RoadChecks.Run();BuildingChecks.Run();HouseholdChecks.Run();
        TrafficChecks.Run();ResidentTransportChecks.Run();JobChecks.Run();IndustryChecks.Run();
        FactoryFinanceChecks.Run();AnalysisChecks.Run();ObservabilityChecks.Run();DashboardVisualChecks.Run();LogChecks.Run();
    }
}
