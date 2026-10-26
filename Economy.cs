using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AzureSky;

public class Economy : MonoBehaviour
{
	public float Population = 1f;

	public float NationPop = 1f;

	public float otherCitiesPop = 1f;

	public float InformalPop;

	public float LCPartPercent;

	public float LCnaturalGrowth;

	public float LCPop;

	public float MCPartPercent;

	public float MCnaturalGrowth;

	public float MCPop;

	public float UCPartPercent;

	public float UCnaturalGrowth;

	public float UCPop;

	public float arcoPop;

	public float PopGrowthNominal;

	public float Liberals;

	public float Conservatives;

	public int ApprovalRate = 50;

	public float Stability = 100f;

	public float GlobalLeaning = 50f;

	private float regionLeaning = 50f;

	public float Slum_Demand;

	public float LC_Demand = 600f;

	public float MC_Demand;

	public float UC_Demand;

	public float M_Demand;

	public float HT_Demand;

	public float R_Demand;

	public float O_Demand;

	public float P_Demand = 100f;

	private DataCity thisCity;

	public float Jobs;

	public float ActivePop;

	public float ActivePercent = 0.6f;

	public float Unemployment;

	public float neighborsJobs;

	public float GDPgrowth;

	public float retailJobs;

	public float retailPart;

	public float officeJobs;

	public float officePart;

	public float manufactureJobs;

	public float manufacturePart;

	public float hightechJobs;

	public float hightechPart;

	public float primaryJobs;

	public float primaryPart;

	public float serviceJobs;

	public float servicePart;

	public float neighborPart;

	private int WorldLength = 20;

	private int tries;

	public World w;

	public Rioting rioting;

	public Roading r;

	public Calendar calendar;

	public float SocialLadderPolicy;

	public float PolicyLeaning;

	public float MCaccess;

	public float UCaccess;

	public float FreedomIndex;

	public float CivilRights;

	public float riotCivilRights;

	public float prisonCivilRights;

	public float BirthRatePolicy;

	public float BirthRate;

	public float Security;

	public float riotSecurity;

	public float EnviroPol;

	public float Environment = 1f;

	public float SchoolEfficiency;

	public float HospitalEfficiency;

	public float Corruption;

	public float ApprovalRight = 50f;

	public float ApprovalLeft = 50f;

	public float Cost;

	public float Religiousness;

	public float ReligionScore;

	public float Retail;

	public float Office;

	public float Manufacturing;

	public float HighTech;

	public int enactedPolicies;

	public int[,,] data = new int[26, 3, 20];

	public float byCar;

	public float byBus;

	public float byTrain;

	public float toNeighbor;

	public float byWalk;

	public float byAirTaxi;

	public float Congestion;

	public int SchoolCapacity = 50;

	public int HighSchoolCapacity = 1;

	public int UniCapacity = 1;

	public int TotalPupils;

	public int TotalStudents;

	public int HospitalCapacity = 50;

	public int HospitalUse;

	public int FireCapacity = 20;

	public int FireUse;

	public int PoliceCapacity = 40;

	public int PoliceUse;

	public int Schools;

	public int HighSchools;

	public int Hospitals;

	public int PoliceStations;

	public int FireStations;

	public int BuildingsCount;

	public int PowerCapacity = 1;

	public int otherCitiesPowerUse;

	public int PowerUse;

	public int WaterCapacity = 1;

	public int WaterUse;

	public int PrisonCapacity = 1;

	public int PrisonUse;

	public int DroneCapacity;

	public int DroneUse;

	public bool growthStopped;

	public float EducationQuality;

	public float EducationLevel;

	public int HealthScore;

	public int SecurityScore;

	public float SecurityLevel;

	public float ParkScore;

	public float ParkCapacity;

	public int QualityOfLife;

	public int Inequality;

	public float PollutionRate = 1f;

	public long GDP;

	public int AverageValue = 200;

	public bool ongoingDecay;

	public bool isBankrupted;

	public float Undocumented = 1f;

	public float Immigrants = 1f;

	public float Locals = 1f;

	private readonly float polEffect = 10f;

	public float immiDivider = 10f;

	public float birthBase = 2.1f;

	private readonly float accessMultipl = 10000f;

	private float GDPmultiplier = 50000f;

	private readonly float stabGrowth = 0.02f;

	public readonly float polBudgetDivider = 0.05f;

	public readonly float R_X = 1.4f;

	public readonly float M_X = 1.2f;

	public readonly float HT_X = 1.3f;

	public readonly float LC_X = 0.079f;

	public float Slum_X = 0.05f;

	public readonly float MC_X = 0.084f;

	public readonly float UC_X = 0.089f;

	public int cityStage = 1;

	public Stats stats;

	public Budget budget;

	public Immigration immi;

	public Legislation_UI leg;

	public Messages messages;

	public GameObject tZone;

	public ServiceCreator HQbuilding;

	public GameObject slumUnit;

	private string slumReason = "NoAffordableH";

	public AzureTimeController timeController;

	private short step;

	private bool invert;

	private float tPollution;

	private float tPark_count;

	private float Park_Count;

	private float retailPOL;

	private float officePOL;

	private float manPOL;

	private float HTPOL;

	private float AM_inflation;

	private float BM_exRate;

	private float asyncR1;

	private float asyncR2;

	private float indusEducation;

	private float mcPol;

	private float mcU;

	private float mcTax;

	private float mcEdu;

	private float mcInf;

	private float ucPol;

	private float ucU;

	private float ucTax;

	private float ucFI;

	private float ucCorru;

	public float ladderHealth;

	public float fix = 50f;

	private float waitTime = 0.25f;

	private int stageUpTries;

	private void Start()
	{
		WorldLength = World.chunkSize * World.worldSize - 1;
		otherCitiesPop = (float)DataRegion.Population - Population;
		isBankrupted = false;
		if (DataRegion.ecoSettings[5] == 0f)
		{
			regionLeaning = 30f;
		}
		else if (DataRegion.ecoSettings[5] == 1f)
		{
			regionLeaning = 50f;
		}
		else
		{
			regionLeaning = 70f;
		}
		thisCity = DataRegion.GetCity(DataRegion.selectedCity.rX, DataRegion.selectedCity.rZ);
		otherCitiesPowerUse = DataRegion.PowerCapacity - thisCity.powerCapacity - (DataRegion.PowerUse - thisCity.powerUse);
		budget.otherCitiesTradeUse = (float)(DataRegion.TradeCapacity - thisCity.TradeCapacity) - (DataRegion.TradeUse - (float)thisCity.TradeUse);
		budget.UI_NationNameBank.text = DataRegion.nationName + " Central Bank";
		if (calendar == null && stats != null)
		{
			if (stats.calendar == null)
			{
				Debug.Log("CANT RETRIEVE CALENDAR!");
				return;
			}
			calendar = stats.calendar;
			Debug.Log("Calendar ref in eco retrieved.");
		}
		StartCoroutine(StatsUpdate());
		StartCoroutine(LandValueUpdate());
	}

	public void LaunchGrowth()
	{
		StartCoroutine(BuildNewCompany());
		StartCoroutine(NewResidential());
		StartCoroutine(StageUpBuilding());
	}

	private void EvolveDemands()
	{
		if (World.dataReady)
		{
			float num;
			float num2;
			if (Population > 2000000f)
			{
				num = 100f;
				num2 = 0.1f;
			}
			else if (Population > 1000000f)
			{
				num = 52f;
				num2 = 0.1f;
			}
			else if (Population > 500000f)
			{
				num = 44f;
				num2 = 0.1f;
			}
			else if (Population > 250000f)
			{
				num = 36f;
				num2 = 0.1f;
			}
			else if (Population > 100000f)
			{
				num = 28f;
				num2 = 0.1f;
			}
			else if (Population > 50000f)
			{
				num = 20f;
				num2 = 0.09f;
			}
			else if (Population > 25000f)
			{
				num = 16f;
				num2 = 0.08f;
			}
			else if (Population > 10000f)
			{
				num = 14f;
				num2 = 0.07f;
			}
			else if (Population > 5000f)
			{
				num = 12f;
				num2 = 0.06f;
			}
			else
			{
				num = 10f;
				num2 = 0.05f;
			}
			float num3 = 40f + LCPop * (LC_X * Retail) + MCPop * (MC_X * Retail) + UCPop * (UC_X * Retail);
			float num4 = Mathf.Clamp(num2 * (num3 - retailJobs), 0f - num, num);
			if (num4 > 0f && !growthStopped && Unemployment > 0f)
			{
				R_Demand += num4;
			}
			else if (num4 <= 0f)
			{
				R_Demand += num4 * 0.5f;
			}
			w.indexer.Growth_R.text = num4.ToString("+0;-0");
			float num5 = manufactureJobs * (M_X * Office) + hightechJobs * (HT_X * Office);
			float num6 = Mathf.Clamp(num2 * (num5 - officeJobs), 0f - num, num);
			if (num6 > 0f && !growthStopped && Unemployment > 0f)
			{
				O_Demand += num6;
			}
			else if (num6 <= 0f)
			{
				O_Demand += num6 * 0.5f;
			}
			w.indexer.Growth_O.text = num6.ToString("+0;-0");
			float num7 = retailJobs * (R_X * Manufacturing);
			float num8 = Mathf.Clamp(num2 * (num7 - manufactureJobs), 0f - num, num);
			if (num8 > 0f && !growthStopped && Unemployment > 0f)
			{
				M_Demand += num8;
			}
			else if (num8 <= 0f)
			{
				M_Demand += num8 * 0.5f;
			}
			w.indexer.Growth_M.text = num8.ToString("+0;-0");
			float num9 = retailJobs * (R_X * HighTech);
			float num10 = Mathf.Clamp(num2 * (num9 - hightechJobs), 0f - num, num);
			if (num10 > 0f && !growthStopped && Unemployment > 0f)
			{
				HT_Demand += num10;
			}
			else if (num10 <= 0f)
			{
				HT_Demand += num10 * 0.5f;
			}
			w.indexer.Growth_HT.text = num10.ToString("+0;-0");
		}
	}

	private IEnumerator LandValueUpdate()
	{
		while (true)
		{
			int _delay = 0;
			tPollution = 1f;
			tPark_count = 0f;
			step = 48;
			if (invert)
			{
				for (int x = 0; x <= WorldLength; x++)
				{
					for (int z = 0; z <= WorldLength; z++)
					{
						_delay++;
						if (_delay > 255)
						{
							_delay = 0;
							yield return null;
						}
						ComputePropagation(x, z);
					}
				}
			}
			else
			{
				for (int x = WorldLength; x >= 0; x--)
				{
					for (int z = WorldLength; z >= 0; z--)
					{
						_delay++;
						if (_delay > 255)
						{
							_delay = 0;
							yield return null;
						}
						ComputePropagation(x, z);
					}
				}
			}
			invert = !invert;
			foreach (ServiceCreator service in w.Services)
			{
				if (!(service.typeName == "CC"))
				{
					continue;
				}
				for (int i = (int)service.Anchor.x - 14; i <= (int)service.Anchor.x + 14; i++)
				{
					for (int j = (int)service.Anchor.z - 14; j <= (int)service.Anchor.z + 14; j++)
					{
						w.GetBounded(i, j).v[1] = -1;
					}
				}
				yield return null;
			}
			PollutionRate = Mathf.Round((tPollution + 1f) / (float)(WorldLength * WorldLength));
			Park_Count = tPark_count;
			yield return new WaitForEndOfFrame();
		}
	}

	private void ComputePropagation(int x, int z)
	{
		float num = 0f;
		float num2 = 0f;
		step = 32;
		BlockData blockData = w.blockData[x, z];
		if (blockData.t == BlockData.Type.GRASSTREE)
		{
			num = 20f;
			step = 256;
		}
		else if (blockData.h == 29.75f)
		{
			num = 75f;
		}
		if (blockData.p)
		{
			num = 75f;
			tPark_count += 0.5f;
			step = 256;
		}
		else if (blockData.lO != null)
		{
			LotCreator lO = blockData.lO;
			if (lO.lClass == LotCreator.LotClass.UPPER || lO.lClass == LotCreator.LotClass.RETAIL || lO.lClass == LotCreator.LotClass.HIGHTECH)
			{
				num += (float)(lO.Stage * lO.Stage) * 3.5f;
			}
			else if (lO.lClass == LotCreator.LotClass.MIDDLE || lO.lClass == LotCreator.LotClass.OFFICE)
			{
				num += (float)(lO.Stage * lO.Stage) * 2.5f;
			}
			else if (lO.lClass == LotCreator.LotClass.MIXRETAIL)
			{
				num += (float)((lO.Stage + 3) * (lO.Stage + 3)) * 3.5f;
			}
			else if (lO.lClass == LotCreator.LotClass.MIXOFFICE)
			{
				num += (float)((lO.Stage + 3) * (lO.Stage + 3)) * 3.5f;
			}
			else if (lO.lClass == LotCreator.LotClass.MANUFACTURE)
			{
				num2 = (float)lO.Occupants * 2f * (2.1f - EnviroPol) * w.pollutionMultiplier;
			}
			else if (lO.lClass == LotCreator.LotClass.PRIMARY)
			{
				num2 = (float)lO.Occupants * 3.5f * (2.1f - EnviroPol) * w.pollutionMultiplier;
			}
			if (lO.isDecayed)
			{
				num = -10f;
			}
			else if (lO.MaxStage == 1)
			{
				num += 20f;
			}
			num += lO.eduLevel * 0.3f + (float)lO.healthLevel * 0.5f + (float)(lO.secuLevel - 50) * 0.6f;
		}
		else if (blockData.r)
		{
			RoadCreator ifNotNull = r.GetIfNotNull(x, 1, z);
			if (ifNotNull != null)
			{
				float num3 = Mathf.Clamp(ifNotNull.TrafficPercent - 80, 0, 200);
				if (num3 > 0f)
				{
					num2 = ((!ifNotNull.treeLine) ? (num3 * 0.8f * (2.2f - EnviroPol) * w.pollutionMultiplier) : (num3 * 0.3f * (2.2f - EnviroPol) * w.pollutionMultiplier));
				}
				if (ifNotNull.stopType != RoadCreator.StopType.NONE)
				{
					num += 140f;
				}
			}
			ifNotNull = r.GetIfNotNull(x, 2, z);
			if (ifNotNull != null)
			{
				float num4 = ifNotNull.TrafficPercent - 80;
				if (num4 > 0f)
				{
					num2 = num4 * (2.1f - EnviroPol) * w.pollutionMultiplier;
				}
			}
		}
		else if (blockData.sO != null)
		{
			if (blockData.sO.type == ServiceCreator.Type.Power)
			{
				if (blockData.sO.typeName == "D")
				{
					num2 = 500f;
					num -= 50f;
				}
				else if (blockData.sO.typeName == "C")
				{
					num2 = 800f;
					num -= 50f;
				}
				else
				{
					num -= 10f;
				}
			}
			else if (blockData.sO.type == ServiceCreator.Type.Park && blockData.sO.typeName != "CC")
			{
				num += 100f;
			}
			else if (blockData.sO.type == ServiceCreator.Type.Drone)
			{
				num += 100f;
				tPark_count++;
			}
			else if (blockData.sO.type == ServiceCreator.Type.School)
			{
				num = 40f;
			}
			else if (blockData.sO.type != ServiceCreator.Type.Arcology)
			{
				if (blockData.sO.type == ServiceCreator.Type.Government)
				{
					num = 75f;
				}
				else
				{
					num = ((blockData.sO.type != ServiceCreator.Type.Prison) ? 5f : (-250f));
				}
			}
			else
			{
				num = blockData.sO.Stage * 20;
				tPark_count += 20f;
			}
		}
		tPollution += num2;
		BlockData bounded = w.GetBounded(x + 1, z);
		BlockData bounded2 = w.GetBounded(x - 1, z);
		BlockData bounded3 = w.GetBounded(x, z + 1);
		BlockData bounded4 = w.GetBounded(x, z - 1);
		if ((float)(bounded.v[1] - step) > num2)
		{
			num2 = bounded.v[1] - step;
		}
		if ((float)(bounded2.v[1] - step) > num2)
		{
			num2 = bounded2.v[1] - step;
		}
		if ((float)(bounded3.v[1] - step) > num2)
		{
			num2 = bounded3.v[1] - step;
		}
		if ((float)(bounded4.v[1] - step) > num2)
		{
			num2 = bounded4.v[1] - step;
		}
		if (num2 < 0f)
		{
			num2 = 0f;
		}
		else if (num2 > 2000f)
		{
			num2 = 2000f;
		}
		if (blockData.v[1] >= 0)
		{
			blockData.v[1] = (short)num2;
		}
		num *= budget.Pro_Effect;
		num -= num2 / 5f;
		if (num < 0f)
		{
			num = 0f;
		}
		blockData.v[0] = (short)(num + 0.2f * (float)(bounded.v[0] + bounded2.v[0] + bounded3.v[0] + bounded4.v[0]));
		if ((float)bounded.v[0] * 0.95f > (float)blockData.v[0])
		{
			blockData.v[0] = (short)((float)bounded.v[0] * 0.95f);
		}
		if ((float)bounded2.v[0] * 0.95f > (float)blockData.v[0])
		{
			blockData.v[0] = (short)((float)bounded2.v[0] * 0.95f);
		}
		if ((float)bounded3.v[0] * 0.95f > (float)blockData.v[0])
		{
			blockData.v[0] = (short)((float)bounded3.v[0] * 0.95f);
		}
		if ((float)bounded4.v[0] * 0.95f > (float)blockData.v[0])
		{
			blockData.v[0] = (short)((float)bounded4.v[0] * 0.95f);
		}
		if (blockData.v[0] > 1600)
		{
			blockData.v[0] = 1600;
		}
		else if (blockData.v[0] < 1)
		{
			blockData.v[0] = 1;
		}
	}

