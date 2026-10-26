using UnityEngine;
using UnityEngine.Scripting;

namespace ES3Types;

[Preserve]
[ES3Properties(new string[] { "lastCamPos", "padPos" })]
public class ES3UserType_Indexer : ES3ComponentType
{
	public static ES3Type Instance;

	public ES3UserType_Indexer()
		: base(typeof(Indexer))
	{
		Instance = this;
		priority = 1;
	}

	protected override void WriteComponent(object obj, ES3Writer writer)
	{
		Indexer indexer = (Indexer)obj;
		writer.WriteProperty("lastCamPos", indexer.lastCamPos, ES3Type_Vector3.Instance);
		writer.WriteProperty("padPos", Indexer.padPos, ES3Type_Vector3.Instance);
	}

	protected override void ReadComponent<T>(ES3Reader reader, object obj)
	{
		Indexer indexer = (Indexer)obj;
		foreach (string property in reader.Properties)
		{
			if (!(property == "lastCamPos"))
			{
				if (property == "padPos")
				{
					Indexer.padPos = reader.Read<Vector3>(ES3Type_Vector3.Instance);
				}
				else
				{
					reader.Skip();
				}
			}
			else
			{
				indexer.lastCamPos = reader.Read<Vector3>(ES3Type_Vector3.Instance);
			}
		}
	}
}
