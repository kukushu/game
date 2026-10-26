using UnityEngine.Scripting;

namespace ES3Types;

[Preserve]
[ES3Properties(new string[] { "step" })]
public class ES3UserType_Messages : ES3ComponentType
{
	public static ES3Type Instance;

	public ES3UserType_Messages()
		: base(typeof(Messages))
	{
		Instance = this;
		priority = 1;
	}

	protected override void WriteComponent(object obj, ES3Writer writer)
	{
		_ = (Messages)obj;
		writer.WriteProperty("step", Messages.step, ES3Type_int.Instance);
	}

	protected override void ReadComponent<T>(ES3Reader reader, object obj)
	{
		_ = (Messages)obj;
		foreach (string property in reader.Properties)
		{
			if (property == "step")
			{
				Messages.step = reader.Read<int>(ES3Type_int.Instance);
			}
			else
			{
				reader.Skip();
			}
		}
	}
}
