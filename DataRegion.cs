using System.Collections.Generic;
using UnityEngine;

public static class DataRegion
{
	public static DataCity[,] cities;

	public static DataCity selectedCity;

	public static DataCity capital;

	public static string mapFile = "Peninsula Isa";

	public static string nationName = "Default2021";

	public static float[] ecoSettings = new float[6];

	public static int Population = 0;

	public static int Treasury = 200000;

	public static float ExchangeFloat = 1f;

	public static int LandPrice = 50000;

	public static float RegionalConnections = 0f;

	public static int PowerCapacity = 1;

	public static int PowerUse = 1;

	public static int FreedomIndex = 50;

	public static int CivilRights = 50;

	public static int Stability = 100;

	public static int UnivCapacity = 1;

	public static float DollarReserves = 0f;

	public static float MoneyPrinted = 0f;

	public static int TradeCapacity = 1;

	public static float TradeUse = 0f;

	public static string c = "CSD";

	public static int version = 120;

	public static PolicyArray policyArray;

	public static Texture2D FlagTexture;

	public static bool[] alreadyBuilt = new bool[50];

	public static int HQstage = 0;

	public static List<string> savedNations = new List<string>();

	public static int tip = 0;

	public static bool dataLoaded = false;

	public static int cityCount = 0;

	public static void UpdatePop()
	{
		Population = 0;
		RegionalConnections = 0f;
		PowerCapacity = 0;
		PowerUse = 0;
		cityCount = 0;
		UnivCapacity = 0;
		TradeUse = 0f;
		TradeCapacity = 0;
		for (int i = 0; i < WorldRegion.worldSize; i++)
		{
			for (int j = 0; j < WorldRegion.worldSize; j++)
			{
				if (cities[i, j].population > 0)
				{
					cityCount++;
					Population += cities[i, j].population;
					RegionalConnections += cities[i, j].aiportConnections;
					PowerCapacity += cities[i, j].powerCapacity;
					PowerUse += cities[i, j].powerUse;
					UnivCapacity += cities[i, j].UnivCapacity;
					TradeCapacity += cities[i, j].TradeCapacity;
					TradeUse += cities[i, j].TradeUse;
				}
			}
		}
	}

	public static DataCity GetCity(int x, int z)
	{
		if (x < 0 || z < 0 || x >= WorldRegion.worldSize || z >= WorldRegion.worldSize)
		{
			return null;
		}
		return cities[x, z];
	}

	public static void SaveDataRegion()
	{
		ES3.Save(nationName + "cities", cities);
		ES3.Save(nationName + "blockData", WorldRegion.blockData, nationName + "blockData.es3");
		ES3.Save(nationName + "treasury", Treasury);
		ES3.Save(nationName + "policies", policyArray);
		ES3.Save(nationName + "capital", capital);
		ES3.Save(nationName + "built", alreadyBuilt);
		ES3.Save(nationName + "HQstage", HQstage);
		ES3.Save(nationName + "MapFile", mapFile);
		ES3.Save(nationName + "LandPrice", LandPrice);
		ES3.Save(nationName + "ExchangeRate", ExchangeFloat);
		ES3.Save(nationName + "Stability", Stability);
		ES3.Save(nationName + "FI", FreedomIndex);
		ES3.Save(nationName + "CR", CivilRights);
		ES3.Save(nationName + "Version", version);
		ES3.Save(nationName + "Reserve", DollarReserves);
		ES3.Save(nationName + "Printed", MoneyPrinted);
		ES3.Save("savedNations", savedNations);
		Debug.Log(nationName + " - Pop= " + Population + " was saved successfully");
	}

	public static void LoadDataRegion(bool inRegion)
	{
		ecoSettings = ES3.Load(nationName + "ecoSettings", ecoSettings);
		cities = ES3.Load(nationName + "cities", cities);
		Treasury = ES3.Load(nationName + "treasury", Treasury);
		policyArray = ES3.Load(nationName + "policies", policyArray);
		capital = ES3.Load(nationName + "capital", capital);
		alreadyBuilt = ES3.Load(nationName + "built", alreadyBuilt);
		HQstage = ES3.Load(nationName + "HQstage", HQstage);
		FlagTexture = ES3.LoadImage(nationName + "Flag.png");
		mapFile = ES3.LoadString(nationName + "MapFile", mapFile);
		LandPrice = ES3.Load(nationName + "LandPrice", LandPrice);
		Stability = ES3.Load(nationName + "Stability", Stability);
		FreedomIndex = ES3.Load(nationName + "FI", FreedomIndex);
		CivilRights = ES3.Load(nationName + "CR", CivilRights);
		DollarReserves = ES3.Load(nationName + "Reserve", DollarReserves);
		MoneyPrinted = ES3.Load(nationName + "Printed", MoneyPrinted);
		savedNations = ES3.Load("savedNations", savedNations);
		if (inRegion && !dataLoaded)
		{
			WorldRegion.blockData = ES3.Load(nationName + "blockData", nationName + "blockData.es3", WorldRegion.blockData);
			dataLoaded = true;
		}
		if (ecoSettings[1] != 8f && ecoSettings[1] != 5f)
		{
			ecoSettings[1] = 8f;
		}
	}

