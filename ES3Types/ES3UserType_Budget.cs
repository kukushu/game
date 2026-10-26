using UnityEngine.Scripting;

namespace ES3Types;

[Preserve]
[ES3Properties(new string[] { "R_Tax", "O_Tax", "M_Tax", "HT_Tax", "P_Tax", "LC_Tax", "MC_Tax", "UC_Tax", "Pro_Tax", "Share_Rate" })]
public class ES3UserType_Budget : ES3ComponentType
{
	public static ES3Type Instance;

	public ES3UserType_Budget()
		: base(typeof(Budget))
	{
		Instance = this;
		priority = 1;
	}

	protected override void WriteComponent(object obj, ES3Writer writer)
	{
		Budget budget = (Budget)obj;
		writer.WriteProperty("R_Tax", budget.R_Tax, ES3Type_float.Instance);
		writer.WriteProperty("O_Tax", budget.O_Tax, ES3Type_float.Instance);
		writer.WriteProperty("M_Tax", budget.M_Tax, ES3Type_float.Instance);
		writer.WriteProperty("HT_Tax", budget.HT_Tax, ES3Type_float.Instance);
		writer.WriteProperty("LC_Tax", budget.LC_Tax, ES3Type_float.Instance);
		writer.WriteProperty("MC_Tax", budget.MC_Tax, ES3Type_float.Instance);
		writer.WriteProperty("UC_Tax", budget.UC_Tax, ES3Type_float.Instance);
		writer.WriteProperty("Pro_Tax", budget.Pro_Tax, ES3Type_float.Instance);
		writer.WriteProperty("Share_Rate", budget.Share_Rate, ES3Type_float.Instance);
	}

	protected override void ReadComponent<T>(ES3Reader reader, object obj)
	{
		Budget budget = (Budget)obj;
		foreach (string property in reader.Properties)
		{
			switch (property)
			{
			case "R_Tax":
				budget.R_Tax = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "O_Tax":
				budget.O_Tax = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "M_Tax":
				budget.M_Tax = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "HT_Tax":
				budget.HT_Tax = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "LC_Tax":
				budget.LC_Tax = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "MC_Tax":
				budget.MC_Tax = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "UC_Tax":
				budget.UC_Tax = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "Pro_Tax":
				budget.Pro_Tax = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "Share_Rate":
				budget.Share_Rate = reader.Read<float>(ES3Type_float.Instance);
				break;
			default:
				reader.Skip();
				break;
			}
		}
	}
}
