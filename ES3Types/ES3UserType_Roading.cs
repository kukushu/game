using ES3Internal;
using UnityEngine.Scripting;

namespace ES3Types;

[Preserve]
[ES3Properties(new string[] { "rD", "rG" })]
public class ES3UserType_Roading : ES3ComponentType
{
	public static ES3Type Instance;

	public ES3UserType_Roading()
		: base(typeof(Roading))
	{
		Instance = this;
		priority = 1;
	}

	protected override void WriteComponent(object obj, ES3Writer writer)
	{
		Roading roading = (Roading)obj;
		writer.WriteProperty("rD", roading.rD, ES3TypeMgr.GetES3Type(typeof(RoadCreator[,,])));
		writer.WriteProperty("rG", roading.rG, ES3TypeMgr.GetES3Type(typeof(RoadCreator[,,])));
	}

	protected override void ReadComponent<T>(ES3Reader reader, object obj)
	{
		Roading roading = (Roading)obj;
		foreach (string property in reader.Properties)
		{
			if (!(property == "rD"))
			{
				if (property == "rG")
				{
					roading.rG = reader.Read<RoadCreator[,,]>();
				}
				else
				{
					reader.Skip();
				}
			}
			else
			{
				roading.rD = reader.Read<RoadCreator[,,]>();
			}
		}
	}
}
