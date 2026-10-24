using UnityEditor;

// Research project only; prevents the normal-Play Save/Load test touching user files.
public static class HarborCityRoutingIntegrationBuild
{
    public static void Build()
    {
        PlayerSettings.companyName="HarborCityIntegration";PlayerSettings.productName="LaneRoadBatching20261024";
        HarborCity.CityPlayerBuild.BuildResearchValidation();
    }
}
