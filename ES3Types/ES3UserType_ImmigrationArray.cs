namespace ES3Types;

public class ES3UserType_ImmigrationArray : ES3ArrayType
{
	public static ES3Type Instance;

	public ES3UserType_ImmigrationArray()
		: base(typeof(Immigration[]), ES3UserType_Immigration.Instance)
	{
		Instance = this;
	}
}