	private void GrowthFactors()
	{
		AM_inflation = (0.25f * budget.Inflation - 0.3f * Mathf.Pow(budget.Inflation, 2f) + 0.5f) / 10f;
		indusEducation = (EducationLevel - 50f) / 40f;
		BM_exRate = (0f - (7.5f - Budget.ExchangeRate)) * 6f / 100f;
		if (AM_inflation < -0.4f)
		{
			AM_inflation = -0.4f;
		}
		else if (AM_inflation > 0.5f)
		{
			AM_inflation = 0.5f;
		}
		HighTech += indusEducation + AM_inflation + budget.HT_Effect / 10f;
		Manufacturing += 0f - indusEducation + BM_exRate + budget.M_Effect / 10f;
		Retail += budget.R_Effect / 10f;
		Office += (FreedomIndex - 50f) / 200f + (EducationLevel - 50f) / 100f + budget.O_Effect / 10f;
		P_Demand = Environment;
		if (Unemployment < 0f)
		{
			float num = Mathf.Abs(Unemployment) * 2f;
			if (num > 5f)
			{
				num = 5f;
			}
			M_Demand -= num;
			HT_Demand -= num;
			R_Demand -= num;
		}
	}

	private void Employment()
	{
		Unemployment = (ActivePop - (Jobs + neighborsJobs)) * 100f / ActivePop;
		if (w.unemployStabilizer)
		{
			if (Population < 400f)
			{
				Unemployment = 10f;
			}
			else if (Population < 8000f)
			{
				Unemployment = (Unemployment + 15f) / 4f;
			}
			else if (Population < 32000f)
			{
				Unemployment = (Unemployment + 10f) / 3f;
			}
			else
			{
				Unemployment = (Unemployment + 5f) / 2f;
			}
		}
		else if (Population < 400f)
		{
			Unemployment = 10f;
		}
		else if (Population < 4000f)
		{
			Unemployment = (Unemployment + 5f) / 2f;
		}
		else if (Population < 12000f)
		{
			Unemployment = (Unemployment * 2f + 5f) / 3f;
		}
		else if (Population < 32000f)
		{
			Unemployment = (Unemployment * 4f + 5f) / 5f;
		}
		Unemployment = Mathf.Clamp(Unemployment, -10f, 99f);
		float num = 0.725f - Unemployment / 60f + (EducationLevel - 25f) / 500f;
		ActivePercent = (ActivePercent * 4f + num) / 5f;
		if (ActivePercent > 0.825f)
		{
			ActivePercent = 0.825f;
		}
		else if (ActivePercent < 0.4f)
		{
			ActivePercent = 0.4f;
		}
		neighborsJobs = (0.825f - ActivePercent) * w.neighborJobsAvailable;
		ActivePop = Mathf.Clamp(Population * ActivePercent, 1f, Population + 1f);
		int num2 = Mathf.RoundToInt(Jobs);
		if (num2 < 0)
		{
			num2 = 0;
		}
		DataRegion.cities[w.rX, w.rZ].jobsAvailable = num2;
		DataRegion.cities[w.rX, w.rZ].jobsShared = Mathf.RoundToInt(neighborsJobs);
		retailPart = retailJobs * 100f / (Jobs + 1f);
		officePart = officeJobs * 100f / (Jobs + 1f);
		manufacturePart = manufactureJobs * 100f / (Jobs + 1f);
		hightechPart = hightechJobs * 100f / (Jobs + 1f);
		servicePart = serviceJobs * 100f / (Jobs + 1f);
		primaryPart = primaryJobs * 100f / (Jobs + 1f);
		neighborPart = neighborsJobs * 100f / (Jobs + 1f);
	}

	private IEnumerator StatsUpdate()
	{
		while (true)
		{
			SocialLadderPolicy = 0.4f;
			FreedomIndex = 50f;
			CivilRights = 50f + prisonCivilRights;
			Security = 0f;
			EnviroPol = 1f;
			SchoolEfficiency = 0f;
			HospitalEfficiency = 0f;
			Cost = 0f;
			Corruption = 0.5f;
			Religiousness = 0.5f;
			Retail = 1f;
			Office = 1f;
			Manufacturing = 1f;
			HighTech = 1f;
			enactedPolicies = 0;
			BirthRatePolicy = birthBase;
			PolicyLeaning = regionLeaning - 50f;
			BirthRatePolicy += (float)budget.edenCount / 3f;
			for (int i = 4; i < leg.Array.totalLines; i += 20)
			{
				int num = int.Parse(leg.Array.grid[2, i + 1]);
				if (num != 4)
				{
					enactedPolicies++;
					SocialLadderPolicy += float.Parse(leg.Array.grid[4 + num, i + 1], CultureInfo.InvariantCulture);
					PolicyLeaning += float.Parse(leg.Array.grid[4 + num, i + 2], CultureInfo.InvariantCulture) * 5f;
					Retail += float.Parse(leg.Array.grid[4 + num, i + 3], CultureInfo.InvariantCulture) / polEffect;
					Office += float.Parse(leg.Array.grid[4 + num, i + 4], CultureInfo.InvariantCulture) / polEffect;
					Manufacturing += float.Parse(leg.Array.grid[4 + num, i + 5], CultureInfo.InvariantCulture) / polEffect;
					HighTech += float.Parse(leg.Array.grid[4 + num, i + 6], CultureInfo.InvariantCulture) / polEffect;
					SchoolEfficiency += float.Parse(leg.Array.grid[4 + num, i + 8], CultureInfo.InvariantCulture);
					HospitalEfficiency += float.Parse(leg.Array.grid[4 + num, i + 9], CultureInfo.InvariantCulture);
					Corruption += float.Parse(leg.Array.grid[4 + num, i + 10], CultureInfo.InvariantCulture);
					Cost += float.Parse(leg.Array.grid[4 + num, i + 11], CultureInfo.InvariantCulture);
					FreedomIndex += float.Parse(leg.Array.grid[4 + num, i + 12], CultureInfo.InvariantCulture) * 10f;
					CivilRights += float.Parse(leg.Array.grid[4 + num, i + 13], CultureInfo.InvariantCulture) * 10f;
					BirthRatePolicy += float.Parse(leg.Array.grid[4 + num, i + 15], CultureInfo.InvariantCulture);
					Security += float.Parse(leg.Array.grid[4 + num, i + 16], CultureInfo.InvariantCulture);
					EnviroPol += float.Parse(leg.Array.grid[4 + num, i + 17], CultureInfo.InvariantCulture) * 0.25f;
					Religiousness += float.Parse(leg.Array.grid[4 + num, i + 18], CultureInfo.InvariantCulture);
				}
			}
			retailPOL = Retail;
			officePOL = Office;
			manPOL = Manufacturing;
			HTPOL = HighTech;
			w.UpdateNeighborJobs();
			GrowthFactors();
			Employment();
			Cost = Cost * polBudgetDivider * Population;
			budget.Policies_Income = Cost;
			if (PollutionRate > 100f)
			{
				PollutionRate = 100f;
			}
			EnviroPol = Mathf.Clamp(EnviroPol, 0.1f, 2f);
			Environment = Mathf.Clamp(100f - PollutionRate, 0f, 100f);
			UpdateLegislationResume();
			if (riotCivilRights > 0f)
			{
				riotCivilRights -= 0.05f;
			}
			else if (riotCivilRights < 0f)
			{
				riotCivilRights += 0.05f;
			}
			CivilRights += riotCivilRights;
			if (Random.value < 0.05f && w.Services.Count > 1 && timeController.m_timeline > 4f && timeController.m_timeline < 22f)
			{
				int index = Random.Range(0, w.Services.Count);
				if (w.Services[index].audioSource != null)
				{
					w.Services[index].audioSource.Play();
				}
			}
			UpdateApproval();
			QualityOfLife = Mathf.RoundToInt((ParkScore + (float)HealthScore + (float)SecurityScore + Environment) / 4f);
			float num2 = ((!(MCPartPercent + LCPartPercent > UCPartPercent)) ? UCPartPercent : MCPartPercent);
			Inequality = Mathf.RoundToInt((LCPartPercent + UCPartPercent - num2 + 0.3f * FreedomIndex) / 1.25f);
			Inequality = Mathf.Clamp(Inequality, 0, 100);
			yield return new WaitForEndOfFrame();
			Population = 1f;
			InformalPop = 0f;
			GDP = 1000L;
			LCPop = 0f;
			MCPop = 0f;
			UCPop = 0f;
			arcoPop = 0f;
			retailJobs = 0f;
			officeJobs = 0f;
			manufactureJobs = 0f;
			hightechJobs = 0f;
			primaryJobs = 0f;
			LCnaturalGrowth = 0f;
			MCnaturalGrowth = 0f;
			UCnaturalGrowth = 0f;
			Jobs = serviceJobs + neighborsJobs;
			WaterUse = 0;
			PowerUse = 0;
			byCar = 0f;
			byBus = 0f;
			byTrain = 0f;
			toNeighbor = 0f;
			byWalk = 0f;
			byAirTaxi = 0f;
			float num3 = 0f;
			foreach (LotCreator item in w.ResidentialBuildings.Concat(w.MixedBuildings).ToList())
			{
				item.watered = true;
				item.powered = true;
				num3 += (float)(item.reliLevel * item.Occupants);
				if (item.commuteState == "cc")
				{
					byCar += item.Occupants;
				}
				else if (item.commuteState == "wc")
				{
					byWalk += item.Occupants;
				}
				else if (item.commuteState == "tc")
				{
					byTrain += item.Occupants;
				}
				else if (item.commuteState == "bc")
				{
					byBus += item.Occupants;
				}
				else if (item.commuteState == "nc")
				{
					toNeighbor += item.Occupants;
				}
				else if (item.commuteState == "at")
				{
					byAirTaxi += item.Occupants;
				}
				if (item.lClass == LotCreator.LotClass.LOW)
				{
					LCPop += item.Occupants;
					LCnaturalGrowth += (float)item.Occupants * ((item.birthRate - 2.1f) / 7500f);
					PowerUse += Mathf.RoundToInt((float)item.Occupants * 0.13f);
					WaterUse += Mathf.RoundToInt((float)item.Occupants * 0.13f);
				}
				else if (item.lClass == LotCreator.LotClass.MIDDLE)
				{
					MCPop += item.Occupants;
					MCnaturalGrowth += (float)item.Occupants * ((item.birthRate - 2.1f) / 7500f);
					PowerUse += Mathf.RoundToInt((float)item.Occupants * 0.2f);
					WaterUse += Mathf.RoundToInt((float)item.Occupants * 0.2f);
				}
				else if (item.lClass == LotCreator.LotClass.UPPER)
				{
					UCPop += item.Occupants;
					UCnaturalGrowth += (float)item.Occupants * ((item.birthRate - 2.1f) / 7500f);
					PowerUse += Mathf.RoundToInt((float)item.Occupants * 0.27f);
					WaterUse += Mathf.RoundToInt((float)item.Occupants * 0.27f);
				}
				else if (item.lClass == LotCreator.LotClass.MIXRETAIL)
				{
					MCPop += item.Occupants;
					retailJobs += item.Visitors;
					MCnaturalGrowth += (float)item.Occupants * ((item.birthRate - 2.1f) / 7500f);
					PowerUse += Mathf.RoundToInt((float)(item.Occupants + item.Visitors) * 0.2f);
					WaterUse += Mathf.RoundToInt((float)(item.Occupants + item.Visitors) * 0.2f);
					GDP += Mathf.RoundToInt((float)(item.Visitors * 2) * GDPmultiplier);
				}
				else if (item.lClass == LotCreator.LotClass.MIXOFFICE)
				{
					UCPop += item.Occupants;
					officeJobs += item.Visitors;
					UCnaturalGrowth += (float)item.Occupants * ((item.birthRate - 2.1f) / 7500f);
					PowerUse += Mathf.RoundToInt((float)(item.Occupants + item.Visitors) * 0.27f);
					WaterUse += Mathf.RoundToInt((float)(item.Occupants + item.Visitors) * 0.27f);
					GDP += Mathf.RoundToInt((float)(item.Visitors * (item.Stage + 2)) * GDPmultiplier);
				}
				if (PowerUse > PowerCapacity)
				{
					item.powered = false;
				}
				if (WaterUse > WaterCapacity)
				{
					item.watered = false;
				}
			}
			foreach (ServiceCreator arcology in w.Arcologies)
			{
				PowerUse += Mathf.RoundToInt((float)arcology.MinEmployees * 0.3f);
				arcoPop += arcology.MinEmployees;
				arcology.Use = Mathf.RoundToInt(0.01f * (float)arcology.MinEmployees + 0.025f * (float)arcology.MinEmployees);
				officeJobs += 0.01f * (float)arcology.MinEmployees;
				hightechJobs += 0.025f * (float)arcology.MinEmployees;
				MCnaturalGrowth += (float)arcology.MinEmployees * (BirthRatePolicy - 2.1f) / 7500f;
				GDP += Mathf.RoundToInt((float)(arcology.MinEmployees * 8) * GDPmultiplier);
			}
			foreach (SlumCreator item2 in w.SlumBuildings.ToList())
			{
				InformalPop += item2.Occupants;
				LCnaturalGrowth += item2.Occupants / 7500;
				PowerUse += Mathf.RoundToInt((float)item2.Occupants * 0.1f);
				WaterUse += Mathf.RoundToInt((float)item2.Occupants * 0.1f);
			}
			float num4 = byCar + byBus + byWalk + byTrain + toNeighbor + byAirTaxi;
			Population = LCPop + MCPop + UCPop + InformalPop + arcoPop;
			LCPartPercent = LCPop * 100f / (Population + 1f);
			MCPartPercent = (MCPop + arcoPop) * 100f / (Population + 1f);
			UCPartPercent = UCPop * 100f / (Population + 1f);
			byCar = byCar * 100f / (num4 + 1f);
			byBus = byBus * 100f / (num4 + 1f);
			byWalk = byWalk * 100f / (num4 + 1f);
			byTrain = byTrain * 100f / (num4 + 1f);
			toNeighbor = toNeighbor * 100f / (num4 + 1f);
			byAirTaxi = byAirTaxi * 100f / (num4 + 1f);
			GDPmultiplier = Mathf.Abs(LCPartPercent * 8f + MCPartPercent * 16f + UCPartPercent * 128f);
			foreach (LotCreator item3 in w.IndustrialBuildings.ToList())
			{
				item3.watered = true;
				item3.powered = true;
				Jobs += item3.Occupants;
				if (item3.lClass == LotCreator.LotClass.HIGHTECH)
				{
					hightechJobs += item3.Occupants;
					PowerUse += Mathf.RoundToInt((float)item3.Occupants * 0.3f);
					WaterUse += Mathf.RoundToInt((float)item3.Occupants * 0.3f);
					GDP += Mathf.RoundToInt((float)(item3.Occupants * (item3.Stage + 6)) * GDPmultiplier);
				}
				else if (item3.lClass == LotCreator.LotClass.PRIMARY)
				{
					if ((item3.Stage != 0 || item3.powered) && (item3.Stage != 0 || item3.watered))
					{
						primaryJobs += item3.Occupants;
					}
					PowerUse += Mathf.RoundToInt((float)item3.Occupants * 0.75f);
					WaterUse += Mathf.RoundToInt((float)item3.Occupants * 0.75f);
					GDP += Mathf.RoundToInt((float)(item3.Occupants * 14) * GDPmultiplier);
				}
				else
				{
					if ((item3.Stage != 0 || item3.powered) && (item3.Stage != 0 || item3.watered))
					{
						manufactureJobs += item3.Occupants;
					}
					PowerUse += Mathf.RoundToInt((float)item3.Occupants * 0.5f);
					WaterUse += Mathf.RoundToInt((float)item3.Occupants * 0.5f);
					GDP += Mathf.RoundToInt((float)(item3.Occupants * (item3.Stage + 2)) * GDPmultiplier);
				}
				if (PowerUse > PowerCapacity)
				{
					item3.powered = false;
				}
				if (WaterUse > WaterCapacity)
				{
					item3.watered = false;
				}
			}
			foreach (LotCreator item4 in w.CommercialBuildings.ToList())
			{
				item4.watered = true;
				item4.powered = true;
				Jobs += item4.Occupants;
				PowerUse += Mathf.RoundToInt((float)item4.Occupants * 0.2f);
				WaterUse += Mathf.RoundToInt((float)item4.Occupants * 0.2f);
				if (PowerUse > PowerCapacity)
				{
					item4.powered = false;
				}
				if (WaterUse > WaterCapacity)
				{
					item4.watered = false;
				}
				if (item4.lClass == LotCreator.LotClass.RETAIL)
				{
					retailJobs += item4.Occupants;
					GDP += Mathf.RoundToInt((float)(item4.Occupants * 2) * GDPmultiplier);
				}
				else
				{
					officeJobs += item4.Occupants;
					Mathf.RoundToInt((float)(item4.Occupants * (item4.Stage + 2)) * GDPmultiplier);
				}
			}
			NationPop = (float)(int)Population + otherCitiesPop;
			yield return new WaitForEndOfFrame();
			if (Population > 1f)
			{
				PopulationGrowth();
			}
			else if (LC_Demand < 20f)
			{
				LC_Demand = 20f;
			}
			yield return new WaitForEndOfFrame();
			StatsPopulationUpdate();
			asyncR2 = Random.value * 100f;
			yield return new WaitForEndOfFrame();
			StatsEcoUpdate(ActivePercent);
			if (budget.Balance < -10000f && !isBankrupted)
			{
				StartCoroutine(BankruptProcess());
			}
			for (int j = 0; j <= 4; j++)
			{
				if (calendar != null)
				{
					calendar.NextDay();
				}
				else
				{
					calendar = stats.calendar;
					Debug.Log("Calendar ref retrieved at the last minute!");
				}
				StartCoroutine(ServiceUpdate());
				yield return new WaitForSeconds(0.75f);
			}
			asyncR1 = Random.value * 100f;
		}
	}

