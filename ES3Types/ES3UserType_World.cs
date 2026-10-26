using UnityEngine.Scripting;

namespace ES3Types;

[Preserve]
[ES3Properties(new string[] { "worldSize", "cityName", "nameCode", "RegionMap" })]
public class ES3UserType_World : ES3ComponentType
{
	public static ES3Type Instance;

	public ES3UserType_World()
		: base(typeof(World))
	{
		Instance = this;
		priority = 1;
	}

	protected override void WriteComponent(object obj, ES3Writer writer)
	{
		World world = (World)obj;
		writer.WriteProperty("worldSize", World.worldSize, ES3Type_int.Instance);
		writer.WriteProperty("cityName", World.cityName, ES3Type_string.Instance);
		writer.WriteProperty("nameCode", world.nameCode, ES3Type_string.Instance);
		writer.WriteProperty("RegionMap", world.RegionMap, ES3Type_string.Instance);
	}

	protected override void ReadComponent<T>(ES3Reader reader, object obj)
	{
		World world = (World)obj;
		foreach (string property in reader.Properties)
		{
			switch (property)
			{
			case "worldSize":
				World.worldSize = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "cityName":
				World.cityName = reader.Read<string>(ES3Type_string.Instance);
				break;
			case "nameCode":
				world.nameCode = reader.Read<string>(ES3Type_string.Instance);
				break;
			case "RegionMap":
				world.RegionMap = reader.Read<string>(ES3Type_string.Instance);
				break;
			default:
				reader.Skip();
				break;
			}
		}
	}
}
