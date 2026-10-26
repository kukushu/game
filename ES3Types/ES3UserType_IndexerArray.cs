namespace ES3Types;

public class ES3UserType_IndexerArray : ES3ArrayType
{
	public static ES3Type Instance;

	public ES3UserType_IndexerArray()
		: base(typeof(Indexer[]), ES3UserType_Indexer.Instance)
	{
		Instance = this;
	}
}
