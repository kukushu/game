namespace ES3Types;

public class ES3UserType_MessagesArray : ES3ArrayType
{
	public static ES3Type Instance;

	public ES3UserType_MessagesArray()
		: base(typeof(Messages[]), ES3UserType_Messages.Instance)
	{
		Instance = this;
	}
}
