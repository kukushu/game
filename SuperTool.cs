using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SuperTool : MonoBehaviour
{
	public TextMeshProUGUI Sector;

	public TextMeshProUGUI LandValue;

	public TextMeshProUGUI Neighborhood;

	public TextMeshProUGUI Pollution;

	public TextMeshProUGUI Services;

	public TextMeshProUGUI EmployOccup;

	public TextMeshProUGUI Occupants;

	public TextMeshProUGUI Stage;

	public TextMeshProUGUI IncomeTax;

	public TextMeshProUGUI PropertyTax;

	public TextMeshProUGUI Health;

	public TextMeshProUGUI Education;

	public TextMeshProUGUI Secu;

	public TextMeshProUGUI Births;

	public TooltipTrigger birthTT;

	public TextMeshProUGUI Religious;

	public TextMeshProUGUI Problem;

	public TextMeshProUGUI WorkplaceName;

	public TextMeshProUGUI CommuteType;

	public TextMeshProUGUI NextStageValue;

	public TextMeshProUGUI NextStageDemand;

	public Image Panel;

	public Sprite Housing;

	public Sprite Business;

	public Image sector;

	public Sprite residential;

	public Sprite commercial;

	public Sprite industrial;

	public Sprite mining;

	public Sprite mix;

	public Image happy;

	public Sprite happyYES;

	public Sprite happyNO;

	public Image StageUp;

	public Image Commute;

	public Image Legend;

	public GameObject panel;

	public bool isDisplayed;

	public GameObject superAnchor;

	public LotCreator lc;

	public Indexer indexer;

	public CommuteRenderer commuteRenderer;

	public void CloseSuperTool()
	{
		panel.SetActive(value: false);
		commuteRenderer.DestroyLine();
		StopCoroutine(UpdateTexts());
		isDisplayed = false;
	}

	public void OpenServiceEditor(LotCreator lc)
	{
		if (!(lc == null))
		{
			panel.SetActive(value: true);
			isDisplayed = true;
			transform.position = superAnchor.transform.position;
			this.lc = lc;
			indexer.bi.lotSelector.transform.position = lc.Anchor + new Vector3(0f, 0.5f, 0f);
			StartCoroutine(UpdateTexts());
		}
	}

	private IEnumerator UpdateTexts()
	{
		while (isDisplayed)
		{
			SetTexts();
			SetImagesAndColors();
			yield return new WaitForSecondsRealtime(0.25f);
			DisplayCommuteLine();
			yield return new WaitForSecondsRealtime(0.25f);
		}
	}

	public void SetTexts()
	{
		if (lc == null)
		{
			return;
		}
		Stage.text = lc.Stage + " / " + lc.MaxStage;
		if (lc.lType == LotCreator.LotType.MIXED)
		{
			Occupants.text = lc.Occupants.ToString("N0") + "(" + lc.Visitors.ToString("N0") + ")";
		}
		else
		{
			Occupants.text = lc.Occupants.ToString("N0");
		}
		Secu.text = lc.secuLevel + "/100";
		IncomeTax.text = "$" + lc.TaxIncome.ToString("N2");
		PropertyTax.text = "$" + (indexer.eco.budget.GetPropertySize(lc) * indexer.eco.budget.Pro_Tax / 10f).ToString("N2");
		CommuteType.text = LOCSYS.Translate(lc.commuteState) + " (" + ((float)lc.roadPath.Count * 8f / 1000f).ToString("N2") + "km)";
		if (lc.Stage < lc.MaxStage)
		{
			NextStageValue.text = LOCSYS.Translate("LandValue") + ": $" + lc.nextValue + "/sqm";
			NextStageDemand.text = LOCSYS.Translate("Demand") + ": " + lc.nextDemand + "pt.";
			StageUp.gameObject.SetActive(value: true);
		}
		else
		{
			NextStageValue.text = "";
			NextStageDemand.text = "";
			StageUp.gameObject.SetActive(value: false);
		}
		BlockData blockData = null;
		if (lc.blockPos != null)
		{
			_ = lc.blockPos[0];
			blockData = indexer.eco.w.GetIfNotNull(lc.blockPos[0].x, lc.blockPos[0].y);
		}
		float num = lc.LandValue;
		float num2 = lc.eduLevel * 0.3f + (float)lc.healthLevel * 0.5f + (float)(lc.secuLevel - 50) * 0.75f;
		float num3 = -(blockData.v[1] / 5);
		float num4 = num - num2 - num3;
		LandValue.text = "$" + lc.LandValue + "/sqm";
		Services.text = "$" + num2.ToString("+0;-0");
		Neighborhood.text = "$" + num4.ToString("+0;-0");
		Pollution.text = "$" + num3.ToString("+0;-0");
		if (lc.isDecayed)
		{
			if (lc.lClass == LotCreator.LotClass.LOW)
			{
				Sector.text = LOCSYS.Translate("AbandonedBuilding") + " " + LOCSYS.Translate("LowIncome");
			}
			else if (lc.lClass == LotCreator.LotClass.MIDDLE)
			{
				Sector.text = LOCSYS.Translate("AbandonedBuilding") + " " + LOCSYS.Translate("MiddleIncome");
			}
			else if (lc.lClass == LotCreator.LotClass.UPPER)
			{
				Sector.text = LOCSYS.Translate("AbandonedBuilding") + " " + LOCSYS.Translate("HighIncome");
			}
			else if (lc.lClass == LotCreator.LotClass.MIXRETAIL)
			{
				Sector.text = LOCSYS.Translate("AbandonedBuilding") + " " + LOCSYS.Translate("RMC");
			}
			else if (lc.lClass == LotCreator.LotClass.MIXOFFICE)
			{
				Sector.text = LOCSYS.Translate("AbandonedBuilding") + " " + LOCSYS.Translate("OUC");
			}
			else if (lc.lClass == LotCreator.LotClass.MANUFACTURE)
			{
				Sector.text = LOCSYS.Translate("AbandonedBuilding") + " " + LOCSYS.Translate("BasicFactory");
			}
			else if (lc.lClass == LotCreator.LotClass.HIGHTECH)
			{
				Sector.text = LOCSYS.Translate("AbandonedBuilding") + " " + LOCSYS.Translate("HighTech");
			}
			else if (lc.lClass == LotCreator.LotClass.PRIMARY)
			{
				Sector.text = LOCSYS.Translate("AbandonedBuilding") + " " + LOCSYS.Translate("Primary Sector");
			}
			else if (lc.lClass == LotCreator.LotClass.RETAIL)
			{
				Sector.text = LOCSYS.Translate("AbandonedBuilding") + " " + LOCSYS.Translate("Retail");
			}
			else if (lc.lClass == LotCreator.LotClass.OFFICE)
			{
				Sector.text = LOCSYS.Translate("AbandonedBuilding") + " " + LOCSYS.Translate("Office");
			}
		}
		else if (lc.lType == LotCreator.LotType.RESIDENTIAL)
		{
			if (lc.Stage == 0)
			{
				if (lc.Occupants < 1)
				{
					Sector.text = LOCSYS.Translate("ResidentialVacant");
				}
				else if (lc.Occupants == 2)
				{
					Sector.text = LOCSYS.Translate("Parking") + " (" + LOCSYS.Translate("ResidentialVacant") + ")";
				}
				else
				{
					Sector.text = LOCSYS.Translate("PublicHousing") + " - " + LOCSYS.Translate("Lower Class");
				}
			}
			else if (lc.Stage == 1 && lc.Occupants == 2)
			{
				Sector.text = LOCSYS.Translate("Parking") + " (" + LOCSYS.Translate("ResidentialVacant") + ")";
			}
			else if (lc.lClass == LotCreator.LotClass.LOW)
			{
				Sector.text = LOCSYS.Translate("LowIncome");
			}
			else if (lc.lClass == LotCreator.LotClass.MIDDLE)
			{
				Sector.text = LOCSYS.Translate("MiddleIncome");
			}
			else if (lc.lClass == LotCreator.LotClass.UPPER)
			{
				Sector.text = LOCSYS.Translate("HighIncome");
			}
			Education.text = (int)lc.eduLevel + "/100";
			Health.text = lc.healthLevel + "/100";
			Religious.text = lc.reliLevel + "/100";
			Births.text = lc.birthRate.ToString("N2");
			EmployOccup.text = LOCSYS.Translate("Occupants");
			birthTT.content = "Birth Rate";
		}
		else if (lc.lType == LotCreator.LotType.MIXED)
		{
			if (lc.Stage == 0)
			{
				if (lc.Occupants < 1)
				{
					Sector.text = LOCSYS.Translate("MixedVacant");
				}
				else if (lc.Occupants == 2)
				{
					Sector.text = LOCSYS.Translate("Parking") + " (" + LOCSYS.Translate("MixedVacant") + ")";
				}
			}
			else if (lc.Stage == 1 && lc.Occupants == 2)
			{
				Sector.text = LOCSYS.Translate("Parking") + " (" + LOCSYS.Translate("MixedVacant") + ")";
			}
			else if (lc.lClass == LotCreator.LotClass.MIXRETAIL)
			{
				Sector.text = LOCSYS.Translate("RMC");
			}
			else if (lc.lClass == LotCreator.LotClass.MIXOFFICE)
			{
				Sector.text = LOCSYS.Translate("OUC");
			}
			Education.text = (int)lc.eduLevel + "/100";
			Health.text = lc.healthLevel + "/100";
			Religious.text = lc.reliLevel + "/100";
			Births.text = lc.birthRate.ToString("N2");
			EmployOccup.text = LOCSYS.Translate("Occupants") + "(" + LOCSYS.Translate("Employees") + ")";
			birthTT.content = "Birth Rate";
		}
		else if (lc.lType == LotCreator.LotType.INDUSTRIAL)
		{
			if (lc.Stage == 0)
			{
				if (lc.Occupants < 1)
				{
					Sector.text = LOCSYS.Translate("IndustrialVacant");
				}
				else if (lc.lClass == LotCreator.LotClass.PRIMARY)
				{
					Sector.text = LOCSYS.Translate("Primary Sector");
				}
				else
				{
					Sector.text = LOCSYS.Translate("StateFac") + " " + LOCSYS.Translate("BasicFactory");
				}
			}
			else if (lc.lClass == LotCreator.LotClass.MANUFACTURE)
			{
				Sector.text = LOCSYS.Translate("BasicFactory");
			}
			else if (lc.lClass == LotCreator.LotClass.HIGHTECH)
			{
				Sector.text = LOCSYS.Translate("HighTech");
			}
			else if (lc.lClass == LotCreator.LotClass.PRIMARY)
			{
				Sector.text = LOCSYS.Translate("Farm");
			}
			Education.text = "-";
			Health.text = "-";
			Religious.text = "-";
			Secu.text = lc.secuLevel + "/100";
			Births.text = "$" + lc.Trade.ToString("+0.0;-0.0");
			EmployOccup.text = LOCSYS.Translate("Employees");
			birthTT.content = "ImpEx";
		}
		else
		{
			if (lc.lType != LotCreator.LotType.COMMERCIAL)
			{
				return;
			}
			if (lc.Stage == 0)
			{
				if (lc.Occupants < 1)
				{
					Sector.text = LOCSYS.Translate("CommercialVacant");
				}
				else if (lc.Occupants > 500)
				{
					Sector.text = LOCSYS.Translate("RedD") + " - " + LOCSYS.Translate("Retail");
				}
				else if (lc.Occupants > 1)
				{
					Sector.text = LOCSYS.Translate("Retail");
				}
			}
			else if (lc.Stage == 1 && lc.Occupants == 2)
			{
				Sector.text = LOCSYS.Translate("Parking") + " (" + LOCSYS.Translate("CommercialVacant") + ")";
			}
			else if (lc.lClass == LotCreator.LotClass.RETAIL)
			{
				Sector.text = LOCSYS.Translate("Retail");
			}
			else if (lc.lClass == LotCreator.LotClass.OFFICE)
			{
				Sector.text = LOCSYS.Translate("Office");
			}
			Education.text = "-";
			Health.text = "-";
			Religious.text = "-";
			Secu.text = lc.secuLevel + "/100";
			Births.text = "$" + lc.Trade.ToString("+0.0;-0.0");
			EmployOccup.text = LOCSYS.Translate("Employees");
			birthTT.content = "ImpEx";
		}
	}

	private void SetImagesAndColors()
	{
		bool flag = false;
		bool flag2 = false;
		if (lc.LandValue >= lc.nextValue)
		{
			NextStageValue.color = indexer.green;
			flag = true;
		}
		else if (lc.LandValue <= lc.desiredValue / 2)
		{
			NextStageValue.color = indexer.red;
		}
		else
		{
			NextStageValue.color = indexer.yellow;
		}
		float demand = GetDemand();
		if (demand < 0f)
		{
			NextStageDemand.color = indexer.red;
		}
		if ((float)lc.nextDemand <= demand)
		{
			NextStageDemand.color = indexer.green;
			flag2 = true;
		}
		else
		{
			NextStageDemand.color = indexer.yellow;
		}
		if (flag2 & flag)
		{
			StageUp.color = indexer.green;
		}
		else
		{
			StageUp.color = indexer.red;
		}
		if (lc.validCommute)
		{
			Commute.color = indexer.green;
			CommuteType.color = indexer.green;
		}
		else
		{
			Commute.color = indexer.red;
			CommuteType.color = indexer.red;
		}
		happy.sprite = happyYES;
		Problem.color = indexer.green;
		if (lc.negatives < 2f && !lc.isDecayed && lc.Occupants > 0)
		{
			if (lc.lType == LotCreator.LotType.RESIDENTIAL || lc.lType == LotCreator.LotType.MIXED)
			{
				Problem.text = LOCSYS.Translate("resprob");
			}
			else
			{
				Problem.text = LOCSYS.Translate("bizprob");
			}
		}
		else if (lc.Occupants < 1 && !lc.isDecayed)
		{
			if (lc.lType == LotCreator.LotType.MIXED)
			{
				if (lc.LandValue < 768)
				{
					Problem.text = LOCSYS.Translate("Land Value Too Low");
					happy.sprite = happyNO;
					Problem.color = indexer.red;
				}
				else if ((indexer.eco.R_Demand > 33f && indexer.eco.MC_Demand > 99f) || (indexer.eco.O_Demand > 27f && indexer.eco.UC_Demand > 60f))
				{
					Problem.text = LOCSYS.Translate("aDev");
				}
				else
				{
					Problem.text = LOCSYS.Translate("mixsTT");
					happy.sprite = happyNO;
					Problem.color = indexer.red;
				}
			}
			else if (lc.lType == LotCreator.LotType.INDUSTRIAL)
			{
				if (lc.LandValue >= 1250)
				{
					Problem.text = LOCSYS.Translate("BMvaluehigh");
				}
				else if (indexer.eco.M_Demand > 20f || indexer.eco.HT_Demand > 20f)
				{
					Problem.text = LOCSYS.Translate("aDev");
				}
				else
				{
					Problem.text = LOCSYS.Translate("InsufficientDemand");
					happy.sprite = happyNO;
					Problem.color = indexer.red;
				}
			}
			else if (lc.lType == LotCreator.LotType.COMMERCIAL)
			{
				if (indexer.eco.R_Demand > 18f || indexer.eco.O_Demand > 80f)
				{
					Problem.text = LOCSYS.Translate("aDev");
				}
				else
				{
					Problem.text = LOCSYS.Translate("InsufficientDemand");
					happy.sprite = happyNO;
					Problem.color = indexer.red;
				}
			}
			else if (lc.lType == LotCreator.LotType.RESIDENTIAL)
			{
				if (indexer.eco.LC_Demand > 5f || indexer.eco.MC_Demand > 5f || indexer.eco.UC_Demand > 5f)
				{
					Problem.text = LOCSYS.Translate("aDev");
				}
				else
				{
					Problem.text = LOCSYS.Translate("InsufficientDemand");
					happy.sprite = happyNO;
					Problem.color = indexer.red;
				}
			}
		}
		else
		{
			happy.sprite = happyNO;
			Problem.text = LOCSYS.Translate(lc.decayReason);
			Problem.color = indexer.red;
		}
		if (lc.lType == LotCreator.LotType.RESIDENTIAL)
		{
			sector.sprite = residential;
			Panel.sprite = Housing;
		}
		else if (lc.lType == LotCreator.LotType.COMMERCIAL)
		{
			sector.sprite = commercial;
			Panel.sprite = Business;
		}
		else if (lc.lType == LotCreator.LotType.MIXED)
		{
			sector.sprite = mix;
			Panel.sprite = Housing;
		}
		else if (lc.lClass == LotCreator.LotClass.PRIMARY)
		{
			sector.sprite = mining;
			Panel.sprite = Business;
		}
		else
		{
			sector.sprite = industrial;
			Panel.sprite = Business;
		}
	}

	private float GetDemand()
	{
		if (lc.lClass == LotCreator.LotClass.LOW)
		{
			return indexer.eco.LC_Demand;
		}
		if (lc.lClass == LotCreator.LotClass.MIDDLE || lc.lClass == LotCreator.LotClass.MIXRETAIL)
		{
			return indexer.eco.MC_Demand;
		}
		if (lc.lClass == LotCreator.LotClass.UPPER || lc.lClass == LotCreator.LotClass.MIXOFFICE)
		{
			return indexer.eco.UC_Demand;
		}
		if (lc.lClass == LotCreator.LotClass.MANUFACTURE)
		{
			return indexer.eco.M_Demand;
		}
		if (lc.lClass == LotCreator.LotClass.HIGHTECH)
		{
			return indexer.eco.HT_Demand;
		}
		if (lc.lClass == LotCreator.LotClass.OFFICE)
		{
			return indexer.eco.O_Demand;
		}
		if (lc.lClass == LotCreator.LotClass.RETAIL)
		{
			return indexer.eco.R_Demand;
		}
		if (lc.lClass == LotCreator.LotClass.PRIMARY)
		{
			return indexer.eco.P_Demand;
		}
		return 0f;
	}

	private void DisplayCommuteLine()
	{
		if (lc.roadPath == null || lc.roadPath.Count < 2 || !lc.validCommute)
		{
			return;
		}
		List<RoadCreator> list = lc.roadPath.ToList();
		List<Vector3> list2 = new List<Vector3>();
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] != null)
			{
				list2.Add(list[i].transform.position + new Vector3(0f, 0.5f, 0f));
			}
		}
		Color lineColor;
		if (lc.commuteState == "cc")
		{
			lineColor = indexer.blue;
			Legend.color = indexer.blue;
		}
		else if (lc.commuteState == "wc")
		{
			lineColor = indexer.purple;
			Legend.color = indexer.purple;
		}
		else if (lc.commuteState == "tc")
		{
			lineColor = indexer.yellow;
			Legend.color = indexer.yellow;
		}
		else if (lc.commuteState == "bc")
		{
			lineColor = indexer.green;
			Legend.color = indexer.green;
		}
		else if (lc.commuteState == "nc")
		{
			lineColor = indexer.red;
			Legend.color = indexer.red;
		}
		else
		{
			lineColor = indexer.blue;
			Legend.color = indexer.blue;
		}
		commuteRenderer.SetUpLine(list2, lineColor);
	}
}
