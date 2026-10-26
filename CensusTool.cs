using TMPro;
using UnityEngine;

public class CensusTool : MonoBehaviour
{
	public GameObject One_one;

	public BlockInteraction bi;

	public TextMeshProUGUI Title;

	public TextMeshProUGUI Line1;

	public TextMeshProUGUI Line2;

	public TextMeshProUGUI Line3;

	public TextMeshProUGUI Line4;

	public TextMeshProUGUI Line5;

	public TextMeshProUGUI Line6;

	public TextMeshProUGUI Line7;

	public TextMeshProUGUI Line8;

	public Color white;

	public Color green;

	public Color red;

	private int oldx1;

	private int oldz1;

	private void Update()
	{
		if (Indexer.Index == 8)
		{
			UpdateWindow();
			Vector3 vector = new Vector3(0f, 0.515f, 0f);
			One_one.transform.position = bi.pos + vector;
		}
	}

	private void UpdateWindow()
	{
		if ((float)oldx1 == bi.pos.x && (float)oldz1 == bi.pos.z)
		{
			return;
		}
		double num = (double)Screen.width * 0.15;
		double num2 = (double)Screen.height * 0.15;
		transform.position = Input.mousePosition + new Vector3((float)num, (float)num2, 0f);
		BlockData bounded = bi.world.GetBounded((int)bi.pos.x, (int)bi.pos.z);
		Title.text = " Citystate II ";
		Line1.text = " - ";
		Line2.text = " - ";
		Line3.text = " - ";
		Line4.text = " - ";
		Line5.text = " - ";
		Line6.text = " - ";
		Line7.text = " - ";
		Line8.text = " - ";
		if (!bounded.b && !bounded.r)
		{
			if (bounded.t == BlockData.Type.GRASS)
			{
				Title.text = LOCSYS.Translate("Grass");
			}
			else if (bounded.t == BlockData.Type.SAND)
			{
				Title.text = LOCSYS.Translate("Sand");
			}
			else if (bounded.t == BlockData.Type.GRASSTREE)
			{
				Title.text = LOCSYS.Translate("Forest");
			}
			Line1.text = LOCSYS.Translate("LandValue") + "  :  $" + Mathf.Round(bounded.v[0]) * 4f + "/sqm";
			Line2.text = LOCSYS.Translate("Elevation") + "  :  " + (bounded.h - 30f) * 8f + "m (" + ((double)((bounded.h - 30f) * 8f) * 3.281).ToString("N0") + "ft)";
			Line3.text = LOCSYS.Translate("Pollutants") + "  :  " + Mathf.Round(bounded.v[1] * 10) + "μg/m3";
			Line4.color = white;
			if (bounded.e == 0)
			{
				Line4.text = LOCSYS.Translate("Unexplored");
			}
			else
			{
				float num3;
				string key;
				if (bounded.e >= 192)
				{
					num3 = (float)(bounded.e - 192) * 3.984375f;
					key = "ctRare";
				}
				else if (bounded.e >= 128)
				{
					num3 = (float)(bounded.e - 128) * 3.984375f;
					key = "ctMine";
				}
				else if (bounded.e >= 64)
				{
					num3 = (float)(bounded.e - 64) * 3.984375f;
					key = "ctGas";
				}
				else if (bounded.e >= 10)
				{
					num3 = (float)(int)bounded.e * 3.984375f;
					key = "ctOil";
				}
				else
				{
					key = "NoRess";
					num3 = 0f;
				}
				if (num3 == 0f)
				{
					Line4.text = LOCSYS.Translate(key);
				}
				else
				{
					Line4.text = LOCSYS.Translate(key) + " " + num3.ToString("N0") + "%";
				}
			}
		}
		if (bounded.b && bounded.lO != null && !bounded.r)
		{
			LotCreator lO = bounded.lO;
			if (lO.isDecayed)
			{
				Title.text = LOCSYS.Translate("AbandonedBuilding");
				Line2.text = LOCSYS.Translate("Cause") + "  :  " + LOCSYS.Translate(lO.decayReason);
				Line3.text = LOCSYS.Translate("Occupants") + "  :  " + lO.Occupants;
				Line4.text = LOCSYS.Translate("LandValue") + "  :  $" + lO.LandValue + "/sqm";
				Line5.text = LOCSYS.Translate("TaxIncome") + "  :  $" + lO.TaxIncome.ToString("N2");
				if (lO.lClass == LotCreator.LotClass.LOW)
				{
					Line1.text = LOCSYS.Translate("LowIncome");
				}
				else if (lO.lClass == LotCreator.LotClass.MIDDLE)
				{
					Line1.text = LOCSYS.Translate("MiddleIncome");
				}
				else if (lO.lClass == LotCreator.LotClass.UPPER)
				{
					Line1.text = LOCSYS.Translate("HighIncome");
				}
				else if (lO.lClass == LotCreator.LotClass.MIXRETAIL)
				{
					Line1.text = LOCSYS.Translate("RMC");
				}
				else if (lO.lClass == LotCreator.LotClass.MIXOFFICE)
				{
					Line1.text = LOCSYS.Translate("OUC");
				}
				else if (lO.lClass == LotCreator.LotClass.MANUFACTURE)
				{
					Line1.text = LOCSYS.Translate("BasicFactory");
				}
				else if (lO.lClass == LotCreator.LotClass.HIGHTECH)
				{
					Line1.text = LOCSYS.Translate("HighTech");
				}
				else if (lO.lClass == LotCreator.LotClass.PRIMARY)
				{
					Line1.text = LOCSYS.Translate("Primary Sector");
				}
				else if (lO.lClass == LotCreator.LotClass.RETAIL)
				{
					Line1.text = LOCSYS.Translate("Retail");
				}
				else if (lO.lClass == LotCreator.LotClass.OFFICE)
				{
					Line1.text = LOCSYS.Translate("Office");
				}
			}
			else if (lO.lType == LotCreator.LotType.RESIDENTIAL)
			{
				if (lO.Stage == 0)
				{
					if (lO.Occupants < 1)
					{
						Title.text = LOCSYS.Translate("ResidentialVacant");
					}
					else if (lO.Occupants == 2)
					{
						Title.text = LOCSYS.Translate("Parking") + " (" + LOCSYS.Translate("ResidentialVacant") + ")";
						Line1.text = LOCSYS.Translate("Cause") + "  :  " + LOCSYS.Translate(lO.decayReason);
					}
					else
					{
						Title.text = LOCSYS.Translate("PublicHousing");
						Line1.text = LOCSYS.Translate("Lower Class");
					}
				}
				else if (lO.Stage == 1 && lO.Occupants == 2)
				{
					Title.text = LOCSYS.Translate("Parking") + " (" + LOCSYS.Translate("ResidentialVacant") + ")";
					Line1.text = LOCSYS.Translate("Cause") + "  :  " + LOCSYS.Translate(lO.decayReason);
				}
				else
				{
					Title.text = LOCSYS.Translate("ResidentialBuilding");
					if (lO.lClass == LotCreator.LotClass.LOW)
					{
						Line1.text = LOCSYS.Translate("LowIncome");
					}
					else if (lO.lClass == LotCreator.LotClass.MIDDLE)
					{
						Line1.text = LOCSYS.Translate("MiddleIncome");
					}
					else if (lO.lClass == LotCreator.LotClass.UPPER)
					{
						Line1.text = LOCSYS.Translate("HighIncome");
					}
				}
				Line2.text = LOCSYS.Translate("Stage") + "  :  " + lO.Stage + " / " + lO.MaxStage;
				Line3.text = LOCSYS.Translate("Occupants") + "  :  " + lO.Occupants;
				Line4.text = LOCSYS.Translate("LandValue") + "  :  $" + lO.LandValue + "/sqm (" + LOCSYS.Translate("Desired") + " $" + lO.desiredValue + ")";
				if (lO.LandValue < lO.desiredValue / 2)
				{
					Line4.color = red;
				}
				else if (lO.LandValue > lO.desiredValue * 2)
				{
					Line4.color = green;
				}
				else
				{
					Line4.color = white;
				}
				Line5.text = LOCSYS.Translate("TaxIncome") + "  :  $" + lO.TaxIncome.ToString("N2");
				Line6.text = LOCSYS.Translate("CommuteToWork") + " " + LOCSYS.Translate(lO.commuteState);
				Line7.text = LOCSYS.Translate("EducationLevel") + "  :  " + (int)lO.eduLevel + "/100";
				Line8.text = LOCSYS.Translate("Birth Rate") + "  :  " + lO.birthRate.ToString("N2");
			}
			else if (lO.lType == LotCreator.LotType.MIXED)
			{
				if (lO.Stage == 0)
				{
					if (lO.Occupants < 1)
					{
						Title.text = LOCSYS.Translate("MixedVacant");
					}
				}
				else if (lO.Stage == 1 && lO.Occupants == 2)
				{
					Title.text = LOCSYS.Translate("Parking") + " (" + LOCSYS.Translate("MixedVacant") + ")";
					Line1.text = LOCSYS.Translate("Cause") + "  :  " + LOCSYS.Translate(lO.decayReason);
				}
				else
				{
					Title.text = LOCSYS.Translate("MixedBuilding");
					if (lO.lClass == LotCreator.LotClass.MIXRETAIL)
					{
						Line1.text = LOCSYS.Translate("RMC");
					}
					else if (lO.lClass == LotCreator.LotClass.MIXOFFICE)
					{
						Line1.text = LOCSYS.Translate("OUC");
					}
				}
				Line2.text = LOCSYS.Translate("Stage") + "  :  " + lO.Stage + " / " + lO.MaxStage;
				Line3.text = LOCSYS.Translate("Occupants") + ": " + lO.Occupants + " - " + LOCSYS.Translate("Employees") + ": " + lO.Visitors;
				Line4.text = LOCSYS.Translate("LandValue") + "  :  $" + lO.LandValue + "/sqm (" + LOCSYS.Translate("Desired") + " $" + lO.desiredValue + ")";
				if (lO.LandValue < lO.desiredValue / 2)
				{
					Line4.color = red;
				}
				else if (lO.LandValue > lO.desiredValue * 2)
				{
					Line4.color = green;
				}
				else
				{
					Line4.color = white;
				}
				Line5.text = LOCSYS.Translate("TaxIncome") + "  :  $" + lO.TaxIncome.ToString("N2");
				Line6.text = LOCSYS.Translate("CommuteToWork") + " " + LOCSYS.Translate(lO.commuteState);
				Line7.text = LOCSYS.Translate("EducationLevel") + "  :  " + (int)lO.eduLevel + "/100";
				Line8.text = LOCSYS.Translate("Birth Rate") + "  :  " + lO.birthRate.ToString("N2");
			}
			else if (lO.lType == LotCreator.LotType.INDUSTRIAL)
			{
				if (lO.Stage == 0)
				{
					if (lO.Occupants < 1)
					{
						Title.text = LOCSYS.Translate("IndustrialVacant");
						if (lO.LandValue > 1250)
						{
							Line1.text = LOCSYS.Translate("BMvaluehigh");
						}
						else
						{
							Line1.text = LOCSYS.Translate("");
						}
					}
					else if (lO.lClass == LotCreator.LotClass.PRIMARY)
					{
						Title.text = LOCSYS.Translate("IndustrialBuilding");
						Line1.text = LOCSYS.Translate("Primary Sector");
					}
					else
					{
						Title.text = LOCSYS.Translate("StateFac");
						Line1.text = LOCSYS.Translate("BasicFactory");
					}
				}
				else
				{
					Title.text = LOCSYS.Translate("IndustrialBuilding");
					if (lO.lClass == LotCreator.LotClass.MANUFACTURE)
					{
						Line1.text = LOCSYS.Translate("BasicFactory");
					}
					else if (lO.lClass == LotCreator.LotClass.HIGHTECH)
					{
						Line1.text = LOCSYS.Translate("HighTech");
					}
					else if (lO.lClass == LotCreator.LotClass.PRIMARY)
					{
						Line1.text = LOCSYS.Translate("Farm");
					}
				}
				Line2.text = LOCSYS.Translate("Stage") + "  :  " + lO.Stage + " / " + lO.MaxStage;
				Line3.text = LOCSYS.Translate("Employees") + "  :  " + lO.Occupants;
				Line4.text = LOCSYS.Translate("LandValue") + "  :  $" + lO.LandValue + "/sqm (" + LOCSYS.Translate("Desired") + " $" + lO.desiredValue + ")";
				if (lO.LandValue < lO.desiredValue / 2)
				{
					Line4.color = red;
				}
				else if (lO.LandValue > lO.desiredValue * 2)
				{
					Line4.color = green;
				}
				else
				{
					Line4.color = white;
				}
				Line5.text = LOCSYS.Translate("TaxIncome") + "  :  $" + lO.TaxIncome.ToString("N2");
				Line6.text = LOCSYS.Translate("TRADEB") + " : $" + lO.Trade.ToString("+0.0;-0.0");
			}
			else if (lO.lType == LotCreator.LotType.COMMERCIAL)
			{
				if (lO.Stage == 0)
				{
					if (lO.Occupants < 1)
					{
						Title.text = LOCSYS.Translate("CommercialVacant");
						Line1.text = LOCSYS.Translate("");
					}
					else if (lO.Occupants > 500)
					{
						Title.text = LOCSYS.Translate("RedD");
						Line1.text = LOCSYS.Translate("Retail");
					}
					else if (lO.Occupants > 1)
					{
						Title.text = LOCSYS.Translate("CommercialBuilding");
						Line1.text = LOCSYS.Translate("Retail");
					}
				}
				else if (lO.Stage == 1 && lO.Occupants == 2)
				{
					Title.text = LOCSYS.Translate("Parking") + " (" + LOCSYS.Translate("CommercialVacant") + ")";
					Line1.text = LOCSYS.Translate("Cause") + "  :  " + LOCSYS.Translate(lO.decayReason);
				}
				else
				{
					Title.text = LOCSYS.Translate("CommercialBuilding");
					if (lO.lClass == LotCreator.LotClass.RETAIL)
					{
						Line1.text = LOCSYS.Translate("Retail");
					}
					else if (lO.lClass == LotCreator.LotClass.OFFICE)
					{
						Line1.text = LOCSYS.Translate("Office");
					}
				}
				Line2.text = LOCSYS.Translate("Stage") + "  :  " + lO.Stage + " / " + lO.MaxStage;
				Line3.text = LOCSYS.Translate("Employees") + "  :  " + lO.Occupants;
				Line4.text = LOCSYS.Translate("LandValue") + "  :  $" + lO.LandValue + "/sqm (" + LOCSYS.Translate("Desired") + " $" + lO.desiredValue + ")";
				if (lO.LandValue < lO.desiredValue / 2)
				{
					Line4.color = red;
				}
				else if (lO.LandValue > lO.desiredValue * 2)
				{
					Line4.color = green;
				}
				else
				{
					Line4.color = white;
				}
				Line5.text = LOCSYS.Translate("TaxIncome") + "  :  $" + lO.TaxIncome.ToString("N2");
				Line6.text = LOCSYS.Translate("TRADEB") + " : $" + lO.Trade.ToString("N0");
			}
		}
		else if (bounded.b && bounded.sO != null && !bounded.r)
		{
			ServiceCreator sO = bounded.sO;
			if (sO.typeName == "B" || sO.type == ServiceCreator.Type.Power || sO.type == ServiceCreator.Type.Water)
			{
				Line5.text = LOCSYS.Translate("Employees") + "  :  " + sO.FixedEmployees;
			}
			else
			{
				Line5.text = LOCSYS.Translate("Employees") + "  :  " + (sO.FixedEmployees + sO.HiredEmployees);
			}
			if (sO.type == ServiceCreator.Type.Government)
			{
				Title.text = LOCSYS.Translate("GovernmentBuilding");
				Line1.text = LOCSYS.Translate(sO.typeName);
				if (sO.typeName == "HQsc")
				{
					Line2.text = LOCSYS.Translate("Stage") + "  :  " + sO.Stage + " / 5";
				}
				Line3.text = LOCSYS.Translate("ExitCensusTool");
			}
			else if (sO.type == ServiceCreator.Type.Park)
			{
				Title.text = LOCSYS.Translate("Parks");
				if (sO.typeName == "CC")
				{
					Title.text = LOCSYS.Translate("Carbon");
				}
				Line1.text = LOCSYS.Translate("Maintenance") + " $" + sO.Maintenance;
			}
			else if (sO.type == ServiceCreator.Type.TrainStation)
			{
				Title.text = LOCSYS.Translate("TrainStop");
				Line1.text = LOCSYS.Translate("QueryTracks");
			}
			else if (sO.type == ServiceCreator.Type.Religion)
			{
				if (sO.typeName.StartsWith("St"))
				{
					Title.text = LOCSYS.Translate("StateMed");
				}
				else
				{
					Title.text = LOCSYS.Translate("RelBlgd");
				}
				Line1.text = LOCSYS.Translate("Maintenance") + " $" + sO.Cost;
			}
			else if (sO.type == ServiceCreator.Type.Arcology)
			{
				Title.text = LOCSYS.Translate(sO.typeName);
				Line1.text = LOCSYS.Translate("Middle Class");
				Line2.text = LOCSYS.Translate("Maintenance") + " $" + sO.Maintenance;
				Line3.text = LOCSYS.Translate("Capacity") + "  :  " + sO.MaxEmployees;
				Line4.text = LOCSYS.Translate("Occupants") + "  :  " + sO.MinEmployees;
				Line5.text = LOCSYS.Translate("Employees") + "  :  " + sO.Use;
				Line6.text = LOCSYS.Translate("TaxIncome") + "  :  $" + sO.Cost;
				Line7.text = LOCSYS.Translate("EducationLevel") + "  :  " + bi.eco.EducationQuality;
				Line8.text = LOCSYS.Translate("Birth Rate") + "  :  " + bi.eco.BirthRatePolicy;
			}
			else
			{
				Title.text = LOCSYS.Translate("ServiceBuilding");
				Line1.text = LOCSYS.Translate("ExitCensusTool");
			}
		}
		else if (bounded.uO != null)
		{
			SlumCreator uO = bounded.uO;
			Title.text = LOCSYS.Translate("Slum");
			Line1.text = LOCSYS.Translate(uO.reason);
			Line2.text = LOCSYS.Translate("Stage") + "  :  " + uO.Stage + " / 4";
			Line3.text = LOCSYS.Translate("Occupants") + "  ≈  " + uO.Occupants;
			Line4.text = LOCSYS.Translate("LandValue") + "  :  $" + uO.landValue * 4f + "/sqm";
			Line5.text = LOCSYS.Translate("TaxIncome") + "  :  $0";
			Line6.text = LOCSYS.Translate("CommuteToWork") + " " + LOCSYS.Translate("Unknown");
			Line7.text = LOCSYS.Translate("EducationLevel") + "  :  " + LOCSYS.Translate("Unknown");
			Line8.text = LOCSYS.Translate("Birth Rate") + "  :  " + LOCSYS.Translate("Unknown");
		}
		else if (bounded.gT < 0)
		{
			Title.text = LOCSYS.Translate("Farming");
			Line1.text = LOCSYS.Translate("LandValue") + "  :  $" + Mathf.Round(bounded.v[0]) * 4f + "/sqm";
			Line2.text = LOCSYS.Translate("Elevation") + "  :  " + (bounded.h - 30f) * 8f + "m (" + ((double)((bounded.h - 30f) * 8f) * 3.281).ToString("N0") + "ft)";
			Line3.text = LOCSYS.Translate("Pollutants") + "  :  " + Mathf.Round(bounded.v[1] * 10) + "μg/m3";
		}
		if (bounded.r)
		{
			RoadCreator roadCreator = bi.roading.rD[bounded.wP.x, 1, bounded.wP.y];
			if (roadCreator == null)
			{
				roadCreator = bi.roading.rD[bounded.wP.x, 2, bounded.wP.y];
			}
			if (roadCreator == null)
			{
				Title.text = " Citystate II ";
				return;
			}
			if (roadCreator.stopType != RoadCreator.StopType.NONE)
			{
				if (roadCreator.stopType == RoadCreator.StopType.BUS)
				{
					Title.text = LOCSYS.Translate("BusStop");
				}
				else
				{
					RoadCreator railIfNotNull = bi.roading.GetRailIfNotNull(bounded.wP.x, 0, bounded.wP.y);
					if (railIfNotNull != null)
					{
						roadCreator = railIfNotNull;
					}
					Title.text = LOCSYS.Translate("Underground Station");
				}
				Line1.text = LOCSYS.Translate("Capacity") + "  :  " + roadCreator.StopCapacity;
				Line2.text = LOCSYS.Translate("Passengers") + "  :  " + roadCreator.StopUsers;
				Line5.text = LOCSYS.Translate("Profit") + "  :  $" + (int)roadCreator.maintenance * -1;
			}
			else
			{
				Title.text = LOCSYS.Translate("Road");
				Line1.text = LOCSYS.Translate("Type") + "  :  " + LOCSYS.Translate(roadCreator.subType);
				Line2.text = LOCSYS.Translate("Traffic") + "  :  " + roadCreator.TrafficPercent + "%";
				Line5.text = LOCSYS.Translate("Maintenance") + "  :  $" + roadCreator.maintenance;
			}
			Line3.text = LOCSYS.Translate("Elevation") + "  :  " + (bounded.h - 30f) * 8f + "m (" + ((double)((bounded.h - 30f) * 8f) * 3.281).ToString("N0") + "ft)";
			Line4.text = LOCSYS.Translate("LandValue") + "  :  $" + bounded.v[0] * 4 + "/sqm";
			Line6.text = LOCSYS.Translate("Pollutants") + "  :  " + Mathf.Round(bounded.v[1] * 10) + "μg/m3";
		}
		else if (bounded.a)
		{
			RoadCreator roadCreator2 = bi.roading.rG[bounded.wP.x, 1, bounded.wP.y];
			if (roadCreator2 != null)
			{
				if (roadCreator2.stopType == RoadCreator.StopType.TRAIN)
				{
					Title.text = LOCSYS.Translate("TrainStop");
					Line1.text = LOCSYS.Translate("Capacity") + "  :  " + roadCreator2.StopCapacity;
					Line2.text = LOCSYS.Translate("Passengers") + "  :  " + roadCreator2.StopUsers;
					Line5.text = LOCSYS.Translate("Profit") + "  :  $" + (int)roadCreator2.maintenance * -1;
				}
				else
				{
					Title.text = LOCSYS.Translate("Rail Tracks");
					Line1.text = LOCSYS.Translate("Type") + "  :  " + LOCSYS.Translate("Rail Tracks");
					Line2.text = LOCSYS.Translate("Traffic") + "  :  " + roadCreator2.TrafficSlipsal + " / " + roadCreator2.Capacity;
					Line5.text = LOCSYS.Translate("Maintenance") + "  :  $" + roadCreator2.maintenance;
				}
				Line3.text = LOCSYS.Translate("Elevation") + "  :  " + (bounded.h - 30f) * 8f + "m (" + ((double)((bounded.h - 30f) * 8f) * 3.281).ToString("N0") + "ft)";
				Line4.text = LOCSYS.Translate("LandValue") + "  :  $" + bounded.v[0] * 4 + "/sqm";
				Line6.text = LOCSYS.Translate("Pollutants") + "  :  " + Mathf.Round(bounded.v[1] * 10) + "μg/m3";
			}
		}
		else if (bounded.p)
		{
			Title.text = "Pedestrian Walkway";
			Line1.text = LOCSYS.Translate("Type") + "  :  " + LOCSYS.Translate("2lane");
			Line4.text = LOCSYS.Translate("LandValue") + "  :  $" + bounded.v[0] * 4 + "/sqm";
			Line6.text = LOCSYS.Translate("Pollutants") + "  :  " + Mathf.Round(bounded.v[1] * 10) + "μg/m3";
		}
		oldx1 = (int)bi.pos.x;
		oldz1 = (int)bi.pos.z;
	}
}
