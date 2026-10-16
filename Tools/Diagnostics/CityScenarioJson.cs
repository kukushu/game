#if CITY_SCENARIO_DOTNET
using System.Text.Json;
// Test-host I/O adapter only. Binary city saves still use the unchanged CityCoreSave.
internal sealed class CityScenarioJson
{
    public int MaxJsonLength {get;set;}
    readonly JsonSerializerOptions options=new JsonSerializerOptions{IncludeFields=true,MaxDepth=128};
    public string Serialize(object value)=>JsonSerializer.Serialize(value,value.GetType(),options);
    public T Deserialize<T>(string json)=>JsonSerializer.Deserialize<T>(json,options);
}
#endif
