namespace ES3Types;

public class ES3UserType_WorldArray : ES3ArrayType
{
	public static ES3Type Instance;

	public ES3UserType_WorldArray()
		: base(typeof(World[]), ES3UserType_World.Instance)
	{
		Instance = this;
	}
}