	private IEnumerator ServiceUpdate()
	{
		int num = SchoolCapacity;
		if (SchoolCapacity <= 50)
		{
			SchoolCapacity = 50;
			num = 50;
		}
		else
		{
			SchoolCapacity = 1;
		}
		int num2 = HighSchoolCapacity;
		if (HighSchoolCapacity < 2)
		{
			HighSchoolCapacity = 2;
			num2 = 2;
		}
		else
		{
			HighSchoolCapacity = 1;
		}
		UniCapacity = 1;
		TotalPupils = Mathf.RoundToInt((Population - arcoPop) * 0.08f);
		float num3 = TotalPupils * 100 / num;
		TotalStudents = Mathf.RoundToInt((Population - arcoPop) * 0.06f);
		float num4 = TotalStudents * 100 / num2;
		int hospitalCapacity = HospitalCapacity;
		if (HospitalCapacity <= 5)
		{
			HospitalCapacity = 5;
		}
		else
		{
			HospitalCapacity = 1;
		}
		HospitalUse = Mathf.RoundToInt((Population - arcoPop) * 0.0075f);
		float num5 = HospitalUse * 100 / hospitalCapacity;
		int powerUse = PowerUse;
		int waterUse = WaterUse;
		int num6 = PowerCapacity;
		int num7 = WaterCapacity;
		PowerCapacity = 1 + otherCitiesPowerUse;
		WaterCapacity = 1;
		if (PowerCapacity == 0)
		{
			PowerCapacity = 1;
		}
		if (num6 == 0)
		{
			num6 = 1;
		}
		if (num7 == 0)
		{
			num7 = 1;
		}
		prisonCivilRights = 0f;
		PrisonUse = Mathf.RoundToInt((205f - FreedomIndex - MCPartPercent) / 175000f * Population);
		int num8 = PrisonCapacity;
		PrisonCapacity = 20;
		if (PrisonCapacity == 0)
		{
			PrisonCapacity = 1;
		}
		if (PrisonUse == 0)
		{
			PrisonUse = 1;
		}
		if (num8 == 0)
		{
			num8 = 1;
		}
		float num9 = (float)PrisonUse * 100f / (float)num8;
		BuildingsCount = w.ResidentialBuildings.Count + w.CommercialBuildings.Count + w.IndustrialBuildings.Count + w.MixedBuildings.Count + w.SlumBuildings.Count;
		FireUse = BuildingsCount;
		PoliceUse = Mathf.RoundToInt((2f * InformalPop + LCPop + MCPop) / 100f);
		float num10 = FireCapacity;
		float num11 = PoliceCapacity;
		float num12 = (float)(PoliceUse * 100) / num11;
		float num13 = (float)(FireUse * 100) / num10;
		float num14 = DroneUse * 100 / (DroneCapacity + 1);
		w.indexer.secuCapExceeded = false;
		if (num12 > 100f || num13 > 100f)
		{
			w.indexer.secuCapExceeded = true;
		}
		FireCapacity = 20;
		PoliceCapacity = 40;
		DroneCapacity = 0;
		Schools = 0;
		Hospitals = 0;
		PoliceStations = 0;
		FireStations = 0;
		HighSchools = 0;
		serviceJobs = 0f;
		ParkCapacity = 0f;
		int schoolrates = 0;
		int highschoolrates = 0;
		int hospitalrates = 0;
		int policerates = 0;
		int firerates = 0;
		int uniScore = 0;
		foreach (ServiceCreator item in w.Services.ToList())
		{
			if (r.GetIfNotNull(item.Entry.x, 1, item.Entry.y) == null && item.open && item.type != ServiceCreator.Type.Park && item.type != ServiceCreator.Type.TrainStation && item.type != ServiceCreator.Type.Government)
			{
				item.open = false;
			}
			if (!item.open)
			{
				item.Cost = (int)(item.Maintenance * 0.1f);
			}
			else if (item.type == ServiceCreator.Type.School)
			{
				if (item.typeName == "S")
				{
					SchoolCapacity += item.Capacity;
					Schools++;
					serviceJobs += item.HiredEmployees + item.FixedEmployees;
					item.Use = Mathf.RoundToInt((float)item.Capacity * (num3 / 100f));
					item.Cost = Mathf.RoundToInt((float)((item.HiredEmployees + item.FixedEmployees) * 10) + item.Maintenance);
					item.Rating();
					schoolrates += item.Grade;
				}
				else if (item.typeName == "H")
				{
					HighSchoolCapacity += item.Capacity;
					HighSchools++;
					serviceJobs += item.HiredEmployees + item.FixedEmployees;
					item.Use = Mathf.RoundToInt((float)item.Capacity * (num4 / 100f));
					item.Cost = Mathf.RoundToInt((float)((item.HiredEmployees + item.FixedEmployees) * 10) + item.Maintenance);
					item.Rating();
					highschoolrates += item.Grade;
				}
				else if (item.typeName == "B")
				{
					item.Cost = Mathf.RoundToInt((float)(item.FixedEmployees * 10) + (float)item.HiredEmployees * 0.025f + item.Maintenance);
					item.Grade = 20 + (item.HiredEmployees - item.MinEmployees) * 100 / (item.MaxEmployees - item.MinEmployees);
					serviceJobs += item.FixedEmployees;
				}
				else if (item.typeName == "U")
				{
					serviceJobs += item.HiredEmployees + item.FixedEmployees;
					UniCapacity += 24000;
					item.Use = Mathf.RoundToInt(NationPop * 0.01f);
					item.Cost = Mathf.RoundToInt((float)((item.HiredEmployees + item.FixedEmployees) * 10) + item.Maintenance);
					item.Capacity = DataRegion.UnivCapacity;
					if (item.Capacity < 24000)
					{
						item.Capacity = 24000;
					}
					if (item.Use > item.Capacity)
					{
						item.Grade = 50;
						uniScore = 10;
					}
					else
					{
						item.Grade = 100;
						uniScore = 20;
					}
				}
			}
			else if (item.type == ServiceCreator.Type.Hospital)
			{
				HospitalCapacity += item.Capacity;
				Hospitals++;
				item.Use = Mathf.RoundToInt((float)item.Capacity * (num5 / 100f));
				item.Cost = Mathf.RoundToInt((float)((item.HiredEmployees + item.FixedEmployees) * 10) + item.Maintenance);
				item.Rating();
				hospitalrates += item.Grade;
			}
			else if (item.type == ServiceCreator.Type.Police)
			{
				PoliceCapacity += item.Capacity;
				PoliceStations++;
				item.Use = Mathf.RoundToInt((float)item.Capacity * (num12 / 100f));
				item.Cost = Mathf.RoundToInt((float)((item.HiredEmployees + item.FixedEmployees) * 10) + item.Maintenance);
				item.Rating();
				policerates += item.Grade;
			}
			else if (item.type == ServiceCreator.Type.Prison)
			{
				PrisonCapacity += item.Capacity;
				item.Use = Mathf.RoundToInt((float)item.Capacity * (num9 / 100f));
				if (item.typeName.StartsWith("G"))
				{
					item.Cost = Mathf.RoundToInt(Mathf.Clamp(item.Use, 0, 1000) * -2);
					prisonCivilRights -= 5f;
				}
				else if (item.typeName.StartsWith("R"))
				{
					item.Cost = Mathf.RoundToInt((float)(item.HiredEmployees * 10) + item.Maintenance + (float)(item.FixedEmployees * 10));
					prisonCivilRights += 5f;
				}
				else
				{
					item.Cost = Mathf.RoundToInt((float)(item.HiredEmployees * 10) + item.Maintenance + (float)(item.FixedEmployees * 10));
				}
			}
			else if (item.type == ServiceCreator.Type.Fire)
			{
				FireCapacity += item.Capacity;
				FireStations++;
				item.Use = Mathf.RoundToInt((float)item.Capacity * (num13 / 100f));
				item.Cost = Mathf.RoundToInt((float)((item.HiredEmployees + item.FixedEmployees) * 20) + item.Maintenance);
				item.Rating();
				firerates += item.Grade;
			}
			else if (item.type == ServiceCreator.Type.Airport)
			{
				item.Cost = Mathf.RoundToInt((float)(item.HiredEmployees * 50 + item.FixedEmployees * 10) + item.Maintenance);
			}
			else if (item.type == ServiceCreator.Type.Port)
			{
				item.Cost = Mathf.RoundToInt((float)(item.HiredEmployees * 4) + item.Maintenance);
			}
			else if (item.type == ServiceCreator.Type.Power)
			{
				PowerCapacity += item.Capacity * item.HiredEmployees;
				item.Use = Mathf.RoundToInt(powerUse * 100 / num6);
				item.Cost = Mathf.RoundToInt((float)item.HiredEmployees * item.Maintenance + (float)(item.FixedEmployees * 10));
			}
			else if (item.type == ServiceCreator.Type.Water)
			{
				WaterCapacity += item.Capacity * item.HiredEmployees;
				item.Use = Mathf.RoundToInt(waterUse * 100 / num7);
				item.Cost = Mathf.RoundToInt((float)item.HiredEmployees * item.Maintenance + (float)(item.FixedEmployees * 10));
			}
			else if (item.type == ServiceCreator.Type.Government || item.type == ServiceCreator.Type.Religion)
			{
				item.Cost = Mathf.RoundToInt((float)(item.HiredEmployees * 2) + item.Maintenance);
			}
			else if (item.type == ServiceCreator.Type.Drone)
			{
				item.Use = Mathf.RoundToInt((float)item.Capacity * (num14 / 100f));
				DroneCapacity += item.Capacity;
			}
			else if (item.type == ServiceCreator.Type.Park)
			{
				if (item.Capacity == 0)
				{
					item.Capacity = item.blockPos.Count * 2;
				}
				ParkCapacity += item.Capacity;
			}
		}
		prisonCivilRights = Mathf.Clamp(prisonCivilRights, -20f, 5f);
		yield return new WaitForEndOfFrame();
		float num15;
		if (Schools > 0)
		{
			num15 = schoolrates / Schools;
		}
		else if (SchoolCapacity >= TotalPupils)
		{
			num15 = 25f;
		}
		else
		{
			num15 = 150 - TotalPupils * 100 / SchoolCapacity;
			if (num15 < 0f)
			{
				num15 = 0f;
			}
		}
		float num16 = ((HighSchools <= 0) ? 1f : ((float)(highschoolrates / HighSchools)));
		if (DataRegion.alreadyBuilt[5])
		{
			uniScore = 20;
		}
		EducationQuality = 0.5f * num15 + 0.3f * num16 + (float)uniScore;
		if (DataRegion.alreadyBuilt[14])
		{
			EducationQuality -= 2f;
		}
		if (DataRegion.UnivCapacity < UniCapacity)
		{
			DataRegion.UnivCapacity = UniCapacity;
		}
		if (Hospitals > 0)
		{
			HealthScore = hospitalrates / Hospitals;
		}
		else if (HospitalCapacity >= HospitalUse)
		{
			HealthScore = 50;
		}
		else
		{
			HealthScore = 150 - HospitalUse * 100 / HospitalCapacity;
			if (HealthScore < 0)
			{
				HealthScore = 0;
			}
		}
		if (PoliceStations > 0)
		{
			policerates /= PoliceStations;
		}
		if (FireStations > 0)
		{
			firerates /= FireStations;
		}
		SecurityScore = Mathf.RoundToInt((policerates + firerates) / 2);
		if (DataRegion.alreadyBuilt[14])
		{
			SecurityScore += 2;
		}
		if (riotSecurity > 0f)
		{
			riotSecurity -= 0.05f;
		}
		else if (riotSecurity < 0f)
		{
			riotSecurity += 0.05f;
		}
		SecurityScore += (int)riotSecurity;
		int value = Mathf.RoundToInt(PrisonCapacity * 50 / PrisonUse - 50);
		value = Mathf.Clamp(value, -20, 5);
		SecurityScore += value;
		prisonCivilRights += value / 2 + 2;
		SecurityScore = Mathf.Clamp(SecurityScore, 0, 120);
		ParkScore = Mathf.Round((Park_Count * 10f + ParkCapacity) / (Population + 1f) * 10000f);
		if (ParkScore > 120f)
		{
			ParkScore = 120f;
		}
		float num17 = 0.5f + (float)(DataRegion.HQstage / 2);
		bool[] alreadyBuilt = DataRegion.alreadyBuilt;
		for (int i = 0; i < alreadyBuilt.Length; i++)
		{
			if (alreadyBuilt[i])
			{
				num17 += 0.5f;
			}
		}
		Stability += num17 * stabGrowth;
		Stability = Mathf.Clamp(Stability, 0f, 100f);
	}

	private void PopulationGrowth()
	{
		if (!growthStopped)
		{
			LC_Demand += LCnaturalGrowth + 0.2f * w.LCbirthNeighbors;
			MC_Demand += MCnaturalGrowth + 0.2f * w.MCbirthNeighbors;
			UC_Demand += UCnaturalGrowth + 0.2f * w.UCbirthNeighbors;
		}
		thisCity.LCbirth = LCnaturalGrowth;
		thisCity.MCbirth = MCnaturalGrowth;
		thisCity.UCbirth = UCnaturalGrowth;
		mcPol = (SocialLadderPolicy + 0.8f) / 4f;
		mcU = (11f - Unemployment) / 10f;
		mcTax = (budget.MC_Effect + budget.LC_Effect) * 3f;
		mcEdu = (EducationLevel - 40f) / 60f;
		mcInf = (1.8f - budget.Inflation) / 4f;
		ucPol = SocialLadderPolicy / 6f;
		ucU = (6f - Unemployment) / 20f;
		ucTax = (budget.MC_Effect + budget.UC_Effect) * 4f;
		ucCorru = (0f - Corruption) / 3f;
		ucFI = (FreedomIndex - 50f) / 50f;
		MCaccess = mcPol + mcU + mcTax + mcEdu + mcInf;
		UCaccess = ucPol + ucU + ucTax + ucCorru + ucFI;
		stats.MC_FromLCtoMC.text = MCaccess.ToString("N1") + "%";
		SetStatTextColor(stats.MC_FromLCtoMC, MCaccess);
		stats.UC_FromMCtoUC.text = UCaccess.ToString("N1") + "%";
		SetStatTextColor(stats.UC_FromMCtoUC, UCaccess);
		float num = MCaccess + UCaccess;
		w.indexer.SocialLadder.text = num.ToString("+0.0;-0.0");
		if (num <= 0f)
		{
			w.indexer.SocialLadder.color = budget.red;
		}
		else if (num < 2.5f)
		{
			w.indexer.SocialLadder.color = w.indexer.yellow;
		}
		else
		{
			w.indexer.SocialLadder.color = budget.green;
		}
		MCaccess /= accessMultipl;
		UCaccess /= accessMultipl;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		if (MCaccess <= 0f)
		{
			num2 = Mathf.Abs(MCPop * MCaccess);
			num3 += MCPop * MCaccess;
		}
		else
		{
			num2 = LCPop * (0f - MCaccess);
			num3 += Mathf.Abs(LCPop * MCaccess);
		}
		if (UCaccess <= 0f)
		{
			num3 += Mathf.Abs(UCPop * UCaccess);
			num4 = UCPop * UCaccess;
		}
		else
		{
			num3 += MCPop * (0f - UCaccess);
			num4 = Mathf.Abs(MCPop * UCaccess);
		}
		stats.LC_SocialLadder.text = num2.ToString("+0.0;-0.0");
		SetStatTextColor(stats.LC_SocialLadder, num2);
		stats.MC_SocialLadder.text = num3.ToString("+0.0;-0.0");
		SetStatTextColor(stats.MC_SocialLadder, num3);
		stats.UC_SocialLadder.text = num4.ToString("+0.0;-0.0");
		SetStatTextColor(stats.UC_SocialLadder, num4);
		w.indexer.Growth_LC.text = (num2 + LCnaturalGrowth + 0.2f * w.LCbirthNeighbors + immi.Arrivals_LC / immiDivider).ToString("+0;-0");
		w.indexer.Growth_MC.text = (num3 + MCnaturalGrowth + 0.2f * w.MCbirthNeighbors + immi.Arrivals_MC / immiDivider).ToString("+0;-0");
		w.indexer.Growth_UC.text = (num4 + UCnaturalGrowth + 0.2f * w.UCbirthNeighbors + immi.Arrivals_UC / immiDivider).ToString("+0;-0");
		LC_Demand += num2;
		MC_Demand += num3;
		UC_Demand += num4;
		ladderHealth = num3 + num4;
		if (!growthStopped)
		{
			LC_Demand += immi.Arrivals_LC / immiDivider;
			MC_Demand += immi.Arrivals_MC / immiDivider;
			UC_Demand += immi.Arrivals_UC / immiDivider;
		}
		float num5 = num2 + immi.Arrivals_LC / immiDivider + LCnaturalGrowth + 0.2f * w.LCbirthNeighbors;
		float num6 = num3 + immi.Arrivals_MC / immiDivider + MCnaturalGrowth + 0.2f * w.MCbirthNeighbors;
		float num7 = num4 + immi.Arrivals_UC / immiDivider + UCnaturalGrowth + 0.2f * w.UCbirthNeighbors;
		PopGrowthNominal = num5 + num6 + num7;
		stats.LC_Total.text = num5.ToString("N1");
		SetStatTextColor(stats.LC_Total, num5);
		stats.MC_Total.text = num6.ToString("N1");
		SetStatTextColor(stats.MC_Total, num6);
		stats.UC_Total.text = num7.ToString("N1");
		SetStatTextColor(stats.UC_Total, num7);
		float num8 = (LC_Demand - 1200f) * 0.075f;
		if (num8 < -20f)
		{
			num8 = -20f;
		}
		float num9 = FreedomIndex - 50f;
		float num10 = 50f - CivilRights;
		float num11 = (budget.LC_Tax - 15f) * 4f;
		float num12 = Mathf.Clamp(num8 + num9 + num10 + num11, -2f, 50f);
		Slum_Demand += num12;
		stats.S_Housing.text = num8.ToString("+0;-0");
		SetStatTextColor(stats.S_Housing, 0f - num8);
		stats.S_FI.text = num9.ToString("+0;-0");
		SetStatTextColor(stats.S_FI, 0f - num9);
		stats.S_CR.text = num10.ToString("+0;-0");
		SetStatTextColor(stats.S_CR, 0f - num10);
		stats.S_Tax.text = num11.ToString("+0;-0");
		SetStatTextColor(stats.S_Tax, 0f - num11);
		stats.SlumDemand.text = Slum_Demand.ToString("N0") + " (" + (num8 + num9 + num10 + num11).ToString("+0;-0") + ")";
		w.indexer.Growth_S.text = (num8 + num9 + num10 + num11).ToString("+0;-0");
		float num13 = LCnaturalGrowth + MCnaturalGrowth + UCnaturalGrowth;
		PolicyLeaning = Mathf.Clamp(PolicyLeaning, -99f, 99f);
		if (num13 > 0f)
		{
			Conservatives += num13 * (PolicyLeaning + 100f) / 200f;
			Liberals += num13 * Mathf.Abs(PolicyLeaning - 100f) / 200f;
		}
		if (immi.reps > 1f)
		{
			Conservatives += immi.reps / 200f;
		}
		if (immi.libs > 1f)
		{
			Liberals += immi.libs / 200f;
		}
		if (Conservatives > 1000f)
		{
			Conservatives *= 0.99f;
		}
		if (Liberals > 1000f)
		{
			Liberals *= 0.99f;
		}
		GlobalLeaning = Mathf.Round(Conservatives * 100f / (Conservatives + Liberals));
		GlobalLeaning = Mathf.Clamp((GlobalLeaning * 6f + ReligionScore) / 7f, 0f, 100f);
		Immigrants += immi.A_Arriv + immi.LA_Arriv + immi.SEA_Arriv;
		Locals += LCnaturalGrowth + MCnaturalGrowth + UCnaturalGrowth;
		Undocumented = 1f;
		Immigrants *= 0.98f;
		Locals *= 0.99f;
		float num14 = Immigrants + Locals + Undocumented;
		stats.Immigres.value = Immigrants * 100f / num14;
		stats.Undocumented.value = Undocumented * 100f / num14;
		stats.Locals.value = Locals * 100f / num14;
		stats.Conservatives.value = GlobalLeaning;
		stats.Liberals.value = 100f - GlobalLeaning;
		stats.Religious.value = ReligionScore;
		stats.NotReligious.value = 100f - ReligionScore;
		if (LC_Demand > 3500f)
		{
			LC_Demand = 3500f;
		}
		else if (LC_Demand < -1500f)
		{
			LC_Demand = -1500f;
		}
		if (MC_Demand > 3000f)
		{
			MC_Demand = 3000f;
		}
		else if (MC_Demand < -800f)
		{
			MC_Demand = -800f;
		}
		if (UC_Demand > 2500f)
		{
			UC_Demand = 2500f;
		}
		else if (UC_Demand < -600f)
		{
			UC_Demand = -600f;
		}
		if (R_Demand > 2500f)
		{
			R_Demand = 2500f;
		}
		else if (R_Demand < -800f)
		{
			R_Demand = -800f;
		}
		if (O_Demand > 4500f)
		{
			O_Demand = 4500f;
		}
		else if (O_Demand < -800f)
		{
			O_Demand = -800f;
		}
		if (M_Demand > 1500f)
		{
			M_Demand = 1500f;
		}
		else if (M_Demand < -400f)
		{
			M_Demand = -400f;
		}
		if (HT_Demand > 1500f)
		{
			HT_Demand = 1500f;
		}
		else if (HT_Demand < -400f)
		{
			HT_Demand = -400f;
		}
		if (Slum_Demand < -500f)
		{
			Slum_Demand = -500f;
		}
		else if (Slum_Demand > 2000f)
		{
			Slum_Demand = 2000f;
		}
	}

