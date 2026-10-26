using TMPro;
using UnityEngine.Scripting;

namespace ES3Types;

[Preserve]
[ES3Properties(new string[]
{
	"Day", "WeekDay", "Month", "sMonth", "Year", "LeapYear", "tWeeks", "tMonths", "tYears", "b",
	"eco", "Date", "popArray", "Approval_LastMonth", "Unemployment_LastMonth", "FI_LastYear", "CR_LastYear"
})]
public class ES3UserType_Calendar : ES3ComponentType
{
	public static ES3Type Instance;

	public ES3UserType_Calendar()
		: base(typeof(Calendar))
	{
		Instance = this;
		priority = 1;
	}

	protected override void WriteComponent(object obj, ES3Writer writer)
	{
		Calendar calendar = (Calendar)obj;
		writer.WritePrivateField("Day", calendar);
		writer.WritePrivateField("WeekDay", calendar);
		writer.WritePrivateField("Month", calendar);
		writer.WritePrivateField("sMonth", calendar);
		writer.WritePrivateField("Year", calendar);
		writer.WritePrivateField("LeapYear", calendar);
		writer.WriteProperty("tWeeks", calendar.tWeeks, ES3Type_int.Instance);
		writer.WriteProperty("tMonths", calendar.tMonths, ES3Type_int.Instance);
		writer.WriteProperty("tYears", calendar.tYears, ES3Type_int.Instance);
		writer.WritePropertyByRef("b", calendar.b);
		writer.WritePropertyByRef("eco", calendar.eco);
		writer.WritePropertyByRef("Date", calendar.Date);
		writer.WriteProperty("popArray", calendar.popArray, ES3Type_intArray.Instance);
		writer.WriteProperty("Approval_LastMonth", Calendar.Approval_LastMonth, ES3Type_int.Instance);
		writer.WriteProperty("Unemployment_LastMonth", Calendar.Unemployment_LastMonth, ES3Type_int.Instance);
		writer.WriteProperty("FI_LastYear", Calendar.FI_LastYear, ES3Type_int.Instance);
		writer.WriteProperty("CR_LastYear", Calendar.CR_LastYear, ES3Type_int.Instance);
	}

	protected override void ReadComponent<T>(ES3Reader reader, object obj)
	{
		Calendar calendar = (Calendar)obj;
		foreach (string property in reader.Properties)
		{
			switch (property)
			{
			case "Day":
				reader.SetPrivateField("Day", reader.Read<int>(), calendar);
				break;
			case "WeekDay":
				reader.SetPrivateField("WeekDay", reader.Read<int>(), calendar);
				break;
			case "Month":
				reader.SetPrivateField("Month", reader.Read<int>(), calendar);
				break;
			case "sMonth":
				reader.SetPrivateField("sMonth", reader.Read<string>(), calendar);
				break;
			case "Year":
				reader.SetPrivateField("Year", reader.Read<int>(), calendar);
				break;
			case "LeapYear":
				reader.SetPrivateField("LeapYear", reader.Read<int>(), calendar);
				break;
			case "tWeeks":
				calendar.tWeeks = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "tMonths":
				calendar.tMonths = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "tYears":
				calendar.tYears = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "b":
				calendar.b = reader.Read<Budget>(ES3UserType_Budget.Instance);
				break;
			case "eco":
				calendar.eco = reader.Read<Economy>(ES3UserType_Economy.Instance);
				break;
			case "Date":
				calendar.Date = reader.Read<TextMeshProUGUI>();
				break;
			case "popArray":
				calendar.popArray = reader.Read<int[]>(ES3Type_intArray.Instance);
				break;
			case "Approval_LastMonth":
				Calendar.Approval_LastMonth = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "Unemployment_LastMonth":
				Calendar.Unemployment_LastMonth = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "FI_LastYear":
				Calendar.FI_LastYear = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "CR_LastYear":
				Calendar.CR_LastYear = reader.Read<int>(ES3Type_int.Instance);
				break;
			default:
				reader.Skip();
				break;
			}
		}
	}
}
