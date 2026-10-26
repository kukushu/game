namespace ES3Types;

public class ES3UserType_BudgetArray : ES3ArrayType
{
	public static ES3Type Instance;

	public ES3UserType_BudgetArray()
		: base(typeof(Budget[]), ES3UserType_Budget.Instance)
	{
		Instance = this;
	}
}