	public void StatsPopulationUpdate()
	{
		stats.LC_BirthRate.text = LCnaturalGrowth.ToString("+0.0;-0.0");
		SetStatTextColor(stats.LC_BirthRate, LCnaturalGrowth);
		stats.LC_Immi.text = (immi.Arrivals_LC / immiDivider).ToString("+0.0;-0.0");
		SetStatTextColor(stats.LC_Immi, immi.Arrivals_LC / immiDivider);
		stats.LC_Neib.text = (0.2f * w.LCbirthNeighbors).ToString("+0.0;-0.0");
		SetStatTextColor(stats.LC_Neib, w.LCbirthNeighbors);
		stats.MC_Policies.text = mcPol.ToString("+0.0;-0.0") + "%";
		SetStatTextColor(stats.MC_Policies, mcPol);
		stats.MC_Unempl.text = mcU.ToString("+0.0;-0.0") + "%";
		SetStatTextColor(stats.MC_Unempl, mcU);
		stats.MC_Edu.text = mcEdu.ToString("+0.0;-0.0") + "%";
		SetStatTextColor(stats.MC_Edu, mcEdu);
		stats.MC_Inflation.text = mcInf.ToString("+0.0;-0.0") + "%";
		SetStatTextColor(stats.MC_Inflation, mcInf);
		stats.MC_Taxes.text = mcTax.ToString("+0.0;-0.0") + "%";
		SetStatTextColor(stats.MC_Taxes, mcTax);
		stats.MC_BirthRate.text = MCnaturalGrowth.ToString("+0.0;-0.0");
		SetStatTextColor(stats.MC_BirthRate, MCnaturalGrowth);
		stats.MC_Immi.text = (immi.Arrivals_MC / immiDivider).ToString("+0.0;-0.0");
		SetStatTextColor(stats.MC_Immi, immi.Arrivals_MC / immiDivider);
		stats.MC_Neib.text = (0.2f * w.MCbirthNeighbors).ToString("+0.0;-0.0");
		SetStatTextColor(stats.MC_Neib, w.MCbirthNeighbors);
		stats.UC_Policies.text = ucPol.ToString("+0.0;-0.0") + "%";
		SetStatTextColor(stats.UC_Policies, ucPol);
		stats.UC_Unempl.text = ucU.ToString("+0.0;-0.0") + "%";
		SetStatTextColor(stats.UC_Unempl, ucU);
		stats.UC_Corruption.text = ucCorru.ToString("+0.0;-0.0") + "%";
		SetStatTextColor(stats.UC_Corruption, ucCorru);
		stats.UC_Freedom.text = ucFI.ToString("+0.0;-0.0") + "%";
		SetStatTextColor(stats.UC_Freedom, ucFI);
		stats.UC_Taxes.text = ucTax.ToString("+0.0;-0.0") + "%";
		SetStatTextColor(stats.UC_Taxes, ucTax);
		stats.UC_BirthRate.text = UCnaturalGrowth.ToString("+0.0;-0.0");
		SetStatTextColor(stats.UC_BirthRate, UCnaturalGrowth);
		stats.UC_Immi.text = (immi.Arrivals_UC / immiDivider).ToString("+0.0;-0.0");
		SetStatTextColor(stats.UC_Immi, immi.Arrivals_UC / immiDivider);
		stats.UC_Neib.text = (0.2f * w.UCbirthNeighbors).ToString("+0.0;-0.0");
		SetStatTextColor(stats.UC_Neib, w.UCbirthNeighbors);
		float num = Corruption * 10f;
		if (num < 0f)
		{
			Corruption = 0f;
		}
		stats.M_Corruption.text = num.ToString("N0") + "%";
		SetApprovalColor(stats.M_Corruption, 100f - num * 10f);
		w.indexer.Corruption.text = num.ToString("N0") + "%";
		stats.M_BirthRate.text = BirthRate.ToString("N1");
		if (BirthRate < 2f)
		{
			stats.M_BirthRate.color = stats.redCity;
		}
		else
		{
			stats.M_BirthRate.color = stats.greenCity;
		}
		stats.M_Inequalities.text = Inequality.ToString("N0");
		SetApprovalColor(stats.M_Inequalities, 100 - Inequality);
		stats.M_Incarceration.text = ((205f - FreedomIndex - MCPartPercent) / 1750f).ToString("N3") + "%";
		stats.Q_Environment.text = Environment.ToString("N0") + " / 100";
		stats.Q_Health.text = HealthScore.ToString("N0") + " / 100";
		stats.Q_Parks.text = ParkScore.ToString("N0") + " / 100";
		stats.Q_Security.text = SecurityScore.ToString("N0") + " / 100";
		stats.Q_Total.text = QualityOfLife.ToString("N0") + " / 100";
		stats.byCar.value = byCar;
		stats.byTrain.value = byTrain;
		stats.byWalk.value = byWalk;
		stats.byBus.value = byBus;
		stats.toNeighbor.value = toNeighbor;
		stats.byAirTaxi.value = byAirTaxi;
		stats.C_Congestion.text = Congestion.ToString("N1") + "%";
		SetApprovalColor(stats.C_Congestion, 100f - 2f * Mathf.Clamp(Congestion, 0f, 100f));
	}

	private void StatsEcoUpdate(float activePercent)
	{
		stats.T_Retail.text = (10f * budget.R_Effect).ToString("+0;-0") + "%";
		stats.P_Retail.text = (100f * (retailPOL - 1f)).ToString("+0;-0") + "%";
		stats.Tot_Retail.text = (100f * (Retail - 1f)).ToString("N0") + "%";
		stats.FI_Office.text = (100f * ((FreedomIndex - 50f) / 200f)).ToString("+0;-0") + "%";
		stats.E_Office.text = (100f * ((EducationLevel - 50f) / 100f)).ToString("+0;-0") + "%";
		stats.T_Office.text = (100f * (budget.O_Effect / 10f)).ToString("+0;-0") + "%";
		stats.P_Office.text = (100f * (officePOL - 1f)).ToString("+0;-0") + "%";
		stats.Tot_Office.text = (100f * (Office - 1f)).ToString("N0") + "%";
		stats.I_Man.text = (100f * BM_exRate).ToString("+0;-0") + "%";
		stats.E_Man.text = (100f * (0f - indusEducation)).ToString("+0;-0") + "%";
		stats.T_Man.text = (100f * (budget.M_Effect / 10f)).ToString("+0;-0") + "%";
		stats.P_Man.text = (100f * (manPOL - 1f)).ToString("+0;-0") + "%";
		stats.Tot_Man.text = (100f * (Manufacturing - 1f)).ToString("N0") + "%";
		stats.I_HT.text = (100f * AM_inflation).ToString("+0;-0") + "%";
		stats.E_HT.text = (100f * indusEducation).ToString("+0;-0") + "%";
		stats.T_HT.text = (100f * (budget.HT_Effect / 10f)).ToString("+0;-0") + "%";
		stats.P_HT.text = (100f * (HTPOL - 1f)).ToString("+0;-0") + "%";
		stats.Tot_HT.text = (100f * (HighTech - 1f)).ToString("N0") + "%";
		stats.Arrow_LC.text = (LC_X * Retail).ToString("N3");
		stats.Arrow_MC.text = (MC_X * Retail).ToString("N3");
		stats.Arrow_UC.text = (UC_X * Retail).ToString("N3");
		stats.Arrow_RtoM.text = (R_X * Manufacturing).ToString("N2");
		stats.Arrow_RtoHT.text = (R_X * HighTech).ToString("N2");
		stats.Arrow_MtoO.text = (M_X * Office).ToString("N2");
		stats.Arrow_HTtoO.text = (HT_X * Office).ToString("N2");
		stats.E_GDPCapita.text = "$" + ((float)GDP / (Population + 1f) / 100f).ToString("F1") + "k";
		stats.E_GDPGrowth.text = GDPgrowth.ToString("N0") + "%";
		stats.J_WorkingPercent.text = (activePercent * 100f).ToString("F1") + "%";
		stats.Rpercent.value = retailPart;
		stats.Opercent.value = officePart;
		stats.Mpercent.value = manufacturePart;
		stats.HTpercent.value = hightechPart;
		stats.GOVpercent.value = servicePart;
		stats.Ppercent.value = primaryPart;
		stats.NeighbPercent.value = neighborPart;
		stats.Upercent.value = Unemployment;
	}

	private void UpdateApproval()
	{
		if (ApprovalLeft < 50f)
		{
			ApprovalLeft += 0.5f;
		}
		else if (ApprovalLeft > 50f)
		{
			ApprovalLeft -= 0.5f;
		}
		if (ApprovalRight < 50f)
		{
			ApprovalRight += 0.5f;
		}
		else if (ApprovalRight > 50f)
		{
			ApprovalRight -= 0.5f;
		}
		fix = -0.0004f * Population + 18f;
		if (fix < -18f)
		{
			fix = -18f;
		}
		float num = Mathf.Round(100f - Unemployment * 5f);
		if (num < 0f)
		{
			num = 0f;
		}
		float num2 = Mathf.Round(50f + budget.R_Approval + budget.LC_Approval + budget.MC_Approval + budget.UC_Approval + budget.Pro_Approval + fix);
		if (num2 < 0f)
		{
			num2 = 0f;
		}
		float num3 = Mathf.Clamp(Mathf.Round((float)(100 - Inequality) + fix), 0f, 100f);
		float num4 = Mathf.Round(CivilRights - 15f + fix);
		if (num4 < 0f)
		{
			num4 = 0f;
		}
		float num5 = Mathf.Round(Environment);
		if (num5 < 0f)
		{
			num5 = 0f;
		}
		float num6 = Mathf.Round((float)HealthScore + fix);
		if (num6 < 0f)
		{
			num6 = 0f;
		}
		float num7 = Mathf.Round(EducationQuality - 10f + fix);
		if (num7 < 0f)
		{
			num7 = 0f;
		}
		float num8 = Mathf.Round((float)SecurityScore + fix);
		if (num8 < 0f)
		{
			num8 = 0f;
		}
		float num9 = Mathf.Round(100f - 33f * (immi.A_Border + immi.LA_Border + immi.SEA_Border));
		if (num9 < 0f)
		{
			num9 = 0f;
		}
		float num10 = Mathf.Round(Stability - 10f);
		if (num10 < 0f)
		{
			num10 = 0f;
		}
		float num11 = Mathf.Round(30f + (3f - budget.Inflation) * 40f);
		if (num11 < 0f)
		{
			num11 = 0f;
		}
		else if (num11 > 100f)
		{
			num11 = 100f;
		}
		float num12 = Mathf.Round(FreedomIndex - 20f + fix);
		if (num12 < 0f)
		{
			num12 = 0f;
		}
		float num13 = w.approvalBonus + (num + num2 + num3 + num4 + num5 + num6 + num7 + ApprovalLeft * 3f) / 10f;
		float num14 = w.approvalBonus + (num + num2 + num8 + num9 + num10 + num11 + num12 + ApprovalRight * 3f) / 10f;
		if (DataRegion.alreadyBuilt[14])
		{
			num13 += 10f;
			num14 += 10f;
		}
		ApprovalRate = Mathf.RoundToInt((num13 * (100f - GlobalLeaning) + num14 * GlobalLeaning) / 100f);
		if (ApprovalRate < 0)
		{
			ApprovalRate = 0;
		}
		else if (ApprovalRate > 100)
		{
			ApprovalRate = 100;
		}
		stats.L_Policies.text = ApprovalLeft.ToString("N0") + "%";
		SetApprovalColor(stats.L_Policies, ApprovalLeft);
		stats.L_Taxes.text = num2 + "%";
		SetApprovalColor(stats.L_Taxes, num2);
		stats.L_Unemployment.text = num + "%";
		SetApprovalColor(stats.L_Unemployment, num);
		stats.L_Inequality.text = num3 + "%";
		SetApprovalColor(stats.L_Inequality, num3);
		stats.L_CR.text = num4 + "%";
		SetApprovalColor(stats.L_CR, num4);
		stats.L_Enviro.text = num5 + "%";
		SetApprovalColor(stats.L_Enviro, num5);
		stats.L_Health.text = num6 + "%";
		SetApprovalColor(stats.L_Health, num6);
		stats.L_Edu.text = num7 + "%";
		SetApprovalColor(stats.L_Edu, num7);
		stats.R_Policies.text = ApprovalRight.ToString("N0") + "%";
		SetApprovalColor(stats.R_Policies, ApprovalRight);
		stats.R_Taxes.text = num2 + "%";
		SetApprovalColor(stats.R_Taxes, num2);
		stats.R_Unemployment.text = num + "%";
		SetApprovalColor(stats.R_Unemployment, num);
		stats.R_Secu.text = num8 + "%";
		SetApprovalColor(stats.R_Secu, num8);
		stats.R_Border.text = num9 + "%";
		SetApprovalColor(stats.R_Border, num9);
		stats.R_Stability.text = num10 + "%";
		SetApprovalColor(stats.R_Stability, num10);
		stats.R_Inflation.text = num11 + "%";
		SetApprovalColor(stats.R_Inflation, num11);
		stats.R_FI.text = num12 + "%";
		SetApprovalColor(stats.R_FI, num12);
		stats.L_Affiliations.text = Mathf.RoundToInt(100f - GlobalLeaning) + "%";
		stats.R_Affiliations.text = Mathf.RoundToInt(GlobalLeaning) + "%";
		stats.L_Approval.text = Mathf.RoundToInt(num13) + "%";
		stats.R_Approval.text = Mathf.RoundToInt(num14) + "%";
		stats.TotalApproval.text = ApprovalRate + "%";
		if (w.Roads.Count <= 0 || Rioting.Rioters >= Rioting.MaxRioters || !(Population > 10000f))
		{
			return;
		}
		float num15 = num13 - (100f - GlobalLeaning) * 0.8f;
		float num16 = num14 - GlobalLeaning * 0.8f;
		if (num15 > 0f)
		{
			num15 = 0f;
		}
		else if (num15 < -100f)
		{
			num15 = -100f;
		}
		if (num16 > 0f)
		{
			num16 = 0f;
		}
		else if (num16 < -100f)
		{
			num16 = -100f;
		}
		float num17 = Random.value * 100f;
		float num18 = 0.002f * Population;
		if (num18 > 500f)
		{
			num18 = 500f + asyncR1;
		}
		if (num15 + num17 < 0f)
		{
			if (num3 + asyncR1 < 30f)
			{
				rioting.NewRiot((int)num18, "AYleft", 1, num3 + asyncR1);
			}
			else if (num4 + asyncR2 < 20f)
			{
				rioting.NewRiot((int)num18, "AYleft", 2, num4 + asyncR2);
			}
			else if (num5 + asyncR1 < 35f)
			{
				rioting.NewRiot((int)num18, "AYleft", 3, num5 + asyncR1);
			}
			else if (num6 + asyncR2 < 45f)
			{
				rioting.NewRiot((int)num18, "AYleft", 4, num6 + asyncR2);
			}
			else if (num7 + asyncR1 < 35f)
			{
				rioting.NewRiot((int)num18, "AYleft", 5, num7 + asyncR1);
			}
			else if (ApprovalLeft + asyncR2 < 10f)
			{
				rioting.NewRiot((int)num18, "AYleft", 11, ApprovalLeft + asyncR2);
			}
			else if (num2 + asyncR1 < 35f)
			{
				rioting.NewRiot((int)num18, "AYleft", 12, num2 + asyncR1);
			}
		}
		else if (num16 + num17 < 0f)
		{
			if (num8 + asyncR1 < 45f)
			{
				rioting.NewRiot((int)num18, "AYright", 6, num8 + asyncR1);
			}
			else if (num9 + asyncR2 < 50f)
			{
				rioting.NewRiot((int)num18, "AYright", 7, num9 + asyncR2);
			}
			else if (num10 + asyncR1 < 50f)
			{
				rioting.NewRiot((int)num18, "AYright", 8, num10 + asyncR1);
			}
			else if (num11 + asyncR2 < 35f)
			{
				rioting.NewRiot((int)num18, "AYright", 9, num11 + asyncR2);
			}
			else if (num12 + asyncR1 < 20f)
			{
				rioting.NewRiot((int)num18, "AYright", 10, num12 + asyncR1);
			}
			else if (ApprovalRight + asyncR1 < 10f)
			{
				rioting.NewRiot((int)num18, "AYright", 11, ApprovalRight + asyncR1);
			}
			else if (num2 + asyncR2 < 45f)
			{
				rioting.NewRiot((int)num18, "AYright", 12, num2 + asyncR2);
			}
			else if (num + asyncR2 < 20f)
			{
				rioting.NewRiot((int)num18, "AYright", 13, num + asyncR2);
			}
		}
	}