	public static void SaveDataCity(World w)
	{
		cities[selectedCity.rX, selectedCity.rZ].built = true;
		cities[selectedCity.rX, selectedCity.rZ].population = (int)w.eco.Population;
		cities[selectedCity.rX, selectedCity.rZ].unemployment = (int)w.eco.Unemployment;
		cities[selectedCity.rX, selectedCity.rZ].balance = (int)w.eco.budget.Balance;
		cities[selectedCity.rX, selectedCity.rZ].LC_City = Mathf.RoundToInt(w.eco.LC_Demand);
		cities[selectedCity.rX, selectedCity.rZ].MC_City = Mathf.RoundToInt(w.eco.MC_Demand);
		cities[selectedCity.rX, selectedCity.rZ].UC_City = Mathf.RoundToInt(w.eco.UC_Demand);
		cities[selectedCity.rX, selectedCity.rZ].M_City = Mathf.RoundToInt(w.eco.M_Demand);
		cities[selectedCity.rX, selectedCity.rZ].HT_City = Mathf.RoundToInt(w.eco.HT_Demand);
		cities[selectedCity.rX, selectedCity.rZ].O_City = Mathf.RoundToInt(w.eco.O_Demand);
		cities[selectedCity.rX, selectedCity.rZ].R_City = Mathf.RoundToInt(w.eco.R_Demand);
		cities[selectedCity.rX, selectedCity.rZ].LCpart = (int)w.eco.LCPartPercent;
		cities[selectedCity.rX, selectedCity.rZ].MCpart = (int)w.eco.MCPartPercent;
		cities[selectedCity.rX, selectedCity.rZ].UCpart = (int)w.eco.UCPartPercent;
		cities[selectedCity.rX, selectedCity.rZ].GDPcap = Mathf.RoundToInt((float)w.eco.GDP / w.eco.Population / 100f);
		cities[selectedCity.rX, selectedCity.rZ].aiportConnections = w.immi.AirportConnections;
		cities[selectedCity.rX, selectedCity.rZ].powerUse = w.eco.PowerUse;
		cities[selectedCity.rX, selectedCity.rZ].powerCapacity = w.eco.PowerCapacity - w.eco.otherCitiesPowerUse;
		cities[selectedCity.rX, selectedCity.rZ].UnivCapacity = w.eco.UniCapacity;
		cities[selectedCity.rX, selectedCity.rZ].TradeUse = Mathf.Abs((int)w.eco.budget.Imports - (int)w.eco.budget.Exports);
		cities[selectedCity.rX, selectedCity.rZ].TradeCapacity = (int)w.eco.budget.TradeCapacity - (int)w.eco.budget.otherCitiesTradeUse;
		int num = ((World.worldSize != 32) ? 320 : 512);
		int num2 = selectedCity.rX * num;
		int num3 = selectedCity.rZ * num;
		for (int i = 0; i < World.chunkSize * World.worldSize; i++)
		{
			for (int j = 0; j < World.chunkSize * World.worldSize; j++)
			{
				BlockData blockData = w.blockData[i, j];
				if (WorldRegion.blockData[i + num2, j + num3] == null)
				{
					BlockLight blockLight = new BlockLight
					{
						h = (short)(blockData.h * 100f),
						s = blockData.st
					};
					blockLight.t[0] = blockData.r;
					if (blockData.t == BlockData.Type.GRASSTREE)
					{
						blockLight.t[1] = true;
					}
					WorldRegion.blockData[i + num2, j + num3] = blockLight;
				}
				else
				{
					WorldRegion.blockData[i + num2, j + num3].h = (short)(blockData.h * 100f);
					WorldRegion.blockData[i + num2, j + num3].s = blockData.st;
					WorldRegion.blockData[i + num2, j + num3].t[0] = blockData.r;
				}
			}
		}
	}

	public static void LoadListOfSavedNations()
	{
		savedNations = ES3.Load("savedNations", savedNations);
	}

	public static void ResetValues()
	{
		capital = null;
		Population = 0;
		Treasury = 200000;
		LandPrice = 50000;
		DollarReserves = 0f;
		RegionalConnections = 0f;
		policyArray = null;
		FlagTexture = null;
		alreadyBuilt = new bool[50];
		HQstage = 0;
		dataLoaded = false;
	}
}
