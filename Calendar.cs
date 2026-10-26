using TMPro;
using UnityEngine;

public class Calendar : MonoBehaviour
{
	private int Day = 1;

	private int WeekDay = 1;

	private int Month = 5;

	private string sMonth = "MAY";

	private int Year = 2021;

	private int LeapYear = 2024;

	public int tWeeks = 1;

	public int tMonths = 1;

	public int tYears = 1;

	public Budget b;

	public Economy eco;

	public TextMeshProUGUI Date;

	public int[] popArray = new int[25];

	public static int Approval_LastMonth;

	public static int Unemployment_LastMonth;

	public static int FI_LastYear;

	public static int CR_LastYear;

	private int yearSinceGC;

	public void NextDay()
	{
		if (Date == null)
		{
			Date = GetComponent<TextMeshProUGUI>();
			Debug.Log("Calendar Date Ref Retrieved");
		}
		if (b == null && eco != null)
		{
			b = eco.budget;
			Debug.Log("Calendar Budget Ref Retrieved");
		}
		Day++;
		WeekDay++;
		if (WeekDay == 7)
		{
			RecordData(0);
			WeekDay = 0;
		}
		if (Month == 2 && Day == 28 && Year != LeapYear)
		{
			Month++;
			SetMonthString();
			Day = 1;
			b.CashIn();
			RecordData(1);
		}
		else if (Month == 2 && Day == 29 && Year == LeapYear)
		{
			Month++;
			SetMonthString();
			Day = 1;
			LeapYear += 4;
			b.CashIn();
			RecordData(1);
		}
		else if (Day == 31)
		{
			if (Month == 4 || Month == 6 || Month == 9 || Month == 11)
			{
				Month++;
				SetMonthString();
				Day = 1;
				b.CashIn();
				RecordData(1);
			}
			else if (Month == 12)
			{
				Month = 1;
				SetMonthString();
				Day = 1;
				Year++;
				b.CashIn();
				RecordData(1);
				RecordData(2);
				yearSinceGC++;
				if (yearSinceGC == 10)
				{
					Resources.UnloadUnusedAssets();
					yearSinceGC = 0;
				}
			}
		}
		else if (Day == 32 && (Month == 1 || Month == 3 || Month == 5 || Month == 7 || Month == 8 || Month == 10))
		{
			Month++;
			SetMonthString();
			Day = 1;
			b.CashIn();
			RecordData(1);
		}
		UpdateDateUI();
	}

	private void UpdateDateUI()
	{
		if (LOCSYS.language == LOCSYS.Language.English)
		{
			Date.text = LOCSYS.Translate(sMonth) + " " + Day + ", " + Year;
		}
		else
		{
			Date.text = Day + " " + LOCSYS.Translate(sMonth) + " " + Year;
		}
	}

	private void SetMonthString()
	{
		switch (Month)
		{
		case 1:
			sMonth = "JAN";
			break;
		case 2:
			sMonth = "FEB";
			break;
		case 3:
			sMonth = "MAR";
			break;
		case 4:
			sMonth = "APR";
			break;
		case 5:
			sMonth = "MAY";
			break;
		case 6:
			sMonth = "JUN";
			break;
		case 7:
			sMonth = "JUL";
			break;
		case 8:
			sMonth = "AUG";
			break;
		case 9:
			sMonth = "SEP";
			break;
		case 10:
			sMonth = "OCT";
			break;
		case 11:
			sMonth = "NOV";
			break;
		case 12:
			sMonth = "DEC";
			break;
		}
	}

	private void RecordData(int timeType)
	{
		int num;
		switch (timeType)
		{
		case 0:
			tWeeks++;
			if (tWeeks > 25)
			{
				tWeeks = 0;
			}
			num = tWeeks;
			break;
		case 1:
			tMonths++;
			if (tMonths > 25)
			{
				tMonths = 0;
			}
			num = tMonths;
			break;
		default:
			tYears++;
			if (tYears > 25)
			{
				tYears = 0;
			}
			num = tYears;
			break;
		}
		eco.data[num, timeType, 0] = Mathf.RoundToInt(eco.Population);
		eco.data[num, timeType, 1] = Mathf.RoundToInt(eco.LCPop);
		eco.data[num, timeType, 2] = Mathf.RoundToInt(eco.MCPop);
		eco.data[num, timeType, 3] = Mathf.RoundToInt(eco.UCPop);
		eco.data[num, timeType, 4] = Mathf.RoundToInt(eco.InformalPop);
		if (num != 0)
		{
			eco.data[num, timeType, 5] = Mathf.RoundToInt(GetPopGrowth(num, timeType) * 10f);
		}
		else
		{
			eco.data[num, timeType, 5] = Mathf.RoundToInt(GetPopGrowth(25, timeType) * 10f);
		}
		if (timeType == 2)
		{
			Unemployment_LastMonth = eco.data[num, timeType, 6];
		}
		eco.data[num, timeType, 6] = Mathf.RoundToInt(eco.Unemployment);
		eco.data[num, timeType, 7] = Mathf.RoundToInt(eco.Inequality);
		if (timeType == 2)
		{
			Approval_LastMonth = eco.data[num, timeType, 8];
		}
		eco.data[num, timeType, 8] = Mathf.RoundToInt(eco.ApprovalRate);
		eco.data[num, timeType, 10] = Mathf.RoundToInt(eco.GDP / 100);
		if (num != 0)
		{
			eco.data[num, timeType, 9] = Mathf.RoundToInt(GetGDPGrowth(num, timeType) * 10f);
		}
		else
		{
			eco.data[num, timeType, 9] = Mathf.RoundToInt(GetGDPGrowth(25, timeType) * 10f);
		}
		if (timeType == 1)
		{
			eco.GDPgrowth = eco.data[num, 1, 9];
		}
		if (timeType == 2)
		{
			FI_LastYear = eco.data[num, timeType, 11];
		}
		eco.data[num, timeType, 11] = Mathf.RoundToInt(eco.FreedomIndex);
		if (timeType == 2)
		{
			CR_LastYear = eco.data[num, timeType, 12];
		}
		eco.data[num, timeType, 12] = Mathf.RoundToInt(eco.CivilRights);
		eco.data[num, timeType, 13] = Mathf.RoundToInt(eco.EducationLevel);
		eco.data[num, timeType, 14] = Mathf.RoundToInt(eco.HealthScore);
		eco.data[num, timeType, 15] = Mathf.RoundToInt(eco.SecurityScore);
		eco.data[num, timeType, 16] = Mathf.RoundToInt(eco.ReligionScore);
		eco.data[num, timeType, 17] = Mathf.RoundToInt(eco.BirthRate);
		eco.data[num, timeType, 18] = Mathf.RoundToInt(eco.Corruption);
	}

	private float GetPopGrowth(int timePosX, int timeType)
	{
		float num = eco.data[timePosX - 1, timeType, 0];
		return (float)eco.data[timePosX, timeType, 0] * 100f / num - 100f;
	}

	private float GetGDPGrowth(int timePosX, int timeType)
	{
		float num = eco.data[timePosX - 1, timeType, 10];
		return (float)eco.data[timePosX, timeType, 10] * 100f / num - 100f;
	}
}