	private void SetStatTextColor(TextMeshProUGUI textUI, float data)
	{
		if (data < 0f)
		{
			textUI.color = stats.redCity;
		}
		else
		{
			textUI.color = stats.greenCity;
		}
	}

	private void SetApprovalColor(TextMeshProUGUI textUI, float data)
	{
		textUI.color = new Color32((byte)(181f - data * 0.32f), (byte)(118f + data * 0.73f), 118, byte.MaxValue);
	}

	private IEnumerator BuildNewCompany()
	{
		while (true)
		{
			BuildManufactureFirstStage();
			yield return new WaitForSeconds(waitTime);
			BuildPrimaryFirstStage();
			yield return new WaitForSeconds(waitTime);
			BuildHighTechFirstStage();
			yield return new WaitForSeconds(waitTime);
			BuildRetailFirstStage();
			yield return new WaitForSeconds(waitTime);
			BuildOfficeFirstStage();
			yield return new WaitForSeconds(0.5f);
		}
	}

	private IEnumerator NewResidential()
	{
		while (true)
		{
			if (Population > 200000f)
			{
				waitTime = 0.1f;
			}
			else if (Population > 100000f)
			{
				waitTime = 0.15f;
			}
			else if (Population > 50000f)
			{
				waitTime = 0.2f;
			}
			NewMixClass();
			yield return new WaitForSeconds(waitTime);
			NewLowClassResidential();
			yield return new WaitForSeconds(waitTime);
			if (LC_Demand > 300f)
			{
				NewLowClassResidential();
			}
			yield return new WaitForSeconds(waitTime);
			NewMiddleClassResidential();
			yield return new WaitForSeconds(waitTime);
			if (MC_Demand > 300f)
			{
				NewMiddleClassResidential();
			}
			yield return new WaitForSeconds(waitTime);
			NewUpperClassResidential();
			yield return new WaitForSeconds(waitTime);
			if (UC_Demand > 300f)
			{
				NewUpperClassResidential();
			}
			yield return new WaitForSeconds(waitTime);
			if (Slum_Demand > 10f)
			{
				NewSlum();
			}
			else if (Slum_Demand < -10f)
			{
				DespawnSlum();
			}
			yield return new WaitForSeconds(0.25f);
			if (NationPop > 92340f)
			{
				cityStage = 10;
			}
			else if (NationPop > 46170f)
			{
				cityStage = 6;
			}
			else if (NationPop > 15390f)
			{
				cityStage = 5;
			}
			else if (NationPop > 6144f)
			{
				cityStage = 4;
			}
			else if (NationPop > 1536f)
			{
				cityStage = 3;
			}
			else if (NationPop > 256f)
			{
				cityStage = 2;
			}
		}
	}

	private IEnumerator StageUpBuilding()
	{
		while (true)
		{
			float evoRate = 0.5f;
			if (Population > 250000f)
			{
				evoRate = 0.25f;
			}
			else if (Population > 175000f)
			{
				evoRate = 0.3f;
			}
			else if (Population > 125000f)
			{
				evoRate = 0.35f;
			}
			else if (Population > 75000f)
			{
				evoRate = 0.4f;
			}
			else if (Population > 50000f)
			{
				evoRate = 0.45f;
			}
			if ((float)PowerUse > (float)PowerCapacity * 1.1f || (float)WaterUse > (float)WaterCapacity * 1.1f)
			{
				growthStopped = true;
			}
			else
			{
				growthStopped = false;
			}
			MixClassStageUp();
			yield return new WaitForSeconds(evoRate);
			LowClassStageUp();
			if (Slum_Demand > 20f)
			{
				SlumStageUp();
			}
			yield return new WaitForSeconds(evoRate);
			MiddleClassStageUp();
			ArcoPop();
			yield return new WaitForSeconds(evoRate);
			UpperClassStageUp();
			yield return new WaitForSeconds(evoRate);
			RetailStageUp();
			yield return new WaitForSeconds(evoRate);
			ManufactureStageUp();
			yield return new WaitForSeconds(evoRate);
			HighTechStageUp();
			yield return new WaitForSeconds(evoRate);
			OfficeStageUp();
			yield return new WaitForSeconds(evoRate);
			ongoingDecay = false;
			messages.CheckForEvents();
			yield return new WaitForEndOfFrame();
			EvolveDemands();
		}
	}

	private void ArcoPop()
	{
		if (w.Arcologies.Count >= 1)
		{
			ServiceCreator serviceCreator = w.Arcologies[Random.Range(0, w.Arcologies.Count)];
			if (MC_Demand > 50f && serviceCreator.MinEmployees < serviceCreator.MaxEmployees)
			{
				serviceCreator.MinEmployees += Mathf.RoundToInt(MC_Demand / 2f);
				MC_Demand -= MC_Demand / 4f;
			}
			else if (MC_Demand < 0f && serviceCreator.MinEmployees > 0)
			{
				serviceCreator.MinEmployees -= 100;
				MC_Demand += 50f;
			}
			if (serviceCreator.MinEmployees < 0)
			{
				serviceCreator.MinEmployees = 0;
			}
			else if (serviceCreator.MinEmployees > serviceCreator.MaxEmployees)
			{
				serviceCreator.MinEmployees = serviceCreator.MaxEmployees;
			}
		}
	}

	private void NewSlum()
	{
		if (tries > 60)
		{
			tries = 0;
		}
		else
		{
			if (w.Roads.Count <= 0 || !(Slum_Demand > 10f))
			{
				return;
			}
			bool isFree = true;
			BlockData blockData = null;
			RoadCreator roadCreator = w.Roads[Random.Range(0, w.Roads.Count)];
			int num = (int)roadCreator.aPos.x;
			int num2 = (int)roadCreator.aPos.z;
			if (roadCreator.cc == 1)
			{
				for (int num3 = num2; num3 > num2 - 6; num3--)
				{
					BlockData ifNotNull = w.GetIfNotNull(num, num3);
					if (ifNotNull == null)
					{
						return;
					}
					if (ifNotNull.t == BlockData.Type.GRASSTREE || ifNotNull.t == BlockData.Type.GRASS)
					{
						blockData = ifNotNull;
						break;
					}
				}
			}
			else if (roadCreator.cc == 2)
			{
				for (int i = num2; i < num2 + 6; i++)
				{
					BlockData ifNotNull = w.GetIfNotNull(num, i);
					if (ifNotNull == null)
					{
						return;
					}
					if (ifNotNull.t == BlockData.Type.GRASSTREE || ifNotNull.t == BlockData.Type.GRASS)
					{
						blockData = ifNotNull;
						break;
					}
				}
			}
			else if (roadCreator.cc == 3)
			{
				for (int num4 = num; num4 > num - 6; num4--)
				{
					BlockData ifNotNull = w.GetIfNotNull(num4, num2);
					if (ifNotNull == null)
					{
						return;
					}
					if (ifNotNull.t == BlockData.Type.GRASSTREE || ifNotNull.t == BlockData.Type.GRASS)
					{
						blockData = ifNotNull;
						break;
					}
				}
			}
			else
			{
				if (roadCreator.cc != 4)
				{
					tries++;
					NewSlum();
					return;
				}
				for (int j = num; j < num + 6; j++)
				{
					BlockData ifNotNull = w.GetIfNotNull(j, num2);
					if (ifNotNull == null)
					{
						return;
					}
					if (ifNotNull.t == BlockData.Type.GRASSTREE || ifNotNull.t == BlockData.Type.GRASS)
					{
						blockData = ifNotNull;
						break;
					}
				}
			}
			if (blockData != null && blockData.v[0] > (AverageValue - 250) / 3)
			{
				TryBuildSlum(blockData, roadCreator, isFree);
				return;
			}
			tries++;
			NewSlum();
		}
	}

	private void BuildManufactureFirstStage()
	{
		if (w.VacantIndusLots.Count <= 0 || !(M_Demand > 20f))
		{
			return;
		}
		LotCreator lotCreator = w.VacantIndusLots[Random.Range(0, w.VacantIndusLots.Count)];
		if (lotCreator.lClass == LotCreator.LotClass.PRIMARY)
		{
			return;
		}
		for (int i = 0; i < 6; i++)
		{
			LotCreator lotCreator2 = w.VacantIndusLots[Random.Range(0, w.VacantIndusLots.Count)];
			if (lotCreator2 != null && !lotCreator.underConstruction && lotCreator2.lClass != LotCreator.LotClass.PRIMARY && lotCreator2.LandValue < lotCreator.LandValue)
			{
				lotCreator = lotCreator2;
			}
		}
		if (lotCreator.LandValue < 1250 && lotCreator.negatives < 1f && lotCreator.MaxStage > 0 && !lotCreator.underConstruction)
		{
			lotCreator.Stage = 1;
			lotCreator.Occupants = 20 + Random.Range(0, 4);
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.MANUFACTURE;
			lotCreator.desiredValue = 4;
			lotCreator.nextValue = 8;
			lotCreator.nextDemand = 60;
			w.VacantIndusLots.Remove(lotCreator);
			w.IndustrialBuildings.Add(lotCreator);
			lotCreator.Aiguilleur(1);
			M_Demand -= lotCreator.Occupants;
		}
		else if (lotCreator.isDecayed)
		{
			ongoingDecay = true;
		}
	}

	private void BuildPrimaryFirstStage()
	{
		if (w.VacantIndusLots.Count > 0 && P_Demand > 20f)
		{
			LotCreator lotCreator = w.VacantIndusLots[Random.Range(0, w.VacantIndusLots.Count)];
			if (lotCreator.lClass == LotCreator.LotClass.PRIMARY && lotCreator.Stage == 0 && !lotCreator.underConstruction)
			{
				lotCreator.Stage = 1;
				lotCreator.Occupants = Mathf.RoundToInt((float)lotCreator.nextDemand / 5f);
				lotCreator.underConstruction = true;
				lotCreator.lClass = LotCreator.LotClass.PRIMARY;
				lotCreator.desiredValue = 0;
				lotCreator.nextValue = 0;
				lotCreator.nextDemand = 20;
				w.VacantIndusLots.Remove(lotCreator);
				w.IndustrialBuildings.Add(lotCreator);
				lotCreator.Aiguilleur(1);
				P_Demand -= lotCreator.Occupants;
			}
		}
	}

	private void BuildHighTechFirstStage()
	{
		if (w.VacantIndusLots.Count <= 0 || !(HT_Demand > 15f))
		{
			return;
		}
		LotCreator lotCreator = w.VacantIndusLots[Random.Range(0, w.VacantIndusLots.Count)];
		if (lotCreator.lClass == LotCreator.LotClass.PRIMARY)
		{
			return;
		}
		for (int i = 0; i < 6; i++)
		{
			LotCreator lotCreator2 = w.VacantIndusLots[Random.Range(0, w.VacantIndusLots.Count)];
			if (lotCreator2 != null && lotCreator2.lClass != LotCreator.LotClass.PRIMARY && !lotCreator.underConstruction && lotCreator2.LandValue > lotCreator.LandValue)
			{
				lotCreator = lotCreator2;
			}
		}
		if (lotCreator.LandValue >= 128 && lotCreator.negatives < 1f && lotCreator.MaxStage > 0 && !lotCreator.underConstruction)
		{
			lotCreator.Stage = 1;
			lotCreator.Occupants = 15 + Random.Range(0, 4);
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.HIGHTECH;
			lotCreator.desiredValue = 128;
			lotCreator.nextValue = 256;
			lotCreator.nextDemand = 45;
			w.VacantIndusLots.Remove(lotCreator);
			w.IndustrialBuildings.Add(lotCreator);
			lotCreator.Aiguilleur(1);
			HT_Demand -= lotCreator.Occupants;
		}
		else if (lotCreator.isDecayed)
		{
			ongoingDecay = true;
		}
	}

	private void BuildRetailFirstStage()
	{
		if (w.VacantCommLots.Count > 0 && R_Demand > 18f)
		{
			LotCreator lotCreator = w.VacantCommLots[Random.Range(0, w.VacantCommLots.Count)];
			if (lotCreator.LandValue >= 8 && lotCreator.negatives < 1f && lotCreator.MaxStage > 0 && !lotCreator.underConstruction)
			{
				lotCreator.Stage = 1;
				lotCreator.Occupants = 18 + Random.Range(0, 4);
				lotCreator.underConstruction = true;
				lotCreator.lClass = LotCreator.LotClass.RETAIL;
				lotCreator.desiredValue = 8;
				lotCreator.nextValue = 64;
				lotCreator.nextDemand = 50;
				w.VacantCommLots.Remove(lotCreator);
				w.CommercialBuildings.Add(lotCreator);
				lotCreator.Aiguilleur(1);
				R_Demand -= lotCreator.Occupants;
			}
			else if (lotCreator.isDecayed)
			{
				ongoingDecay = true;
			}
		}
	}

	private void BuildOfficeFirstStage()
	{
		if (w.VacantCommLots.Count > 0 && O_Demand > 80f)
		{
			LotCreator lotCreator = w.VacantCommLots[Random.Range(0, w.VacantCommLots.Count)];
			if (lotCreator.LandValue >= 32 && lotCreator.negatives < 1f && lotCreator.MaxStage > 0)
			{
				lotCreator.Stage = 1;
				lotCreator.Occupants = 80 + Random.Range(0, 8);
				lotCreator.underConstruction = true;
				lotCreator.lClass = LotCreator.LotClass.OFFICE;
				lotCreator.desiredValue = 32;
				lotCreator.nextValue = 256;
				lotCreator.nextDemand = 160;
				w.VacantCommLots.Remove(lotCreator);
				w.CommercialBuildings.Add(lotCreator);
				lotCreator.Aiguilleur(1);
				O_Demand -= lotCreator.Occupants;
			}
			else if (lotCreator.isDecayed)
			{
				ongoingDecay = true;
			}
		}
	}

	private void NewLowClassResidential()
	{
		if (w.VacantResLots.Count <= 0 || !(LC_Demand > 5f))
		{
			return;
		}
		LotCreator lotCreator = w.VacantResLots[Random.Range(0, w.VacantResLots.Count)];
		for (int i = 0; i < 6; i++)
		{
			LotCreator lotCreator2 = w.VacantResLots[Random.Range(0, w.VacantResLots.Count)];
			if (lotCreator2 != null && lotCreator2.LandValue < lotCreator.LandValue && !lotCreator.underConstruction)
			{
				lotCreator = lotCreator2;
			}
		}
		if (lotCreator.MaxStage > 0 && lotCreator.negatives < 5f && !lotCreator.underConstruction)
		{
			lotCreator.Stage = 1;
			lotCreator.Occupants = 5 + Random.Range(0, 4);
			lotCreator.underConstruction = true;
			lotCreator.desiredValue = 4;
			lotCreator.nextValue = 16;
			lotCreator.nextDemand = 20;
			lotCreator.lClass = LotCreator.LotClass.LOW;
			w.VacantResLots.Remove(lotCreator);
			w.ResidentialBuildings.Add(lotCreator);
			lotCreator.Aiguilleur(1);
			LC_Demand -= lotCreator.Occupants;
		}
		else if (lotCreator.isDecayed)
		{
			ongoingDecay = true;
		}
	}

	private void NewMiddleClassResidential()
	{
		if (w.VacantResLots.Count <= 0 || !(MC_Demand > 5f))
		{
			return;
		}
		LotCreator lotCreator = w.VacantResLots[Random.Range(0, w.VacantResLots.Count)];
		for (int i = 0; i < 6; i++)
		{
			LotCreator lotCreator2 = w.VacantResLots[Random.Range(0, w.VacantResLots.Count)];
			if (lotCreator2 != null && lotCreator2.LandValue > lotCreator.LandValue && !lotCreator.underConstruction && lotCreator.negatives < 1f)
			{
				lotCreator = lotCreator2;
			}
		}
		if (lotCreator.LandValue >= 64 && lotCreator.negatives < 1f && lotCreator.MaxStage > 0 && !lotCreator.underConstruction)
		{
			lotCreator.Stage = 1;
			lotCreator.Occupants = 5 + Random.Range(0, 3);
			lotCreator.underConstruction = true;
			lotCreator.desiredValue = 64;
			lotCreator.nextValue = 96;
			lotCreator.nextDemand = 14;
			lotCreator.lClass = LotCreator.LotClass.MIDDLE;
			w.VacantResLots.Remove(lotCreator);
			w.ResidentialBuildings.Add(lotCreator);
			lotCreator.Aiguilleur(1);
			MC_Demand -= lotCreator.Occupants;
		}
		else if (lotCreator.isDecayed)
		{
			ongoingDecay = true;
		}
	}

