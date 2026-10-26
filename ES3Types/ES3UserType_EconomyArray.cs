namespace ES3Types;

public class ES3UserType_EconomyArray : ES3ArrayType
{
	public static ES3Type Instance;

	public ES3UserType_EconomyArray()
		: base(typeof(Economy[]), ES3UserType_Economy.Instance)
	{
		Instance = this;
	}
}
