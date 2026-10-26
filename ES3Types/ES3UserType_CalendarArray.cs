namespace ES3Types;

public class ES3UserType_CalendarArray : ES3ArrayType
{
	public static ES3Type Instance;

	public ES3UserType_CalendarArray()
		: base(typeof(Calendar[]), ES3UserType_Calendar.Instance)
	{
		Instance = this;
	}
}