	private void NewUpperClassResidential()
	{
		if (w.VacantResLots.Count <= 0 || !(UC_Demand > 5f))
		{
			return;
		}
		LotCreator lotCreator = w.VacantResLots[Random.Range(0, w.VacantResLots.Count)];
		for (int i = 0; i < 6; i++)
		{
			LotCreator lotCreator2 = w.VacantResLots[Random.Range(0, w.VacantResLots.Count)];
			if (lotCreator2 != null && lotCreator2.LandValue > lotCreator.LandValue && !lotCreator.underConstruction && lotCreator.negatives < 1f)
			{
				lotCreator = lotCreator2;
			}
		}
		if (lotCreator.Stage == 0 && lotCreator.LandValue >= 128 && lotCreator.negatives < 1f && lotCreator.MaxStage > 0 && !lotCreator.underConstruction)
		{
			lotCreator.Stage = 1;
			lotCreator.Occupants = 5 + Random.Range(0, 2);
			lotCreator.underConstruction = true;
			lotCreator.desiredValue = 128;
			lotCreator.nextValue = 192;
			lotCreator.nextDemand = 10;
			lotCreator.lClass = LotCreator.LotClass.UPPER;
			w.VacantResLots.Remove(lotCreator);
			w.ResidentialBuildings.Add(lotCreator);
			lotCreator.Aiguilleur(1);
			UC_Demand -= lotCreator.Occupants;
		}
		else if (lotCreator.isDecayed)
		{
			ongoingDecay = true;
		}
	}

	private void NewMixClass()
	{
		if (w.VacantMixedLots.Count <= 0)
		{
			return;
		}
		LotCreator lotCreator = w.VacantMixedLots[Random.Range(0, w.VacantMixedLots.Count)];
		for (int i = 0; i < 6; i++)
		{
			LotCreator lotCreator2 = w.VacantMixedLots[Random.Range(0, w.VacantMixedLots.Count)];
			if (lotCreator2 != null && lotCreator2.LandValue > lotCreator.LandValue && !lotCreator.underConstruction && lotCreator.negatives < 1f)
			{
				lotCreator = lotCreator2;
			}
		}
		bool flag = false;
		if (UC_Demand + O_Demand > MC_Demand + R_Demand && UC_Demand > 55f)
		{
			flag = true;
		}
		if (!flag && MC_Demand > 99f && R_Demand > 33f)
		{
			if (lotCreator.Stage == 0 && lotCreator.LandValue >= 768 && lotCreator.negatives < 1f && lotCreator.MaxStage > 0 && !lotCreator.underConstruction)
			{
				lotCreator.Stage = 1;
				lotCreator.Occupants = 90 + Random.Range(0, 18);
				lotCreator.Visitors = (short)(30 + Random.Range(0, 6));
				lotCreator.underConstruction = true;
				lotCreator.desiredValue = 768;
				lotCreator.nextValue = 1536;
				lotCreator.nextDemand = 180;
				lotCreator.lClass = LotCreator.LotClass.MIXRETAIL;
				w.VacantMixedLots.Remove(lotCreator);
				w.MixedBuildings.Add(lotCreator);
				lotCreator.Aiguilleur(3);
				MC_Demand -= lotCreator.Occupants;
				R_Demand -= lotCreator.Visitors;
			}
			else if (lotCreator.isDecayed)
			{
				ongoingDecay = true;
			}
		}
		else if (UC_Demand > 60f && O_Demand > 27f)
		{
			if (lotCreator.Stage == 0 && lotCreator.LandValue >= 1536 && lotCreator.negatives < 1f && lotCreator.MaxStage > 0 && !lotCreator.underConstruction)
			{
				lotCreator.Stage = 1;
				lotCreator.Occupants = 55 + Random.Range(0, 11);
				lotCreator.Visitors = (short)(25 + Random.Range(0, 5));
				lotCreator.underConstruction = true;
				lotCreator.desiredValue = 1536;
				lotCreator.nextValue = 2560;
				lotCreator.nextDemand = 125;
				lotCreator.lClass = LotCreator.LotClass.MIXOFFICE;
				w.VacantMixedLots.Remove(lotCreator);
				w.MixedBuildings.Add(lotCreator);
				lotCreator.Aiguilleur(3);
				UC_Demand -= lotCreator.Occupants;
				O_Demand -= lotCreator.Visitors;
			}
			else if (lotCreator.isDecayed)
			{
				ongoingDecay = true;
			}
		}
	}

	private void LowClassStageUp()
	{
		if (w.ResidentialBuildings == null)
		{
			return;
		}
		List<LotCreator> list = w.ResidentialBuildings.Where((LotCreator LotCreator) => LotCreator.lClass == LotCreator.LotClass.LOW).ToList();
		if (list.Count <= 0)
		{
			return;
		}
		LotCreator lotCreator = list[Random.Range(0, list.Count)];
		if (lotCreator.Stage < 7 && LC_Demand > 1800f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 7 && lotCreator.lWidth > 3 && lotCreator.LandValue >= 256 && cityStage >= 7)
		{
			int num = 1800 + Random.Range(1, 100);
			LC_Demand -= num - lotCreator.Occupants;
			lotCreator.Stage = 7;
			lotCreator.Occupants = num;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.LOW;
			lotCreator.desiredValue = 256;
			lotCreator.nextValue = 256;
			lotCreator.Aiguilleur(26);
		}
		else if (lotCreator.Stage < 6 && LC_Demand > 900f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 6 && lotCreator.lWidth > 2 && lotCreator.LandValue >= 128 && cityStage >= 6)
		{
			int num2 = 900 + Random.Range(0, 60);
			LC_Demand -= num2 - lotCreator.Occupants;
			lotCreator.Stage = 6;
			lotCreator.Occupants = num2;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.LOW;
			lotCreator.desiredValue = 128;
			lotCreator.nextValue = 256;
			lotCreator.nextDemand = 1800;
			lotCreator.Aiguilleur(15);
		}
		else if (lotCreator.Stage < 5 && LC_Demand > 425f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 5 && lotCreator.lWidth > 2 && lotCreator.LandValue >= 64 && cityStage >= 5)
		{
			int num3 = 425 + Random.Range(0, 32);
			LC_Demand -= num3 - lotCreator.Occupants;
			lotCreator.Stage = 5;
			lotCreator.Occupants = num3;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.LOW;
			lotCreator.desiredValue = 64;
			lotCreator.nextValue = 128;
			lotCreator.nextDemand = 900;
			lotCreator.Aiguilleur(9);
		}
		else if (lotCreator.Stage < 4 && LC_Demand > 190f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 4 && lotCreator.LandValue >= 32 && cityStage >= 4)
		{
			int num4 = 190 + Random.Range(0, 15);
			LC_Demand -= num4 - lotCreator.Occupants;
			lotCreator.Stage = 4;
			lotCreator.Occupants = num4;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.LOW;
			lotCreator.desiredValue = 32;
			lotCreator.nextValue = 64;
			lotCreator.nextDemand = 425;
			lotCreator.Aiguilleur(5);
		}
		else if (lotCreator.Stage < 3 && LC_Demand > 60f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 3 && lotCreator.LandValue >= 16 && cityStage >= 3)
		{
			int num5 = 60 + Random.Range(0, 10);
			LC_Demand -= num5 - lotCreator.Occupants;
			lotCreator.Stage = 3;
			lotCreator.Occupants = num5;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.LOW;
			lotCreator.desiredValue = 16;
			lotCreator.nextValue = 32;
			lotCreator.nextDemand = 190;
			lotCreator.Aiguilleur(3);
		}
		else if (lotCreator.Stage < 2 && LC_Demand > 26f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 2 && lotCreator.LandValue >= 8 && cityStage >= 2)
		{
			int num6 = 26 + Random.Range(0, 6);
			LC_Demand -= num6 - lotCreator.Occupants;
			lotCreator.Stage = 2;
			lotCreator.Occupants = num6;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.LOW;
			lotCreator.desiredValue = 8;
			lotCreator.nextValue = 16;
			lotCreator.nextDemand = 60;
			lotCreator.Aiguilleur(2);
		}
		else if (lotCreator.Stage == 0)
		{
			if (LC_Demand < -50f)
			{
				LC_Demand += (float)lotCreator.Occupants * 0.5f;
				lotCreator.Occupants = 0;
				lotCreator.decayReason = "NotEnoughDemand";
			}
			else if (LC_Demand > (float)lotCreator.nextDemand * 0.5f)
			{
				LC_Demand -= (float)lotCreator.nextDemand * 0.5f;
				lotCreator.Occupants = lotCreator.nextDemand;
				lotCreator.decayReason = "";
			}
		}
		else if (LC_Demand < -100f && lotCreator.Stage > 0)
		{
			lotCreator.decayReason = "NotEnoughDemand";
			lotCreator.DecayLot(toParking: true);
			ongoingDecay = true;
		}
		else if (Unemployment > (float)(30 + Random.Range(-10, 10)) && lotCreator.Stage > 0 && Population > 800f)
		{
			lotCreator.decayReason = "Unemployment Too High";
			lotCreator.DecayLot(toParking: true);
			ongoingDecay = true;
		}
		else if (lotCreator.negatives > 8f && lotCreator.Stage > 0)
		{
			lotCreator.DecayLot(toParking: true);
			ongoingDecay = true;
		}
		else if (stageUpTries < 10 && !growthStopped)
		{
			stageUpTries++;
			LowClassStageUp();
		}
		else
		{
			stageUpTries = 0;
		}
	}

	private void MiddleClassStageUp()
	{
		bool flag = false;
		IEnumerable<LotCreator> source = w.ResidentialBuildings.Where((LotCreator LotCreator) => LotCreator.lClass == LotCreator.LotClass.MIDDLE);
		if (MC_Demand > 250f + LC_Demand && Random.value < 0.2f)
		{
			source = w.ResidentialBuildings.Where((LotCreator LotCreator) => LotCreator.lClass == LotCreator.LotClass.LOW);
			flag = true;
		}
		List<LotCreator> list = source.ToList();
		if (list.Count <= 0)
		{
			return;
		}
		LotCreator lotCreator = list[Random.Range(0, list.Count)];
		if (lotCreator.Stage < 7 && MC_Demand > 1380f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 7 && lotCreator.lWidth > 3 && lotCreator.LandValue >= 3072 && cityStage >= 7)
		{
			int num = 1380 + Random.Range(0, 100);
			if (flag)
			{
				MC_Demand -= num;
				LC_Demand += lotCreator.Occupants;
			}
			else
			{
				MC_Demand -= num - lotCreator.Occupants;
			}
			lotCreator.Stage = 7;
			lotCreator.Occupants = num;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.MIDDLE;
			lotCreator.desiredValue = 3072;
			lotCreator.nextValue = 3072;
			lotCreator.Aiguilleur(32);
		}
		else if (lotCreator.Stage < 6 && MC_Demand > 725f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 6 && lotCreator.lWidth > 2 && lotCreator.LandValue >= 2048 && cityStage >= 6)
		{
			int num2 = 725 + Random.Range(0, 46);
			if (flag)
			{
				MC_Demand -= num2;
				LC_Demand += lotCreator.Occupants;
			}
			else
			{
				MC_Demand -= num2 - lotCreator.Occupants;
			}
			lotCreator.Stage = 6;
			lotCreator.Occupants = num2;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.MIDDLE;
			lotCreator.desiredValue = 2048;
			lotCreator.nextValue = 3072;
			lotCreator.nextDemand = 1380;
			lotCreator.Aiguilleur(22);
		}
		else if (lotCreator.Stage < 5 && MC_Demand > 310f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 5 && lotCreator.lWidth > 2 && lotCreator.LandValue >= 1024 && cityStage >= 5)
		{
			int num3 = 310 + Random.Range(0, 24);
			if (flag)
			{
				MC_Demand -= num3;
				LC_Demand += lotCreator.Occupants;
			}
			else
			{
				MC_Demand -= num3 - lotCreator.Occupants;
			}
			lotCreator.Stage = 5;
			lotCreator.Occupants = num3;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.MIDDLE;
			lotCreator.desiredValue = 1024;
			lotCreator.nextValue = 2048;
			lotCreator.nextDemand = 725;
			lotCreator.Aiguilleur(10);
		}
		else if (lotCreator.Stage < 4 && MC_Demand > 125f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 4 && lotCreator.LandValue >= 512 && cityStage >= 4)
		{
			int num4 = 125 + Random.Range(0, 12);
			if (flag)
			{
				MC_Demand -= num4;
				LC_Demand += lotCreator.Occupants;
			}
			else
			{
				MC_Demand -= num4 - lotCreator.Occupants;
			}
			lotCreator.Stage = 4;
			lotCreator.Occupants = num4;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.MIDDLE;
			lotCreator.desiredValue = 512;
			lotCreator.nextValue = 1024;
			lotCreator.nextDemand = 310;
			lotCreator.Aiguilleur(5);
		}
		else if (lotCreator.Stage < 3 && MC_Demand > 45f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 3 && lotCreator.LandValue >= 256 && cityStage >= 3)
		{
			int num5 = 45 + Random.Range(0, 8);
			if (flag)
			{
				MC_Demand -= num5;
				LC_Demand += lotCreator.Occupants;
			}
			else
			{
				MC_Demand -= num5 - lotCreator.Occupants;
			}
			lotCreator.Stage = 3;
			lotCreator.Occupants = num5;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.MIDDLE;
			lotCreator.desiredValue = 256;
			lotCreator.nextValue = 512;
			lotCreator.nextDemand = 125;
			lotCreator.Aiguilleur(3);
		}
		else if (lotCreator.Stage < 2 && MC_Demand > 22f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 2 && lotCreator.LandValue >= 128 && cityStage >= 2)
		{
			int num6 = 22 + Random.Range(0, 5);
			if (flag)
			{
				MC_Demand -= num6;
				LC_Demand += lotCreator.Occupants;
			}
			else
			{
				MC_Demand -= num6 - lotCreator.Occupants;
			}
			lotCreator.Stage = 2;
			lotCreator.Occupants = 20 + Random.Range(0, 5);
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.MIDDLE;
			lotCreator.Aiguilleur(2);
			lotCreator.desiredValue = 128;
			lotCreator.nextValue = 256;
			lotCreator.nextDemand = 45;
		}
		else if (flag && lotCreator.Stage < 2 && MC_Demand > 14f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 1 && lotCreator.LandValue >= 96)
		{
			int num7 = 14 + Random.Range(0, 3);
			if (flag)
			{
				MC_Demand -= num7;
				LC_Demand += lotCreator.Occupants;
			}
			else
			{
				MC_Demand -= num7 - lotCreator.Occupants;
			}
			lotCreator.Stage = 1;
			lotCreator.Occupants = num7;
			lotCreator.underConstruction = true;
			lotCreator.desiredValue = 96;
			lotCreator.nextValue = 128;
			lotCreator.nextDemand = 22;
			lotCreator.lClass = LotCreator.LotClass.MIDDLE;
			lotCreator.Aiguilleur(2);
		}
		else if (MC_Demand < -75f && !flag)
		{
			lotCreator.decayReason = "NotEnoughDemand";
			lotCreator.DecayLot(toParking: true);
			ongoingDecay = true;
		}
		else if (Unemployment > (float)(30 + Random.Range(-10, 10)) && !flag)
		{
			lotCreator.decayReason = "Unemployment Too High";
			lotCreator.DecayLot(toParking: true);
			ongoingDecay = true;
		}
		else if (lotCreator.negatives > 5f && !flag)
		{
			lotCreator.DecayLot(toParking: true);
			ongoingDecay = true;
		}
		else if (stageUpTries < 10 && !growthStopped)
		{
			stageUpTries++;
			MiddleClassStageUp();
		}
		else
		{
			stageUpTries = 0;
		}
	}

