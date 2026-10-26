using UnityEngine.Scripting;

namespace ES3Types;

[Preserve]
[ES3Properties(new string[] { "AirportConnections", "LA_Border", "ImmiLeaning", "A_Border", "SEA_Border" })]
public class ES3UserType_Immigration : ES3ComponentType
{
	public static ES3Type Instance;

	public ES3UserType_Immigration()
		: base(typeof(Immigration))
	{
		Instance = this;
		priority = 1;
	}

	protected override void WriteComponent(object obj, ES3Writer writer)
	{
		Immigration immigration = (Immigration)obj;
		writer.WriteProperty("AirportConnections", immigration.AirportConnections, ES3Type_int.Instance);
		writer.WriteProperty("LA_Border", immigration.LA_Border, ES3Type_float.Instance);
		writer.WriteProperty("ImmiLeaning", immigration.ImmiLeaning, ES3Type_float.Instance);
		writer.WriteProperty("A_Border", immigration.A_Border, ES3Type_float.Instance);
		writer.WriteProperty("SEA_Border", immigration.SEA_Border, ES3Type_float.Instance);
	}

	protected override void ReadComponent<T>(ES3Reader reader, object obj)
	{
		Immigration immigration = (Immigration)obj;
		foreach (string property in reader.Properties)
		{
			switch (property)
			{
			case "AirportConnections":
				immigration.AirportConnections = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "LA_Border":
				immigration.LA_Border = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "ImmiLeaning":
				immigration.ImmiLeaning = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "A_Border":
				immigration.A_Border = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "SEA_Border":
				immigration.SEA_Border = reader.Read<float>(ES3Type_float.Instance);
				break;
			default:
				reader.Skip();
				break;
			}
		}
	}
}
