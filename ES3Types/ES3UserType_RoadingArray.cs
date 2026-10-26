namespace ES3Types;

public class ES3UserType_RoadingArray : ES3ArrayType
{
	public static ES3Type Instance;

	public ES3UserType_RoadingArray()
		: base(typeof(Roading[]), ES3UserType_Roading.Instance)
	{
		Instance = this;
	}
}