	private void UpperClassStageUp()
	{
		bool flag = false;
		IEnumerable<LotCreator> source = w.ResidentialBuildings.Where((LotCreator LotCreator) => LotCreator.lClass == LotCreator.LotClass.UPPER);
		if (UC_Demand > 250f + LC_Demand && Random.value < 0.15f)
		{
			source = w.ResidentialBuildings.Where((LotCreator LotCreator) => LotCreator.lClass == LotCreator.LotClass.LOW);
			flag = true;
		}
		else if (UC_Demand > 200f + MC_Demand && Random.value > 0.85f)
		{
			source = w.ResidentialBuildings.Where((LotCreator LotCreator) => LotCreator.lClass == LotCreator.LotClass.MIDDLE);
			flag = true;
		}
		List<LotCreator> list = source.ToList();
		if (list.Count <= 0)
		{
			return;
		}
		LotCreator lotCreator = list[Random.Range(0, list.Count)];
		if (lotCreator.Stage < 7 && lotCreator.lWidth > 3 && UC_Demand > 1100f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 7 && lotCreator.LandValue >= 4096 && cityStage >= 7)
		{
			int num = 1100 + Random.Range(0, 100);
			if (flag)
			{
				if (lotCreator.lClass == LotCreator.LotClass.LOW)
				{
					UC_Demand -= num;
					LC_Demand += lotCreator.Occupants;
				}
				else
				{
					UC_Demand -= num;
					MC_Demand += lotCreator.Occupants;
				}
			}
			else
			{
				UC_Demand -= num - lotCreator.Occupants;
			}
			lotCreator.Stage = 7;
			lotCreator.Occupants = num;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.UPPER;
			lotCreator.desiredValue = 4096;
			lotCreator.nextValue = 4096;
			lotCreator.Aiguilleur(60);
			if (!messages.achievements[19])
			{
				SteamAchievements.script.UnlockAchievement("UC7");
				messages.achievements[19] = true;
			}
		}
		else if (lotCreator.Stage < 6 && lotCreator.lWidth > 2 && UC_Demand > 650f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 6 && lotCreator.LandValue >= 3072 && cityStage >= 6)
		{
			int num2 = 650 + Random.Range(0, 60);
			if (flag)
			{
				if (lotCreator.lClass == LotCreator.LotClass.LOW)
				{
					UC_Demand -= num2;
					LC_Demand += lotCreator.Occupants;
				}
				else
				{
					UC_Demand -= num2;
					MC_Demand += lotCreator.Occupants;
				}
			}
			else
			{
				UC_Demand -= num2 - lotCreator.Occupants;
			}
			lotCreator.Stage = 6;
			lotCreator.Occupants = num2;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.UPPER;
			lotCreator.desiredValue = 3072;
			lotCreator.nextValue = 4096;
			lotCreator.nextDemand = 1100;
			lotCreator.Aiguilleur(30);
		}
		else if (lotCreator.Stage < 5 && lotCreator.lWidth > 2 && UC_Demand > 275f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 5 && lotCreator.LandValue >= 2048 && cityStage >= 5)
		{
			int num3 = 275 + Random.Range(0, 25);
			if (flag)
			{
				if (lotCreator.lClass == LotCreator.LotClass.LOW)
				{
					UC_Demand -= num3;
					LC_Demand += lotCreator.Occupants;
				}
				else
				{
					UC_Demand -= num3;
					MC_Demand += lotCreator.Occupants;
				}
			}
			else
			{
				UC_Demand -= num3 - lotCreator.Occupants;
			}
			lotCreator.Stage = 5;
			lotCreator.Occupants = num3;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.UPPER;
			lotCreator.desiredValue = 2048;
			lotCreator.nextValue = 3072;
			lotCreator.nextDemand = 650;
			lotCreator.Aiguilleur(16);
		}
		else if (lotCreator.Stage < 4 && UC_Demand > 100f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 4 && lotCreator.LandValue >= 1024 && cityStage >= 4)
		{
			int num4 = 100 + Random.Range(0, 10);
			if (flag)
			{
				if (lotCreator.lClass == LotCreator.LotClass.LOW)
				{
					UC_Demand -= num4;
					LC_Demand += lotCreator.Occupants;
				}
				else
				{
					UC_Demand -= num4;
					MC_Demand += lotCreator.Occupants;
				}
			}
			else
			{
				UC_Demand -= num4 - lotCreator.Occupants;
			}
			lotCreator.Stage = 4;
			lotCreator.Occupants = num4;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.UPPER;
			lotCreator.desiredValue = 1024;
			lotCreator.nextValue = 2048;
			lotCreator.nextDemand = 275;
			lotCreator.Aiguilleur(9);
		}
		else if (lotCreator.Stage < 3 && UC_Demand > 40f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 3 && lotCreator.LandValue >= 512 && cityStage >= 3)
		{
			int num5 = 40 + Random.Range(0, 5);
			if (flag)
			{
				if (lotCreator.lClass == LotCreator.LotClass.LOW)
				{
					UC_Demand -= num5;
					LC_Demand += lotCreator.Occupants;
				}
				else
				{
					UC_Demand -= num5;
					MC_Demand += lotCreator.Occupants;
				}
			}
			else
			{
				UC_Demand -= num5 - lotCreator.Occupants;
			}
			lotCreator.Stage = 3;
			lotCreator.Occupants = num5;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.UPPER;
			lotCreator.desiredValue = 512;
			lotCreator.nextValue = 1024;
			lotCreator.nextDemand = 100;
			lotCreator.Aiguilleur(5);
		}
		else if (lotCreator.Stage < 2 && UC_Demand > 20f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 2 && lotCreator.LandValue >= 256 && cityStage >= 2)
		{
			int num6 = 20 + Random.Range(0, 4);
			if (flag)
			{
				if (lotCreator.lClass == LotCreator.LotClass.LOW)
				{
					UC_Demand -= num6;
					LC_Demand += lotCreator.Occupants;
				}
				else
				{
					UC_Demand -= num6;
					MC_Demand += lotCreator.Occupants;
				}
			}
			else
			{
				UC_Demand -= num6 - lotCreator.Occupants;
			}
			lotCreator.Stage = 2;
			lotCreator.Occupants = num6;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.UPPER;
			lotCreator.desiredValue = 256;
			lotCreator.nextValue = 512;
			lotCreator.nextDemand = 40;
			lotCreator.Aiguilleur(2);
		}
		else if (flag && lotCreator.Stage < 2 && UC_Demand > 10f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 1 && lotCreator.LandValue >= 192)
		{
			int num7 = 20 + Random.Range(0, 4);
			if (flag)
			{
				if (lotCreator.lClass == LotCreator.LotClass.LOW)
				{
					UC_Demand -= num7;
					LC_Demand += lotCreator.Occupants;
				}
				else
				{
					UC_Demand -= num7;
					MC_Demand += lotCreator.Occupants;
				}
			}
			else
			{
				UC_Demand -= num7 - lotCreator.Occupants;
			}
			lotCreator.Stage = 1;
			lotCreator.Occupants = 10 + Random.Range(0, 2);
			lotCreator.underConstruction = true;
			lotCreator.desiredValue = 192;
			lotCreator.nextValue = 256;
			lotCreator.nextDemand = 20;
			lotCreator.lClass = LotCreator.LotClass.UPPER;
			lotCreator.Aiguilleur(2);
		}
		else if (UC_Demand < -100f && !flag)
		{
			lotCreator.decayReason = "NotEnoughDemand";
			lotCreator.DecayLot(toParking: true);
			ongoingDecay = true;
		}
		else if (Unemployment > (float)(40 + Random.Range(-20, 10)) && !flag)
		{
			lotCreator.decayReason = "Unemployment Too High";
			lotCreator.DecayLot(toParking: true);
			ongoingDecay = true;
		}
		else if (lotCreator.negatives > 3f && !flag)
		{
			lotCreator.DecayLot(toParking: true);
			ongoingDecay = true;
		}
		else if (stageUpTries < 10 && !growthStopped)
		{
			stageUpTries++;
			UpperClassStageUp();
		}
		else
		{
			stageUpTries = 0;
		}
	}

	private void MixClassStageUp()
	{
		List<LotCreator> list = w.MixedBuildings.ToList();
		if (list.Count > 0)
		{
			LotCreator lotCreator = list[Random.Range(0, list.Count)];
			if (lotCreator.Stage < 4 && lotCreator.lWidth > 3 && UC_Demand > 780f && O_Demand > 385f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 4 && lotCreator.LandValue >= 4600 && cityStage >= 7)
			{
				int num = 750 + Random.Range(0, 65);
				UC_Demand -= num - lotCreator.Occupants;
				O_Demand -= 165f;
				lotCreator.Occupants = num;
				lotCreator.Visitors = (short)(370 + Random.Range(0, 31));
				lotCreator.Stage = 4;
				lotCreator.underConstruction = true;
				lotCreator.lClass = LotCreator.LotClass.MIXOFFICE;
				lotCreator.desiredValue = 4600;
				lotCreator.nextValue = 4600;
				lotCreator.Aiguilleur(50);
			}
			else if (lotCreator.Stage < 4 && lotCreator.lWidth > 3 && MC_Demand > 940f && R_Demand > 350f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 4 && lotCreator.LandValue >= 3328 && cityStage >= 7)
			{
				int num2 = 920 + Random.Range(0, 78);
				MC_Demand -= num2 - lotCreator.Occupants;
				R_Demand -= 130f;
				lotCreator.Occupants = num2;
				lotCreator.Visitors = (short)(340 + Random.Range(0, 26));
				lotCreator.Stage = 4;
				lotCreator.underConstruction = true;
				lotCreator.lClass = LotCreator.LotClass.MIXRETAIL;
				lotCreator.desiredValue = 3328;
				lotCreator.nextValue = 3328;
				lotCreator.Aiguilleur(40);
			}
			else if (lotCreator.Stage < 3 && lotCreator.lWidth > 3 && UC_Demand > 312f && O_Demand > 145f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 3 && lotCreator.LandValue >= 4096 && cityStage >= 6)
			{
				int num3 = 300 + Random.Range(0, 27);
				UC_Demand -= num3 - lotCreator.Occupants;
				O_Demand -= 70f;
				lotCreator.Occupants = num3;
				lotCreator.Visitors = (short)(140 + Random.Range(0, 13));
				lotCreator.Stage = 3;
				lotCreator.underConstruction = true;
				lotCreator.lClass = LotCreator.LotClass.MIXOFFICE;
				lotCreator.desiredValue = 4096;
				lotCreator.nextValue = 4600;
				lotCreator.Aiguilleur(26);
			}
			else if (lotCreator.Stage < 3 && lotCreator.lWidth > 3 && MC_Demand > 415f && R_Demand > 165f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 3 && lotCreator.LandValue >= 2560 && cityStage >= 6)
			{
				int num4 = 400 + Random.Range(0, 36);
				MC_Demand -= num4 - lotCreator.Occupants;
				R_Demand -= 66f;
				lotCreator.Occupants = num4;
				lotCreator.Visitors = (short)(160 + Random.Range(0, 12));
				lotCreator.Stage = 3;
				lotCreator.underConstruction = true;
				lotCreator.lClass = LotCreator.LotClass.MIXRETAIL;
				lotCreator.desiredValue = 2560;
				lotCreator.nextValue = 3328;
				lotCreator.Aiguilleur(18);
			}
			else if (lotCreator.Stage < 2 && lotCreator.lWidth > 2 && UC_Demand > 131f && O_Demand > 63f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 2 && lotCreator.LandValue >= 2560 && cityStage >= 4)
			{
				int num5 = 125 + Random.Range(0, 12);
				UC_Demand -= num5 - lotCreator.Occupants;
				O_Demand -= 33f;
				lotCreator.Occupants = num5;
				lotCreator.Visitors = (short)(60 + Random.Range(0, 6));
				lotCreator.Stage = 2;
				lotCreator.underConstruction = true;
				lotCreator.lClass = LotCreator.LotClass.MIXOFFICE;
				lotCreator.desiredValue = 2560;
				lotCreator.nextValue = 4096;
				lotCreator.Aiguilleur(13);
			}
			else if (lotCreator.Stage < 2 && MC_Demand > 189f && R_Demand > 63f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 2 && lotCreator.LandValue >= 1536 && cityStage >= 4)
			{
				int num6 = 180 + Random.Range(0, 18);
				MC_Demand -= num6 - lotCreator.Occupants;
				R_Demand -= 33f;
				lotCreator.Occupants = num6;
				lotCreator.Visitors = (short)(60 + Random.Range(0, 6));
				lotCreator.Stage = 2;
				lotCreator.underConstruction = true;
				lotCreator.lClass = LotCreator.LotClass.MIXRETAIL;
				lotCreator.desiredValue = 1536;
				lotCreator.nextValue = 2560;
				lotCreator.Aiguilleur(9);
			}
			else if (UC_Demand < -200f && lotCreator.lClass == LotCreator.LotClass.MIXOFFICE)
			{
				lotCreator.decayReason = "NotEnoughDemand";
				lotCreator.DecayLot(toParking: true);
				ongoingDecay = true;
			}
			else if (MC_Demand < -250f && lotCreator.lClass == LotCreator.LotClass.MIXRETAIL)
			{
				lotCreator.decayReason = "NotEnoughDemand";
				lotCreator.DecayLot(toParking: true);
				ongoingDecay = true;
			}
			else if (Unemployment > (float)(40 + Random.Range(-20, 60)))
			{
				lotCreator.decayReason = "Unemployment Too High";
				lotCreator.DecayLot(toParking: true);
				ongoingDecay = true;
			}
			else if (lotCreator.negatives > 3f)
			{
				lotCreator.DecayLot(toParking: true);
				ongoingDecay = true;
			}
			else if (stageUpTries < 10 && !growthStopped)
			{
				stageUpTries++;
				MixClassStageUp();
			}
			else
			{
				stageUpTries = 0;
			}
		}
	}

	private void ManufactureStageUp()
	{
		bool flag = false;
		IEnumerable<LotCreator> source = w.IndustrialBuildings.Where((LotCreator LotCreator) => LotCreator.lClass == LotCreator.LotClass.MANUFACTURE);
		if (M_Demand > 100f && M_Demand > HT_Demand * 2f && Random.value < 0.15f)
		{
			source = w.IndustrialBuildings.Where((LotCreator LotCreator) => LotCreator.lClass == LotCreator.LotClass.HIGHTECH);
			flag = true;
		}
		List<LotCreator> list = source.ToList();
		if (list.Count <= 0)
		{
			return;
		}
		LotCreator lotCreator = list[Random.Range(0, list.Count)];
		if (lotCreator.Stage < 4 && M_Demand > 540f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 4 && lotCreator.lWidth > 3 && lotCreator.LandValue >= 4 && cityStage >= 4)
		{
			int num = 540 + Random.Range(0, 30);
			if (flag)
			{
				M_Demand -= num;
				HT_Demand += lotCreator.Occupants;
			}
			else
			{
				M_Demand -= num - lotCreator.Occupants;
			}
			lotCreator.Stage = 4;
			lotCreator.Occupants = num;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.MANUFACTURE;
			lotCreator.desiredValue = 4;
			lotCreator.nextValue = 4;
			lotCreator.Aiguilleur(4);
		}
		else if (lotCreator.Stage < 3 && M_Demand > 180f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 3 && lotCreator.lWidth > 2 && lotCreator.LandValue >= 4 && cityStage >= 3)
		{
			int num2 = 180 + Random.Range(0, 16);
			M_Demand -= num2 - lotCreator.Occupants;
			lotCreator.Stage = 3;
			lotCreator.Occupants = num2;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.MANUFACTURE;
			lotCreator.desiredValue = 4;
			lotCreator.nextValue = 4;
			lotCreator.nextDemand = 540;
			lotCreator.Aiguilleur(3);
		}
		else if (lotCreator.Stage < 2 && M_Demand > 60f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 2 && lotCreator.lWidth > 2 && lotCreator.LandValue >= 4 && cityStage >= 2)
		{
			int num3 = 60 + Random.Range(0, 12);
			M_Demand -= num3 - lotCreator.Occupants;
			lotCreator.Stage = 2;
			lotCreator.Occupants = num3;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.MANUFACTURE;
			lotCreator.desiredValue = 4;
			lotCreator.nextValue = 4;
			lotCreator.nextDemand = 180;
			lotCreator.Aiguilleur(2);
		}
		else if (M_Demand < -80f && lotCreator.Stage > 0)
		{
			lotCreator.decayReason = "NotEnoughDemand";
			lotCreator.DecayLot(toParking: true);
			ongoingDecay = true;
		}
		else if (Unemployment <= -1f && Population > 10000f && lotCreator.Stage > 0)
		{
			if (!(Random.value > 0.2f))
			{
				lotCreator.decayReason = "NoWorkforce";
				lotCreator.DecayLot(toParking: true);
				ongoingDecay = true;
			}
		}
		else if (lotCreator.negatives > 8f && Population > 300f && lotCreator.Stage > 0)
		{
			lotCreator.DecayLot(toParking: true);
			ongoingDecay = true;
		}
		else if (stageUpTries < 10 && !growthStopped)
		{
			stageUpTries++;
			ManufactureStageUp();
		}
		else
		{
			stageUpTries = 0;
		}
	}

	private void HighTechStageUp()
	{
		bool flag = false;
		IEnumerable<LotCreator> source = w.IndustrialBuildings.Where((LotCreator LotCreator) => LotCreator.lClass == LotCreator.LotClass.HIGHTECH);
		if (HT_Demand > 100f && HT_Demand > M_Demand * 2f && Random.value > 0.5f)
		{
			source = w.IndustrialBuildings.Where((LotCreator LotCreator) => LotCreator.lClass == LotCreator.LotClass.MANUFACTURE);
			flag = true;
		}
		List<LotCreator> list = source.ToList();
		if (list.Count <= 0)
		{
			return;
		}
		LotCreator lotCreator = list[Random.Range(0, list.Count)];
		if (lotCreator.Stage < 4 && HT_Demand > 405f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 4 && lotCreator.LandValue >= 1024 && lotCreator.lWidth > 3 && cityStage >= 4)
		{
			int num = 405 + Random.Range(0, 24);
			if (flag)
			{
				HT_Demand -= num;
				M_Demand += lotCreator.Occupants;
			}
			else
			{
				HT_Demand -= num - lotCreator.Occupants;
			}
			lotCreator.Stage = 4;
			lotCreator.Occupants = num;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.HIGHTECH;
			lotCreator.desiredValue = 1024;
			lotCreator.nextValue = 1024;
			lotCreator.Aiguilleur(8);
		}
		else if (lotCreator.Stage < 3 && lotCreator.lWidth > 2 && HT_Demand > 135f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 3 && lotCreator.LandValue >= 512 && cityStage >= 3)
		{
			int num2 = 135 + Random.Range(0, 18);
			if (flag)
			{
				HT_Demand -= num2;
				M_Demand += lotCreator.Occupants;
			}
			else
			{
				HT_Demand -= num2 - lotCreator.Occupants;
			}
			lotCreator.Stage = 3;
			lotCreator.Occupants = num2;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.HIGHTECH;
			lotCreator.desiredValue = 512;
			lotCreator.nextValue = 1024;
			lotCreator.nextDemand = 405;
			lotCreator.Aiguilleur(3);
		}
		else if (lotCreator.Stage < 2 && lotCreator.lWidth > 2 && HT_Demand > 45f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 2 && lotCreator.LandValue >= 256 && cityStage >= 2)
		{
			int num3 = 45 + Random.Range(0, 9);
			if (flag)
			{
				HT_Demand -= num3;
				M_Demand += lotCreator.Occupants;
			}
			else
			{
				HT_Demand -= num3 - lotCreator.Occupants;
			}
			lotCreator.Stage = 2;
			lotCreator.Occupants = num3;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.HIGHTECH;
			lotCreator.desiredValue = 256;
			lotCreator.nextValue = 512;
			lotCreator.nextDemand = 135;
			lotCreator.Aiguilleur(2);
		}
		else if (HT_Demand < -150f && !flag)
		{
			lotCreator.decayReason = "NotEnoughDemand";
			lotCreator.DecayLot(toParking: true);
			ongoingDecay = true;
		}
		else if (Unemployment < 0f && !flag)
		{
			if (!(Random.value > 0.2f))
			{
				lotCreator.decayReason = "NoWorkforce";
				lotCreator.DecayLot(toParking: true);
				ongoingDecay = true;
			}
		}
		else if (lotCreator.negatives > 3f && !flag)
		{
			lotCreator.DecayLot(toParking: true);
			ongoingDecay = true;
		}
		else if (stageUpTries < 10 && !growthStopped)
		{
			stageUpTries++;
			HighTechStageUp();
		}
		else
		{
			stageUpTries = 0;
		}
	}

