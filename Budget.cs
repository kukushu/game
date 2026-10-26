using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Budget : MonoBehaviour
{
	public Economy eco;

	public Stats stats;

	public SoundManager sm;

	public GameObject budgetPanel;

	public float Balance = 30000f;

	public static float Expenses = 0f;

	public static float Income = 0f;

	public static float CashFlow = 0f;

	public Color green;

	public Color red;

	public Color white;

	public static float ExchangeRate = 2f;

	public float Inflation;

	public static float DollarPrice = 2000f;

	public float Imports;

	public float Exports;

	public float TradeCapacity = 1f;

	public float otherCitiesTradeUse;

	public float TradeBalanceRaw;

	public float TradeBalanceReal;

	public float R_Tax = 5f;

	public float O_Tax = 7f;

	public float M_Tax = 7f;

	public float HT_Tax = 7f;

	public readonly float P_Tax = 10f;

	public float LC_Tax = 14f;

	public float MC_Tax = 15f;

	public float UC_Tax = 16f;

	public float Pro_Tax = 1f;

	public float Share_Rate = 1f;

	public float R_Income;

	public float O_Income;

	public float M_Income;

	public float HT_Income;

	public float P_Income;

	public float LC_Income;

	public float MC_Income;

	public float UC_Income;

	public float Pro_Income;

	public float Policies_Income;

	public float R_Effect;

	public float O_Effect;

	public float M_Effect;

	public float HT_Effect;

	public float LC_Effect;

	public float MC_Effect;

	public float UC_Effect;

	public float Pro_Effect;

	public float R_Approval;

	public float O_Approval;

	public float M_Approval;

	public float HT_Approval;

	public float P_Approval;

	public float LC_Approval;

	public float MC_Approval;

	public float UC_Approval;

	public float Pro_Approval;

	public float Gov_Expenses;

	public float Edu_Expenses;

	public float Health_Expenses;

	public float Secu_Expenses;

	public float Water_Expenses;

	public float Energy_Expenses;

	public float Trans_Expenses;

	public float Parks_Expenses;

	public float State_Expenses;

	public float Bonds_Expenses;

	public TextMeshProUGUI UI_Balance;

	public TextMeshProUGUI UI_Expenses;

	public TextMeshProUGUI UI_Income;

	public TextMeshProUGUI UI_MainCashFlow;

	public TextMeshProUGUI UI_BudgetCashFlow;

	public TextMeshProUGUI UI_R_Tax;

	public TextMeshProUGUI UI_O_Tax;

	public TextMeshProUGUI UI_M_Tax;

	public TextMeshProUGUI UI_HT_Tax;

	public TextMeshProUGUI UI_LC_Tax;

	public TextMeshProUGUI UI_MC_Tax;

	public TextMeshProUGUI UI_UC_Tax;

	public TextMeshProUGUI UI_Pro_Tax;

	public TextMeshProUGUI UI_Shared_Tax;

	public TextMeshProUGUI UI_R_Income;

	public TextMeshProUGUI UI_O_Income;

	public TextMeshProUGUI UI_M_Income;

	public TextMeshProUGUI UI_HT_Income;

	public TextMeshProUGUI UI_LC_Income;

	public TextMeshProUGUI UI_MC_Income;

	public TextMeshProUGUI UI_UC_Income;

	public TextMeshProUGUI UI_Pro_Income;

	public TextMeshProUGUI UI_Policies_Income;

	public TextMeshProUGUI UI_R_Effect;

	public TextMeshProUGUI UI_O_Effect;

	public TextMeshProUGUI UI_M_Effect;

	public TextMeshProUGUI UI_HT_Effect;

	public TextMeshProUGUI UI_ExchangeRate;

	public TextMeshProUGUI UI_Inflation;

	public TextMeshProUGUI UI_DollarBuyPrice;

	public TextMeshProUGUI UI_DollarSellPrice;

	public TextMeshProUGUI UI_NationNameBank;

	public TextMeshProUGUI UI_MoneyPrinted;

	public TextMeshProUGUI UI_DollarReserves;

	public TextMeshProUGUI UI_R_Approval;

	public TextMeshProUGUI UI_LC_Approval;

	public TextMeshProUGUI UI_MC_Approval;

	public TextMeshProUGUI UI_UC_Approval;

	public TextMeshProUGUI UI_Pro_Approval;

	public TextMeshProUGUI UI_Gov_Expenses;

	public TextMeshProUGUI UI_Edu_Expenses;

	public TextMeshProUGUI UI_Health_Expenses;

	public TextMeshProUGUI UI_Secu_Expenses;

	public TextMeshProUGUI UI_Services_Expenses;

	public TextMeshProUGUI UI_Energy_Expenses;

	public TextMeshProUGUI UI_Trans_Expenses;

	public TextMeshProUGUI UI_Parks_Expenses;

	public TextMeshProUGUI UI_State_Expenses;

	public TextMeshProUGUI UI_State_Treasury;

	public Button More_R_Tax;

	public Button More_O_Tax;

	public Button More_M_Tax;

	public Button More_HT_Tax;

	public Button More_LC_Tax;

	public Button More_MC_Tax;

	public Button More_UC_Tax;

	public Button More_Pro_Tax;

	public Button Less_R_Tax;

	public Button Less_O_Tax;

	public Button Less_M_Tax;

	public Button Less_HT_Tax;

	public Button Less_LC_Tax;

	public Button Less_MC_Tax;

	public Button Less_UC_Tax;

	public Button Less_Pro_Tax;

	private int printCount;

	public World w;

	public Immigration immi;

	public Roading roading;

	public int sharedTillNow;

	private static float LC_TaxPaid = 1.6f;

	private static float MC_TaxPaid = 2.2f;

	private static float UC_TaxPaid = 2.8f;

	private static float M_Profits = 250f;

	private static float HT_Profits = 160f;

	private static float R_Profits = 300f;

	private static float O_Profits = 180f;

	public int churchCount;

	public int edenCount;

	public void Click_More_R_Tax()
	{
		R_Tax += 0.5f;
		UI_R_Tax.text = R_Tax + "%";
		CheckTaxRatex();
		w.eco.Stability--;
	}

	public void Click_More_O_Tax()
	{
		O_Tax += 0.5f;
		UI_O_Tax.text = O_Tax + "%";
		CheckTaxRatex();
		w.eco.Stability--;
	}

	public void Click_More_M_Tax()
	{
		M_Tax += 0.5f;
		UI_M_Tax.text = M_Tax + "%";
		CheckTaxRatex();
		w.eco.Stability--;
	}

	public void Click_More_HT_Tax()
	{
		HT_Tax += 0.5f;
		UI_HT_Tax.text = HT_Tax + "%";
		CheckTaxRatex();
		w.eco.Stability--;
	}

	public void Click_More_LC_Tax()
	{
		LC_Tax += 0.5f;
		UI_LC_Tax.text = LC_Tax + "%";
		CheckTaxRatex();
		w.eco.Stability--;
	}

	public void Click_More_MC_Tax()
	{
		MC_Tax += 0.5f;
		UI_MC_Tax.text = MC_Tax + "%";
		CheckTaxRatex();
		w.eco.Stability--;
	}

	public void Click_More_UC_Tax()
	{
		UC_Tax += 0.5f;
		UI_UC_Tax.text = UC_Tax + "%";
		CheckTaxRatex();
		w.eco.Stability--;
	}

	public void Click_More_Pro_Tax()
	{
		Pro_Tax += 0.5f;
		UI_Pro_Tax.text = Pro_Tax + "%";
		CheckTaxRatex();
		w.eco.Stability--;
	}

	public void Click_More_Shared()
	{
		Share_Rate += 0.5f;
		UI_Shared_Tax.text = Share_Rate + "% (+" + DataRegion.ecoSettings[4].ToString("N0") + "%)";
		CheckTaxRatex();
	}

	public void Click_Less_R_Tax()
	{
		R_Tax -= 0.5f;
		UI_R_Tax.text = R_Tax + "%";
		CheckTaxRatex();
		w.eco.Stability -= 0.5f;
	}

	public void Click_Less_O_Tax()
	{
		O_Tax -= 0.5f;
		UI_O_Tax.text = O_Tax + "%";
		CheckTaxRatex();
		w.eco.Stability -= 0.5f;
	}

	public void Click_Less_M_Tax()
	{
		M_Tax -= 0.5f;
		UI_M_Tax.text = M_Tax + "%";
		CheckTaxRatex();
		w.eco.Stability -= 0.5f;
	}

	public void Click_Less_HT_Tax()
	{
		HT_Tax -= 0.5f;
		UI_HT_Tax.text = HT_Tax + "%";
		CheckTaxRatex();
		w.eco.Stability -= 0.5f;
	}

	public void Click_Less_LC_Tax()
	{
		LC_Tax -= 0.5f;
		UI_LC_Tax.text = LC_Tax + "%";
		CheckTaxRatex();
		w.eco.Stability -= 0.5f;
	}

	public void Click_Less_MC_Tax()
	{
		MC_Tax -= 0.5f;
		UI_MC_Tax.text = MC_Tax + "%";
		CheckTaxRatex();
		w.eco.Stability -= 0.5f;
	}

	public void Click_Less_UC_Tax()
	{
		UC_Tax -= 0.5f;
		UI_UC_Tax.text = UC_Tax + "%";
		CheckTaxRatex();
		w.eco.Stability -= 0.5f;
	}

	public void Click_Less_Pro_Tax()
	{
		Pro_Tax -= 0.5f;
		UI_Pro_Tax.text = Pro_Tax + "%";
		CheckTaxRatex();
		w.eco.Stability -= 0.5f;
	}

	public void Click_Less_Shared()
	{
		Share_Rate -= 0.5f;
		UI_Shared_Tax.text = Share_Rate + "% (+" + DataRegion.ecoSettings[4].ToString("N0") + "%)";
		CheckTaxRatex();
	}

	public void BuyDollars()
	{
		if (Balance < DollarPrice || DataRegion.DollarReserves > 1980000f)
		{
			sm.Play("Impractical");
			return;
		}
		DataRegion.DollarReserves += 20000f;
		Balance -= DollarPrice;
		w.eco.Stability += 15f;
		UpdateInflation();
		UpadteBudgetUI();
		UI_Balance.text = "$" + Balance.ToString("N0");
		if (Balance < 1f)
		{
			UI_Balance.color = red;
		}
		else
		{
			UI_Balance.color = white;
		}
	}

	public void SellDollars()
	{
		if (DataRegion.DollarReserves < 1f)
		{
			sm.Play("Impractical");
			return;
		}
		DataRegion.DollarReserves -= 20000f;
		Balance += DollarPrice;
		w.eco.Stability -= 15f;
		UpdateInflation();
		UpadteBudgetUI();
		UI_Balance.text = "$" + Balance.ToString("N0");
		if (Balance < 1f)
		{
			UI_Balance.color = red;
		}
		else
		{
			UI_Balance.color = white;
		}
	}

	public void PrintMoney()
	{
		DataRegion.MoneyPrinted += 10000f;
		printCount++;
		Balance += 10000f;
		w.eco.Stability -= 15f;
		UpdateInflation();
		UpadteBudgetUI();
		UI_Balance.text = "$" + Balance.ToString("N0");
		if (Balance < 1f)
		{
			UI_Balance.color = red;
		}
		else
		{
			UI_Balance.color = white;
		}
		if (printCount > 2 && !eco.messages.achievements[25])
		{
			SteamAchievements.script.UnlockAchievement("print");
			eco.messages.achievements[25] = true;
		}
	}

	public void CheckTaxRatex()
	{
		if (R_Tax < 0f)
		{
			R_Tax = 0f;
			UI_R_Tax.text = R_Tax + "%";
		}
		else if (R_Tax > 20f)
		{
			R_Tax = 20f;
			UI_R_Tax.text = R_Tax + "%";
		}
		if (O_Tax < 0f)
		{
			O_Tax = 0f;
			UI_O_Tax.text = O_Tax + "%";
		}
		else if (O_Tax > 20f)
		{
			O_Tax = 20f;
			UI_O_Tax.text = O_Tax + "%";
		}
		if (M_Tax < 0f)
		{
			M_Tax = 0f;
			UI_M_Tax.text = M_Tax + "%";
		}
		else if (M_Tax > 20f)
		{
			M_Tax = 20f;
			UI_M_Tax.text = M_Tax + "%";
		}
		if (HT_Tax < 0f)
		{
			HT_Tax = 0f;
			UI_HT_Tax.text = HT_Tax + "%";
		}
		else if (HT_Tax > 20f)
		{
			HT_Tax = 20f;
			UI_HT_Tax.text = HT_Tax + "%";
		}
		if (LC_Tax < 0f)
		{
			LC_Tax = 0f;
			UI_LC_Tax.text = LC_Tax + "%";
		}
		else if (LC_Tax > 75f)
		{
			LC_Tax = 75f;
			UI_LC_Tax.text = LC_Tax + "%";
		}
		if (MC_Tax < 0f)
		{
			MC_Tax = 0f;
			UI_MC_Tax.text = MC_Tax + "%";
		}
		else if (MC_Tax > 75f)
		{
			MC_Tax = 75f;
			UI_MC_Tax.text = MC_Tax + "%";
		}
		if (UC_Tax < 0f)
		{
			UC_Tax = 0f;
			UI_UC_Tax.text = UC_Tax + "%";
		}
		else if (UC_Tax > 75f)
		{
			UC_Tax = 75f;
			UI_UC_Tax.text = UC_Tax + "%";
		}
		if (Pro_Tax < 0f)
		{
			Pro_Tax = 0f;
			UI_Pro_Tax.text = Pro_Tax + "%";
		}
		else if (Pro_Tax > 10f)
		{
			Pro_Tax = 10f;
			UI_Pro_Tax.text = Pro_Tax + "%";
		}
		if (Share_Rate < -75f)
		{
			Share_Rate = -75f;
			UI_Shared_Tax.text = Share_Rate + "% (+" + DataRegion.ecoSettings[4].ToString("N0") + "%)";
		}
		else if (Share_Rate > 75f)
		{
			Share_Rate = 75f;
			UI_Shared_Tax.text = Share_Rate + "% (+" + DataRegion.ecoSettings[4].ToString("N0") + "%)";
		}
		UI_Shared_Tax.text = Share_Rate + "% (+" + DataRegion.ecoSettings[4].ToString("N0") + "%)";
		UpadteBudgetUI();
	}

	private void Start()
	{
		StartCoroutine(UpdateCashFlow());
		StartCoroutine(DelayBudgetStart());
	}

	private IEnumerator UpdateCashFlow()
	{
		while (true)
		{
			UpdateInflation();
			float tLC_Income = 0f;
			float tMC_Income = 0f;
			float tUC_Income = 0f;
			float tM_Income = 0f;
			float tHT_Income = 0f;
			float tR_Income = 0f;
			float tO_Income = 0f;
			float tPRO_Income = 0f;
			float _imports = 1f;
			float _exports = 1f;
			int num = 0;
			foreach (LotCreator lc in w.ResidentialBuildings.ToList())
			{
				num++;
				if (num > 48)
				{
					yield return new WaitForEndOfFrame();
					num = 0;
				}
				tPRO_Income += GetPropertySize(lc) * Pro_Tax / 10f;
				if (lc.lClass == LotCreator.LotClass.LOW)
				{
					float num2 = (float)lc.Occupants * LC_TaxPaid * (LC_Tax / 100f);
					lc.TaxIncome = num2 * w.incomeMultiplier;
					tLC_Income += num2 * w.incomeMultiplier;
				}
				else if (lc.lClass == LotCreator.LotClass.MIDDLE)
				{
					float num3 = (float)lc.Occupants * MC_TaxPaid * (MC_Tax / 100f);
					lc.TaxIncome = num3 * w.incomeMultiplier;
					tMC_Income += num3 * w.incomeMultiplier;
				}
				else if (lc.lClass == LotCreator.LotClass.UPPER)
				{
					float num4 = (float)lc.Occupants * UC_TaxPaid * (UC_Tax / 100f);
					lc.TaxIncome = num4 * w.incomeMultiplier;
					tUC_Income += num4 * w.incomeMultiplier;
				}
			}
			foreach (LotCreator lc in w.MixedBuildings.ToList())
			{
				num++;
				if (num > 48)
				{
					yield return new WaitForEndOfFrame();
					num = 0;
				}
				tPRO_Income += GetPropertySize(lc) * Pro_Tax / 10f;
				if (lc.lClass == LotCreator.LotClass.MIXRETAIL)
				{
					float num5 = (float)lc.Occupants * MC_TaxPaid * (MC_Tax / 100f) + ((float)lc.Stage + 0.75f * (float)(lc.Stage * lc.Stage) * R_Profits * (R_Tax / 100f));
					lc.TaxIncome = num5 * w.incomeMultiplier;
					tMC_Income += num5 * w.incomeMultiplier;
				}
				else if (lc.lClass == LotCreator.LotClass.MIXOFFICE)
				{
					float num6 = (float)lc.Occupants * UC_TaxPaid * (UC_Tax / 100f) + ((float)lc.Stage + 0.75f * (float)(lc.Stage * lc.Stage) * O_Profits * (O_Tax / 100f));
					lc.TaxIncome = num6 * w.incomeMultiplier;
					tUC_Income += num6 * w.incomeMultiplier;
				}
			}
			foreach (ServiceCreator item in w.Arcologies.ToList())
			{
				int num7 = Mathf.RoundToInt((float)item.MinEmployees * 1.5f * MC_TaxPaid * (MC_Tax / 100f));
				tPRO_Income += 36f * Pro_Tax / 10f;
				item.Cost = num7;
				tMC_Income += (float)num7;
			}
			LC_Income = Mathf.Round(tLC_Income);
			MC_Income = Mathf.Round(tMC_Income);
			UC_Income = Mathf.Round(tUC_Income);
			float activityStabilizer = (eco.ActivePercent - 0.6f) * 75f;
			foreach (LotCreator lc in w.IndustrialBuildings.ToList())
			{
				num++;
				if (num > 48)
				{
					yield return new WaitForEndOfFrame();
					num = 0;
				}
				tPRO_Income += GetPropertySize(lc) * Pro_Tax / 10f;
				if (lc.lClass == LotCreator.LotClass.MANUFACTURE)
				{
					float num8 = ((lc.Stage != 0 || lc.Occupants <= 0) ? ((float)lc.Stage + 0.75f * (float)(lc.Stage * lc.Stage) * M_Profits * (M_Tax / (100f + activityStabilizer))) : (0.01f * (float)lc.Occupants * M_Profits * (M_Tax / (100f + activityStabilizer))));
					lc.TaxIncome = num8 * w.incomeMultiplier;
					tM_Income += num8 * w.incomeMultiplier;
					lc.Trade = (float)lc.Occupants * 0.42f;
					_exports += lc.Trade;
				}
				else if (lc.lClass == LotCreator.LotClass.HIGHTECH)
				{
					float num9 = (float)lc.Stage + 0.75f * (float)(lc.Stage * lc.Stage) * HT_Profits * (HT_Tax / (100f + activityStabilizer));
					lc.TaxIncome = num9 * w.incomeMultiplier;
					tHT_Income += num9 * w.incomeMultiplier;
					lc.Trade = (float)lc.Occupants * 0.56f;
					_exports += lc.Trade;
				}
				else if (lc.lClass == LotCreator.LotClass.PRIMARY)
				{
					float num10 = (float)lc.Occupants * 0.3f;
					lc.TaxIncome = num10 * w.incomeMultiplier;
					tM_Income += num10 * w.incomeMultiplier;
					if (lc.Stage == 0)
					{
						lc.Trade = (float)lc.Occupants * 9.5f;
					}
					else
					{
						lc.Trade = (float)lc.Occupants * 1.75f;
					}
					_exports += lc.Trade;
				}
			}
			foreach (LotCreator lc in w.CommercialBuildings.ToList())
			{
				num++;
				if (num > 48)
				{
					yield return new WaitForEndOfFrame();
					num = 0;
				}
				tPRO_Income += GetPropertySize(lc) * Pro_Tax / 10f;
				if (lc.lClass == LotCreator.LotClass.RETAIL)
				{
					float num11 = ((lc.Stage != 0 || lc.Occupants <= 500) ? ((float)lc.Stage + 0.75f * (float)(lc.Stage * lc.Stage) * R_Profits * (R_Tax / (100f + activityStabilizer))) : (0.01f * (float)lc.Occupants * M_Profits * (M_Tax / (100f + activityStabilizer))));
					lc.TaxIncome = num11 * w.incomeMultiplier;
					tR_Income += num11 * w.incomeMultiplier;
					lc.Trade = (float)(-lc.Occupants) * 1.2f;
					_imports -= lc.Trade;
				}
				else if (lc.lClass == LotCreator.LotClass.OFFICE)
				{
					float num12 = (float)lc.Stage + 0.75f * (float)(lc.Stage * lc.Stage) * O_Profits * (O_Tax / (100f + activityStabilizer));
					lc.TaxIncome = num12 * w.incomeMultiplier;
					tO_Income += num12 * w.incomeMultiplier;
					lc.Trade = (float)(-lc.Occupants) * 0.05f;
					_imports -= lc.Trade;
				}
			}
			M_Income = Mathf.Round(tM_Income);
			HT_Income = Mathf.Round(tHT_Income);
			R_Income = Mathf.Round(tR_Income);
			O_Income = Mathf.Round(tO_Income);
			Pro_Income = tPRO_Income;
			Income = M_Income + HT_Income + R_Income + O_Income + Pro_Income + LC_Income + MC_Income + UC_Income;
			TradeBalanceRaw = _exports - _imports;
			Exports = _exports;
			Imports = _imports;
			LC_Effect = Mathf.Round(3f - Mathf.Pow(LC_Tax - 15f, 3f) / 2000f - 0.2f * LC_Tax) / 10f;
			MC_Effect = Mathf.Round(3f - Mathf.Pow(MC_Tax - 10f, 3f) / 3000f - 0.2f * MC_Tax) / 10f;
			UC_Effect = Mathf.Round(2f - Mathf.Pow(UC_Tax, 3f) / 3500f - 0.05f * UC_Tax) / 10f;
			R_Effect = 0.1f * (5f - R_Tax * R_Tax / 10f) * 2f;
			O_Effect = 0.1f * (5f - O_Tax * O_Tax / 10f) * 2f;
			HT_Effect = 0.1f * (5f - HT_Tax * HT_Tax / 10f) * 2f;
			M_Effect = 0.1f * (5f - M_Tax * M_Tax / 10f) * 2f;
			Pro_Effect = 1f + (-2f * (Pro_Tax * Pro_Tax) + 1f) / 30f;
			Pro_Effect = Mathf.Clamp(Pro_Effect, 0.1f, 1f);
			yield return new WaitForEndOfFrame();
			LC_Approval = Mathf.Round((5f - Mathf.Pow(LC_Tax, 3f) / 1000f - 0.3f * LC_Tax) * 10f) / 10f;
			MC_Approval = Mathf.Round((5f - Mathf.Pow(MC_Tax, 3f) / 2000f - 0.3f * MC_Tax) * 10f) / 10f;
			UC_Approval = Mathf.Round((10f - Mathf.Pow(UC_Tax, 3f) / 3000f - 0.2f * UC_Tax) * 10f) / 10f;
			R_Approval = Mathf.Round((1f - Mathf.Pow(R_Tax, 3f) / 20000f - 0.05f * R_Tax) * 10f) / 10f;
			Pro_Approval = Mathf.Round(-5f * Pro_Tax + 1f) * 10f / 10f;
			yield return new WaitForEndOfFrame();
			float tGov_Expenses = 0f;
			float tEdu_Expenses = 0f;
			float tHealth_Expenses = 0f;
			float tSecu_Expenses = 0f;
			float tWater_Expenses = 0f;
			float tEnergy_Expenses = 0f;
			float tTrans_Expenses = 0f;
			float tParks_Expenses = 0f;
			int delay = 0;
			foreach (RoadCreator rc in w.Roads.ToList())
			{
				delay++;
				if (delay >= 128)
				{
					delay = 0;
					yield return new WaitForEndOfFrame();
				}
				tTrans_Expenses += rc.maintenance;
			}
			foreach (RoadCreator rc in w.Rails.Concat(w.Underground).ToList())
			{
				delay++;
				if (delay >= 128)
				{
					delay = 0;
					yield return new WaitForEndOfFrame();
				}
				tTrans_Expenses += rc.maintenance;
			}
			float num13 = 0f;
			float num14 = 0f;
			int num15 = 0;
			float num16 = Mathf.Abs(Imports - Exports) * 100f / TradeCapacity / 100f;
			TradeCapacity = 1f + otherCitiesTradeUse;
			if (TradeCapacity < 1f)
			{
				TradeCapacity = 1f;
			}
			int num17 = 0;
			int num18 = 0;
			foreach (ServiceCreator item2 in w.Services.ToList())
			{
				if (item2.type == ServiceCreator.Type.School)
				{
					tEdu_Expenses += (float)item2.Cost;
				}
				else if (item2.type == ServiceCreator.Type.Hospital)
				{
					tHealth_Expenses += (float)item2.Cost;
				}
				else if (item2.type == ServiceCreator.Type.Government)
				{
					tGov_Expenses += (float)item2.Cost;
				}
				else if (item2.type == ServiceCreator.Type.Airport)
				{
					tTrans_Expenses += (float)item2.Cost;
					item2.Use = (int)(immi.Arrivals_LC + immi.Arrivals_MC + immi.Arrivals_UC);
					item2.Capacity = item2.HiredEmployees * Immigration.connectionCapacity;
					num13 += (float)item2.Capacity;
					num14 += (float)item2.Use;
					TradeCapacity += item2.Capacity * 10;
					if (item2.open)
					{
						num15 += item2.HiredEmployees;
					}
					else
					{
						item2.Capacity = 0;
					}
				}
				else if (item2.type == ServiceCreator.Type.Power)
				{
					tEnergy_Expenses += (float)item2.Cost;
				}
				else if (item2.type == ServiceCreator.Type.Water)
				{
					tWater_Expenses += (float)item2.Cost;
				}
				else if (item2.type == ServiceCreator.Type.Park)
				{
					tParks_Expenses += item2.Maintenance;
				}
				else if (item2.type == ServiceCreator.Type.Religion)
				{
					if (item2.typeName == "Edenium")
					{
						num18++;
					}
					num17++;
					tGov_Expenses += item2.Maintenance;
				}
				else if (item2.type == ServiceCreator.Type.TrainStation || item2.type == ServiceCreator.Type.Drone)
				{
					tTrans_Expenses += item2.Maintenance;
				}
				else if (item2.type == ServiceCreator.Type.Port)
				{
					tTrans_Expenses += (float)item2.Cost;
					item2.Capacity = item2.HiredEmployees * 450;
					item2.Use = Mathf.RoundToInt(num16 * (float)item2.Capacity);
					TradeCapacity += item2.Capacity;
					if (!item2.open)
					{
						item2.Capacity = 0;
					}
				}
				else
				{
					tSecu_Expenses += (float)item2.Cost;
				}
			}
			churchCount = num17;
			edenCount = Mathf.Clamp(num18, 0, 6);
			immi.AirportConnections = num15;
			Trans_Expenses = Mathf.Round(tTrans_Expenses);
			Edu_Expenses = Mathf.Round(tEdu_Expenses);
			Health_Expenses = Mathf.Round(tHealth_Expenses);
			Gov_Expenses = Mathf.Round(tGov_Expenses);
			Energy_Expenses = Mathf.Round(tEnergy_Expenses);
			Water_Expenses = Mathf.Round(tWater_Expenses);
			Secu_Expenses = Mathf.Round(tSecu_Expenses);
			Parks_Expenses = Mathf.Round(tParks_Expenses);
			if (sharedTillNow < 0 && DataRegion.Treasury + sharedTillNow <= 0)
			{
				Share_Rate = 0f;
				State_Expenses = 0f;
			}
			State_Expenses = Mathf.Round(Share_Rate / 100f * Income);
			float num19;
			float num20;
			if (Policies_Income > 0f)
			{
				num19 = Mathf.Round(Policies_Income);
				num20 = 0f;
			}
			else
			{
				num19 = 0f;
				num20 = Mathf.Round(0f - Policies_Income);
			}
			Expenses = Parks_Expenses + Water_Expenses + Trans_Expenses + Edu_Expenses + Health_Expenses + Gov_Expenses + Energy_Expenses + Secu_Expenses + State_Expenses + num19;
			Income += num20;
			if (World.dataReady)
			{
				CashFlow = Income - Expenses;
			}
			else
			{
				CashFlow = 0f;
			}
			yield return new WaitForEndOfFrame();
			UpadteBudgetUI();
			yield return new WaitForEndOfFrame();
		}
	}

	private IEnumerator DelayBudgetStart()
	{
		yield return new WaitForSeconds(1f);
		LoadData();
	}

	private void LoadData()
	{
		UI_Balance.text = "$" + DataRegion.selectedCity.balance.ToString("N0");
		UI_R_Tax.text = R_Tax + "%";
		UI_O_Tax.text = O_Tax + "%";
		UI_M_Tax.text = M_Tax + "%";
		UI_HT_Tax.text = HT_Tax + "%";
		UI_LC_Tax.text = LC_Tax + "%";
		UI_MC_Tax.text = MC_Tax + "%";
		UI_UC_Tax.text = UC_Tax + "%";
		UI_Pro_Tax.text = Pro_Tax + "%";
		UI_Shared_Tax.text = Share_Rate + "% (+" + DataRegion.ecoSettings[4].ToString("N0") + "%)";
	}

	private void UpadteBudgetUI()
	{
		UI_R_Income.text = "$" + R_Income.ToString("N0");
		UI_O_Income.text = "$" + O_Income.ToString("N0");
		UI_M_Income.text = "$" + M_Income.ToString("N0");
		UI_HT_Income.text = "$" + HT_Income.ToString("N0");
		UI_LC_Income.text = "$" + LC_Income.ToString("N0");
		UI_MC_Income.text = "$" + MC_Income.ToString("N0");
		UI_UC_Income.text = "$" + UC_Income.ToString("N0");
		UI_Pro_Income.text = "$" + Pro_Income.ToString("N0");
		UI_Policies_Income.text = "$" + (0f - Policies_Income).ToString("N0");
		if (Policies_Income < 0f)
		{
			UI_Policies_Income.color = green;
		}
		else
		{
			UI_Policies_Income.color = red;
		}
		UI_R_Effect.text = (10f * R_Effect).ToString("+0;-0") + "%";
		UI_M_Effect.text = (10f * M_Effect).ToString("+0;-0") + "%";
		UI_HT_Effect.text = (10f * HT_Effect).ToString("+0;-0") + "%";
		UI_O_Effect.text = (10f * O_Effect).ToString("+0;-0") + "%";
		UI_LC_Approval.text = LC_Approval.ToString("+0;-0");
		UI_MC_Approval.text = MC_Approval.ToString("+0;-0");
		UI_UC_Approval.text = UC_Approval.ToString("+0;-0");
		UI_R_Approval.text = R_Approval.ToString("+0;-0");
		UI_Pro_Approval.text = Pro_Approval.ToString("+0;-0");
		UI_Income.text = "$" + Income.ToString("N0");
		UI_Gov_Expenses.text = "$" + Gov_Expenses.ToString("N0");
		UI_Edu_Expenses.text = "$" + Edu_Expenses.ToString("N0");
		UI_Health_Expenses.text = "$" + Health_Expenses.ToString("N0");
		UI_Secu_Expenses.text = "$" + Secu_Expenses.ToString("N0");
		UI_Services_Expenses.text = "$" + Water_Expenses.ToString("N0");
		UI_Energy_Expenses.text = "$" + Energy_Expenses.ToString("N0");
		UI_Trans_Expenses.text = "$" + Trans_Expenses.ToString("N0");
		UI_Parks_Expenses.text = "$" + Parks_Expenses.ToString("N0");
		UI_State_Expenses.text = "$" + State_Expenses.ToString("N0");
		UI_State_Treasury.text = "$" + (DataRegion.Treasury + sharedTillNow).ToString("N0");
		UI_DollarBuyPrice.text = "(-$" + DollarPrice.ToString("N0") + ")";
		UI_DollarSellPrice.text = "(+$" + DollarPrice.ToString("N0") + ")";
		UI_ExchangeRate.text = "1 USD = " + ExchangeRate.ToString("F2") + " " + DataRegion.c;
		UI_DollarReserves.text = "USD " + DataRegion.DollarReserves.ToString("N0");
		UI_MoneyPrinted.text = "$ " + DataRegion.MoneyPrinted.ToString("N0");
		UI_Expenses.text = "$" + Expenses.ToString("N0");
		if (CashFlow >= 0f)
		{
			UI_MainCashFlow.text = "(+" + CashFlow.ToString("N0") + ")";
			UI_MainCashFlow.color = green;
			UI_BudgetCashFlow.text = "$+" + CashFlow.ToString("N0");
			UI_BudgetCashFlow.color = green;
		}
		else
		{
			UI_MainCashFlow.text = "(" + CashFlow.ToString("N0") + ")";
			UI_MainCashFlow.color = red;
			UI_BudgetCashFlow.text = "$" + CashFlow.ToString("N0");
			UI_BudgetCashFlow.color = red;
		}
	}

	public void CashIn()
	{
		if (World.dataReady)
		{
			Balance += CashFlow;
			if (Balance > 2E+09f)
			{
				Balance = 2E+09f;
			}
			sharedTillNow += (int)(State_Expenses * (1f + DataRegion.ecoSettings[4] / 100f));
			if (DataRegion.MoneyPrinted < 30f)
			{
				DataRegion.MoneyPrinted = 0f;
			}
			else if (eco.Population > 1000f)
			{
				DataRegion.MoneyPrinted -= 25f;
			}
			UI_Balance.text = "$" + Balance.ToString("N0");
			if (Balance < 1f)
			{
				UI_Balance.color = red;
			}
			else
			{
				UI_Balance.color = white;
			}
		}
	}

	public void ClosePanel()
	{
		budgetPanel.SetActive(value: false);
		Indexer.sliderOnScreen = false;
	}

	private void UpdateInflation()
	{
		if (!World.dataReady)
		{
			ExchangeRate = 10f;
			TradeBalanceReal = 0f;
			Inflation = 1f;
			return;
		}
		float num = Mathf.Clamp(DataRegion.MoneyPrinted, 1f, 200000f) / 20000f;
		float num2 = 5f - Mathf.Clamp(DataRegion.DollarReserves, 1f, 1950000f) / 400000f;
		float num3 = 5.25f - Mathf.Clamp(10f * ((float)eco.GDP / (eco.Population + 1f)), 10000f, 300000f) / 60000f;
		ExchangeRate = num2 + num + num3;
		float num4 = (6f - ExchangeRate) * 4.5f;
		float num5 = Mathf.Clamp(Exports * 100f / Imports - 100f, -50f, 50f);
		float num6 = Mathf.Clamp(100f - Mathf.Abs(Imports - Exports) * 100f / TradeCapacity, -50f, 10f);
		float num7 = Exports * (1f + num6 / 100f) - Imports * (1f - num4 / 100f);
		float num8 = Exports * (1f + num6 / 100f) + Imports * (1f - num4 / 100f);
		TradeBalanceReal = num7;
		float num9 = Mathf.Clamp(num7 * 100f / num8, -3f, 0.2f);
		float num10 = (12f - Mathf.Clamp(eco.Unemployment, 5f, 15f)) / 30f + 0.7f;
		float num11 = (eco.LC_Demand + eco.MC_Demand + eco.UC_Demand) / 12000f;
		Inflation = num10 + num11 - num9;
		stats.I_Dollars.text = num2.ToString("+0.0;-0.0") + DataRegion.c;
		if (num2 > 1f)
		{
			stats.I_Dollars.color = red;
		}
		else
		{
			stats.I_Dollars.color = green;
		}
		stats.I_Printed.text = num.ToString("+0.0;-0.0") + DataRegion.c;
		if (num > 0.1f)
		{
			stats.I_Printed.color = red;
		}
		else
		{
			stats.I_Printed.color = green;
		}
		stats.I_GDP.text = num3.ToString("+0.0;-0.0") + DataRegion.c;
		if (num3 > 2.5f)
		{
			stats.I_GDP.color = red;
		}
		else
		{
			stats.I_GDP.color = green;
		}
		stats.ExchangeRate.text = "1 USD = " + ExchangeRate.ToString("F2") + " " + DataRegion.c;
		if (ExchangeRate < 5f)
		{
			stats.ExchangeRate.color = green;
			stats.I_ExchangeRate.color = green;
		}
		else
		{
			stats.ExchangeRate.color = red;
			stats.I_ExchangeRate.color = red;
		}
		stats.I_ExchangeRate.text = num4.ToString("+0.0;-0.0") + "%";
		if (num4 <= 0f)
		{
			stats.I_ExchangeRate.color = red;
		}
		else
		{
			stats.I_ExchangeRate.color = green;
		}
		stats.I_Imports.text = num5.ToString("+0.0;-0.0") + "%";
		if (num5 <= 0f)
		{
			stats.I_Imports.color = red;
		}
		else
		{
			stats.I_Imports.color = green;
		}
		stats.I_Ports.text = num6.ToString("+0.0;-0.0") + "%";
		if (num6 <= 0f)
		{
			stats.I_Ports.color = red;
		}
		else
		{
			stats.I_Ports.color = green;
		}
		stats.TradeBalance.text = (num7 * 100f / num8).ToString("+0.0;-0.0") + "%";
		if (TradeBalanceReal <= 0f)
		{
			stats.TradeBalance.color = red;
		}
		else
		{
			stats.TradeBalance.color = green;
		}
		stats.I_TradeBalance.text = (0f - num9).ToString("+0.0;-0.0");
		if (num9 <= 0f)
		{
			stats.I_TradeBalance.color = red;
		}
		else
		{
			stats.I_TradeBalance.color = green;
		}
		stats.I_Unemployment.text = num10.ToString("+0.0;-0.0");
		if (num10 < 1f)
		{
			stats.I_Unemployment.color = green;
		}
		else
		{
			stats.I_Unemployment.color = red;
		}
		stats.I_Housing.text = num11.ToString("+0.0;-0.0");
		if (num11 < 1f)
		{
			stats.I_Housing.color = green;
		}
		else
		{
			stats.I_Housing.color = red;
		}
		stats.Inflation.text = Inflation.ToString("N1") + "%";
		if (Inflation > 1.8f)
		{
			stats.Inflation.color = red;
		}
		else
		{
			stats.Inflation.color = green;
		}
		w.indexer.Inflation.text = Inflation.ToString("N1") + "%";
		DollarPrice = 20000f * ExchangeRate;
		if (DollarPrice < 20000f)
		{
			DollarPrice = 20000f;
		}
		UI_Inflation.text = Inflation.ToString("N1") + "%";
	}

	public float GetPropertySize(LotCreator lc)
	{
		return lc.lSize switch
		{
			LotCreator.LotSize._3x4 => 12f, 
			LotCreator.LotSize._4x4 => 16f, 
			LotCreator.LotSize._2x4 => 8f, 
			LotCreator.LotSize._2x5 => 10f, 
			LotCreator.LotSize._2x6 => 12f, 
			LotCreator.LotSize._3x5 => 15f, 
			LotCreator.LotSize._3x6 => 18f, 
			LotCreator.LotSize._4x5 => 20f, 
			LotCreator.LotSize._4x6 => 24f, 
			LotCreator.LotSize._6x4 => 24f, 
			LotCreator.LotSize._6x5 => 30f, 
			LotCreator.LotSize._6x6 => 36f, 
			_ => 0f, 
		};
	}
}