	private void RetailStageUp()
	{
		bool flag = false;
		IEnumerable<LotCreator> source = w.CommercialBuildings.Where((LotCreator LotCreator) => LotCreator.lClass == LotCreator.LotClass.RETAIL);
		if (R_Demand > 300f && R_Demand > O_Demand * 2f && Random.value < 0.15f)
		{
			source = w.CommercialBuildings.Where((LotCreator LotCreator) => LotCreator.lClass == LotCreator.LotClass.OFFICE);
			flag = true;
		}
		List<LotCreator> list = source.ToList();
		if (list.Count <= 0)
		{
			return;
		}
		LotCreator lotCreator = list[Random.Range(0, list.Count)];
		if (lotCreator.Stage < 5 && R_Demand > 1200f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.lWidth > 3 && lotCreator.MaxStage >= 5 && lotCreator.LandValue >= 2048 && cityStage > 5)
		{
			int num = 1200 + Random.Range(0, 60);
			if (flag)
			{
				R_Demand -= num;
				O_Demand += lotCreator.Occupants;
			}
			else
			{
				R_Demand -= num - lotCreator.Occupants;
			}
			lotCreator.Stage = 5;
			lotCreator.Occupants = num;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.RETAIL;
			lotCreator.desiredValue = 2048;
			lotCreator.nextValue = 2048;
			lotCreator.Aiguilleur(18);
		}
		else if (lotCreator.Stage < 4 && R_Demand > 420f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.lWidth > 3 && lotCreator.MaxStage >= 4 && lotCreator.LandValue >= 1024 && cityStage > 4)
		{
			int num2 = 420 + Random.Range(0, 40);
			if (flag)
			{
				R_Demand -= num2;
				O_Demand += lotCreator.Occupants;
			}
			else
			{
				R_Demand -= num2 - lotCreator.Occupants;
			}
			lotCreator.Stage = 4;
			lotCreator.Occupants = num2;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.RETAIL;
			lotCreator.desiredValue = 1024;
			lotCreator.nextValue = 2048;
			lotCreator.nextDemand = 1200;
			lotCreator.Aiguilleur(4);
		}
		else if (lotCreator.Stage < 3 && lotCreator.lWidth > 2 && R_Demand > 140f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 3 && lotCreator.LandValue >= 256 && cityStage >= 3)
		{
			int num3 = 140 + Random.Range(0, 20);
			if (flag)
			{
				R_Demand -= num3;
				O_Demand += lotCreator.Occupants;
			}
			else
			{
				R_Demand -= num3 - lotCreator.Occupants;
			}
			lotCreator.Stage = 3;
			lotCreator.Occupants = num3;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.RETAIL;
			lotCreator.desiredValue = 256;
			lotCreator.nextValue = 1024;
			lotCreator.nextDemand = 420;
			lotCreator.Aiguilleur(3);
		}
		else if (lotCreator.Stage < 2 && R_Demand > 50f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 2 && lotCreator.LandValue >= 64 && cityStage >= 2)
		{
			int num4 = 50 + Random.Range(0, 10);
			if (flag)
			{
				R_Demand -= num4;
				O_Demand += lotCreator.Occupants;
			}
			else
			{
				R_Demand -= num4 - lotCreator.Occupants;
			}
			lotCreator.Stage = 2;
			lotCreator.Occupants = num4;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.RETAIL;
			lotCreator.desiredValue = 64;
			lotCreator.nextValue = 256;
			lotCreator.nextDemand = 140;
			lotCreator.Aiguilleur(2);
		}
		else if (R_Demand < -150f && lotCreator.Stage > 0 && !flag)
		{
			lotCreator.decayReason = "NotEnoughDemand";
			lotCreator.DecayLot(toParking: true);
			ongoingDecay = true;
		}
		else if (lotCreator.negatives > 8f && Population > 300f && lotCreator.Stage > 0 && !flag)
		{
			lotCreator.DecayLot(toParking: true);
			ongoingDecay = true;
		}
		else if (stageUpTries < 10 && !growthStopped)
		{
			stageUpTries++;
			RetailStageUp();
		}
		else
		{
			stageUpTries = 0;
		}
	}

	private void OfficeStageUp()
	{
		bool flag = false;
		IEnumerable<LotCreator> source = w.CommercialBuildings.Where((LotCreator LotCreator) => LotCreator.lClass == LotCreator.LotClass.OFFICE);
		if (O_Demand > 300f && O_Demand > R_Demand * 2f && Random.value < 0.15f)
		{
			source = w.CommercialBuildings.Where((LotCreator LotCreator) => LotCreator.lClass == LotCreator.LotClass.RETAIL);
			flag = true;
		}
		List<LotCreator> list = source.ToList();
		if (list.Count <= 0)
		{
			return;
		}
		LotCreator lotCreator = list[Random.Range(0, list.Count)];
		if (lotCreator.Stage < 6 && O_Demand > 2600f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.lWidth > 3 && lotCreator.MaxStage >= 6 && lotCreator.LandValue >= 3072 && cityStage > 6)
		{
			int num = 2600 + Random.Range(0, 160);
			if (flag)
			{
				O_Demand -= num;
				R_Demand += lotCreator.Occupants;
			}
			else
			{
				O_Demand -= num - lotCreator.Occupants;
			}
			lotCreator.Stage = 6;
			lotCreator.Occupants = num;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.OFFICE;
			lotCreator.desiredValue = 3072;
			lotCreator.nextValue = 3072;
			lotCreator.Aiguilleur(55);
			if (!messages.achievements[20])
			{
				SteamAchievements.script.UnlockAchievement("O6");
				messages.achievements[20] = true;
			}
		}
		else if (lotCreator.Stage < 5 && O_Demand > 1800f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.lWidth > 3 && lotCreator.MaxStage >= 5 && lotCreator.LandValue >= 2048 && cityStage > 5)
		{
			int num2 = 1800 + Random.Range(0, 100);
			if (flag)
			{
				O_Demand -= num2;
				R_Demand += lotCreator.Occupants;
			}
			else
			{
				O_Demand -= num2 - lotCreator.Occupants;
			}
			lotCreator.Stage = 5;
			lotCreator.Occupants = num2;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.OFFICE;
			lotCreator.desiredValue = 2048;
			lotCreator.nextValue = 3072;
			lotCreator.nextDemand = 2600;
			lotCreator.Aiguilleur(30);
		}
		else if (lotCreator.Stage < 4 && lotCreator.lWidth > 2 && O_Demand > 840f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 4 && lotCreator.LandValue >= 1024 && cityStage > 4)
		{
			int num3 = 840 + Random.Range(0, 60);
			if (flag)
			{
				O_Demand -= num3;
				R_Demand += lotCreator.Occupants;
			}
			else
			{
				O_Demand -= num3 - lotCreator.Occupants;
			}
			lotCreator.Stage = 4;
			lotCreator.Occupants = num3;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.OFFICE;
			lotCreator.desiredValue = 1024;
			lotCreator.nextValue = 2048;
			lotCreator.nextDemand = 1800;
			lotCreator.Aiguilleur(21);
		}
		else if (lotCreator.Stage < 3 && lotCreator.lWidth > 2 && O_Demand > 420f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 3 && lotCreator.LandValue >= 512 && cityStage > 3)
		{
			int num4 = 420 + Random.Range(0, 42);
			if (flag)
			{
				O_Demand -= num4;
				R_Demand += lotCreator.Occupants;
			}
			else
			{
				O_Demand -= num4 - lotCreator.Occupants;
			}
			lotCreator.Stage = 3;
			lotCreator.Occupants = num4;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.OFFICE;
			lotCreator.desiredValue = 512;
			lotCreator.nextValue = 1024;
			lotCreator.nextDemand = 840;
			lotCreator.Aiguilleur(12);
		}
		else if (lotCreator.Stage < 2 && lotCreator.lWidth > 2 && O_Demand > 160f && !lotCreator.underConstruction && lotCreator.negatives < 1f && lotCreator.MaxStage >= 2 && lotCreator.LandValue >= 256 && cityStage >= 2)
		{
			int num5 = 160 + Random.Range(0, 16);
			if (flag)
			{
				O_Demand -= num5;
				R_Demand += lotCreator.Occupants;
			}
			else
			{
				O_Demand -= num5 - lotCreator.Occupants;
			}
			lotCreator.Stage = 2;
			lotCreator.Occupants = num5;
			lotCreator.underConstruction = true;
			lotCreator.lClass = LotCreator.LotClass.OFFICE;
			lotCreator.desiredValue = 256;
			lotCreator.nextValue = 512;
			lotCreator.nextDemand = 420;
			lotCreator.Aiguilleur(5);
		}
		else if (O_Demand < -300f && lotCreator.Stage > 0 && !flag)
		{
			lotCreator.decayReason = "NotEnoughDemand";
			lotCreator.DecayLot(toParking: true);
			ongoingDecay = true;
		}
		else if (Unemployment < 1f && lotCreator.Stage > 0 && !flag)
		{
			if (!(Random.value > 0.2f))
			{
				lotCreator.decayReason = "NoWorkforce";
				lotCreator.DecayLot(toParking: true);
				ongoingDecay = true;
			}
		}
		else if (lotCreator.negatives > 4f && lotCreator.Stage > 0 && !flag && Population > 1000f)
		{
			lotCreator.DecayLot(toParking: true);
			ongoingDecay = true;
		}
		else if (stageUpTries < 10 && !growthStopped)
		{
			stageUpTries++;
			OfficeStageUp();
		}
		else
		{
			stageUpTries = 0;
		}
	}

	private void TryBuildSlum(BlockData choice, RoadCreator rc, bool isFree)
	{
		int x = choice.wP.x;
		int y = choice.wP.y;
		List<Vector2Int> list = new List<Vector2Int>();
		Vector3 pos = Vector3.zero;
		if (rc.cc == 1)
		{
			list.Add(new Vector2Int(x, y));
			list.Add(new Vector2Int(x + 1, y));
			list.Add(new Vector2Int(x + 1, y - 1));
			list.Add(new Vector2Int(x, y - 1));
			pos = new Vector3((float)x + 0.5f, choice.h, (float)y - 0.5f);
		}
		else if (rc.cc == 2)
		{
			list.Add(new Vector2Int(x, y));
			list.Add(new Vector2Int(x + 1, y));
			list.Add(new Vector2Int(x + 1, y + 1));
			list.Add(new Vector2Int(x, y + 1));
			pos = new Vector3((float)x + 0.5f, choice.h, (float)y + 0.5f);
		}
		else if (rc.cc == 3)
		{
			list.Add(new Vector2Int(x - 1, y));
			list.Add(new Vector2Int(x, y));
			list.Add(new Vector2Int(x - 1, y - 1));
			list.Add(new Vector2Int(x, y - 1));
			pos = new Vector3((float)x - 0.5f, choice.h, (float)y - 0.5f);
		}
		else if (rc.cc == 4)
		{
			list.Add(new Vector2Int(x + 1, y));
			list.Add(new Vector2Int(x, y));
			list.Add(new Vector2Int(x + 1, y - 1));
			list.Add(new Vector2Int(x, y - 1));
			pos = new Vector3((float)x + 0.5f, choice.h, (float)y - 0.5f);
		}
		foreach (Vector2Int item in list)
		{
			BlockData ifNotNull = w.GetIfNotNull(item.x, item.y);
			if (ifNotNull == null)
			{
				return;
			}
			if (ifNotNull.b)
			{
				isFree = false;
				break;
			}
			if (ifNotNull.t == BlockData.Type.ROAD)
			{
				isFree = false;
				break;
			}
		}
		if (isFree)
		{
			BuildNewSlum(pos, list, slumReason);
		}
	}

	private void BuildNewSlum(Vector3 pos, List<Vector2Int> bdList, string _reason)
	{
		GameObject gameObject = Object.Instantiate(slumUnit);
		SlumCreator component = gameObject.GetComponent<SlumCreator>();
		gameObject.name = pos.x + "_slum_" + pos.z;
		gameObject.transform.position = pos;
		component.world = w;
		component.Building = gameObject;
		component.bdList = bdList;
		component.wP = pos;
		component.Occupants = Random.Range(8, 12);
		component.reason = _reason;
		w.SlumBuildings.Add(component);
		Slum_Demand -= component.Occupants;
		LC_Demand -= component.Occupants;
		gameObject.transform.parent = tZone.transform;
		component.LevelTerrain();
	}

	private void SlumStageUp()
	{
		if (Random.value > 0.5f)
		{
			return;
		}
		List<SlumCreator> list = w.SlumBuildings.OrderBy((SlumCreator x) => x.landValue).ToList();
		for (int num = 0; num < list.Count; num++)
		{
			SlumCreator slumCreator = list[num];
			slumCreator.landValue = w.GetIfNotNull(slumCreator.bdList[0].x, slumCreator.bdList[0].y).v[0];
			if (slumCreator.Stage == 1 && Slum_Demand > 26f && slumCreator.landValue > 50f)
			{
				slumCreator.SlumStageUp();
				slumCreator.Occupants = Random.Range(26, 34);
				Slum_Demand -= slumCreator.Occupants;
				break;
			}
			if (slumCreator.Stage == 2 && Slum_Demand > 78f && slumCreator.landValue > 100f)
			{
				slumCreator.SlumStageUp();
				slumCreator.Occupants = Random.Range(78, 94);
				Slum_Demand -= slumCreator.Occupants;
				break;
			}
			if (slumCreator.Stage == 3 && Slum_Demand > 234f && slumCreator.landValue > 200f)
			{
				slumCreator.SlumStageUp();
				slumCreator.Occupants = Random.Range(234, 274);
				Slum_Demand -= slumCreator.Occupants;
				break;
			}
		}
	}

	private void DespawnSlum()
	{
		if (w.SlumBuildings.Count < 1)
		{
			return;
		}
		SlumCreator slumCreator = w.SlumBuildings[Random.Range(0, w.SlumBuildings.Count)];
		foreach (Vector2Int bd in slumCreator.bdList)
		{
			w.blockData[bd.x, bd.y].b = false;
			w.blockData[bd.x, bd.y].t = BlockData.Type.GRASS;
			w.blockData[bd.x, bd.y].g = true;
		}
		w.eco.LC_Demand += slumCreator.Occupants;
		w.SlumBuildings.Remove(slumCreator);
		Object.Destroy(slumCreator.gameObject);
	}

	private IEnumerator BankruptProcess()
	{
		isBankrupted = true;
		yield return new WaitForEndOfFrame();
		messages.NewAlertMessage(7);
		if (!messages.achievements[23])
		{
			SteamAchievements.script.UnlockAchievement("fail");
			messages.achievements[23] = true;
		}
		if (budget.Balance < -15000f)
		{
			budget.LC_Tax = 15f;
			budget.UI_LC_Tax.text = budget.LC_Tax + "%";
			budget.MC_Tax = 16f;
			budget.UI_MC_Tax.text = budget.MC_Tax + "%";
			budget.UC_Tax = 17f;
			budget.UI_UC_Tax.text = budget.UC_Tax + "%";
			budget.R_Tax = 6f;
			budget.UI_R_Tax.text = budget.R_Tax + "%";
			budget.M_Tax = 7f;
			budget.UI_M_Tax.text = budget.M_Tax + "%";
			budget.HT_Tax = 7f;
			budget.UI_HT_Tax.text = budget.HT_Tax + "%";
		}
		for (int i = 0; i < 10; i++)
		{
			yield return new WaitForSeconds(1f);
			if (w.Services.Count >= 1)
			{
				w.Services[Random.Range(0, w.Services.Count)].DecayService();
			}
		}
		if (Population > 2000f && budget.Balance < -10000f && w.ResidentialBuildings.Count > 0)
		{
			for (int i = 0; i < 20; i++)
			{
				LotCreator lotCreator = w.ResidentialBuildings[Random.Range(0, w.ResidentialBuildings.Count)];
				if (!lotCreator.isDecayed)
				{
					lotCreator.DecayLot(toParking: true);
					budget.Balance += lotCreator.Occupants * 2;
				}
				else if (lotCreator.isDecayed && !lotCreator.isRubles)
				{
					StartCoroutine(lotCreator.RublesTransform());
					lotCreator.isRubles = true;
					budget.Balance += 250f;
				}
				yield return new WaitForSeconds(0.5f);
			}
		}
		isBankrupted = false;
	}

	private void UpdateLegislationResume()
	{
		leg.R_Res.text = (Retail - 1f).ToString("+0.0;-0.0");
		leg.O_Res.text = (Office - 1f).ToString("+0.0;-0.0");
		leg.BM_Res.text = (Manufacturing - 1f).ToString("+0.0;-0.0");
		leg.AM_Res.text = (HighTech - 1f).ToString("+0.0;-0.0");
		leg.CR_Res.text = (CivilRights - 50f).ToString("+0.0;-0.0");
		leg.FI_Res.text = (FreedomIndex - 50f).ToString("+0.0;-0.0");
		leg.SL_Res.text = (SocialLadderPolicy - 0.4f).ToString("+0.0;-0.0");
		leg.He_Res.text = HospitalEfficiency.ToString("+0.0;-0.0");
		leg.Ed_Res.text = SchoolEfficiency.ToString("+0.0;-0.0");
		leg.Se_Res.text = Security.ToString("+0.0;-0.0");
		leg.Br_Res.text = (BirthRatePolicy - birthBase).ToString("+0.0;-0.0");
		leg.Rel_Res.text = (Religiousness - 0.3f).ToString("+0.0;-0.0");
		leg.Polu_Res.text = (0f - EnviroPol + 1f).ToString("+0.0;-0.0");
		leg.Co_Res.text = Corruption.ToString("+0.0;-0.0");
		if (PolicyLeaning < 0f)
		{
			leg.Cons_Res.text = "+0.0";
			leg.Libs_Res.text = Mathf.Abs(PolicyLeaning).ToString("+0.0;-0.0");
		}
		else
		{
			leg.Cons_Res.text = PolicyLeaning.ToString("+0.0;-0.0");
			leg.Libs_Res.text = "+0.0";
		}
	}
}
