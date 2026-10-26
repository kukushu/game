using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Plopping : MonoBehaviour
{
	public BlockInteraction bi;

	public GameObject lotParent;

	public CostHelper costHelper;

	public Button B_Legislation;

	public Button B_ImmiPanel;

	public Button B_EcoPolicies;

	public Button B_SocialPolicies;

	public Button B_PrintMoney;

	public Button B_BuyDollars;

	public Button B_Univ;

	public GameObject ServiceBuildings;

	public GameObject Hover4x4;

	public Material PlacerMaterial;

	private Vector3 Center;

	private int Orientation;

	private bool isFree;

	private bool isConnected;

	private bool isFlat;

	private BlockData roadAccess;

	private HashSet<Chunk> updates = new HashSet<Chunk>();

	public Roading roading;

	public Railing railing;

	public Indexer indexer;

	public Messages messages;

	public Zoning zoning;

	public GameObject serviceUnit;

	public SoundManager sm;

	private int x2;

	private int z2;

	private int oldx2;

	private int oldz2;

	private Vector3 pos = new Vector3(0f, 0f, 0f);

	private Color32 green = new Color32(0, byte.MaxValue, 0, 60);

	private Color32 red = new Color32(150, 0, 0, 60);

	private readonly int schoolPrice = 2500;

	private readonly int hospitalPrice = 7500;

	private readonly int highschoolPrice = 4500;

	private readonly int univPrice = 48000;

	private readonly int firePrice = 3500;

	private readonly int policePrice = 4500;

	private readonly int libraryPrice = 3000;

	private readonly int prisonPrice = 20000;

	private readonly int HQPrice = 3000;

	private readonly int ImmigrationPrice = 2500;

	private readonly int BankPrice = 25000;

	private readonly int CongressPrice = 45000;

	private readonly int CommercePrice = 12500;

	private readonly int SocialPrice = 17500;

	private readonly int AirportPrice = 6000;

	private readonly int PortPrice = 15000;

	private readonly int BusPrice = 250;

	private readonly int SubwayPrice = 1500;

	private readonly int ParkPrice = 100;

	private readonly int LargeParkPrice = 900;

	private readonly int marinaPrice = 1750;

	private readonly int ExtraLargeParkPrice = 1500;

	private readonly int StadiumPrice = 75000;

	private readonly int MuseumPrice = 30000;

	private readonly int PoolPrice = 7500;

	private readonly int HoloPrice = 35000;

	private readonly int StationPrice = 750;

	private readonly int HeliPrice = 100;

	private readonly int ConnectionPrice = 250;

	private readonly int PumpingPrice = 2500;

	private readonly int CoalPrice = 12500;

	private readonly int SolarPrice = 9000;

	private readonly int TurbinePrice = 10000;

	private readonly int NuclearPrice = 95000;

	private readonly int FusionPrice = 425000;

	private readonly int CarbonPrice = 22500;

	private readonly int GeneratorsPrice = 1250;

	private readonly int TowerPrice = 400;

	private readonly int ChurchPrice = 4000;

	private readonly int HLM1Price = 600;

	private readonly int HLM2Price = 1800;

	private readonly int HLM3Price = 5200;

	private readonly int FactorySPrice = 2250;

	private readonly int FactoryMPrice = 3750;

	private readonly int MarketPrice = 2100;

	private readonly int ArcoAPrice = 80000;

	private readonly int ArcoBPrice = 160000;

	private readonly int ArcoCPrice = 380000;

	private readonly int MediaPrice = 50000;

	private readonly int EdeniumPrice = 35000;

	private readonly int GulagPrice = 27500;

	private readonly int RehabPrice = 45000;

	private readonly int DronePrice = 75000;

	private readonly int minePrice = 100;

	private int rotated;

	private string _resource = "none";

	private float prevalence;

	private void Update()
	{
		if ((Indexer.Index >= 30 && Indexer.Index <= 37) || (Indexer.Index >= 40 && Indexer.Index <= 47) || Indexer.Index == 25 || Indexer.Index == 16 || Indexer.Index == 15 || Indexer.Index == 18 || Indexer.Index == 19 || Indexer.Index >= 55)
		{
			x2 = bi.x2;
			oldx2 = bi.oldx2;
			z2 = bi.z2;
			oldz2 = bi.oldz2;
			pos = bi.pos;
		}
		if (x2 != oldx2 || z2 != oldz2)
		{
			TooltipSystem.Hide();
			if (Indexer.Index == 30)
			{
				BuildingPlacer4x4(indexer.SchoolPlacer, schoolPrice);
			}
			else if (Indexer.Index == 31)
			{
				BuildingPlacer4x4(indexer.HospitalPlacer, hospitalPrice);
			}
			else if (Indexer.Index == 32)
			{
				BuildingPlacer4x4(indexer.FirePlacer, firePrice);
			}
			else if (Indexer.Index == 33)
			{
				BuildingPlacer4x4(indexer.PolicePlacer, policePrice);
			}
			else if (Indexer.Index == 36)
			{
				BuildingPlacer4x4(indexer.Empty4x, libraryPrice);
			}
			else if (Indexer.Index == 34)
			{
				GovPlacer4x6(indexer.HighSchoolPlacer, highschoolPrice, build: false);
			}
			else if (Indexer.Index == 35)
			{
				BuildingPlacer12x12(indexer.UniversityPlacer, univPrice);
			}
			else if (Indexer.Index == 240 || Indexer.Index == 241 || Indexer.Index == 242)
			{
				BuildingPlacer4x4(indexer.Empty4x, ChurchPrice);
			}
			else if (Indexer.Index == 19)
			{
				PortPlacer12x12(indexer.PortPlacer, PortPrice);
			}
			else if (Indexer.Index == 110)
			{
				BuildingPlacer16x16(indexer.Universal16x, StadiumPrice);
			}
			else if (Indexer.Index == 111)
			{
				BuildingPlacer8x8(indexer.Empty8x, MuseumPrice);
			}
			else if (Indexer.Index == 114)
			{
				BuildingPlacer8x8(indexer.Empty8x, HoloPrice);
			}
			else if (Indexer.Index == 40)
			{
				BuildingPlacer8x8(indexer.HQPlacer, HQPrice);
			}
			else if (Indexer.Index == 41)
			{
				BuildingPlacer4x4(indexer.ImmigrationPlacer, ImmigrationPrice);
			}
			else if (Indexer.Index == 42)
			{
				BuildingPlacer4x4(indexer.BankPlacer, BankPrice);
			}
			else if (Indexer.Index == 43)
			{
				GovPlacer4x6(indexer.CommercePlacer, CommercePrice, build: false);
			}
			else if (Indexer.Index == 44)
			{
				GovPlacer4x6(indexer.SocialPlacer, SocialPrice, build: false);
			}
			else if (Indexer.Index == 83)
			{
				BuildingPlacer8x8(indexer.Media, MediaPrice);
			}
			else if (Indexer.Index == 46)
			{
				BuildingPlacer2x2(indexer.HelipadPlacer, HeliPrice);
			}
			else if (Indexer.Index == 66)
			{
				BuildingPlacer2x2(indexer.Carbon, CarbonPrice);
			}
			else if (Indexer.Index == 47)
			{
				BuildingPlacer8x8(indexer.Congress, CongressPrice);
			}
			else if (Indexer.Index == 37)
			{
				BuildingPlacer8x8(indexer.Empty8x, prisonPrice);
			}
			else if (Indexer.Index == 25)
			{
				MiningPlacer8x8(indexer.Empty8x, minePrice);
			}
			else if (Indexer.Index == 18)
			{
				StationPlacer2x4(indexer.StationPlacer, StationPrice, build: false);
			}
			else if (Indexer.Index == 192)
			{
				ConnectionPlacer2x4(indexer.ConnectionPlacer, ConnectionPrice, build: false);
			}
			else if (Indexer.Index == 16)
			{
				BuildingPlacerAirport(indexer.AirportPlacer, AirportPrice);
			}
			else if (Indexer.Index == 15)
			{
				BusPlacer(indexer.Universal1x, BusPrice);
			}
			else if (Indexer.Index == 191)
			{
				BusPlacer(indexer.Universal1x, SubwayPrice);
			}
			else if (Indexer.Index >= 90 && Indexer.Index <= 97)
			{
				ParkPlacer1x1(indexer.Universal1x, ParkPrice);
			}
			else if ((Indexer.Index >= 98 && Indexer.Index <= 101) || Indexer.Index == 107)
			{
				ParkPlacer3x3(indexer.Universal1x, LargeParkPrice);
			}
			else if (Indexer.Index == 103 || Indexer.Index == 104 || Indexer.Index == 105)
			{
				BuildingPlacer4x4(indexer.Universal1x, ExtraLargeParkPrice);
			}
			else if (Indexer.Index == 55 || Indexer.Index == 56)
			{
				VegetationPlacer();
			}
			else if (Indexer.Index == 58)
			{
				BuildingPlacer6x6(indexer.Turbine, TurbinePrice);
			}
			else if (Indexer.Index == 59)
			{
				BuildingPlacer4x4(indexer.Pumping, PumpingPrice);
			}
			else if (Indexer.Index == 60)
			{
				BuildingPlacer8x8(indexer.CoalPlant, CoalPrice);
			}
			else if (Indexer.Index == 61)
			{
				BuildingPlacer2x2(indexer.Generators, GeneratorsPrice);
			}
			else if (Indexer.Index == 62)
			{
				BuildingPlacer2x2(indexer.WaterTower, TowerPrice);
			}
			else if (Indexer.Index == 63)
			{
				BuildingPlacer4x4(indexer.SolarPlant, SolarPrice);
			}
			else if (Indexer.Index == 64)
			{
				BuildingPlacer8x8(indexer.NuclearPlant, NuclearPrice);
			}
			else if (Indexer.Index == 65)
			{
				BuildingPlacer8x8(indexer.HydrogenPlant, FusionPrice);
			}
			else if (Indexer.Index == 70)
			{
				BuildingPlacer4x4(indexer.HLM1, HLM1Price);
			}
			else if (Indexer.Index == 71)
			{
				BuildingPlacer4x4(indexer.HLM2, HLM2Price);
			}
			else if (Indexer.Index == 72)
			{
				BuildingPlacer4x4(indexer.HLM3, HLM3Price);
			}
			else if (Indexer.Index == 73)
			{
				BuildingPlacer4x4(indexer.Empty4x, FactorySPrice);
			}
			else if (Indexer.Index == 74)
			{
				BuildingPlacer4x4(indexer.Empty4x, FactoryMPrice);
			}
			else if (Indexer.Index == 75)
			{
				BuildingPlacer4x4(indexer.Empty4x, MarketPrice);
			}
			else if (Indexer.Index == 80)
			{
				BuildingPlacer6x6(indexer.ArcoA, ArcoAPrice);
			}
			else if (Indexer.Index == 81)
			{
				BuildingPlacer6x6(indexer.ArcoB, ArcoBPrice);
			}
			else if (Indexer.Index == 82)
			{
				BuildingPlacer6x6(indexer.ArcoC, ArcoCPrice);
			}
			else if (Indexer.Index == 67)
			{
				BuildingPlacer6x6(indexer.Empty6x, GulagPrice);
			}
			else if (Indexer.Index == 68)
			{
				BuildingPlacer6x6(indexer.Empty6x, RehabPrice);
			}
			else if (Indexer.Index == 112)
			{
				BuildingPlacer6x6(indexer.Empty6x, PoolPrice);
			}
			else if (Indexer.Index == 69)
			{
				BuildingPlacer4x4(indexer.Drone, DronePrice);
			}
			else if (Indexer.Index == 84)
			{
				BuildingPlacer8x8(indexer.Empty8x, 0);
			}
			else if (Indexer.Index == 85)
			{
				BuildingPlacer8x8(indexer.Empty8x, EdeniumPrice);
			}
			else if (Indexer.Index == 102)
			{
				MarinaPlacer6x6(indexer.Marina, marinaPrice);
			}
			else if (Indexer.Index == 106)
			{
				MarinaPlacer6x6(indexer.Pier, 3500);
			}
			else if (Indexer.Index == 113)
			{
				BeachPlacer3x3(indexer.Universal1x, 100);
			}
		}
		if (Input.GetMouseButtonUp(0) && isFree && isConnected)
		{
			if (EventSystem.current.IsPointerOverGameObject())
			{
				return;
			}
			if ((Indexer.Index >= 30 && Indexer.Index <= 33) || Indexer.Index == 63 || Indexer.Index == 36 || Indexer.Index == 69 || Indexer.Index == 240 || Indexer.Index == 241 || Indexer.Index == 242)
			{
				ServicePlopper4x4();
				indexer.HideAll();
			}
			else if (Indexer.Index == 34)
			{
				GovPlacer4x6(indexer.HighSchoolPlacer, highschoolPrice, build: true);
				indexer.HideAll();
			}
			else if (Indexer.Index == 35)
			{
				GovPlopper12x12();
				indexer.HideAll();
			}
			else if (Indexer.Index == 110)
			{
				ServicePlopper16x16();
				indexer.HideAll();
			}
			else if (Indexer.Index == 40 || Indexer.Index == 60 || Indexer.Index == 83 || Indexer.Index == 64 || Indexer.Index == 65 || Indexer.Index == 47 || Indexer.Index == 85 || Indexer.Index == 37 || Indexer.Index == 111 || Indexer.Index == 114)
			{
				GovPlopper8x8();
				indexer.HideAll();
			}
			else if (Indexer.Index == 41 || Indexer.Index == 42)
			{
				GovPlopper4x4();
				indexer.HideAll();
			}
			else if (Indexer.Index == 43)
			{
				GovPlacer4x6(indexer.CommercePlacer, CommercePrice, build: true);
				indexer.HideAll();
			}
			else if (Indexer.Index == 44)
			{
				GovPlacer4x6(indexer.SocialPlacer, SocialPrice, build: true);
				indexer.HideAll();
			}
			else if (Indexer.Index == 59)
			{
				ServicePlopper4x4();
				indexer.HideAll();
			}
			else if (Indexer.Index == 46)
			{
				GovPlopper2x2();
				indexer.HideAll();
			}
			else if (Indexer.Index == 61 || Indexer.Index == 62 || Indexer.Index == 66)
			{
				GovPlopper2x2();
			}
			else if (Indexer.Index == 18)
			{
				StationPlacer2x4(indexer.StationPlacer, StationPrice, build: true);
			}
			else if (Indexer.Index == 192)
			{
				ConnectionPlacer2x4(indexer.ConnectionPlacer, ConnectionPrice, build: true);
				indexer.HideAll();
			}
			else if (Indexer.Index == 16)
			{
				AirportPlopper14x8();
				indexer.HideAll();
			}
			else if (Indexer.Index == 15)
			{
				BusPlopper();
			}
			else if (Indexer.Index == 191)
			{
				SubwayPlopper();
			}
			else if (Indexer.Index >= 90 && Indexer.Index <= 97)
			{
				ParkPlopper1x1();
			}
			else if ((Indexer.Index >= 98 && Indexer.Index <= 101) || Indexer.Index == 107 || Indexer.Index == 113)
			{
				ServicePlopper3x3();
			}
			else if (Indexer.Index >= 70 && Indexer.Index <= 72)
			{
				HLMPlopper();
			}
			else if (Indexer.Index == 73 || Indexer.Index == 74)
			{
				FactoryPlopper();
			}
			else if (Indexer.Index == 75)
			{
				RetailPlopper();
			}
			else if (Indexer.Index >= 80 && Indexer.Index <= 82)
			{
				SpecialPlopper6x6();
			}
			else if (Indexer.Index == 67 || Indexer.Index == 68 || Indexer.Index == 112 || Indexer.Index == 58)
			{
				SpecialPlopper6x6();
			}
			else if (Indexer.Index == 84)
			{
				RedPlopper8x8();
			}
			else if (Indexer.Index == 102 || Indexer.Index == 106)
			{
				MarinaPlopper6x6();
			}
			else if (Indexer.Index == 103 || Indexer.Index == 104 || Indexer.Index == 105)
			{
				ServicePlopper4x4();
			}
			else if (Indexer.Index == 25)
			{
				MinePlopper8x8();
			}
			else if (Indexer.Index == 19)
			{
				PortPlopper12x12();
			}
			else if (Indexer.Index == 55 || Indexer.Index == 56)
			{
				VegetationPlopper();
			}
		}
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			rotated += 90;
			if (rotated >= 360)
			{
				rotated = 0;
			}
		}
	}

	private void BuildingPlacer4x4(GameObject placer, int Cost)
	{
		oldx2 = x2;
		oldz2 = z2;
		Center = new Vector3(pos.x - 0.5f, pos.y + 0.515f, pos.z + -0.5f);
		isFree = true;
		isFlat = true;
		float num = pos.y;
		float num2 = pos.y;
		for (int i = (int)pos.z - 2; i <= (int)pos.z + 1; i++)
		{
			for (int j = (int)pos.x - 2; j <= (int)pos.x + 1; j++)
			{
				BlockData ifNotNull = bi.world.GetIfNotNull(j, i);
				if (ifNotNull == null)
				{
					isFree = false;
					isConnected = false;
					return;
				}
				if (ifNotNull.h > num)
				{
					num = ifNotNull.h;
				}
				else if (ifNotNull.h < num2)
				{
					num2 = ifNotNull.h;
				}
				if (ifNotNull.t == BlockData.Type.ROAD || ifNotNull.t == BlockData.Type.BUILDING || ifNotNull.h < 29.5f)
				{
					isFree = false;
				}
			}
		}
		if (Mathf.Abs(num - num2) > 2f)
		{
			isFlat = false;
		}
		Center.y = num + 0.515f;
		isConnected = false;
		Orientation = 0;
		if (isFree)
		{
			RoadCreator access = roading.GetAccess((int)pos.x - 3, (int)pos.z);
			RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 2);
			RoadCreator access3 = roading.GetAccess((int)pos.x + 2, (int)pos.z);
			RoadCreator access4 = roading.GetAccess((int)pos.x, (int)pos.z - 3);
			if (access != null)
			{
				Orientation = 270;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x - 3, (int)pos.z);
			}
			else if (access2 != null)
			{
				Orientation = 0;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 2);
			}
			else if (access3 != null)
			{
				Orientation = 90;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x + 2, (int)pos.z);
			}
			else if (access4 != null)
			{
				Orientation = 180;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 3);
			}
			if (Indexer.Index == 103 || Indexer.Index == 104 || Indexer.Index == 105)
			{
				isConnected = true;
			}
		}
		costHelper.SetCost(Cost);
		if (!costHelper.CanAfford)
		{
			isFree = false;
			TooltipSystem.Show("", "NoMo");
		}
		else if (!isConnected && isFree)
		{
			TooltipSystem.Show("", "No Road Access");
		}
		else if (!isFlat)
		{
			TooltipSystem.Show("", "Too Steep");
		}
		if (isFree && isConnected && isFlat)
		{
			placer.transform.position = Center;
			placer.transform.eulerAngles = new Vector3(0f, Orientation, 0f);
			PlacerMaterial.SetColor("_Color", green);
		}
		else
		{
			PlacerMaterial.SetColor("_Color", red);
			placer.transform.position = Center;
		}
	}

	private void BuildingPlacer6x6(GameObject placer, int Cost)
	{
		oldx2 = x2;
		oldz2 = z2;
		Center = new Vector3(pos.x - 0.5f, pos.y + 0.515f, pos.z + -0.5f);
		isFree = true;
		for (int i = (int)pos.z - 3; i <= (int)pos.z + 2; i++)
		{
			for (int j = (int)pos.x - 3; j <= (int)pos.x + 2; j++)
			{
				if (i < 0 || i > bi.world.blockData.Length - 1 || j < 0 || j > bi.world.blockData.Length - 1)
				{
					isFree = false;
					isConnected = false;
					return;
				}
				BlockData ifNotNull = bi.world.GetIfNotNull(j, i);
				if (ifNotNull == null)
				{
					isFree = false;
					isConnected = false;
					return;
				}
				if (ifNotNull.t == BlockData.Type.ROAD || ifNotNull.t == BlockData.Type.BUILDING || ifNotNull.h < 29.5f)
				{
					isFree = false;
				}
			}
		}
		isConnected = false;
		Orientation = 0;
		if (isFree)
		{
			RoadCreator access = roading.GetAccess((int)pos.x - 4, (int)pos.z);
			RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 3);
			RoadCreator access3 = roading.GetAccess((int)pos.x + 3, (int)pos.z);
			RoadCreator access4 = roading.GetAccess((int)pos.x, (int)pos.z - 4);
			if (access != null)
			{
				Orientation = 270;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x - 4, (int)pos.z);
			}
			else if (access2 != null)
			{
				Orientation = 0;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 3);
			}
			else if (access3 != null)
			{
				Orientation = 90;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x + 3, (int)pos.z);
			}
			else if (access4 != null)
			{
				Orientation = 180;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 4);
			}
		}
		costHelper.SetCost(Cost);
		if (!costHelper.CanAfford)
		{
			isFree = false;
			TooltipSystem.Show("", "NoMo");
		}
		else if (!isConnected && isFree)
		{
			TooltipSystem.Show("", "No Road Access");
		}
		if (isFree && isConnected)
		{
			placer.transform.position = Center;
			placer.transform.eulerAngles = new Vector3(0f, Orientation - 90, 0f);
			PlacerMaterial.SetColor("_Color", green);
		}
		else
		{
			PlacerMaterial.SetColor("_Color", red);
			placer.transform.position = Center;
		}
	}

	private void MarinaPlacer6x6(GameObject placer, int Cost)
	{
		oldx2 = x2;
		oldz2 = z2;
		Center = new Vector3(pos.x - 0.5f, pos.y + 0.515f, pos.z + -0.5f);
		isFree = true;
		for (int i = (int)pos.z - 3; i <= (int)pos.z + 2; i++)
		{
			for (int j = (int)pos.x - 3; j <= (int)pos.x + 2; j++)
			{
				if (i < 0 || i > bi.world.blockData.Length - 1 || j < 0 || j > bi.world.blockData.Length - 1)
				{
					isFree = false;
					isConnected = false;
					return;
				}
				BlockData ifNotNull = bi.world.GetIfNotNull(j, i);
				if (ifNotNull == null)
				{
					isFree = false;
					isConnected = false;
					return;
				}
				if (ifNotNull.t == BlockData.Type.ROAD || ifNotNull.t == BlockData.Type.BUILDING)
				{
					isFree = false;
				}
			}
		}
		for (int k = (int)pos.z - 2; k <= (int)pos.z + 1; k++)
		{
			for (int l = (int)pos.x - 2; l <= (int)pos.x + 1; l++)
			{
				if (bi.world.GetIfNotNull(l, k).h >= 29.75f)
				{
					isFree = false;
				}
			}
		}
		isConnected = false;
		Orientation = 0;
		if (isFree)
		{
			BlockData bounded = bi.world.GetBounded((int)pos.x - 4, (int)pos.z);
			BlockData bounded2 = bi.world.GetBounded((int)pos.x, (int)pos.z + 3);
			BlockData bounded3 = bi.world.GetBounded((int)pos.x + 3, (int)pos.z);
			BlockData bounded4 = bi.world.GetBounded((int)pos.x, (int)pos.z - 4);
			if (bounded.h == 29.75f && bounded3.h < 29.75f)
			{
				Orientation = 270;
				isConnected = true;
				roadAccess = bounded;
			}
			else if (bounded2.h == 29.75f && bounded4.h < 29.75f)
			{
				Orientation = 0;
				isConnected = true;
				roadAccess = bounded2;
			}
			else if (bounded3.h == 29.75f && bounded.h < 29.75f)
			{
				Orientation = 90;
				isConnected = true;
				roadAccess = bounded3;
			}
			else if (bounded4.h == 29.75f && bounded2.h < 29.75f)
			{
				Orientation = 180;
				isConnected = true;
				roadAccess = bounded4;
			}
		}
		costHelper.SetCost(Cost);
		if (!costHelper.CanAfford)
		{
			isFree = false;
			TooltipSystem.Show("", "NoMo");
		}
		Vector3 position = ((!isConnected) ? new Vector3(Center.x, Center.y + 1.002f, Center.z) : new Vector3(Center.x, roadAccess.h + 0.505f, Center.z));
		if (isFree && isConnected)
		{
			placer.transform.position = position;
			placer.transform.eulerAngles = new Vector3(0f, Orientation - 90, 0f);
			PlacerMaterial.SetColor("_Color", green);
		}
		else
		{
			PlacerMaterial.SetColor("_Color", red);
			placer.transform.position = position;
		}
	}

	private void PortPlacer12x12(GameObject placer, int Cost)
	{
		oldx2 = x2;
		oldz2 = z2;
		Center = new Vector3(pos.x - 0.5f, pos.y + 0.515f, pos.z + -0.5f);
		Orientation = rotated;
		bool flag = false;
		isFree = true;
		for (int i = (int)pos.z - 6; i <= (int)pos.z + 5; i++)
		{
			for (int j = (int)pos.x - 6; j <= (int)pos.x + 5; j++)
			{
				if (i < 0 || i > bi.world.blockData.Length - 1 || j < 0 || j > bi.world.blockData.Length - 1)
				{
					isFree = false;
					isConnected = false;
					return;
				}
				BlockData ifNotNull = bi.world.GetIfNotNull(j, i);
				if (ifNotNull == null)
				{
					isFree = false;
					isConnected = false;
					return;
				}
				if (ifNotNull.t == BlockData.Type.ROAD || ifNotNull.t == BlockData.Type.BUILDING)
				{
					isFree = false;
				}
			}
		}
		BlockData bounded = bi.world.GetBounded((int)pos.x - 7, (int)pos.z);
		BlockData bounded2 = bi.world.GetBounded((int)pos.x, (int)pos.z + 6);
		BlockData bounded3 = bi.world.GetBounded((int)pos.x + 6, (int)pos.z);
		BlockData bounded4 = bi.world.GetBounded((int)pos.x, (int)pos.z - 7);
		isConnected = false;
		roadAccess = null;
		bool flag2 = false;
		if (Orientation == 270 && bounded.h >= 29.75f && bounded3.h < 29.75f)
		{
			flag2 = true;
			roadAccess = bounded;
		}
		else if (Orientation == 0 && bounded2.h >= 29.75f && bounded4.h < 29.75f)
		{
			flag2 = true;
			roadAccess = bounded2;
		}
		else if (Orientation == 90 && bounded3.h >= 29.75f && bounded.h < 29.75f)
		{
			flag2 = true;
			roadAccess = bounded3;
		}
		else if (Orientation == 180 && bounded4.h >= 29.75f && bounded2.h < 29.75f)
		{
			flag2 = true;
			roadAccess = bounded4;
		}
		if (bi.world.GetBounded(0, (int)pos.z).h < 29.75f || bi.world.GetBounded(World.worldSize * World.chunkSize, (int)pos.z).h < 29.75f || bi.world.GetBounded((int)pos.x, 0).h < 29.75f || bi.world.GetBounded((int)pos.x, World.worldSize * World.chunkSize).h < 29.75f)
		{
			flag = true;
		}
		if (Orientation == 270 && bi.world.GetBounded((int)pos.x + 14, (int)pos.z).h >= 29.75f)
		{
			flag = false;
		}
		else if (Orientation == 0 && bi.world.GetBounded((int)pos.x - 14, (int)pos.z).h >= 29.75f)
		{
			flag = false;
		}
		else if (Orientation == 90 && bi.world.GetBounded((int)pos.x, (int)pos.z - 14).h >= 29.75f)
		{
			flag = false;
		}
		else if (Orientation == 180 && bi.world.GetBounded((int)pos.x, (int)pos.z + 14).h >= 29.75f)
		{
			flag = false;
		}
		if (roadAccess != null && roadAccess.r)
		{
			isConnected = true;
		}
		costHelper.SetCost(Cost);
		if (!isConnected || !isFree)
		{
			TooltipSystem.Show("PushTab");
		}
		else if (!costHelper.CanAfford)
		{
			isFree = false;
			TooltipSystem.Show("", "NoMo");
		}
		else if (!flag)
		{
			isFree = false;
			TooltipSystem.Show("", "PortReq");
		}
		else
		{
			TooltipSystem.Hide();
		}
		Vector3 position = ((!isConnected) ? new Vector3(Center.x, Center.y + 1.002f, Center.z) : new Vector3(Center.x, roadAccess.h + 0.505f, Center.z));
		placer.transform.eulerAngles = new Vector3(0f, Orientation, 0f);
		placer.transform.position = position;
		if ((isFree && isConnected) & flag2)
		{
			PlacerMaterial.SetColor("_Color", green);
		}
		else
		{
			PlacerMaterial.SetColor("_Color", red);
		}
	}

	private void BuildingPlacer2x2(GameObject placer, int Cost)
	{
		oldx2 = x2;
		oldz2 = z2;
		Center = new Vector3(pos.x - 0.5f, pos.y + 0.515f, pos.z + -0.5f);
		isFree = true;
		for (int i = (int)pos.z - 1; i <= (int)pos.z; i++)
		{
			for (int j = (int)pos.x - 1; j <= (int)pos.x; j++)
			{
				BlockData ifNotNull = bi.world.GetIfNotNull(j, i);
				if (ifNotNull == null)
				{
					isFree = false;
					isConnected = false;
					return;
				}
				if (ifNotNull.t == BlockData.Type.ROAD || ifNotNull.t == BlockData.Type.BUILDING || ifNotNull.h < 29.5f)
				{
					isFree = false;
				}
			}
		}
		isConnected = false;
		Orientation = 0;
		if (isFree)
		{
			RoadCreator access = roading.GetAccess((int)pos.x - 2, (int)pos.z);
			RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 1);
			RoadCreator access3 = roading.GetAccess((int)pos.x + 1, (int)pos.z);
			RoadCreator access4 = roading.GetAccess((int)pos.x, (int)pos.z - 2);
			if (access != null)
			{
				Orientation = 270;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x - 2, (int)pos.z);
			}
			else if (access2 != null)
			{
				Orientation = 0;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 1);
			}
			else if (access3 != null)
			{
				Orientation = 90;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x + 1, (int)pos.z);
			}
			else if (access4 != null)
			{
				Orientation = 180;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 2);
			}
		}
		if (roadAccess != null)
		{
			RoadCreator ifNotNull2 = roading.GetIfNotNull(roadAccess.wP.x, 1, roadAccess.wP.y);
			if (ifNotNull2 != null)
			{
				if (ifNotNull2.rType != RoadCreator.RoadType.AVENUE && ifNotNull2.rType != RoadCreator.RoadType.ROAD)
				{
					isConnected = false;
				}
			}
			else
			{
				isConnected = false;
			}
		}
		costHelper.SetCost(Cost);
		if (!costHelper.CanAfford)
		{
			isFree = false;
			TooltipSystem.Show("", "NoMo");
		}
		else if (!isConnected && isFree)
		{
			TooltipSystem.Show("", "No Road Access");
		}
		if (isFree && isConnected)
		{
			placer.transform.position = Center;
			placer.transform.eulerAngles = new Vector3(0f, Orientation, 0f);
			PlacerMaterial.SetColor("_Color", green);
		}
		else
		{
			PlacerMaterial.SetColor("_Color", red);
			placer.transform.position = Center;
		}
	}

	private void StationPlacer2x4(GameObject placer, int Cost, bool build)
	{
		oldx2 = x2;
		oldz2 = z2;
		Center = new Vector3(pos.x - 0.5f, pos.y + 0.515f, pos.z - 0.5f);
		isConnected = false;
		Orientation = 0;
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		BlockData ifNotNull = bi.world.GetIfNotNull((int)pos.x, (int)pos.z);
		if (ifNotNull == null)
		{
			isFree = false;
			isConnected = false;
			return;
		}
		indexer.gridTrain.transform.position = new Vector3(ifNotNull.wP.x, 100f, ifNotNull.wP.y);
		RoadCreator railIfNotNull = roading.GetRailIfNotNull((int)pos.x, 1, (int)pos.z);
		if (railIfNotNull == null)
		{
			PlacerMaterial.SetColor("_Color", red);
			placer.transform.position = Center;
			TooltipSystem.Show("", "OnRails");
			isFree = false;
			isConnected = false;
			return;
		}
		if (railIfNotNull.rType == RoadCreator.RoadType.RAIL && railIfNotNull.tile == "Rail_Straight")
		{
			if (railIfNotNull.Bitwise == 5)
			{
				RoadCreator roadCreator = roading.rG[(int)railIfNotNull.wPos.x, 1, (int)railIfNotNull.wPos.z - 2];
				RoadCreator roadCreator2 = roading.rG[(int)railIfNotNull.wPos.x, 1, (int)railIfNotNull.wPos.z - 1];
				RoadCreator roadCreator3 = roading.rG[(int)railIfNotNull.wPos.x, 1, (int)railIfNotNull.wPos.z + 1];
				if (roadCreator == null)
				{
					isFree = false;
					isConnected = false;
					return;
				}
				if (roadCreator.tile == "Rail_Straight" && roadCreator2.tile == "Rail_Straight" && roadCreator3.tile == "Rail_Straight")
				{
					if (bi.mousePos.x < railIfNotNull.wPos.x)
					{
						Orientation = 90;
						isConnected = true;
						roadAccess = ifNotNull;
						num = (int)railIfNotNull.wPos.x - 4;
						num2 = (int)railIfNotNull.wPos.x - 1;
						num3 = (int)railIfNotNull.wPos.z - 1;
						num4 = (int)railIfNotNull.wPos.z;
						Center = new Vector3(pos.x + 0.5f, pos.y + 0.515f, pos.z - 0.5f);
					}
					else
					{
						Orientation = 270;
						isConnected = true;
						roadAccess = ifNotNull;
						num = (int)railIfNotNull.wPos.x + 1;
						num2 = (int)railIfNotNull.wPos.x + 4;
						num3 = (int)railIfNotNull.wPos.z - 1;
						num4 = (int)railIfNotNull.wPos.z;
						Center = new Vector3(pos.x - 0.5f, pos.y + 0.515f, pos.z - 0.5f);
					}
				}
				else
				{
					TooltipSystem.Show("", "TrackNotFlat");
				}
			}
			else if (railIfNotNull.Bitwise == 10)
			{
				RoadCreator roadCreator4 = roading.rG[(int)railIfNotNull.wPos.x + 2, 1, (int)railIfNotNull.wPos.z];
				RoadCreator roadCreator5 = roading.rG[(int)railIfNotNull.wPos.x - 1, 1, (int)railIfNotNull.wPos.z];
				RoadCreator roadCreator6 = roading.rG[(int)railIfNotNull.wPos.x + 1, 1, (int)railIfNotNull.wPos.z];
				if (roadCreator4 == null)
				{
					isFree = false;
					isConnected = false;
					return;
				}
				if (roadCreator4.tile == "Rail_Straight" && roadCreator5.tile == "Rail_Straight" && roadCreator6.tile == "Rail_Straight")
				{
					if (bi.mousePos.z < railIfNotNull.wPos.z)
					{
						Orientation = 0;
						isConnected = true;
						roadAccess = ifNotNull;
						num = (int)railIfNotNull.wPos.x;
						num2 = (int)railIfNotNull.wPos.x + 1;
						num3 = (int)railIfNotNull.wPos.z - 4;
						num4 = (int)railIfNotNull.wPos.z - 1;
						Center = new Vector3(pos.x + 0.5f, pos.y + 0.515f, pos.z + 0.5f);
					}
					else
					{
						Orientation = 180;
						isConnected = true;
						roadAccess = ifNotNull;
						num = (int)railIfNotNull.wPos.x;
						num2 = (int)railIfNotNull.wPos.x + 1;
						num3 = (int)railIfNotNull.wPos.z + 1;
						num4 = (int)railIfNotNull.wPos.z + 4;
						Center = new Vector3(pos.x + 0.5f, pos.y + 0.515f, pos.z - 0.5f);
					}
				}
				else
				{
					TooltipSystem.Show("", "TrackNotFlat");
				}
			}
		}
		isFree = true;
		if (isConnected)
		{
			for (int i = num3; i <= num4; i++)
			{
				for (int j = num; j <= num2; j++)
				{
					if (i < 0 || i > bi.world.blockData.Length - 1 || j < 0 || j > bi.world.blockData.Length - 1)
					{
						isFree = false;
						isConnected = false;
						return;
					}
					BlockData blockData = bi.world.blockData[j, i];
					if (blockData.t == BlockData.Type.ROAD || blockData.t == BlockData.Type.BUILDING || blockData.h < 29.5f)
					{
						isFree = false;
						break;
					}
				}
			}
		}
		costHelper.SetCost(Cost);
		if (!costHelper.CanAfford)
		{
			isFree = false;
			TooltipSystem.Show("", "NoMo");
		}
		if (isFree && isConnected)
		{
			placer.transform.position = Center;
			placer.transform.eulerAngles = new Vector3(0f, Orientation, 0f);
			PlacerMaterial.SetColor("_Color", green);
			roadAccess = ifNotNull;
			TooltipSystem.Hide();
		}
		else
		{
			PlacerMaterial.SetColor("_Color", red);
			placer.transform.position = Center;
			TooltipSystem.Show("", "TrackNotFlat");
		}
		if (!build)
		{
			return;
		}
		GameObject gameObject = Object.Instantiate(serviceUnit);
		ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
		((GameObject)Object.Instantiate(Resources.Load("Services/Train_Station"))).transform.parent = gameObject.transform;
		Center.y = roadAccess.h + 0.505f;
		gameObject.transform.position = Center;
		gameObject.name = railIfNotNull.name;
		component.worldPos = Center;
		component.Orientation = Orientation;
		component.Anchor = new Vector3(pos.x - 0.5f, pos.y, pos.z - 0.5f);
		component.TreesParent = new GameObject("TreesParent");
		component.TreesParent.transform.parent = component.gameObject.transform;
		roading.w.Services.Add(component);
		component.Capacity = 4000;
		component.Maintenance = 125f;
		component.MinEmployees = 8;
		component.MaxEmployees = 8;
		component.FixedEmployees = 4;
		component.HiredEmployees = 8;
		component.type = ServiceCreator.Type.TrainStation;
		component.MaxStage = 1;
		costHelper.PayForIt();
		for (int k = num3; k <= num4; k++)
		{
			for (int l = num; l <= num2; l++)
			{
				BlockData blockData2 = bi.world.blockData[l, k];
				blockData2.t = BlockData.Type.BUILDING;
				blockData2.sO = component;
				blockData2.FlatHeight(roadAccess.h);
				blockData2.b = true;
				component.blockPos.Add(new Vector2Int(l, k));
				CheckChunks(blockData2);
			}
		}
		gameObject.transform.Rotate(0f, Orientation, 0f, Space.Self);
		gameObject.transform.parent = ServiceBuildings.transform;
		railIfNotNull.stopType = RoadCreator.StopType.TRAIN;
		railIfNotNull.StopCapacity = 100000;
		railIfNotNull.maintenance = 125f;
		roading.trainStops.Add(railIfNotNull);
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
		Indexer.Index = 0;
		sm.Play("PlopBuild");
		sm.Play("TrainStation");
	}

	private void GovPlacer4x6(GameObject placer, int Cost, bool build)
	{
		oldx2 = x2;
		oldz2 = z2;
		if (Orientation == 90 || Orientation == 270)
		{
			Center = new Vector3(pos.x, pos.y + 0.515f, pos.z - 0.5f);
		}
		else
		{
			Center = new Vector3(pos.x - 0.5f, pos.y + 0.515f, pos.z);
		}
		isConnected = false;
		Orientation = 0;
		BlockData ifNotNull = bi.world.GetIfNotNull((int)pos.x, (int)pos.z);
		if (ifNotNull == null)
		{
			isFree = false;
			isConnected = false;
			return;
		}
		RoadCreator access = roading.GetAccess((int)pos.x + 1, (int)pos.z);
		RoadCreator access2 = roading.GetAccess((int)pos.x - 1, (int)pos.z);
		RoadCreator access3 = roading.GetAccess((int)pos.x, (int)pos.z + 1);
		RoadCreator access4 = roading.GetAccess((int)pos.x, (int)pos.z - 1);
		if (access == null && access2 == null && access3 == null && access4 == null)
		{
			PlacerMaterial.SetColor("_Color", red);
			placer.transform.position = Center;
			isFree = false;
			isConnected = false;
			return;
		}
		int num;
		int num2;
		int num3;
		int num4;
		if (access != null)
		{
			Orientation = 90;
			isConnected = true;
			roadAccess = bi.world.GetIfNotNull((int)pos.x + 1, (int)pos.z);
			num = ifNotNull.wP.x - 3;
			num2 = ifNotNull.wP.x;
			num3 = ifNotNull.wP.y - 2;
			num4 = ifNotNull.wP.y + 3;
			Center = new Vector3(pos.x, pos.y + 0.515f, pos.z - 0.5f);
		}
		else if (access2 != null)
		{
			Orientation = 270;
			isConnected = true;
			roadAccess = bi.world.GetIfNotNull((int)pos.x - 1, (int)pos.z);
			num = ifNotNull.wP.x;
			num2 = ifNotNull.wP.x + 3;
			num3 = ifNotNull.wP.y - 2;
			num4 = ifNotNull.wP.y + 3;
			Center = new Vector3(pos.x, pos.y + 0.515f, pos.z + 1.5f);
		}
		else if (access3 != null)
		{
			Orientation = 0;
			isConnected = true;
			roadAccess = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 1);
			num = ifNotNull.wP.x - 2;
			num2 = ifNotNull.wP.x + 3;
			num3 = ifNotNull.wP.y - 3;
			num4 = ifNotNull.wP.y;
			Center = new Vector3(pos.x + 1.5f, pos.y + 0.515f, pos.z);
		}
		else
		{
			Orientation = 180;
			isConnected = true;
			roadAccess = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 1);
			num = ifNotNull.wP.x - 2;
			num2 = ifNotNull.wP.x + 3;
			num3 = ifNotNull.wP.y;
			num4 = ifNotNull.wP.y + 3;
			Center = new Vector3(pos.x - 0.5f, pos.y + 0.515f, pos.z);
		}
		if (roadAccess != null)
		{
			RoadCreator ifNotNull2 = roading.GetIfNotNull(roadAccess.wP.x, 1, roadAccess.wP.y);
			if (ifNotNull2 != null)
			{
				if (ifNotNull2.rType != RoadCreator.RoadType.AVENUE && ifNotNull2.rType != RoadCreator.RoadType.ROAD)
				{
					isConnected = false;
				}
			}
			else
			{
				isConnected = false;
			}
		}
		isFree = true;
		float num5 = pos.y;
		float num6 = pos.y;
		isFlat = true;
		if (isConnected)
		{
			for (int i = num3; i <= num4; i++)
			{
				for (int j = num; j <= num2; j++)
				{
					if (i < 0 || i > bi.world.blockData.Length - 1 || j < 0 || j > bi.world.blockData.Length - 1)
					{
						isFree = false;
						isConnected = false;
						return;
					}
					BlockData blockData = bi.world.blockData[j, i];
					if (blockData.t == BlockData.Type.ROAD || blockData.t == BlockData.Type.BUILDING || blockData.h < 29.5f)
					{
						isFree = false;
						isConnected = false;
						break;
					}
					if (blockData.h > num5)
					{
						num5 = blockData.h;
					}
					else if (blockData.h < num6)
					{
						num6 = blockData.h;
					}
				}
			}
		}
		if (Mathf.Abs(num5 - num6) > 2f)
		{
			isFlat = false;
		}
		Center.y = num5 + 0.515f;
		costHelper.SetCost(Cost);
		if (!costHelper.CanAfford)
		{
			isFree = false;
			TooltipSystem.Show("", "NoMo");
		}
		else if (!isConnected && isFree)
		{
			TooltipSystem.Show("", "No Road Access");
		}
		else if (!isFlat)
		{
			TooltipSystem.Show("", "Too Steep");
		}
		if (isFree && isConnected && isFlat)
		{
			placer.transform.position = Center;
			placer.transform.eulerAngles = new Vector3(0f, Orientation, 0f);
			PlacerMaterial.SetColor("_Color", green);
		}
		else
		{
			PlacerMaterial.SetColor("_Color", red);
			placer.transform.position = Center;
		}
		if (!build)
		{
			return;
		}
		string path = "none";
		if (Indexer.Index == 43)
		{
			path = "Government/GovCommerce";
		}
		else if (Indexer.Index == 44)
		{
			path = "Government/GovSocial";
		}
		else if (Indexer.Index == 34)
		{
			path = "Services/HighSchool_Evo1";
		}
		GameObject gameObject = Object.Instantiate(serviceUnit);
		ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
		((GameObject)Object.Instantiate(Resources.Load(path))).transform.parent = gameObject.transform;
		Center.y = roadAccess.h + 0.505f;
		gameObject.transform.position = Center;
		gameObject.name = pos.x + "_gov_" + pos.z;
		component.worldPos = Center;
		component.Orientation = Orientation;
		component.Anchor = Center;
		component.TreesParent = new GameObject("TreesParent");
		component.TreesParent.transform.parent = component.gameObject.transform;
		roading.w.Services.Add(component);
		component.Entry = roadAccess.wP;
		if (Indexer.Index == 43)
		{
			component.MinEmployees = 0;
			component.MaxEmployees = 130;
			component.HiredEmployees = 0;
			component.Capacity = 250;
			component.Maintenance = 0f;
			component.type = ServiceCreator.Type.Government;
			component.MaxStage = 1;
			component.typeName = "Commsc";
			DataRegion.alreadyBuilt[2] = true;
			costHelper.PayForIt();
			sm.Play("Gov");
			B_EcoPolicies.interactable = true;
			indexer.B_Comm.interactable = false;
			messages.BlueBox.gameObject.SetActive(value: false);
		}
		else if (Indexer.Index == 44)
		{
			component.MinEmployees = 0;
			component.MaxEmployees = 130;
			component.HiredEmployees = 0;
			component.Capacity = 250;
			component.Maintenance = 0f;
			component.type = ServiceCreator.Type.Government;
			component.MaxStage = 1;
			component.typeName = "Socsc";
			DataRegion.alreadyBuilt[3] = true;
			costHelper.PayForIt();
			sm.Play("Gov");
			B_SocialPolicies.interactable = true;
			indexer.B_Social.interactable = false;
			messages.BlueBox.gameObject.SetActive(value: false);
		}
		else if (Indexer.Index == 34)
		{
			component.Capacity = 650;
			component.Maintenance = 60f;
			component.MinEmployees = 20;
			component.MaxEmployees = 30;
			component.FixedEmployees = 10;
			component.HiredEmployees = 25;
			component.type = ServiceCreator.Type.School;
			component.typeName = "H";
			component.MaxStage = 2;
			costHelper.PayForIt();
			sm.Play("HighSchool");
		}
		bi.world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
		costHelper.PayForIt();
		for (int k = num3; k <= num4; k++)
		{
			for (int l = num; l <= num2; l++)
			{
				BlockData blockData2 = bi.world.blockData[l, k];
				blockData2.t = BlockData.Type.BUILDING;
				blockData2.sO = component;
				blockData2.FlatHeight(roadAccess.h);
				blockData2.b = true;
				component.blockPos.Add(new Vector2Int(l, k));
				CheckChunks(blockData2);
			}
		}
		if (Indexer.Index == 43)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 0.75f, component.Anchor.y, component.Anchor.z + 0.28f));
			component.CustomSpawnCar(new Vector2(-0.75f, 0.25f), 90);
			component.CustomSpawnCar(new Vector2(-1.6f, 0.25f), 90);
			component.CustomSpawnCar(new Vector2(-2.4f, 0.25f), 90);
			component.CustomSpawnCar(new Vector2(-3.2f, 0.25f), 90);
		}
		else if (Indexer.Index == 44)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 1.59f, component.Anchor.y, component.Anchor.z - 2.45f));
			component.CustomSpawnCar(new Vector2(1.5f, 0.25f), 90);
			component.CustomSpawnCar(new Vector2(0.6f, 0.25f), 90);
			component.CustomSpawnCar(new Vector2(-0.3f, 0.25f), 90);
			component.CustomSpawnCar(new Vector2(-1.2f, 0.25f), 90);
			component.CustomSpawnCar(new Vector2(-2f, 0.25f), 90);
		}
		else if (Indexer.Index == 34)
		{
			component.SpawnTree(new Vector3(component.Anchor.x - 1f, component.Anchor.y, component.Anchor.z - 0.5f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.53f, component.Anchor.y, component.Anchor.z + 0.05f));
		}
		gameObject.transform.Rotate(0f, Orientation, 0f, Space.Self);
		gameObject.transform.parent = ServiceBuildings.transform;
		component.CreateVehicles(bi.world);
		component.CreateTrees(bi.world);
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
		Indexer.Index = 0;
		sm.Play("PlopBuild");
	}

	private void ConnectionPlacer2x4(GameObject placer, int Cost, bool build)
	{
		oldx2 = x2;
		oldz2 = z2;
		Center = new Vector3(pos.x, pos.y + 0.515f, pos.z);
		isConnected = false;
		Orientation = 0;
		if (pos.x < 0f || pos.z < 0f || pos.x > (float)(bi.world.blockData.Length - 1) || pos.z > (float)(bi.world.blockData.Length - 1))
		{
			isFree = false;
			isConnected = false;
			return;
		}
		BlockData ifNotNull = bi.world.GetIfNotNull((int)pos.x, (int)pos.z);
		RoadCreator railIfNotNull = roading.GetRailIfNotNull((int)pos.x, 1, (int)pos.z);
		RoadCreator roadCreator = null;
		RoadCreator roadCreator2 = null;
		RoadCreator roadCreator3 = null;
		RoadCreator roadCreator4 = null;
		if (railIfNotNull == null || ifNotNull == null)
		{
			PlacerMaterial.SetColor("_Color", red);
			placer.transform.position = Center;
			isFree = false;
			isConnected = false;
			return;
		}
		if (railIfNotNull.rType == RoadCreator.RoadType.RAIL && railIfNotNull.tile == "Rail_Straight")
		{
			if (railIfNotNull.Bitwise == 5)
			{
				roadCreator = roading.rG[(int)railIfNotNull.wPos.x, 1, (int)railIfNotNull.wPos.z - 1];
				roadCreator2 = roading.rG[(int)railIfNotNull.wPos.x, 1, (int)railIfNotNull.wPos.z];
				roadCreator3 = roading.rG[(int)railIfNotNull.wPos.x, 1, (int)railIfNotNull.wPos.z + 1];
				if (roadCreator == null)
				{
					isFree = false;
					isConnected = false;
					return;
				}
				if (roadCreator.tile == "Rail_End" && roadCreator2.tile == "Rail_Straight" && roadCreator3.tile == "Rail_Straight")
				{
					Orientation = 270;
					isConnected = true;
					isFree = true;
					roadAccess = ifNotNull;
					roadCreator4 = roadCreator;
				}
				else if (roadCreator.tile == "Rail_Straight" && roadCreator2.tile == "Rail_Straight" && roadCreator3.tile == "Rail_End")
				{
					Orientation = 90;
					isConnected = true;
					isFree = true;
					roadAccess = ifNotNull;
					roadCreator4 = roadCreator3;
				}
			}
			else if (railIfNotNull.Bitwise == 10)
			{
				roadCreator = roading.rG[(int)railIfNotNull.wPos.x - 1, 1, (int)railIfNotNull.wPos.z];
				roadCreator2 = roading.rG[(int)railIfNotNull.wPos.x, 1, (int)railIfNotNull.wPos.z];
				roadCreator3 = roading.rG[(int)railIfNotNull.wPos.x + 1, 1, (int)railIfNotNull.wPos.z];
				if (roadCreator == null)
				{
					isFree = false;
					isConnected = false;
					return;
				}
				if (roadCreator.tile == "Rail_End" && roadCreator2.tile == "Rail_Straight" && roadCreator3.tile == "Rail_Straight")
				{
					Orientation = 0;
					isConnected = true;
					isFree = true;
					roadAccess = ifNotNull;
					roadCreator4 = roadCreator;
				}
				else if (roadCreator.tile == "Rail_Straight" && roadCreator2.tile == "Rail_Straight" && roadCreator3.tile == "Rail_End")
				{
					Orientation = 180;
					isConnected = true;
					isFree = true;
					roadAccess = ifNotNull;
					roadCreator4 = roadCreator3;
				}
			}
		}
		costHelper.SetCost(Cost);
		if (!costHelper.CanAfford)
		{
			isFree = false;
			TooltipSystem.Show("", "NoMo");
		}
		if (isFree && isConnected)
		{
			placer.transform.position = Center;
			placer.transform.eulerAngles = new Vector3(0f, Orientation, 0f);
			PlacerMaterial.SetColor("_Color", green);
			roadAccess = ifNotNull;
		}
		else
		{
			PlacerMaterial.SetColor("_Color", red);
			placer.transform.position = Center;
		}
		if (!build)
		{
			return;
		}
		if (roadCreator4 == null)
		{
			sm.Play("Impractical");
			isFree = false;
			isConnected = false;
			return;
		}
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Roads/Rail_Connection"));
		gameObject.transform.position = Center;
		gameObject.name = pos.x + "_connection_" + pos.z;
		costHelper.PayForIt();
		gameObject.transform.Rotate(0f, Orientation, 0f, Space.Self);
		roadCreator4.Bitwise = 16;
		BlockData bounded = railing.world.GetBounded((int)railIfNotNull.wPos.x, (int)railIfNotNull.wPos.z);
		BlockData bounded2;
		BlockData bounded3;
		BlockData bounded4;
		BlockData bounded5;
		if (Orientation == 0)
		{
			bounded2 = railing.world.GetBounded((int)railIfNotNull.wPos.x - 1, (int)railIfNotNull.wPos.z);
			bounded3 = railing.world.GetBounded((int)railIfNotNull.wPos.x + 1, (int)railIfNotNull.wPos.z);
			bounded4 = railing.world.GetBounded((int)railIfNotNull.wPos.x + 2, (int)railIfNotNull.wPos.z);
			bounded5 = railing.world.GetBounded((int)railIfNotNull.wPos.x - 2, (int)railIfNotNull.wPos.z);
		}
		else if (Orientation == 180)
		{
			bounded2 = railing.world.GetBounded((int)railIfNotNull.wPos.x + 1, (int)railIfNotNull.wPos.z);
			bounded3 = railing.world.GetBounded((int)railIfNotNull.wPos.x - 1, (int)railIfNotNull.wPos.z);
			bounded4 = railing.world.GetBounded((int)railIfNotNull.wPos.x - 2, (int)railIfNotNull.wPos.z);
			bounded5 = railing.world.GetBounded((int)railIfNotNull.wPos.x + 2, (int)railIfNotNull.wPos.z);
		}
		else if (Orientation == 90)
		{
			bounded2 = railing.world.GetBounded((int)railIfNotNull.wPos.x, (int)railIfNotNull.wPos.z + 1);
			bounded3 = railing.world.GetBounded((int)railIfNotNull.wPos.x, (int)railIfNotNull.wPos.z - 1);
			bounded4 = railing.world.GetBounded((int)railIfNotNull.wPos.x, (int)railIfNotNull.wPos.z - 2);
			bounded5 = railing.world.GetBounded((int)railIfNotNull.wPos.x, (int)railIfNotNull.wPos.z + 2);
		}
		else
		{
			bounded2 = railing.world.GetBounded((int)railIfNotNull.wPos.x, (int)railIfNotNull.wPos.z - 1);
			bounded3 = railing.world.GetBounded((int)railIfNotNull.wPos.x, (int)railIfNotNull.wPos.z + 1);
			bounded4 = railing.world.GetBounded((int)railIfNotNull.wPos.x, (int)railIfNotNull.wPos.z + 2);
			bounded5 = railing.world.GetBounded((int)railIfNotNull.wPos.x, (int)railIfNotNull.wPos.z - 2);
		}
		bounded2.FlatHeight(bounded4.h - 1.5f);
		bounded.FlatHeight(bounded4.h - 1f);
		bounded3.FlatHeight(bounded4.h - 0.5f);
		Railing.style = "Ground/";
		railing.RoadBitwise(bounded2.wP);
		railing.RoadBitwise(bounded.wP);
		railing.RoadBitwise(bounded3.wP);
		RoadCreator roadCreator5 = roading.rG[bounded2.wP.x, 1, bounded2.wP.y];
		if (roadCreator5 == null)
		{
			Debug.Log("Can't find connector tile?");
		}
		roadCreator5.Bitwise = 16;
		roadCreator5.tile = roading.rG[bounded.wP.x, 1, bounded.wP.y].tile;
		roadCreator5.Orientation = roading.rG[bounded.wP.x, 1, bounded.wP.y].Orientation;
		gameObject.transform.parent = roadCreator5.gameObject.transform;
		Railing.style = "Underground/";
		bounded5.u = true;
		railing.RoadBitwise(bounded5.wP);
		StartCoroutine(railing.UpdateChunks());
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
		Indexer.Index = 0;
		sm.Play("PlopBuild");
	}

	private void BuildingPlacer8x8(GameObject placer, int Cost)
	{
		oldx2 = x2;
		oldz2 = z2;
		Center = new Vector3(pos.x - 0.5f, pos.y + 0.515f, pos.z + -0.5f);
		isFree = true;
		isFlat = true;
		float num = pos.y;
		float num2 = pos.y;
		for (int i = (int)pos.z - 4; i <= (int)pos.z + 3; i++)
		{
			for (int j = (int)pos.x - 4; j <= (int)pos.x + 3; j++)
			{
				BlockData ifNotNull = bi.world.GetIfNotNull(j, i);
				if (ifNotNull == null)
				{
					isFree = false;
					isConnected = false;
					return;
				}
				if (ifNotNull.t == BlockData.Type.ROAD || ifNotNull.t == BlockData.Type.BUILDING || ifNotNull.h < 29.5f)
				{
					isFree = false;
				}
				if (ifNotNull.h > num)
				{
					num = ifNotNull.h;
				}
				else if (ifNotNull.h < num2)
				{
					num2 = ifNotNull.h;
				}
			}
		}
		if (Mathf.Abs(num - num2) > 2f)
		{
			isFlat = false;
		}
		Center.y = num + 0.515f;
		isConnected = false;
		Orientation = 0;
		if (isFree)
		{
			RoadCreator access = roading.GetAccess((int)pos.x - 5, (int)pos.z);
			RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 4);
			RoadCreator access3 = roading.GetAccess((int)pos.x + 4, (int)pos.z);
			RoadCreator access4 = roading.GetAccess((int)pos.x, (int)pos.z - 5);
			if (access != null)
			{
				Orientation = 270;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x - 5, (int)pos.z);
			}
			else if (access2 != null)
			{
				Orientation = 0;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 4);
			}
			else if (access3 != null)
			{
				Orientation = 90;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x + 4, (int)pos.z);
			}
			else if (access4 != null)
			{
				Orientation = 180;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 5);
			}
		}
		costHelper.SetCost(Cost);
		if (!costHelper.CanAfford)
		{
			isFree = false;
			TooltipSystem.Show("", "NoMo");
		}
		else if (!isConnected && isFree)
		{
			TooltipSystem.Show("", "No Road Access");
		}
		else if (!isFlat)
		{
			TooltipSystem.Show("", "Too Steep");
		}
		if (isFree && isConnected && isFlat)
		{
			placer.transform.position = Center;
			placer.transform.eulerAngles = new Vector3(0f, Orientation, 0f);
			PlacerMaterial.SetColor("_Color", green);
		}
		else
		{
			PlacerMaterial.SetColor("_Color", red);
			placer.transform.position = Center;
		}
	}

	private void MiningPlacer8x8(GameObject placer, int Cost)
	{
		oldx2 = x2;
		oldz2 = z2;
		Center = new Vector3(pos.x - 0.5f, pos.y + 0.515f, pos.z + -0.5f);
		prevalence = 0f;
		_resource = "none";
		for (int i = (int)pos.z - 3; i <= (int)pos.z + 2; i++)
		{
			for (int j = (int)pos.x - 3; j <= (int)pos.x + 2; j++)
			{
				BlockData bounded = bi.world.GetBounded(j, i);
				if (bounded.e > 194 && bounded.e < 253)
				{
					_resource = "Rare";
				}
				else if (bounded.e > 130 && bounded.e < 190)
				{
					_resource = "Mine";
				}
				else if (bounded.e > 66 && bounded.e < 126)
				{
					_resource = "Gas";
				}
				else if (bounded.e > 2 && bounded.e < 60)
				{
					_resource = "Oil";
				}
			}
		}
		for (int k = (int)pos.z - 4; k <= (int)pos.z + 3; k++)
		{
			for (int l = (int)pos.x - 4; l <= (int)pos.x + 3; l++)
			{
				BlockData bounded2 = bi.world.GetBounded(l, k);
				if (bounded2 != null)
				{
					if (bounded2.e > 194 && bounded2.e < 253 && _resource == "Rare")
					{
						prevalence += Mathf.Clamp((float)(bounded2.e - 192) * 3.984375f, 0f, 100f);
					}
					else if (bounded2.e > 130 && bounded2.e < 190 && _resource == "Mine")
					{
						prevalence += Mathf.Clamp((float)(bounded2.e - 128) * 3.984375f, 0f, 100f);
					}
					else if (bounded2.e > 66 && bounded2.e < 126 && _resource == "Gas")
					{
						prevalence += Mathf.Clamp((float)(bounded2.e - 64) * 3.984375f, 0f, 100f);
					}
					else if (bounded2.e > 2 && bounded2.e < 60 && _resource == "Oil")
					{
						prevalence += Mathf.Clamp((float)(int)bounded2.e * 3.984375f, 0f, 100f);
					}
				}
			}
		}
		prevalence /= 8f;
		if (prevalence > 2f && _resource != "none")
		{
			TooltipSystem.Show(_resource, "", (int)prevalence);
		}
		else
		{
			TooltipSystem.Show("", "NoRess");
		}
		isFree = true;
		isFlat = true;
		float num = pos.y;
		float num2 = pos.y;
		for (int m = (int)pos.z - 4; m <= (int)pos.z + 3; m++)
		{
			for (int n = (int)pos.x - 4; n <= (int)pos.x + 3; n++)
			{
				BlockData ifNotNull = bi.world.GetIfNotNull(n, m);
				if (ifNotNull == null)
				{
					isFree = false;
					isConnected = false;
					return;
				}
				if (ifNotNull.t == BlockData.Type.ROAD || ifNotNull.t == BlockData.Type.BUILDING)
				{
					isFree = false;
				}
				if (ifNotNull.h > num)
				{
					num = ifNotNull.h;
				}
				else if (ifNotNull.h < num2)
				{
					num2 = ifNotNull.h;
				}
			}
		}
		if (Mathf.Abs(num - num2) > 2f)
		{
			isFlat = false;
		}
		Center.y = num + 0.515f;
		isConnected = false;
		Orientation = 0;
		if (isFree)
		{
			RoadCreator access = roading.GetAccess((int)pos.x - 5, (int)pos.z);
			RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 4);
			RoadCreator access3 = roading.GetAccess((int)pos.x + 4, (int)pos.z);
			RoadCreator access4 = roading.GetAccess((int)pos.x, (int)pos.z - 5);
			if (access != null)
			{
				Orientation = 270;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x - 5, (int)pos.z);
			}
			else if (access2 != null)
			{
				Orientation = 0;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 4);
			}
			else if (access3 != null)
			{
				Orientation = 90;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x + 4, (int)pos.z);
			}
			else if (access4 != null)
			{
				Orientation = 180;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 5);
			}
		}
		if (_resource == "Oil" && pos.y < 29.5f)
		{
			isConnected = true;
			Center.y = 30.75f;
		}
		costHelper.SetCost(Cost);
		if (!costHelper.CanAfford)
		{
			isFree = false;
			TooltipSystem.Show("", "NoMo");
		}
		else if (!isConnected && pos.y >= 29.75f && isFree)
		{
			TooltipSystem.Show("", "No Road Access");
		}
		else if (_resource == "none")
		{
			TooltipSystem.Show("", "NoRess");
		}
		else if (!isFlat)
		{
			TooltipSystem.Show("", "Too Steep");
		}
		if (isFree && isConnected && _resource != "none" && isFlat)
		{
			placer.transform.position = Center;
			placer.transform.eulerAngles = new Vector3(0f, Orientation, 0f);
			PlacerMaterial.SetColor("_Color", green);
		}
		else
		{
			PlacerMaterial.SetColor("_Color", red);
			placer.transform.position = Center;
		}
	}

	private void BuildingPlacer12x12(GameObject placer, int Cost)
	{
		oldx2 = x2;
		oldz2 = z2;
		Center = new Vector3(pos.x - 0.5f, pos.y + 0.515f, pos.z + -0.5f);
		isFree = true;
		isFlat = true;
		float num = pos.y;
		float num2 = pos.y;
		for (int i = (int)pos.z - 6; i <= (int)pos.z + 5; i++)
		{
			for (int j = (int)pos.x - 6; j <= (int)pos.x + 5; j++)
			{
				BlockData ifNotNull = bi.world.GetIfNotNull(j, i);
				if (ifNotNull == null)
				{
					isFree = false;
					isConnected = false;
					return;
				}
				if (ifNotNull.t == BlockData.Type.ROAD || ifNotNull.t == BlockData.Type.BUILDING || ifNotNull.h < 29.5f)
				{
					isFree = false;
				}
				if (ifNotNull.h > num)
				{
					num = ifNotNull.h;
				}
				else if (ifNotNull.h < num2)
				{
					num2 = ifNotNull.h;
				}
			}
		}
		Center.y = num + 0.515f;
		if (Mathf.Abs(num - num2) > 2f)
		{
			isFlat = false;
		}
		isConnected = false;
		Orientation = 0;
		if (isFree)
		{
			RoadCreator access = roading.GetAccess((int)pos.x - 7, (int)pos.z);
			RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 6);
			RoadCreator access3 = roading.GetAccess((int)pos.x + 6, (int)pos.z);
			RoadCreator access4 = roading.GetAccess((int)pos.x, (int)pos.z - 7);
			if (access != null)
			{
				Orientation = 270;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x - 7, (int)pos.z);
			}
			else if (access2 != null)
			{
				Orientation = 0;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 6);
			}
			else if (access3 != null)
			{
				Orientation = 90;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x + 6, (int)pos.z);
			}
			else if (access4 != null)
			{
				Orientation = 180;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 7);
			}
		}
		costHelper.SetCost(Cost);
		if (!costHelper.CanAfford)
		{
			isFree = false;
			TooltipSystem.Show("", "NoMo");
		}
		else if (!isConnected && isFree)
		{
			TooltipSystem.Show("", "No Road Access");
		}
		else if (!isFlat)
		{
			TooltipSystem.Show("", "Too Steep");
		}
		if (isFree && isConnected && isFlat)
		{
			placer.transform.position = Center;
			placer.transform.eulerAngles = new Vector3(0f, Orientation, 0f);
			PlacerMaterial.SetColor("_Color", green);
		}
		else
		{
			PlacerMaterial.SetColor("_Color", red);
			placer.transform.position = Center;
		}
	}

	private void BuildingPlacer16x16(GameObject placer, int Cost)
	{
		oldx2 = x2;
		oldz2 = z2;
		Center = new Vector3(pos.x - 0.5f, pos.y + 0.515f, pos.z + -0.5f);
		isFree = true;
		isFlat = true;
		float num = pos.y;
		float num2 = pos.y;
		for (int i = (int)pos.z - 8; i <= (int)pos.z + 7; i++)
		{
			for (int j = (int)pos.x - 8; j <= (int)pos.x + 7; j++)
			{
				BlockData ifNotNull = bi.world.GetIfNotNull(j, i);
				if (ifNotNull == null)
				{
					isFree = false;
					isConnected = false;
					return;
				}
				if (ifNotNull.t == BlockData.Type.ROAD || ifNotNull.t == BlockData.Type.BUILDING || ifNotNull.h < 29.5f)
				{
					isFree = false;
				}
				if (ifNotNull.h > num)
				{
					num = ifNotNull.h;
				}
				else if (ifNotNull.h < num2)
				{
					num2 = ifNotNull.h;
				}
			}
		}
		Center.y = num + 0.515f;
		if (Mathf.Abs(num - num2) > 3f)
		{
			isFlat = false;
		}
		isConnected = false;
		Orientation = 0;
		if (isFree)
		{
			RoadCreator access = roading.GetAccess((int)pos.x - 9, (int)pos.z);
			RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 8);
			RoadCreator access3 = roading.GetAccess((int)pos.x + 9, (int)pos.z);
			RoadCreator access4 = roading.GetAccess((int)pos.x, (int)pos.z - 8);
			if (access != null)
			{
				Orientation = 270;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x - 9, (int)pos.z);
			}
			else if (access2 != null)
			{
				Orientation = 0;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 8);
			}
			else if (access3 != null)
			{
				Orientation = 90;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x + 8, (int)pos.z);
			}
			else if (access4 != null)
			{
				Orientation = 180;
				isConnected = true;
				roadAccess = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 9);
			}
		}
		costHelper.SetCost(Cost);
		if (!costHelper.CanAfford)
		{
			isFree = false;
			TooltipSystem.Show("", "NoMo");
		}
		else if (!isConnected && isFree)
		{
			TooltipSystem.Show("", "No Road Access");
		}
		else if (!isFlat)
		{
			TooltipSystem.Show("", "Too Steep");
		}
		if (isFree && isConnected && isFlat)
		{
			placer.transform.position = Center;
			placer.transform.eulerAngles = new Vector3(0f, Orientation, 0f);
			PlacerMaterial.SetColor("_Color", green);
		}
		else
		{
			PlacerMaterial.SetColor("_Color", red);
			placer.transform.position = Center;
		}
	}

	private void BuildingPlacerAirport(GameObject placer, int Cost)
	{
		oldx2 = x2;
		oldz2 = z2;
		isConnected = false;
		Orientation = rotated;
		Center = new Vector3(pos.x - 0.5f, pos.y + 0.515f, pos.z + -0.5f);
		RoadCreator roadCreator;
		if (Orientation == 270)
		{
			roadCreator = roading.GetAccess((int)pos.x - 15, (int)pos.z);
		}
		else if (Orientation != 0)
		{
			roadCreator = ((Orientation != 90) ? roading.GetAccess((int)pos.x, (int)pos.z - 15) : roading.GetAccess((int)pos.x + 14, (int)pos.z));
		}
		else
		{
			roadCreator = roading.GetAccess((int)pos.x, (int)pos.z + 14);
		}
		if (roadCreator == null)
		{
			isFree = false;
			isConnected = false;
		}
		else
		{
			isConnected = true;
			roadAccess = bi.world.GetIfNotNull((int)roadCreator.wPos.x, (int)roadCreator.wPos.z);
		}
		isFree = true;
		isFlat = true;
		float num = pos.y;
		float num2 = pos.y;
		if (Orientation == 0 || Orientation == 180)
		{
			for (int i = (int)pos.z - 14; i <= (int)pos.z + 13; i++)
			{
				for (int j = (int)pos.x - 8; j <= (int)pos.x + 7; j++)
				{
					BlockData ifNotNull = bi.world.GetIfNotNull(j, i);
					if (ifNotNull == null)
					{
						isFree = false;
						isConnected = false;
						return;
					}
					if (ifNotNull.b)
					{
						isFree = false;
					}
					if (ifNotNull.h > num)
					{
						num = ifNotNull.h;
					}
					else if (ifNotNull.h < num2)
					{
						num2 = ifNotNull.h;
					}
				}
			}
		}
		else
		{
			for (int k = (int)pos.z - 8; k <= (int)pos.z + 7; k++)
			{
				for (int l = (int)pos.x - 14; l <= (int)pos.x + 13; l++)
				{
					if (k < 0 || k > bi.world.blockData.Length - 1 || l < 0 || l > bi.world.blockData.Length - 1)
					{
						return;
					}
					BlockData ifNotNull2 = bi.world.GetIfNotNull(l, k);
					if (ifNotNull2 == null)
					{
						isFree = false;
						isConnected = false;
						return;
					}
					if (ifNotNull2.b)
					{
						isFree = false;
					}
					if (ifNotNull2.h > num)
					{
						num = ifNotNull2.h;
					}
					else if (ifNotNull2.h < num2)
					{
						num2 = ifNotNull2.h;
					}
				}
			}
		}
		Center.y = num + 0.515f;
		placer.transform.position = Center;
		placer.transform.eulerAngles = new Vector3(0f, Orientation, 0f);
		if (Mathf.Abs(num - num2) > 2f)
		{
			isFlat = false;
		}
		costHelper.SetCost(Cost);
		if (!costHelper.CanAfford)
		{
			isFree = false;
			TooltipSystem.Show("", "NoMo");
		}
		else if (!isConnected && isFree)
		{
			TooltipSystem.Show("", "No Road Access");
		}
		else if (!isFlat)
		{
			TooltipSystem.Show("", "Too Steep");
		}
		else
		{
			TooltipSystem.Show("PushTab");
		}
		if (isFree && isConnected && isFlat)
		{
			PlacerMaterial.SetColor("_Color", green);
		}
		else
		{
			PlacerMaterial.SetColor("_Color", red);
		}
	}

	private void ParkPlacer3x3(GameObject placer, int Cost)
	{
		oldx2 = x2;
		oldz2 = z2;
		Center = new Vector3(pos.x, pos.y + 0.515f, pos.z);
		if (Indexer.Index == 98 || Indexer.Index == 107)
		{
			Cost = 500;
		}
		isFree = true;
		float num = pos.y;
		float num2 = pos.y;
		for (int i = (int)pos.z - 1; i <= (int)pos.z + 1; i++)
		{
			for (int j = (int)pos.x - 1; j <= (int)pos.x + 1; j++)
			{
				BlockData ifNotNull = bi.world.GetIfNotNull(j, i);
				if (ifNotNull == null)
				{
					isFree = false;
					isConnected = false;
					return;
				}
				if (ifNotNull.t == BlockData.Type.ROAD || ifNotNull.t == BlockData.Type.BUILDING || ifNotNull.h < 29.5f)
				{
					isFree = false;
				}
				if (ifNotNull.h >= num)
				{
					num = ifNotNull.h;
				}
				else if (ifNotNull.h <= num2)
				{
					num2 = ifNotNull.h;
				}
			}
		}
		Center.y = num + 0.515f;
		if (Mathf.Abs(num - num2) > 2f)
		{
			TooltipSystem.Show("", "Too Steep");
			isFree = false;
		}
		isConnected = true;
		Orientation = 0;
		if (isFree)
		{
			BlockData bounded = bi.world.GetBounded((int)pos.x - 2, (int)pos.z);
			BlockData bounded2 = bi.world.GetBounded((int)pos.x, (int)pos.z + 2);
			BlockData bounded3 = bi.world.GetBounded((int)pos.x + 2, (int)pos.z);
			BlockData bounded4 = bi.world.GetBounded((int)pos.x, (int)pos.z - 2);
			if (bounded.t == BlockData.Type.ROAD)
			{
				Orientation = 270;
			}
			else if (bounded2.t == BlockData.Type.ROAD)
			{
				Orientation = 0;
			}
			else if (bounded3.t == BlockData.Type.ROAD)
			{
				Orientation = 90;
			}
			else if (bounded4.t == BlockData.Type.ROAD)
			{
				Orientation = 180;
			}
		}
		costHelper.SetCost(Cost);
		if (!costHelper.CanAfford)
		{
			isFree = false;
			TooltipSystem.Show("", "NoMo");
		}
		if (isFree)
		{
			placer.transform.position = Center;
			placer.transform.eulerAngles = new Vector3(0f, Orientation, 0f);
			PlacerMaterial.SetColor("_Color", green);
		}
		else
		{
			PlacerMaterial.SetColor("_Color", red);
			placer.transform.position = Center;
		}
	}

	private void BeachPlacer3x3(GameObject placer, int Cost)
	{
		oldx2 = x2;
		oldz2 = z2;
		Center = new Vector3(pos.x, pos.y + 0.515f, pos.z);
		if (Indexer.Index == 113)
		{
			Cost = 100;
		}
		isFree = true;
		float num = pos.y;
		float num2 = pos.y;
		for (int i = (int)pos.z - 1; i <= (int)pos.z + 1; i++)
		{
			for (int j = (int)pos.x - 1; j <= (int)pos.x + 1; j++)
			{
				BlockData ifNotNull = bi.world.GetIfNotNull(j, i);
				if (ifNotNull == null)
				{
					isFree = false;
					isConnected = false;
					return;
				}
				if (ifNotNull.t == BlockData.Type.ROAD || ifNotNull.t == BlockData.Type.BUILDING || ifNotNull.h < 29.5f)
				{
					isFree = false;
				}
				if (ifNotNull.h >= num)
				{
					num = ifNotNull.h;
				}
				else if (ifNotNull.h <= num2)
				{
					num2 = ifNotNull.h;
				}
				if (ifNotNull.h < 29.75f || ifNotNull.h > 30f)
				{
					isFree = false;
				}
			}
		}
		Center.y = num + 0.515f;
		if (Mathf.Abs(num - num2) > 0.5f)
		{
			isFree = false;
		}
		if (isFree)
		{
			TooltipSystem.Show("beaTT");
		}
		else
		{
			TooltipSystem.Show("Unsuitable Terrain");
		}
		isConnected = true;
		Orientation = rotated;
		costHelper.SetCost(Cost);
		if (!costHelper.CanAfford)
		{
			isFree = false;
			TooltipSystem.Show("", "NoMo");
		}
		if (isFree)
		{
			placer.transform.position = Center;
			placer.transform.eulerAngles = new Vector3(0f, Orientation, 0f);
		}
		else
		{
			placer.transform.eulerAngles = new Vector3(0f, Orientation, 0f);
			placer.transform.position = Center;
		}
	}

	private void ParkPlacer1x1(GameObject placer, int Cost)
	{
		oldx2 = x2;
		oldz2 = z2;
		Center = new Vector3(pos.x, pos.y + 0.515f, pos.z);
		isFree = true;
		BlockData ifNotNull = bi.world.GetIfNotNull((int)pos.x, (int)pos.z);
		if (ifNotNull == null)
		{
			isFree = false;
			isConnected = false;
			return;
		}
		if (ifNotNull.b || ifNotNull.h < 29.5f)
		{
			isFree = false;
		}
		isConnected = true;
		Orientation = 0;
		costHelper.SetCost(Cost);
		if (!costHelper.CanAfford)
		{
			isFree = false;
			TooltipSystem.Show("", "NoMo");
		}
		if (isFree && isConnected)
		{
			placer.transform.position = Center;
			placer.transform.eulerAngles = new Vector3(0f, Orientation, 0f);
		}
		else
		{
			placer.transform.position = Center;
		}
	}

	private void BusPlacer(GameObject placer, int Cost)
	{
		oldx2 = x2;
		oldz2 = z2;
		TooltipSystem.Hide();
		Center = new Vector3(pos.x, pos.y + 0.502f, pos.z);
		int num = 0;
		isFree = false;
		BlockData ifNotNull = bi.world.GetIfNotNull((int)pos.x, (int)pos.z);
		if (ifNotNull == null)
		{
			return;
		}
		if (Indexer.Index == 191)
		{
			indexer.gridTrain.transform.position = new Vector3(ifNotNull.wP.x, 100f, ifNotNull.wP.y);
		}
		else
		{
			indexer.gridBus.transform.position = new Vector3(ifNotNull.wP.x, 100f, ifNotNull.wP.y);
		}
		RoadCreator ifNotNull2 = roading.GetIfNotNull((int)pos.x, 1, (int)pos.z);
		if (ifNotNull.t == BlockData.Type.ROAD && ifNotNull2 != null)
		{
			if (ifNotNull2.cc == 1)
			{
				num = 180;
				isFree = true;
			}
			else if (ifNotNull2.cc == 2)
			{
				num = 0;
				isFree = true;
			}
			else if (ifNotNull2.cc == 3)
			{
				num = 90;
				isFree = true;
			}
			else if (ifNotNull2.cc == 4)
			{
				num = 270;
				isFree = true;
			}
			else
			{
				TooltipSystem.Show("", "Not On Curves");
			}
		}
		costHelper.SetCost(Cost);
		if (!costHelper.CanAfford)
		{
			isFree = false;
			TooltipSystem.Show("", "NoMo");
		}
		if (isFree && isConnected)
		{
			placer.transform.position = Center;
			placer.transform.eulerAngles = new Vector3(0f, num, 0f);
			if (ifNotNull2.tile == "side" || ifNotNull2.tile == "straight" || ifNotNull2.tile == "straighttree" || ifNotNull2.tile == "sidetree")
			{
				isFree = true;
			}
			else
			{
				isFree = false;
				TooltipSystem.Show("", "Not On Slopes");
			}
			if (Indexer.Index == 191)
			{
				if (roading.rG[(int)pos.x, 0, (int)pos.z] != null)
				{
					isFree = true;
				}
				else
				{
					isFree = false;
					TooltipSystem.Show("", "Build Track First");
				}
			}
		}
		else
		{
			placer.transform.position = Center;
		}
		isConnected = true;
		Orientation = 0;
	}

	private void VegetationPlacer()
	{
		oldx2 = x2;
		oldz2 = z2;
		Center = new Vector3(pos.x, pos.y + 0.515f, pos.z);
		isFree = true;
		BlockData blockData = bi.world.blockData[(int)pos.x, (int)pos.z];
		if (blockData.t == BlockData.Type.ROAD || blockData.t == BlockData.Type.BUILDING || blockData.h < 30f)
		{
			isFree = false;
		}
		isConnected = true;
		Orientation = 0;
		int cost = ((Indexer.Index != 55) ? 2 : 5);
		costHelper.SetCost(cost);
		if (!costHelper.CanAfford)
		{
			isFree = false;
			TooltipSystem.Show("", "NoMo");
		}
		if (isFree && isConnected)
		{
			bi.One_one.transform.position = Center;
			bi.One_one.transform.eulerAngles = new Vector3(0f, Orientation, 0f);
		}
		else
		{
			bi.One_one.transform.position = Center;
		}
	}

	private void HLMPlopper()
	{
		int height;
		int num;
		if (Indexer.Index == 70)
		{
			height = 2;
			num = 30 + Random.Range(0, 3);
		}
		else if (Indexer.Index == 71)
		{
			height = 5;
			num = 120 + Random.Range(0, 12);
		}
		else
		{
			height = 13;
			num = 400 + Random.Range(0, 20);
		}
		GameObject gameObject = Object.Instantiate(zoning.lotUnit);
		LotCreator component = gameObject.GetComponent<LotCreator>();
		Center.y = roadAccess.h + 0.005f;
		gameObject.transform.position = Center;
		gameObject.transform.Rotate(0f, Orientation + 90, 0f, Space.Self);
		gameObject.name = pos.x + "_HLM_" + pos.z;
		gameObject.transform.parent = lotParent.transform;
		component.r = roading;
		component.w = bi.world;
		for (int i = (int)pos.z - 2; i <= (int)pos.z + 1; i++)
		{
			for (int j = (int)pos.x - 2; j <= (int)pos.x + 1; j++)
			{
				BlockData blockData = bi.world.blockData[j, i];
				blockData.t = BlockData.Type.BUILDING;
				blockData.lO = component;
				blockData.FlatHeight(roadAccess.h);
				blockData.b = true;
				component.blockPos.Add(new Vector2Int(j, i));
				CheckChunks(blockData);
			}
		}
		RoadCreator access = roading.GetAccess((int)pos.x - 3, (int)pos.z);
		RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 2);
		RoadCreator access3 = roading.GetAccess((int)pos.x + 2, (int)pos.z);
		if (access != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x - 3, (int)pos.z).wP;
		}
		else if (access2 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 2).wP;
		}
		else if (access3 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x + 2, (int)pos.z).wP;
		}
		else
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 3).wP;
		}
		int num2 = Orientation + 90;
		if (num2 == 360)
		{
			Orientation = 0;
		}
		component.wP = new Vector3(pos.x, Center.y, pos.z);
		component.Orientation = (short)num2;
		component.Anchor = new Vector3(pos.x - 0.5f, pos.y, pos.z - 0.5f);
		component.lType = LotCreator.LotType.RESIDENTIAL;
		component.lClass = LotCreator.LotClass.LOW;
		component.lSize = LotCreator.LotSize._4x4;
		component.MaxStage = 0;
		component.Stage = 0;
		if (indexer.eco.LC_Demand < -500f)
		{
			component.Occupants = 0;
			component.decayReason = "NotEnoughDemand";
		}
		else
		{
			component.Occupants = num;
		}
		component.nextDemand = (short)num;
		component.desiredValue = 1;
		bi.world.blockData[component.Entry.x, component.Entry.y].o.lots.Add(component);
		bi.world.ResidentialBuildings.Add(component);
		bi.eco.LC_Demand -= (float)component.Occupants * 1.2f;
		bi.eco.R_Demand += (float)component.Occupants * (bi.eco.MC_X * bi.eco.Retail);
		component.Aiguilleur(height);
		costHelper.PayForIt();
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
		sm.Play("PlopBuild");
	}

	private void FactoryPlopper()
	{
		int height;
		int occupants;
		if (Indexer.Index == 73)
		{
			height = 2;
			occupants = 250 + Random.Range(-3, 3);
		}
		else
		{
			height = 3;
			occupants = 350 + Random.Range(-6, 6);
		}
		GameObject gameObject = Object.Instantiate(zoning.lotUnit);
		LotCreator component = gameObject.GetComponent<LotCreator>();
		Center.y = roadAccess.h + 0.005f;
		gameObject.transform.position = Center;
		gameObject.transform.Rotate(0f, Orientation + 90, 0f, Space.Self);
		gameObject.transform.parent = lotParent.transform;
		gameObject.name = pos.x + "_Fac_" + pos.z;
		component.r = roading;
		component.w = bi.world;
		for (int i = (int)pos.z - 2; i <= (int)pos.z + 1; i++)
		{
			for (int j = (int)pos.x - 2; j <= (int)pos.x + 1; j++)
			{
				BlockData blockData = bi.world.blockData[j, i];
				blockData.t = BlockData.Type.BUILDING;
				blockData.lO = component;
				blockData.FlatHeight(roadAccess.h);
				blockData.b = true;
				component.blockPos.Add(new Vector2Int(j, i));
				CheckChunks(blockData);
			}
		}
		RoadCreator access = roading.GetAccess((int)pos.x - 3, (int)pos.z);
		RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 2);
		RoadCreator access3 = roading.GetAccess((int)pos.x + 2, (int)pos.z);
		if (access != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x - 3, (int)pos.z).wP;
		}
		else if (access2 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 2).wP;
		}
		else if (access3 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x + 2, (int)pos.z).wP;
		}
		else
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 3).wP;
		}
		int num = Orientation + 90;
		if (num == 360)
		{
			Orientation = 0;
		}
		component.wP = new Vector3(pos.x, Center.y, pos.z);
		component.Orientation = (short)num;
		component.Anchor = new Vector3(pos.x - 0.5f, pos.y, pos.z - 0.5f);
		component.lType = LotCreator.LotType.INDUSTRIAL;
		component.lClass = LotCreator.LotClass.MANUFACTURE;
		component.lSize = LotCreator.LotSize._4x4;
		component.MaxStage = 0;
		component.Stage = 0;
		component.Occupants = occupants;
		component.desiredValue = 1;
		bi.world.blockData[component.Entry.x, component.Entry.y].o.lots.Add(component);
		bi.world.IndustrialBuildings.Add(component);
		bi.eco.M_Demand -= (float)component.Occupants * 0.3f;
		bi.eco.O_Demand += (float)component.Occupants * 0.5f * (bi.eco.M_X * bi.eco.Office);
		component.Aiguilleur(height);
		costHelper.PayForIt();
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
		sm.Play("PlopBuild");
	}

	private void RetailPlopper()
	{
		int height;
		int occupants;
		if (Indexer.Index == 75)
		{
			height = 2;
			occupants = 45;
		}
		else
		{
			height = 3;
			occupants = 90;
		}
		GameObject gameObject = Object.Instantiate(zoning.lotUnit);
		LotCreator component = gameObject.GetComponent<LotCreator>();
		Center.y = roadAccess.h + 0.005f;
		gameObject.transform.position = Center;
		gameObject.transform.Rotate(0f, Orientation + 90, 0f, Space.Self);
		gameObject.transform.parent = lotParent.transform;
		gameObject.name = pos.x + "_Ret_" + pos.z;
		component.r = roading;
		component.w = bi.world;
		for (int i = (int)pos.z - 2; i <= (int)pos.z + 1; i++)
		{
			for (int j = (int)pos.x - 2; j <= (int)pos.x + 1; j++)
			{
				BlockData blockData = bi.world.blockData[j, i];
				blockData.t = BlockData.Type.BUILDING;
				blockData.lO = component;
				blockData.FlatHeight(roadAccess.h);
				blockData.b = true;
				component.blockPos.Add(new Vector2Int(j, i));
				CheckChunks(blockData);
			}
		}
		RoadCreator access = roading.GetAccess((int)pos.x - 3, (int)pos.z);
		RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 2);
		RoadCreator access3 = roading.GetAccess((int)pos.x + 2, (int)pos.z);
		if (access != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x - 3, (int)pos.z).wP;
		}
		else if (access2 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 2).wP;
		}
		else if (access3 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x + 2, (int)pos.z).wP;
		}
		else
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 3).wP;
		}
		int num = Orientation + 90;
		if (num == 360)
		{
			Orientation = 0;
		}
		component.wP = new Vector3(pos.x, Center.y, pos.z);
		component.Orientation = (short)num;
		component.Anchor = new Vector3(pos.x - 0.5f, pos.y, pos.z - 0.5f);
		component.lType = LotCreator.LotType.COMMERCIAL;
		component.lClass = LotCreator.LotClass.RETAIL;
		component.lSize = LotCreator.LotSize._4x4;
		component.MaxStage = 0;
		component.Stage = 0;
		component.Occupants = occupants;
		component.desiredValue = 1;
		bi.world.blockData[component.Entry.x, component.Entry.y].o.lots.Add(component);
		bi.world.CommercialBuildings.Add(component);
		bi.eco.R_Demand -= (float)component.Occupants * 0.8f;
		bi.eco.M_Demand += (float)component.Occupants * 0.8f * (bi.eco.R_X * bi.eco.Manufacturing);
		bi.eco.HT_Demand += (float)component.Occupants * 0.8f * (bi.eco.R_X * bi.eco.HighTech);
		component.Aiguilleur(height);
		costHelper.PayForIt();
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
		sm.Play("PlopBuild");
	}

	private void ServicePlopper4x4()
	{
		string path = "none";
		if (Indexer.Index == 30)
		{
			path = "Services/School_Evo1";
		}
		else if (Indexer.Index == 31)
		{
			path = "Services/Hospital_Evo1";
		}
		else if (Indexer.Index == 32)
		{
			path = "Services/Firestation_Evo1";
		}
		else if (Indexer.Index == 33)
		{
			path = "Services/Policestation_Evo1";
		}
		else if (Indexer.Index == 36)
		{
			path = "Services/Library";
		}
		else if (Indexer.Index == 59)
		{
			path = "Facilities/PumpingStation";
		}
		else if (Indexer.Index == 240)
		{
			path = "Services/Church";
		}
		else if (Indexer.Index == 241)
		{
			path = "Services/Mosque";
		}
		else if (Indexer.Index == 242)
		{
			path = "Services/Pagoda";
		}
		else if (Indexer.Index == 63)
		{
			path = "Facilities/Plant_Solar";
		}
		else if (Indexer.Index == 103)
		{
			path = "Parks/LargePlaza_D";
		}
		else if (Indexer.Index == 104)
		{
			path = "Parks/LargePlaza_E";
		}
		else if (Indexer.Index == 105)
		{
			path = "Parks/LargePlaza_F";
		}
		else if (Indexer.Index == 69)
		{
			path = "Services/AirTaxi_Evo1";
		}
		GameObject gameObject = Object.Instantiate(serviceUnit);
		ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
		((GameObject)Object.Instantiate(Resources.Load(path))).transform.parent = gameObject.transform;
		if (Indexer.Index == 103 || Indexer.Index == 104 || Indexer.Index == 105)
		{
			roadAccess = bi.world.GetBounded((int)pos.x, (int)pos.z);
		}
		Center.y = roadAccess.h + 0.505f;
		gameObject.transform.position = Center;
		gameObject.name = pos.x + "_service_" + pos.z;
		component.worldPos = new Vector3(pos.x, Center.y, pos.z);
		component.Orientation = Orientation;
		component.Anchor = new Vector3(pos.x - 0.5f, pos.y, pos.z - 0.5f);
		component.TreesParent = new GameObject("TreesParent");
		component.TreesParent.transform.parent = component.gameObject.transform;
		if (Indexer.Index == 30)
		{
			component.Capacity = 240;
			component.Maintenance = 50f;
			component.MinEmployees = 6;
			component.MaxEmployees = 12;
			component.FixedEmployees = 2;
			component.HiredEmployees = 9;
			component.type = ServiceCreator.Type.School;
			component.typeName = "S";
			component.MaxStage = 4;
			costHelper.PayForIt();
			sm.Play("School");
			if (Messages.step == 15)
			{
				Messages.counter++;
			}
		}
		else if (Indexer.Index == 36)
		{
			component.Capacity = 1000;
			component.Maintenance = 50f;
			component.MinEmployees = 2000;
			component.MaxEmployees = 8000;
			component.FixedEmployees = 4;
			component.HiredEmployees = 2000;
			component.type = ServiceCreator.Type.School;
			component.typeName = "B";
			component.MaxStage = 1;
			costHelper.PayForIt();
			sm.Play("Library");
		}
		else if (Indexer.Index == 31)
		{
			component.Capacity = 80;
			component.Maintenance = 50f;
			component.MinEmployees = 15;
			component.MaxEmployees = 30;
			component.FixedEmployees = 30;
			component.HiredEmployees = 20;
			component.type = ServiceCreator.Type.Hospital;
			component.MaxStage = 3;
			costHelper.PayForIt();
			sm.Play("Hospital");
		}
		else if (Indexer.Index == 32)
		{
			component.Capacity = 200;
			component.Maintenance = 10f;
			component.MinEmployees = 6;
			component.MaxEmployees = 12;
			component.FixedEmployees = 2;
			component.HiredEmployees = 9;
			component.type = ServiceCreator.Type.Fire;
			component.MaxStage = 2;
			component.Use = 0;
			costHelper.PayForIt();
			sm.Play("Fire");
		}
		else if (Indexer.Index == 33)
		{
			component.Capacity = 200;
			component.Maintenance = 10f;
			component.MinEmployees = 18;
			component.MaxEmployees = 36;
			component.FixedEmployees = 6;
			component.HiredEmployees = 26;
			component.type = ServiceCreator.Type.Police;
			component.MaxStage = 2;
			component.Use = 0;
			costHelper.PayForIt();
			sm.Play("Police");
		}
		else if (Indexer.Index == 59)
		{
			component.Capacity = 10000;
			component.Maintenance = 500f;
			component.MinEmployees = 1;
			component.MaxEmployees = 4;
			component.FixedEmployees = 10;
			component.HiredEmployees = 1;
			component.type = ServiceCreator.Type.Water;
			component.MaxStage = 1;
			costHelper.PayForIt();
			sm.Play("Pumping");
		}
		else if (Indexer.Index == 63)
		{
			component.Capacity = 200;
			component.Maintenance = 30f;
			component.MinEmployees = 1;
			component.MaxEmployees = 1;
			component.FixedEmployees = 1;
			component.HiredEmployees = 1;
			component.type = ServiceCreator.Type.Power;
			component.MaxStage = 1;
			component.typeName = "S";
			costHelper.PayForIt();
			sm.Play("Power");
		}
		else if (Indexer.Index == 69)
		{
			component.Capacity = 30000;
			component.Maintenance = 900f;
			component.MinEmployees = 1;
			component.MaxEmployees = 1;
			component.FixedEmployees = 1;
			component.HiredEmployees = 1;
			component.type = ServiceCreator.Type.Drone;
			component.MaxStage = 3;
			component.typeName = "Drone";
			costHelper.PayForIt();
			sm.Play("Future");
		}
		else if (Indexer.Index == 240 || Indexer.Index == 241 || Indexer.Index == 242)
		{
			component.Capacity = 10000;
			component.Maintenance = 25f;
			component.MinEmployees = 1;
			component.MaxEmployees = 1;
			component.FixedEmployees = 0;
			component.HiredEmployees = 1;
			component.type = ServiceCreator.Type.Religion;
			component.MaxStage = 1;
			if (Indexer.Index == 240)
			{
				component.typeName = "Christian";
				sm.Play("Church");
			}
			else if (Indexer.Index == 241)
			{
				component.typeName = "Muslim";
				sm.Play("Mosque");
			}
			else
			{
				component.typeName = "Pagoda";
				sm.Play("Pagoda");
			}
			costHelper.PayForIt();
			if (Messages.step == 20)
			{
				Messages.counter++;
			}
		}
		else if (Indexer.Index == 103 || Indexer.Index == 104 || Indexer.Index == 105)
		{
			component.Capacity = 64;
			component.type = ServiceCreator.Type.Park;
			component.Cost = 150;
			component.Maintenance = 150f;
			component.typeName = "L";
			costHelper.PayAmount(1500);
			sm.Play("Park");
		}
		sm.Play("PlopBuild");
		RoadCreator access = roading.GetAccess((int)pos.x - 3, (int)pos.z);
		RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 2);
		RoadCreator access3 = roading.GetAccess((int)pos.x + 2, (int)pos.z);
		if (access != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x - 3, (int)pos.z).wP;
		}
		else if (access2 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 2).wP;
		}
		else if (access3 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x + 2, (int)pos.z).wP;
		}
		else
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 3).wP;
		}
		bi.world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
		roading.w.Services.Add(component);
		for (int i = (int)pos.z - 2; i <= (int)pos.z + 1; i++)
		{
			for (int j = (int)pos.x - 2; j <= (int)pos.x + 1; j++)
			{
				BlockData blockData = bi.world.blockData[j, i];
				blockData.t = BlockData.Type.BUILDING;
				blockData.sO = component;
				blockData.FlatHeight(roadAccess.h);
				blockData.b = true;
				component.blockPos.Add(new Vector2Int(j, i));
				CheckChunks(blockData);
			}
		}
		if (Indexer.Index == 30)
		{
			component.CustomSpawnCar(new Vector2(0.34f, 1.76f), 90);
			component.SpawnTree(new Vector3(component.Anchor.x - 1.1f, component.worldPos.y, component.Anchor.z + 1.82f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.64f, component.worldPos.y, component.Anchor.z - 1.18f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.77f, component.worldPos.y, component.Anchor.z - 0.66f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.45f, component.worldPos.y, component.Anchor.z + 1.8f));
			Indexer.Index = 0;
		}
		else if (Indexer.Index == 36)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 1.4f, component.worldPos.y, component.Anchor.z + 1.6f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.4f, component.worldPos.y, component.Anchor.z + 1.6f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.65f, component.worldPos.y, component.Anchor.z - 1.4f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.65f, component.worldPos.y, component.Anchor.z - 1.4f));
			Indexer.Index = 0;
		}
		else if (Indexer.Index == 31)
		{
			component.SpawnTree(new Vector3(component.Anchor.x - 1.72f, component.worldPos.y, component.Anchor.z + 0.85f));
			component.CustomSpawnCar(new Vector2(-0.6f, 1.72f), 90);
			Indexer.Index = 0;
		}
		else if (Indexer.Index == 59)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 1.1f, component.worldPos.y, component.Anchor.z - 1.4f));
			component.SpawnTree(new Vector3(component.Anchor.x - 0.1f, component.worldPos.y, component.Anchor.z - 1.6f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.6f, component.worldPos.y, component.Anchor.z - 1.4f));
			component.CustomSpawnCar(new Vector2(1.3f, 1.3f), 90);
			component.CustomSpawnCar(new Vector2(1.3f, 1.7f), 270);
			Indexer.Index = 0;
		}
		else if (Indexer.Index == 32)
		{
			component.SpawnTree(new Vector3(component.Anchor.x - 1.45f, component.worldPos.y, component.Anchor.z + 1.3f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1f, component.worldPos.y, component.Anchor.z - 1.2f));
			component.CustomSpawnCar(new Vector2(0.58f, 1.25f), 0);
			Indexer.Index = 0;
		}
		else if (Indexer.Index == 33)
		{
			component.CustomSpawnCar(new Vector2(0.94f, 1.43f), 0);
			component.CustomSpawnCar(new Vector2(0f, 1.43f), 0);
			component.CustomSpawnCar(new Vector2(-0.45f, 1.43f), 180);
			Indexer.Index = 0;
		}
		else if (Indexer.Index == 240)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 1.32f, component.worldPos.y, component.Anchor.z + 1.51f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.42f, component.worldPos.y, component.Anchor.z - 1.62f));
			Indexer.Index = 0;
		}
		else if (Indexer.Index == 241)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 1.3f, component.worldPos.y, component.Anchor.z + 1.2f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.2f, component.worldPos.y, component.Anchor.z + 1.2f));
			Indexer.Index = 0;
		}
		else if (Indexer.Index == 242)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 1.35f, component.worldPos.y, component.Anchor.z + 1.35f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.35f, component.worldPos.y, component.Anchor.z - 1.35f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.35f, component.worldPos.y, component.Anchor.z + 1.35f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.35f, component.worldPos.y, component.Anchor.z - 1.35f));
			Indexer.Index = 0;
		}
		else if (Indexer.Index == 104)
		{
			component.SpawnTree(new Vector3(component.Anchor.x, component.worldPos.y, component.Anchor.z));
			component.SpawnTree(new Vector3(component.Anchor.x, component.worldPos.y, component.Anchor.z + 1.37f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.34f, component.worldPos.y, component.Anchor.z + 1.3f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.3f, component.worldPos.y, component.Anchor.z + 1.3f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.32f, component.worldPos.y, component.Anchor.z - 1.28f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.28f, component.worldPos.y, component.Anchor.z - 1.28f));
			component.SpawnTree(new Vector3(component.Anchor.x, component.worldPos.y, component.Anchor.z - 1.3f));
		}
		else if (Indexer.Index == 103)
		{
			component.SpawnTree(new Vector3(component.Anchor.x, component.worldPos.y, component.Anchor.z));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.57f, component.worldPos.y, component.Anchor.z + 1.66f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.46f, component.worldPos.y, component.Anchor.z + 1.65f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.48f, component.worldPos.y, component.Anchor.z - 1.38f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.47f, component.worldPos.y, component.Anchor.z - 1.45f));
		}
		else if (Indexer.Index == 69)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 1.5f, component.worldPos.y, component.Anchor.z + 1.5f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.5f, component.worldPos.y, component.Anchor.z + 1.5f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.5f, component.worldPos.y, component.Anchor.z - 1.5f));
		}
		gameObject.transform.Rotate(0f, Orientation, 0f, Space.Self);
		gameObject.transform.parent = ServiceBuildings.transform;
		component.CreateVehicles(bi.world);
		component.CreateTrees(bi.world);
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
	}

	private void ServicePlopper3x3()
	{
		string path = "none";
		if (Indexer.Index == 98)
		{
			path = "Parks/Basket";
		}
		else if (Indexer.Index == 99)
		{
			path = "Parks/LargePlaza_A";
		}
		else if (Indexer.Index == 100)
		{
			path = "Parks/LargePlaza_B";
		}
		else if (Indexer.Index == 101)
		{
			path = "Parks/LargePlaza_C";
		}
		else if (Indexer.Index == 113)
		{
			float value = Random.value;
			if (value > 0.75f)
			{
				path = "Parks/BeachA";
			}
			else if (value > 0.5f)
			{
				path = "Parks/BeachB";
			}
			else
			{
				path = ((!(value > 0.25f)) ? "Parks/BeachD" : "Parks/BeachC");
			}
		}
		else if (Indexer.Index == 107)
		{
			path = "Parks/Tennis";
		}
		GameObject gameObject = Object.Instantiate(serviceUnit);
		ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
		((GameObject)Object.Instantiate(Resources.Load(path))).transform.parent = gameObject.transform;
		Center.y = bi.world.GetBounded((int)pos.x, (int)pos.z).h + 0.505f;
		gameObject.transform.position = Center;
		gameObject.name = pos.x + "_park_" + pos.z;
		component.worldPos = new Vector3(pos.x, Center.y, pos.z);
		component.Orientation = Orientation;
		component.Anchor = new Vector3(pos.x, pos.y, pos.z);
		component.TreesParent = new GameObject("TreesParent");
		component.TreesParent.transform.parent = component.gameObject.transform;
		if (Indexer.Index == 98 || Indexer.Index == 107)
		{
			component.Capacity = 64;
			component.type = ServiceCreator.Type.Park;
			component.Cost = 50;
			component.Maintenance = 50f;
			component.typeName = "L";
			costHelper.PayAmount(500);
			sm.Play("Park");
		}
		else if (Indexer.Index == 113)
		{
			component.Capacity = 80;
			component.type = ServiceCreator.Type.Park;
			component.Cost = 5;
			component.Maintenance = 5f;
			component.typeName = "L";
			costHelper.PayAmount(100);
			sm.Play("Park");
		}
		else
		{
			component.Capacity = 42;
			component.type = ServiceCreator.Type.Park;
			component.Cost = 90;
			component.Maintenance = 90f;
			component.typeName = "L";
			costHelper.PayAmount(900);
			sm.Play("Park");
		}
		sm.Play("PlopBuild");
		BlockData bounded = bi.world.GetBounded((int)pos.x - 2, (int)pos.z);
		BlockData bounded2 = bi.world.GetBounded((int)pos.x, (int)pos.z + 2);
		BlockData bounded3 = bi.world.GetBounded((int)pos.x + 2, (int)pos.z);
		BlockData bounded4 = bi.world.GetBounded((int)pos.x, (int)pos.z - 2);
		if (bounded.r)
		{
			component.Entry = bounded.wP;
		}
		else if (bounded2.r)
		{
			component.Entry = bounded2.wP;
		}
		else if (bounded3.r)
		{
			component.Entry = bounded3.wP;
		}
		else
		{
			component.Entry = bounded4.wP;
		}
		bi.world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
		roading.w.Services.Add(component);
		for (int i = (int)pos.z - 1; i <= (int)pos.z + 1; i++)
		{
			for (int j = (int)pos.x - 1; j <= (int)pos.x + 1; j++)
			{
				BlockData blockData = bi.world.blockData[j, i];
				blockData.t = BlockData.Type.BUILDING;
				blockData.sO = component;
				blockData.FlatHeight(bi.world.GetBounded((int)pos.x, (int)pos.z).h);
				blockData.b = true;
				component.blockPos.Add(new Vector2Int(j, i));
				CheckChunks(blockData);
			}
		}
		gameObject.transform.Rotate(0f, Orientation, 0f, Space.Self);
		gameObject.transform.parent = ServiceBuildings.transform;
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
	}

	private void GovPlopper2x2()
	{
		string path = "none";
		if (Indexer.Index == 46)
		{
			path = "Government/GovHelipad";
		}
		else if (Indexer.Index == 61)
		{
			path = "Facilities/Plant_Generator";
		}
		else if (Indexer.Index == 62)
		{
			path = "Facilities/WaterTower";
		}
		else if (Indexer.Index == 66)
		{
			path = "Facilities/CarbonCapture";
		}
		GameObject gameObject = Object.Instantiate(serviceUnit);
		ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
		((GameObject)Object.Instantiate(Resources.Load(path))).transform.parent = gameObject.transform;
		Center.y = roadAccess.h + 0.505f;
		gameObject.transform.position = Center;
		gameObject.name = pos.x + "_gov_" + pos.z;
		component.worldPos = new Vector3(pos.x, Center.y, pos.z);
		component.Orientation = Orientation;
		component.Anchor = new Vector3(pos.x - 0.5f, pos.y, pos.z - 0.5f);
		component.TreesParent = new GameObject("TreesParent");
		component.TreesParent.transform.parent = component.gameObject.transform;
		roading.w.Services.Add(component);
		if (Indexer.Index == 46)
		{
			component.Capacity = 1;
			component.Maintenance = 5f;
			component.MinEmployees = 0;
			component.MaxEmployees = 5;
			component.HiredEmployees = 0;
			component.type = ServiceCreator.Type.Government;
			component.MaxStage = 1;
			component.typeName = "Hsc";
			costHelper.PayForIt();
			indexer.CheckToUnlockFeature();
		}
		else if (Indexer.Index == 61)
		{
			component.Capacity = 550;
			component.Maintenance = 175f;
			component.MinEmployees = 1;
			component.MaxEmployees = 2;
			component.FixedEmployees = 1;
			component.HiredEmployees = 1;
			component.type = ServiceCreator.Type.Power;
			component.typeName = "D";
			component.MaxStage = 1;
			costHelper.PayForIt();
			sm.Play("Power");
			if (Messages.step == 4)
			{
				Messages.counter++;
				messages.SetArrowStep(4.3f);
			}
		}
		else if (Indexer.Index == 62)
		{
			component.Capacity = 900;
			component.Maintenance = 80f;
			component.MinEmployees = 1;
			component.MaxEmployees = 1;
			component.FixedEmployees = 1;
			component.HiredEmployees = 1;
			component.type = ServiceCreator.Type.Water;
			component.typeName = "Tower";
			component.MaxStage = 1;
			costHelper.PayForIt();
			sm.Play("Pumping");
			if (Messages.step == 4)
			{
				Messages.counter++;
			}
		}
		else if (Indexer.Index == 66)
		{
			component.Capacity = 1;
			component.Maintenance = 250f;
			component.MinEmployees = 1;
			component.MaxEmployees = 1;
			component.FixedEmployees = 1;
			component.HiredEmployees = 1;
			component.type = ServiceCreator.Type.Park;
			component.typeName = "CC";
			component.MaxStage = 1;
			costHelper.PayForIt();
			sm.Play("Park");
		}
		sm.Play("PlopBuild");
		RoadCreator access = roading.GetAccess((int)pos.x - 2, (int)pos.z);
		RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 1);
		RoadCreator access3 = roading.GetAccess((int)pos.x + 1, (int)pos.z);
		if (access != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x - 2, (int)pos.z).wP;
		}
		else if (access2 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 1).wP;
		}
		else if (access3 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x + 1, (int)pos.z).wP;
		}
		else
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 2).wP;
		}
		bi.world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
		for (int i = (int)pos.z - 1; i <= (int)pos.z; i++)
		{
			for (int j = (int)pos.x - 1; j <= (int)pos.x; j++)
			{
				BlockData blockData = bi.world.blockData[j, i];
				blockData.t = BlockData.Type.BUILDING;
				blockData.sO = component;
				blockData.FlatHeight(roadAccess.h);
				blockData.b = true;
				component.blockPos.Add(new Vector2Int(j, i));
				CheckChunks(blockData);
			}
		}
		if (Indexer.Index == 46)
		{
			Indexer.padPos = gameObject.transform.position + new Vector3(0f, 0.1f, 0f);
			indexer.copterModel.SetActive(value: true);
			indexer.copterModel.transform.position = gameObject.transform.position + new Vector3(0f, 0.1f, 0f);
			indexer.copterTooltip.header = "";
		}
		gameObject.transform.Rotate(0f, Orientation, 0f, Space.Self);
		gameObject.transform.parent = ServiceBuildings.transform;
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
		Indexer.Index = 0;
	}

	private void GovPlopper4x4()
	{
		string path = "none";
		if (Indexer.Index == 41)
		{
			path = "Government/GovImmigration";
		}
		else if (Indexer.Index == 42)
		{
			path = "Government/GovBank";
		}
		GameObject gameObject = Object.Instantiate(serviceUnit);
		ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
		((GameObject)Object.Instantiate(Resources.Load(path))).transform.parent = gameObject.transform;
		Center.y = roadAccess.h + 0.505f;
		gameObject.transform.position = Center;
		gameObject.name = pos.x + "_gov_" + pos.z;
		component.worldPos = new Vector3(pos.x, Center.y, pos.z);
		component.Orientation = Orientation;
		component.Anchor = new Vector3(pos.x - 0.5f, pos.y, pos.z - 0.5f);
		component.TreesParent = new GameObject("TreesParent");
		component.TreesParent.transform.parent = component.gameObject.transform;
		roading.w.Services.Add(component);
		if (Indexer.Index == 41)
		{
			component.MinEmployees = 0;
			component.MaxEmployees = 50;
			component.HiredEmployees = 0;
			component.Capacity = 250;
			component.Maintenance = 0f;
			component.type = ServiceCreator.Type.Government;
			component.MaxStage = 1;
			component.typeName = "Immisc";
			DataRegion.alreadyBuilt[0] = true;
			costHelper.PayForIt();
			sm.Play("Gov");
			B_ImmiPanel.interactable = true;
			indexer.B_Immi.interactable = false;
			messages.BlueBox.gameObject.SetActive(value: false);
			if (Messages.step == 18)
			{
				Messages.counter++;
			}
		}
		else if (Indexer.Index == 42)
		{
			component.MinEmployees = 0;
			component.MaxEmployees = 500;
			component.HiredEmployees = 0;
			component.Capacity = 250;
			component.Maintenance = 0f;
			component.type = ServiceCreator.Type.Government;
			component.MaxStage = 1;
			component.typeName = "CBsc";
			DataRegion.alreadyBuilt[1] = true;
			costHelper.PayForIt();
			sm.Play("Gov");
			B_BuyDollars.interactable = true;
			B_PrintMoney.interactable = true;
			messages.BlueBox.gameObject.SetActive(value: false);
			indexer.B_Bank.interactable = false;
		}
		sm.Play("PlopBuild");
		indexer.CheckToUnlockFeature();
		RoadCreator access = roading.GetAccess((int)pos.x - 3, (int)pos.z);
		RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 2);
		RoadCreator access3 = roading.GetAccess((int)pos.x + 2, (int)pos.z);
		if (access != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x - 3, (int)pos.z).wP;
		}
		else if (access2 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 2).wP;
		}
		else if (access3 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x + 2, (int)pos.z).wP;
		}
		else
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 3).wP;
		}
		bi.world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
		for (int i = (int)pos.z - 2; i <= (int)pos.z + 1; i++)
		{
			for (int j = (int)pos.x - 2; j <= (int)pos.x + 1; j++)
			{
				BlockData blockData = bi.world.blockData[j, i];
				blockData.t = BlockData.Type.BUILDING;
				blockData.sO = component;
				blockData.FlatHeight(roadAccess.h);
				blockData.b = true;
				component.blockPos.Add(new Vector2Int(j, i));
				CheckChunks(blockData);
			}
		}
		if (Indexer.Index == 41)
		{
			component.SpawnTree(new Vector3(component.Anchor.x - 1.75f, component.worldPos.y, component.Anchor.z + 1.5f));
		}
		else
		{
			_ = Indexer.Index;
			_ = 42;
		}
		gameObject.transform.Rotate(0f, Orientation, 0f, Space.Self);
		gameObject.transform.parent = ServiceBuildings.transform;
		component.CreateTrees(bi.world);
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
		Indexer.Index = 0;
	}

	private void SpecialPlopper6x6()
	{
		string path = "none";
		if (Indexer.Index == 80)
		{
			path = "Government/ArcoA";
		}
		else if (Indexer.Index == 81)
		{
			path = "Government/ArcoB";
		}
		else if (Indexer.Index == 82)
		{
			path = "Government/ArcoC";
		}
		else if (Indexer.Index == 67)
		{
			path = "Government/GovGulag";
		}
		else if (Indexer.Index == 68)
		{
			path = "Government/GovRehab";
		}
		else if (Indexer.Index == 112)
		{
			path = "Parks/PublicPool";
		}
		else if (Indexer.Index == 58)
		{
			path = "Facilities/Plant_Turbine";
		}
		GameObject gameObject = Object.Instantiate(serviceUnit);
		ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
		((GameObject)Object.Instantiate(Resources.Load(path))).transform.parent = gameObject.transform;
		Center.y = roadAccess.h + 0.505f;
		gameObject.transform.position = Center;
		gameObject.name = pos.x + "_special_" + pos.z;
		component.worldPos = new Vector3(pos.x, Center.y, pos.z);
		component.Orientation = Orientation - 90;
		component.Anchor = new Vector3(pos.x - 0.5f, pos.y, pos.z - 0.5f);
		component.TreesParent = new GameObject("TreesParent");
		component.TreesParent.transform.parent = component.gameObject.transform;
		if (Indexer.Index == 80)
		{
			component.MinEmployees = 0;
			component.MaxEmployees = 3000;
			component.Maintenance = 250f;
			component.type = ServiceCreator.Type.Arcology;
			component.typeName = "ArA";
			component.Stage = 5;
			bi.world.Arcologies.Add(component);
			costHelper.PayForIt();
			sm.Play("Future");
		}
		else if (Indexer.Index == 81)
		{
			component.MinEmployees = 0;
			component.MaxEmployees = 6000;
			component.Maintenance = 500f;
			component.type = ServiceCreator.Type.Arcology;
			component.typeName = "ArB";
			component.Stage = 6;
			bi.world.Arcologies.Add(component);
			costHelper.PayForIt();
			sm.Play("Future");
		}
		else if (Indexer.Index == 82)
		{
			component.MinEmployees = 0;
			component.MaxEmployees = 12000;
			component.Maintenance = 750f;
			component.type = ServiceCreator.Type.Arcology;
			component.typeName = "ArC";
			component.Stage = 7;
			bi.world.Arcologies.Add(component);
			costHelper.PayForIt();
			sm.Play("Future");
		}
		else if (Indexer.Index == 67)
		{
			component.Capacity = 550;
			component.Maintenance = 0f;
			component.MinEmployees = 5;
			component.MaxEmployees = 5;
			component.FixedEmployees = 0;
			component.HiredEmployees = 5;
			component.type = ServiceCreator.Type.Prison;
			component.typeName = "Gulag";
			component.MaxStage = 1;
			component.Use = 0;
			costHelper.PayForIt();
			sm.Play("Prison");
			roading.w.Services.Add(component);
		}
		else if (Indexer.Index == 68)
		{
			component.Capacity = 400;
			component.Maintenance = 850f;
			component.MinEmployees = 35;
			component.MaxEmployees = 35;
			component.FixedEmployees = 0;
			component.HiredEmployees = 35;
			component.type = ServiceCreator.Type.Prison;
			component.typeName = "Rehab";
			component.MaxStage = 1;
			component.Use = 0;
			costHelper.PayForIt();
			sm.Play("Prison");
			roading.w.Services.Add(component);
		}
		else if (Indexer.Index == 112)
		{
			component.Capacity = 100;
			component.type = ServiceCreator.Type.Park;
			component.Cost = 125;
			component.Maintenance = 125f;
			component.typeName = "L";
			costHelper.PayForIt();
			sm.Play("Park");
			roading.w.Services.Add(component);
		}
		else if (Indexer.Index == 58)
		{
			component.Capacity = 400;
			component.Maintenance = 70f;
			component.MinEmployees = 1;
			component.MaxEmployees = 1;
			component.FixedEmployees = 1;
			component.HiredEmployees = 1;
			component.type = ServiceCreator.Type.Power;
			component.MaxStage = 1;
			component.typeName = "W";
			costHelper.PayForIt();
			sm.Play("Power");
			roading.w.Services.Add(component);
		}
		sm.Play("PlopBuild");
		RoadCreator access = roading.GetAccess((int)pos.x - 4, (int)pos.z);
		RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 3);
		RoadCreator access3 = roading.GetAccess((int)pos.x + 3, (int)pos.z);
		if (access != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x - 4, (int)pos.z).wP;
		}
		else if (access2 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 3).wP;
		}
		else if (access3 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x + 3, (int)pos.z).wP;
		}
		else
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 4).wP;
		}
		bi.world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
		for (int i = (int)pos.z - 3; i <= (int)pos.z + 2; i++)
		{
			for (int j = (int)pos.x - 3; j <= (int)pos.x + 2; j++)
			{
				BlockData blockData = bi.world.blockData[j, i];
				blockData.t = BlockData.Type.BUILDING;
				blockData.sO = component;
				blockData.FlatHeight(roadAccess.h);
				blockData.b = true;
				component.blockPos.Add(new Vector2Int(j, i));
				CheckChunks(blockData);
			}
		}
		if (Indexer.Index == 80)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 2f, component.worldPos.y, component.Anchor.z + 2.23f));
			component.SpawnTree(new Vector3(component.Anchor.x + 0.78f, component.worldPos.y, component.Anchor.z + 2.22f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.63f, component.worldPos.y, component.Anchor.z + 1.57f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.07f, component.worldPos.y, component.Anchor.z + 0.01f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.67f, component.worldPos.y, component.Anchor.z - 0.99f));
			component.SpawnTree(new Vector3(component.Anchor.x + 0.78f, component.worldPos.y, component.Anchor.z - 2.24f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.95f, component.worldPos.y, component.Anchor.z - 2.23f));
		}
		else if (Indexer.Index == 81)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 2.5f, component.worldPos.y, component.Anchor.z - 2.5f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1f, component.worldPos.y, component.Anchor.z - 2.5f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1f, component.worldPos.y, component.Anchor.z - 2.5f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.5f, component.worldPos.y, component.Anchor.z - 2.5f));
			component.SpawnTree(new Vector3(component.Anchor.x - 0f, component.worldPos.y, component.Anchor.z - 1.5f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.35f, component.worldPos.y, component.Anchor.z + 2.35f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.35f, component.worldPos.y, component.Anchor.z + 2.35f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.65f, component.worldPos.y, component.Anchor.z + 1.5f));
			component.SpawnTree(new Vector3(component.Anchor.x + 2.65f, component.worldPos.y, component.Anchor.z + 1.5f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.65f, component.worldPos.y, component.Anchor.z - 0.5f));
			component.SpawnTree(new Vector3(component.Anchor.x + 2.65f, component.worldPos.y, component.Anchor.z - 0.5f));
		}
		else if (Indexer.Index == 82)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 2.51f, component.worldPos.y, component.Anchor.z + 2.1f));
			component.SpawnTree(new Vector3(component.Anchor.x + 0.77f, component.worldPos.y, component.Anchor.z + 2.44f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.23f, component.worldPos.y, component.Anchor.z + 2.77f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.02f, component.worldPos.y, component.Anchor.z - 2.43f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.01f, component.worldPos.y, component.Anchor.z - 1.57f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.01f, component.worldPos.y, component.Anchor.z + 2.52f));
			component.SpawnTree(new Vector3(component.Anchor.x + 2.62f, component.worldPos.y, component.Anchor.z - 2.42f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.32f, component.worldPos.y, component.Anchor.z - 2.43f));
		}
		else if (Indexer.Index == 68)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 2.37f, component.worldPos.y, component.Anchor.z + 0.49f));
			component.SpawnTree(new Vector3(component.Anchor.x + 2.39f, component.worldPos.y, component.Anchor.z - 2.52f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.59f, component.worldPos.y, component.Anchor.z - 2.24f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.77f, component.worldPos.y, component.Anchor.z + 0.74f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.55f, component.worldPos.y, component.Anchor.z + 2.59f));
			component.SpawnTree(new Vector3(component.Anchor.x + 0.36f, component.worldPos.y, component.Anchor.z + 1.33f));
			component.SpawnTree(new Vector3(component.Anchor.x + 0.1f, component.worldPos.y, component.Anchor.z - 2.59f));
		}
		else if (Indexer.Index == 112)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 2.73f, component.worldPos.y, component.Anchor.z - 2.2f));
			component.SpawnTree(new Vector3(component.Anchor.x + 2.73f, component.worldPos.y, component.Anchor.z - 1.3f));
			component.SpawnTree(new Vector3(component.Anchor.x + 2.73f, component.worldPos.y, component.Anchor.z - 0.2f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.76f, component.worldPos.y, component.Anchor.z - 1.9f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.76f, component.worldPos.y, component.Anchor.z + 0f));
			component.SpawnTree(new Vector3(component.Anchor.x + 2f, component.worldPos.y, component.Anchor.z + 2.9f));
		}
		gameObject.transform.Rotate(0f, Orientation - 90, 0f, Space.Self);
		gameObject.transform.parent = ServiceBuildings.transform;
		component.CreateTrees(bi.world);
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
		Indexer.Index = 0;
	}

	private void MarinaPlopper6x6()
	{
		string path = "none";
		if (Indexer.Index == 102)
		{
			path = "Parks/Marina";
		}
		else if (Indexer.Index == 106)
		{
			path = "Parks/Pier";
		}
		GameObject gameObject = Object.Instantiate(serviceUnit);
		ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
		GameObject gameObject2 = (GameObject)Object.Instantiate(Resources.Load(path));
		gameObject2.transform.parent = gameObject.transform;
		Center.y = roadAccess.h + 0.505f;
		gameObject.transform.position = Center;
		gameObject.name = pos.x + "_special_" + pos.z;
		component.worldPos = new Vector3(pos.x, Center.y, pos.z);
		component.Orientation = Orientation - 90;
		component.Anchor = new Vector3(pos.x - 0.5f, pos.y, pos.z - 0.5f);
		if (Indexer.Index == 102)
		{
			component.Capacity = 160;
			component.type = ServiceCreator.Type.Park;
			component.Cost = 75;
			component.Maintenance = 75f;
			component.typeName = "L";
			costHelper.PayAmount(marinaPrice);
			sm.Play("Park");
		}
		else if (Indexer.Index == 106)
		{
			component.Capacity = 200;
			component.type = ServiceCreator.Type.Park;
			component.Cost = 150;
			component.Maintenance = 150f;
			component.typeName = "L";
			costHelper.PayAmount(3500);
			sm.Play("Park");
		}
		sm.Play("PlopBuild");
		bi.world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
		roading.w.Services.Add(component);
		for (int i = (int)pos.z - 3; i <= (int)pos.z + 2; i++)
		{
			for (int j = (int)pos.x - 3; j <= (int)pos.x + 2; j++)
			{
				BlockData blockData = bi.world.blockData[j, i];
				blockData.t = BlockData.Type.BUILDING;
				blockData.sO = component;
				blockData.b = true;
				component.blockPos.Add(new Vector2Int(j, i));
				CheckChunks(blockData);
			}
		}
		if (Indexer.Index == 102)
		{
			component.CustomSpawnSpecialVehicle(new Vector2(0.42f, -1.6f), 0, Dico.P_Boats);
			component.CustomSpawnSpecialVehicle(new Vector2(-0.59f, -1.6f), 0, Dico.P_Boats);
			component.CustomSpawnSpecialVehicle(new Vector2(-1.22f, -1.6f), 0, Dico.P_Boats);
			component.CustomSpawnSpecialVehicle(new Vector2(0.42f, 1.6f), 180, Dico.P_Boats);
			component.CustomSpawnSpecialVehicle(new Vector2(-0.59f, 1.6f), 180, Dico.P_Boats);
			component.CustomSpawnSpecialVehicle(new Vector2(-1.22f, 1.6f), 180, Dico.P_Boats);
		}
		gameObject.transform.Rotate(0f, Orientation - 90, 0f, Space.Self);
		gameObject.transform.parent = ServiceBuildings.transform;
		BoatSpawner componentInChildren = gameObject2.GetComponentInChildren<BoatSpawner>();
		if (componentInChildren != null)
		{
			componentInChildren.w = bi.world;
		}
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
		Indexer.Index = 0;
	}

	private void PortPlopper12x12()
	{
		GameObject gameObject = Object.Instantiate(serviceUnit);
		ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
		GameObject gameObject2 = (GameObject)Object.Instantiate(Resources.Load("Services/Port"));
		gameObject2.transform.parent = gameObject.transform;
		Center.y = 30.25f;
		gameObject.transform.position = Center;
		gameObject.name = pos.x + "_port_" + pos.z;
		component.worldPos = new Vector3(pos.x, Center.y + 0.505f, pos.z);
		component.Orientation = Orientation - 90;
		component.Anchor = new Vector3(pos.x - 0.5f, pos.y, pos.z - 0.5f);
		component.TreesParent = new GameObject("TreesParent");
		component.TreesParent.transform.parent = component.gameObject.transform;
		roading.w.Services.Add(component);
		component.Capacity = 400;
		component.type = ServiceCreator.Type.Port;
		component.MaxStage = 1;
		component.Cost = 350;
		component.Maintenance = 150f;
		component.MinEmployees = 50;
		component.MaxEmployees = 50;
		component.HiredEmployees = 50;
		costHelper.PayAmount(PortPrice);
		sm.Play("Port");
		sm.Play("PlopBuild");
		BlockData bounded = bi.world.GetBounded((int)pos.x - 7, (int)pos.z);
		BlockData bounded2 = bi.world.GetBounded((int)pos.x, (int)pos.z + 6);
		BlockData bounded3 = bi.world.GetBounded((int)pos.x + 6, (int)pos.z);
		BlockData bounded4 = bi.world.GetBounded((int)pos.x, (int)pos.z - 7);
		if (bounded.r)
		{
			component.Entry = bounded.wP;
		}
		else if (bounded2.r)
		{
			component.Entry = bounded2.wP;
		}
		else if (bounded3.r)
		{
			component.Entry = bounded3.wP;
		}
		else
		{
			component.Entry = bounded4.wP;
		}
		bi.world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
		roading.w.Services.Add(component);
		for (int i = (int)pos.z - 6; i <= (int)pos.z + 5; i++)
		{
			for (int j = (int)pos.x - 6; j <= (int)pos.x + 5; j++)
			{
				BlockData blockData = bi.world.blockData[j, i];
				blockData.t = BlockData.Type.BUILDING;
				blockData.sO = component;
				if (blockData.h > 30.25f)
				{
					blockData.FlatHeight(30.25f);
				}
				blockData.b = true;
				component.blockPos.Add(new Vector2Int(j, i));
				CheckChunks(blockData);
			}
		}
		component.CustomSpawnCar(new Vector2(5.45f, 2.75f), 180);
		component.CustomSpawnCar(new Vector2(5.45f, 3.76f), 180);
		component.CustomSpawnCar(new Vector2(5.45f, 4.25f), 0);
		component.CustomSpawnCar(new Vector2(3.6f, 5.25f), 180);
		component.CustomSpawnCar(new Vector2(3.6f, 4.25f), 0);
		component.CustomSpawnCar(new Vector2(3.6f, 3.75f), 0);
		component.CustomSpawnCar(new Vector2(3.6f, 3.25f), 0);
		component.CustomSpawnCar(new Vector2(3.6f, 2.25f), 0);
		gameObject.transform.Rotate(0f, Orientation, 0f, Space.Self);
		gameObject.transform.parent = ServiceBuildings.transform;
		component.CreateVehicles(bi.world);
		ShipSpawner componentInChildren = gameObject2.GetComponentInChildren<ShipSpawner>();
		if (componentInChildren != null)
		{
			componentInChildren.w = bi.world;
		}
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
		Indexer.Index = 0;
	}

	private void GovPlopper8x8()
	{
		string path = "none";
		if (Indexer.Index == 40)
		{
			path = "Government/GovHQ_S1";
		}
		else if (Indexer.Index == 60)
		{
			path = "Facilities/Plant_Coal";
		}
		else if (Indexer.Index == 83)
		{
			path = "Facilities/StateMedia";
		}
		else if (Indexer.Index == 47)
		{
			path = "Government/GovCongress";
		}
		else if (Indexer.Index == 64)
		{
			path = "Facilities/Plant_Nuclear";
		}
		else if (Indexer.Index == 65)
		{
			path = "Facilities/Plant_Fusion";
		}
		else if (Indexer.Index == 85)
		{
			path = "Government/Edenium";
		}
		else if (Indexer.Index == 37)
		{
			path = "Services/Prison_Evo1";
		}
		else if (Indexer.Index == 111)
		{
			path = "Parks/Museum";
		}
		else if (Indexer.Index == 114)
		{
			path = "Parks/Holodeck";
		}
		GameObject gameObject = Object.Instantiate(serviceUnit);
		ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
		((GameObject)Object.Instantiate(Resources.Load(path))).transform.parent = gameObject.transform;
		Center.y = roadAccess.h + 0.505f;
		gameObject.transform.position = Center;
		gameObject.name = pos.x + "_gov_" + pos.z;
		component.worldPos = new Vector3(pos.x, Center.y, pos.z);
		component.Orientation = Orientation;
		component.Anchor = new Vector3(pos.x - 0.5f, pos.y, pos.z - 0.5f);
		component.TreesParent = new GameObject("TreesParent");
		component.TreesParent.transform.parent = component.gameObject.transform;
		if (Indexer.Index == 40)
		{
			component.Capacity = 1;
			component.Maintenance = 0f;
			component.MinEmployees = 0;
			component.MaxEmployees = 5000;
			component.FixedEmployees = 0;
			component.HiredEmployees = 0;
			component.MaxStage = 5;
			component.type = ServiceCreator.Type.Government;
			component.typeName = "HQsc";
			costHelper.PayForIt();
			sm.Play("Gov");
			DataRegion.alreadyBuilt[9] = true;
			indexer.eco.HQbuilding = component;
			indexer.legislation.UnlockNewPolicies(1);
			if (Messages.step == 9)
			{
				Messages.counter++;
			}
			messages.BlueBox.gameObject.SetActive(value: false);
		}
		else if (Indexer.Index == 60)
		{
			component.Capacity = 4000;
			component.Maintenance = 800f;
			component.MinEmployees = 1;
			component.MaxEmployees = 3;
			component.FixedEmployees = 10;
			component.HiredEmployees = 1;
			component.type = ServiceCreator.Type.Power;
			component.typeName = "C";
			component.MaxStage = 1;
			costHelper.PayForIt();
			sm.Play("Power");
		}
		else if (Indexer.Index == 83)
		{
			component.Capacity = 1;
			component.Maintenance = 50f;
			component.MinEmployees = 35;
			component.MaxEmployees = 35;
			component.FixedEmployees = 0;
			component.HiredEmployees = 35;
			component.type = ServiceCreator.Type.Religion;
			DataRegion.alreadyBuilt[14] = true;
			component.typeName = "StateMed";
			costHelper.PayForIt();
			sm.Play("Gov");
			indexer.B_Research.interactable = false;
		}
		else if (Indexer.Index == 85)
		{
			component.Capacity = 1;
			component.Maintenance = 50f;
			component.MinEmployees = 5;
			component.MaxEmployees = 5;
			component.FixedEmployees = 0;
			component.HiredEmployees = 5;
			component.type = ServiceCreator.Type.Religion;
			component.typeName = "Eden";
			costHelper.PayForIt();
			sm.Play("Pagoda");
			DataRegion.alreadyBuilt[16] = true;
		}
		else if (Indexer.Index == 47)
		{
			component.Capacity = 1;
			component.Maintenance = 0f;
			component.MinEmployees = 0;
			component.MaxEmployees = 5000;
			component.FixedEmployees = 0;
			component.HiredEmployees = 0;
			component.type = ServiceCreator.Type.Government;
			component.typeName = "Capitol";
			component.MaxStage = 1;
			costHelper.PayForIt();
			DataRegion.alreadyBuilt[6] = true;
			sm.Play("Gov");
			indexer.B_Congress.interactable = false;
			messages.BlueBox.gameObject.SetActive(value: false);
		}
		else if (Indexer.Index == 64)
		{
			component.Capacity = 8000;
			component.Maintenance = 2200f;
			component.MinEmployees = 1;
			component.MaxEmployees = 5;
			component.FixedEmployees = 58;
			component.HiredEmployees = 1;
			component.type = ServiceCreator.Type.Power;
			component.typeName = "N";
			component.MaxStage = 1;
			costHelper.PayForIt();
			sm.Play("Power");
		}
		else if (Indexer.Index == 65)
		{
			component.Capacity = 19000;
			component.Maintenance = 1600f;
			component.MinEmployees = 1;
			component.MaxEmployees = 16;
			component.FixedEmployees = 200;
			component.HiredEmployees = 1;
			component.type = ServiceCreator.Type.Power;
			component.typeName = "F";
			component.MaxStage = 1;
			costHelper.PayForIt();
			sm.Play("Power");
		}
		else if (Indexer.Index == 37)
		{
			component.Capacity = 275;
			component.Maintenance = 350f;
			component.MinEmployees = 10;
			component.MaxEmployees = 10;
			component.FixedEmployees = 10;
			component.HiredEmployees = 10;
			component.type = ServiceCreator.Type.Prison;
			component.typeName = "Prison";
			component.MaxStage = 2;
			costHelper.PayForIt();
			sm.Play("Prison");
		}
		else if (Indexer.Index == 111)
		{
			component.Capacity = 350;
			component.type = ServiceCreator.Type.Park;
			component.Cost = 475;
			component.Maintenance = 475f;
			component.typeName = "L";
			costHelper.PayForIt();
			sm.Play("Park");
		}
		else if (Indexer.Index == 114)
		{
			component.Capacity = 2000;
			component.type = ServiceCreator.Type.Park;
			component.Cost = 550;
			component.Maintenance = 550f;
			component.typeName = "L";
			costHelper.PayForIt();
			sm.Play("Park");
		}
		sm.Play("PlopBuild");
		RoadCreator access = roading.GetAccess((int)pos.x - 5, (int)pos.z);
		RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 4);
		RoadCreator access3 = roading.GetAccess((int)pos.x + 4, (int)pos.z);
		if (access != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x - 5, (int)pos.z).wP;
		}
		else if (access2 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 4).wP;
		}
		else if (access3 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x + 4, (int)pos.z).wP;
		}
		else
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 5).wP;
		}
		bi.world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
		roading.w.Services.Add(component);
		for (int i = (int)pos.z - 4; i <= (int)pos.z + 3; i++)
		{
			for (int j = (int)pos.x - 4; j <= (int)pos.x + 3; j++)
			{
				BlockData blockData = bi.world.blockData[j, i];
				blockData.t = BlockData.Type.BUILDING;
				blockData.sO = component;
				if (Indexer.Index == 111)
				{
					blockData.FlatHeight(roadAccess.h - 0.5f);
				}
				else
				{
					blockData.FlatHeight(roadAccess.h);
				}
				blockData.b = true;
				component.blockPos.Add(new Vector2Int(j, i));
				CheckChunks(blockData);
			}
		}
		if (Indexer.Index == 40)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 3.6f, component.worldPos.y, component.Anchor.z + 3.6f));
			component.SpawnTree(new Vector3(component.Anchor.x + 3.6f, component.worldPos.y, component.Anchor.z + 2f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.6f, component.worldPos.y, component.Anchor.z + 2f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.6f, component.worldPos.y, component.Anchor.z + 3.6f));
			component.SpawnTree(new Vector3(component.Anchor.x + 2.85f, component.worldPos.y, component.Anchor.z - 1.45f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.85f, component.worldPos.y, component.Anchor.z - 1.45f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.3f, component.worldPos.y, component.Anchor.z - 3.45f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.3f, component.worldPos.y, component.Anchor.z - 3.45f));
			component.CustomSpawnCar(new Vector2(1.15f, 1.9f), 270);
			component.CustomSpawnCar(new Vector2(-1.05f, 1.9f), 270);
			B_Legislation.interactable = true;
		}
		else if (Indexer.Index == 83)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 0.13f, component.worldPos.y, component.Anchor.z - 0.13f));
			component.SpawnTree(new Vector3(component.Anchor.x + 0.13f, component.worldPos.y, component.Anchor.z + 3.1f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.5f, component.worldPos.y, component.Anchor.z + 3.5f));
			component.SpawnTree(new Vector3(component.Anchor.x + 2.5f, component.worldPos.y, component.Anchor.z + 3.5f));
			component.SpawnTree(new Vector3(component.Anchor.x + 2.9f, component.worldPos.y, component.Anchor.z + 1.7f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.2f, component.worldPos.y, component.Anchor.z + 1.7f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.95f, component.worldPos.y, component.Anchor.z - 1.85f));
			component.SpawnTree(new Vector3(component.Anchor.x + 2.8f, component.worldPos.y, component.Anchor.z - 1.85f));
			component.SpawnTree(new Vector3(component.Anchor.x + 2.27f, component.worldPos.y, component.Anchor.z - 3.22f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2f, component.worldPos.y, component.Anchor.z - 3.22f));
		}
		else if (Indexer.Index == 85)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 3.2f, component.worldPos.y, component.Anchor.z + 3.4f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.83f, component.worldPos.y, component.Anchor.z + 3.63f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.96f, component.worldPos.y, component.Anchor.z + 3.39f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.4f, component.worldPos.y, component.Anchor.z + 3.57f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.28f, component.worldPos.y, component.Anchor.z + 2.23f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.26f, component.worldPos.y, component.Anchor.z + 2.12f));
			component.SpawnTree(new Vector3(component.Anchor.x + 3.67f, component.worldPos.y, component.Anchor.z + 0.39f));
			component.SpawnTree(new Vector3(component.Anchor.x + 3.16f, component.worldPos.y, component.Anchor.z - 3.17f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.35f, component.worldPos.y, component.Anchor.z - 1.74f));
			component.SpawnTree(new Vector3(component.Anchor.x - 0.74f, component.worldPos.y, component.Anchor.z - 1.95f));
			component.SpawnTree(new Vector3(component.Anchor.x + 0.24f, component.worldPos.y, component.Anchor.z - 3.75f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.08f, component.worldPos.y, component.Anchor.z - 3.02f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.77f, component.worldPos.y, component.Anchor.z - 0.64f));
			component.SpawnTree(new Vector3(component.Anchor.x, component.worldPos.y, component.Anchor.z));
		}
		else if (Indexer.Index == 60)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 3.3f, component.worldPos.y, component.Anchor.z - 1.8f));
			component.SpawnTree(new Vector3(component.Anchor.x + 2.7f, component.worldPos.y, component.Anchor.z + 3.7f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.6f, component.worldPos.y, component.Anchor.z - 0.7f));
			component.CustomSpawnCar(new Vector2(-2.7f, 2.6f), 0);
			component.CustomSpawnCar(new Vector2(-2.2f, 2.6f), 0);
			component.CustomSpawnCar(new Vector2(-1.7f, 2.6f), 180);
		}
		else if (Indexer.Index == 47)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 3.5f, component.worldPos.y, component.Anchor.z + 2f));
			component.SpawnTree(new Vector3(component.Anchor.x + 3.5f, component.worldPos.y, component.Anchor.z - 2f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.5f, component.worldPos.y, component.Anchor.z + 2f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.5f, component.worldPos.y, component.Anchor.z - 2f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2f, component.worldPos.y, component.Anchor.z - 3.5f));
			component.SpawnTree(new Vector3(component.Anchor.x + 2f, component.worldPos.y, component.Anchor.z - 3.5f));
		}
		else if (Indexer.Index == 64)
		{
			component.CustomSpawnCar(new Vector2(-1.65f, 3.3f), 0);
			component.CustomSpawnCar(new Vector2(-2.55f, 3.3f), 0);
			component.CustomSpawnCar(new Vector2(-2.97f, 3.3f), 180);
			component.CustomSpawnCar(new Vector2(-3.4f, 3.3f), 0);
			component.CustomSpawnCar(new Vector2(0.16f, 2.05f), 90);
			component.CustomSpawnCar(new Vector2(0.16f, 1.59f), 270);
		}
		else if (Indexer.Index == 65)
		{
			component.CustomSpawnCar(new Vector2(3.48f, 2.54f), 0);
			component.CustomSpawnCar(new Vector2(2.52f, 2.54f), 0);
			component.CustomSpawnCar(new Vector2(2.05f, 2.54f), 180);
			component.CustomSpawnCar(new Vector2(1.58f, 2.54f), 0);
			component.CustomSpawnCar(new Vector2(0.65f, 2.54f), 0);
			component.CustomSpawnCar(new Vector2(-0.31f, 2.54f), 180);
			component.CustomSpawnCar(new Vector2(-0.8f, 2.54f), 0);
		}
		else if (Indexer.Index == 111)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 3.4f, component.worldPos.y, component.Anchor.z + 3.53f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.9f, component.worldPos.y, component.Anchor.z - 2.96f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.77f, component.worldPos.y, component.Anchor.z - 0.2f));
			component.SpawnTree(new Vector3(component.Anchor.x - 0.75f, component.worldPos.y, component.Anchor.z + 1.54f));
		}
		else if (Indexer.Index == 114)
		{
			component.SpawnTree(new Vector3(component.Anchor.x - 3.8f, component.worldPos.y, component.Anchor.z + 2.3f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.8f, component.worldPos.y, component.Anchor.z + 1.3f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.8f, component.worldPos.y, component.Anchor.z + 0.3f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.8f, component.worldPos.y, component.Anchor.z - 0.7f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.8f, component.worldPos.y, component.Anchor.z - 1.7f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.8f, component.worldPos.y, component.Anchor.z - 2.7f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.8f, component.worldPos.y, component.Anchor.z - 3.7f));
			component.SpawnTree(new Vector3(component.Anchor.x + 3.8f, component.worldPos.y, component.Anchor.z + 2.3f));
			component.SpawnTree(new Vector3(component.Anchor.x + 3.8f, component.worldPos.y, component.Anchor.z + 1.3f));
			component.SpawnTree(new Vector3(component.Anchor.x + 3.8f, component.worldPos.y, component.Anchor.z + 0.3f));
			component.SpawnTree(new Vector3(component.Anchor.x + 3.8f, component.worldPos.y, component.Anchor.z - 0.7f));
			component.SpawnTree(new Vector3(component.Anchor.x + 3.8f, component.worldPos.y, component.Anchor.z - 1.7f));
			component.SpawnTree(new Vector3(component.Anchor.x + 3.8f, component.worldPos.y, component.Anchor.z - 2.7f));
			component.SpawnTree(new Vector3(component.Anchor.x + 3.8f, component.worldPos.y, component.Anchor.z - 3.7f));
			component.SpawnTree(new Vector3(component.Anchor.x + 2f, component.worldPos.y, component.Anchor.z + 3.4f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2f, component.worldPos.y, component.Anchor.z + 3.4f));
			component.SpawnTree(new Vector3(component.Anchor.x + 2.4f, component.worldPos.y, component.Anchor.z - 3.6f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1f, component.worldPos.y, component.Anchor.z - 3.6f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1f, component.worldPos.y, component.Anchor.z - 3.6f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.4f, component.worldPos.y, component.Anchor.z - 3.6f));
		}
		gameObject.transform.Rotate(0f, Orientation, 0f, Space.Self);
		gameObject.transform.parent = ServiceBuildings.transform;
		component.CreateVehicles(bi.world);
		component.CreateTrees(bi.world);
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
		Indexer.Index = 0;
		indexer.CheckToUnlockFeature();
	}

	private void RedPlopper8x8()
	{
		string path = "none";
		if (Indexer.Index == 84)
		{
			path = "Commercial/RedDistrict";
		}
		GameObject gameObject = Object.Instantiate(zoning.lotUnit);
		LotCreator component = gameObject.GetComponent<LotCreator>();
		Center.y = roadAccess.h + 0.508f;
		gameObject.name = pos.x + "_Red_" + pos.z;
		GameObject gameObject2 = (GameObject)Object.Instantiate(Resources.Load(path));
		gameObject2.transform.Rotate(0f, Orientation - 90, 0f, Space.Self);
		gameObject2.transform.position = Center;
		gameObject2.transform.parent = gameObject.transform;
		component.r = roading;
		component.w = bi.world;
		if (Indexer.Index == 84)
		{
			component.wP = new Vector3(pos.x, Center.y - 0.5f, pos.z);
			component.Orientation = (short)(Orientation + 90);
			component.Anchor = new Vector3(pos.x - 0.5f, pos.y, pos.z - 0.5f);
			component.lType = LotCreator.LotType.COMMERCIAL;
			component.lClass = LotCreator.LotClass.RETAIL;
			component.lSize = LotCreator.LotSize._4x4;
			component.MaxStage = 0;
			component.Stage = 0;
			component.Occupants = 600 + Random.Range(-10, 20);
			component.desiredValue = 1;
			bi.world.blockData[component.Entry.x, component.Entry.y].o.lots.Add(component);
			bi.world.CommercialBuildings.Add(component);
			costHelper.PayForIt();
			sm.Play("Red");
			DataRegion.alreadyBuilt[15] = true;
		}
		sm.Play("PlopBuild");
		RoadCreator access = roading.GetAccess((int)pos.x - 5, (int)pos.z);
		RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 4);
		RoadCreator access3 = roading.GetAccess((int)pos.x + 4, (int)pos.z);
		if (access != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x - 5, (int)pos.z).wP;
		}
		else if (access2 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 4).wP;
		}
		else if (access3 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x + 4, (int)pos.z).wP;
		}
		else
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 5).wP;
		}
		for (int i = (int)pos.z - 4; i <= (int)pos.z + 3; i++)
		{
			for (int j = (int)pos.x - 4; j <= (int)pos.x + 3; j++)
			{
				BlockData blockData = bi.world.blockData[j, i];
				blockData.t = BlockData.Type.BUILDING;
				blockData.lO = component;
				blockData.FlatHeight(roadAccess.h);
				blockData.b = true;
				component.blockPos.Add(new Vector2Int(j, i));
				CheckChunks(blockData);
			}
		}
		GameObject gameObject3 = new GameObject();
		gameObject3.transform.position = Vector3.zero;
		gameObject3.transform.parent = gameObject2.transform;
		gameObject3.name = "ground";
		gameObject.transform.parent = lotParent.transform;
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
		Indexer.Index = 0;
		indexer.CheckToUnlockFeature();
	}

	private void MinePlopper8x8()
	{
		if (roadAccess != null)
		{
			Center.y = roadAccess.h + 0.508f;
		}
		string path;
		if (_resource == "Gas")
		{
			path = "Industrial/P_Gas";
		}
		else if (_resource == "Rare")
		{
			path = "Industrial/P_Rare";
		}
		else if (_resource == "Mine")
		{
			path = "Industrial/P_Mine";
		}
		else
		{
			if (!(_resource == "Oil"))
			{
				Debug.Log("No Resource?");
				return;
			}
			if (pos.y < 29.5f)
			{
				path = "Industrial/P_Offshore";
				Center.y = 30.75f;
			}
			else
			{
				path = "Industrial/P_Oil";
			}
		}
		GameObject gameObject = Object.Instantiate(zoning.lotUnit);
		LotCreator component = gameObject.GetComponent<LotCreator>();
		gameObject.name = pos.x + "_Mine_" + pos.z;
		GameObject gameObject2 = (GameObject)Object.Instantiate(Resources.Load(path));
		gameObject2.transform.Rotate(0f, Orientation - 90, 0f, Space.Self);
		gameObject2.transform.position = Center;
		gameObject2.transform.parent = gameObject.transform;
		component.r = roading;
		component.w = bi.world;
		component.wP = new Vector3(pos.x, Center.y - 0.5f, pos.z);
		component.Orientation = (short)(Orientation + 90);
		component.Anchor = new Vector3(pos.x - 0.5f, pos.y, pos.z - 0.5f);
		component.lType = LotCreator.LotType.INDUSTRIAL;
		component.lClass = LotCreator.LotClass.PRIMARY;
		component.lSize = LotCreator.LotSize._4x4;
		component.MaxStage = 0;
		component.Stage = 0;
		component.Occupants = Mathf.RoundToInt(10f + 0.5f * prevalence);
		component.desiredValue = 1;
		bi.world.blockData[component.Entry.x, component.Entry.y].o.lots.Add(component);
		bi.world.IndustrialBuildings.Add(component);
		costHelper.PayForIt();
		sm.Play("Mine");
		sm.Play("PlopBuild");
		RoadCreator access = roading.GetAccess((int)pos.x - 5, (int)pos.z);
		RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 4);
		RoadCreator access3 = roading.GetAccess((int)pos.x + 4, (int)pos.z);
		if (access != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x - 5, (int)pos.z).wP;
		}
		else if (access2 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 4).wP;
		}
		else if (access3 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x + 4, (int)pos.z).wP;
		}
		else
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 5).wP;
		}
		for (int i = (int)pos.z - 4; i <= (int)pos.z + 3; i++)
		{
			for (int j = (int)pos.x - 4; j <= (int)pos.x + 3; j++)
			{
				BlockData blockData = bi.world.blockData[j, i];
				blockData.t = BlockData.Type.BUILDING;
				blockData.lO = component;
				if (pos.y >= 29.5f)
				{
					blockData.FlatHeight(roadAccess.h);
				}
				blockData.b = true;
				component.blockPos.Add(new Vector2Int(j, i));
				CheckChunks(blockData);
			}
		}
		if (_resource == "Mine")
		{
			for (int k = (int)pos.z - 3; k <= (int)pos.z + 2; k++)
			{
				for (int l = (int)pos.x - 3; l <= (int)pos.x + 2; l++)
				{
					bi.world.blockData[l, k].FlatHeight(roadAccess.h - 0.75f);
				}
			}
			for (int m = (int)pos.z - 2; m <= (int)pos.z + 1; m++)
			{
				for (int n = (int)pos.x - 2; n <= (int)pos.x + 1; n++)
				{
					bi.world.blockData[n, m].FlatHeight(roadAccess.h - 1.5f);
				}
			}
			for (int num = (int)pos.z - 1; num <= (int)pos.z; num++)
			{
				for (int num2 = (int)pos.x - 1; num2 <= (int)pos.x; num2++)
				{
					bi.world.blockData[num2, num].FlatHeight(roadAccess.h - 2.25f);
				}
			}
		}
		GameObject gameObject3 = new GameObject();
		gameObject3.transform.position = Vector3.zero;
		gameObject3.transform.parent = gameObject2.transform;
		gameObject3.name = "ground";
		gameObject.transform.parent = lotParent.transform;
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
		Indexer.Index = 0;
	}

	private void GovPlopper12x12()
	{
		string path = "none";
		if (Indexer.Index == 35)
		{
			path = "Services/Gov_Univ";
		}
		GameObject gameObject = Object.Instantiate(serviceUnit);
		ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
		((GameObject)Object.Instantiate(Resources.Load(path))).transform.parent = gameObject.transform;
		Center.y = roadAccess.h + 0.505f;
		gameObject.transform.position = Center;
		gameObject.name = pos.x + "_gov_" + pos.z;
		component.worldPos = new Vector3(pos.x, Center.y, pos.z);
		component.Orientation = Orientation;
		component.Anchor = new Vector3(pos.x - 0.5f, pos.y, pos.z - 0.5f);
		component.TreesParent = new GameObject("TreesParent");
		component.TreesParent.transform.parent = component.gameObject.transform;
		if (Indexer.Index == 35)
		{
			component.Capacity = 24000;
			component.Maintenance = 540f;
			component.MinEmployees = 100;
			component.MaxEmployees = 1000;
			component.FixedEmployees = 60;
			component.HiredEmployees = 100;
			component.Stage = 1;
			component.MaxStage = 1;
			component.type = ServiceCreator.Type.School;
			component.typeName = "U";
			costHelper.PayForIt();
			sm.Play("HighSchool");
			DataRegion.alreadyBuilt[5] = true;
		}
		sm.Play("PlopBuild");
		RoadCreator access = roading.GetAccess((int)pos.x - 7, (int)pos.z);
		RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 6);
		RoadCreator access3 = roading.GetAccess((int)pos.x + 6, (int)pos.z);
		if (access != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x - 7, (int)pos.z).wP;
		}
		else if (access2 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 6).wP;
		}
		else if (access3 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x + 6, (int)pos.z).wP;
		}
		else
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 7).wP;
		}
		bi.world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
		roading.w.Services.Add(component);
		for (int i = (int)pos.z - 6; i <= (int)pos.z + 5; i++)
		{
			for (int j = (int)pos.x - 6; j <= (int)pos.x + 5; j++)
			{
				BlockData blockData = bi.world.blockData[j, i];
				blockData.t = BlockData.Type.BUILDING;
				blockData.sO = component;
				blockData.FlatHeight(roadAccess.h);
				blockData.b = true;
				component.blockPos.Add(new Vector2Int(j, i));
				CheckChunks(blockData);
			}
		}
		if (Indexer.Index == 35)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 3f, component.worldPos.y, component.Anchor.z + 4.68f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.42f, component.worldPos.y, component.Anchor.z + 5.69f));
			component.SpawnTree(new Vector3(component.Anchor.x + 0.93f, component.worldPos.y, component.Anchor.z + 1.05f));
			component.SpawnTree(new Vector3(component.Anchor.x + 3.14f, component.worldPos.y, component.Anchor.z + 1.49f));
			component.SpawnTree(new Vector3(component.Anchor.x + 5.64f, component.worldPos.y, component.Anchor.z + 0.92f));
			component.SpawnTree(new Vector3(component.Anchor.x + 5.8f, component.worldPos.y, component.Anchor.z - 3.14f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.98f, component.worldPos.y, component.Anchor.z - 1.96f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.81f, component.worldPos.y, component.Anchor.z - 2.57f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.42f, component.worldPos.y, component.Anchor.z + 0.7f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.7f, component.worldPos.y, component.Anchor.z + 0.18f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.78f, component.worldPos.y, component.Anchor.z - 1.12f));
			component.SpawnTree(new Vector3(component.Anchor.x - 5.78f, component.worldPos.y, component.Anchor.z + 0.52f));
			component.SpawnTree(new Vector3(component.Anchor.x - 4.68f, component.worldPos.y, component.Anchor.z - 0.28f));
			component.SpawnTree(new Vector3(component.Anchor.x - 5.47f, component.worldPos.y, component.Anchor.z - 1.57f));
			component.SpawnTree(new Vector3(component.Anchor.x - 4.38f, component.worldPos.y, component.Anchor.z - 2.63f));
			component.SpawnTree(new Vector3(component.Anchor.x - 5.79f, component.worldPos.y, component.Anchor.z - 4.1f));
			component.SpawnTree(new Vector3(component.Anchor.x - 4.54f, component.worldPos.y, component.Anchor.z - 4.63f));
			component.SpawnTree(new Vector3(component.Anchor.x - 5.3f, component.worldPos.y, component.Anchor.z - 5.5f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.93f, component.worldPos.y, component.Anchor.z - 5.07f));
			component.SpawnTree(new Vector3(component.Anchor.x - 2.49f, component.worldPos.y, component.Anchor.z - 3.8f));
			component.SpawnTree(new Vector3(component.Anchor.x - 0.7f, component.worldPos.y, component.Anchor.z - 5.9f));
			component.SpawnTree(new Vector3(component.Anchor.x + 0.45f, component.worldPos.y, component.Anchor.z - 5.4f));
			component.SpawnTree(new Vector3(component.Anchor.x - 0.49f, component.worldPos.y, component.Anchor.z - 3.58f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.06f, component.worldPos.y, component.Anchor.z - 3.26f));
			component.CustomSpawnCar(new Vector2(5.4f, 5.55f), 0);
			component.CustomSpawnCar(new Vector2(4.45f, 5.55f), 0);
			component.CustomSpawnCar(new Vector2(4f, 5.55f), 0);
			component.CustomSpawnCar(new Vector2(3.5f, 5.55f), 0);
			component.CustomSpawnCar(new Vector2(3f, 5.55f), 0);
		}
		gameObject.transform.Rotate(0f, Orientation, 0f, Space.Self);
		gameObject.transform.parent = ServiceBuildings.transform;
		component.CreateVehicles(bi.world);
		component.CreateTrees(bi.world);
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
		Indexer.Index = 0;
	}

	private void ServicePlopper16x16()
	{
		string path = "none";
		if (Indexer.Index == 110)
		{
			path = "Parks/Stadium";
		}
		GameObject gameObject = Object.Instantiate(serviceUnit);
		ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
		((GameObject)Object.Instantiate(Resources.Load(path))).transform.parent = gameObject.transform;
		Center.y = roadAccess.h + 0.505f;
		gameObject.transform.position = Center;
		gameObject.name = pos.x + "_gov_" + pos.z;
		component.worldPos = new Vector3(pos.x, Center.y, pos.z);
		component.Orientation = Orientation;
		component.Anchor = new Vector3(pos.x - 0.5f, pos.y, pos.z - 0.5f);
		component.TreesParent = new GameObject("TreesParent");
		component.TreesParent.transform.parent = component.gameObject.transform;
		if (Indexer.Index == 110)
		{
			component.Capacity = 3000;
			component.type = ServiceCreator.Type.Park;
			component.Cost = 1250;
			component.Maintenance = 1250f;
			component.typeName = "L";
			costHelper.PayForIt();
			sm.Play("Park");
		}
		sm.Play("PlopBuild");
		RoadCreator access = roading.GetAccess((int)pos.x - 9, (int)pos.z);
		RoadCreator access2 = roading.GetAccess((int)pos.x, (int)pos.z + 8);
		RoadCreator access3 = roading.GetAccess((int)pos.x + 8, (int)pos.z);
		roading.GetAccess((int)pos.x, (int)pos.z - 9);
		if (access != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x - 9, (int)pos.z).wP;
		}
		else if (access2 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z + 8).wP;
		}
		else if (access3 != null)
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x + 8, (int)pos.z).wP;
		}
		else
		{
			component.Entry = bi.world.GetIfNotNull((int)pos.x, (int)pos.z - 9).wP;
		}
		bi.world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
		roading.w.Services.Add(component);
		for (int i = (int)pos.z - 8; i <= (int)pos.z + 7; i++)
		{
			for (int j = (int)pos.x - 8; j <= (int)pos.x + 7; j++)
			{
				BlockData blockData = bi.world.blockData[j, i];
				blockData.t = BlockData.Type.BUILDING;
				blockData.sO = component;
				blockData.FlatHeight(roadAccess.h);
				blockData.b = true;
				component.blockPos.Add(new Vector2Int(j, i));
				CheckChunks(blockData);
			}
		}
		if (Indexer.Index == 110)
		{
			component.SpawnTree(new Vector3(component.Anchor.x + 3.8f, component.worldPos.y, component.Anchor.z + 6f));
			component.SpawnTree(new Vector3(component.Anchor.x + 3.8f, component.worldPos.y, component.Anchor.z + 7.7f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.75f, component.worldPos.y, component.Anchor.z + 6f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.75f, component.worldPos.y, component.Anchor.z + 7.7f));
			component.SpawnTree(new Vector3(component.Anchor.x + 7.65f, component.worldPos.y, component.Anchor.z + 7.5f));
			component.SpawnTree(new Vector3(component.Anchor.x - 7.65f, component.worldPos.y, component.Anchor.z + 7.5f));
			component.SpawnTree(new Vector3(component.Anchor.x - 7.6f, component.worldPos.y, component.Anchor.z + 4.7f));
			component.SpawnTree(new Vector3(component.Anchor.x - 7.6f, component.worldPos.y, component.Anchor.z + 3.2f));
			component.SpawnTree(new Vector3(component.Anchor.x - 7.6f, component.worldPos.y, component.Anchor.z + 0.7f));
			component.SpawnTree(new Vector3(component.Anchor.x - 7.6f, component.worldPos.y, component.Anchor.z - 0.7f));
			component.SpawnTree(new Vector3(component.Anchor.x - 7.6f, component.worldPos.y, component.Anchor.z - 3.4f));
			component.SpawnTree(new Vector3(component.Anchor.x - 7.6f, component.worldPos.y, component.Anchor.z - 4.6f));
			component.SpawnTree(new Vector3(component.Anchor.x - 7.6f, component.worldPos.y, component.Anchor.z - 7.4f));
			component.SpawnTree(new Vector3(component.Anchor.x + 7.6f, component.worldPos.y, component.Anchor.z + 4.7f));
			component.SpawnTree(new Vector3(component.Anchor.x + 7.6f, component.worldPos.y, component.Anchor.z + 3.2f));
			component.SpawnTree(new Vector3(component.Anchor.x + 7.6f, component.worldPos.y, component.Anchor.z + 0.7f));
			component.SpawnTree(new Vector3(component.Anchor.x + 7.6f, component.worldPos.y, component.Anchor.z - 0.7f));
			component.SpawnTree(new Vector3(component.Anchor.x + 7.6f, component.worldPos.y, component.Anchor.z - 3.4f));
			component.SpawnTree(new Vector3(component.Anchor.x + 7.6f, component.worldPos.y, component.Anchor.z - 4.6f));
			component.SpawnTree(new Vector3(component.Anchor.x + 7.6f, component.worldPos.y, component.Anchor.z - 7.4f));
			component.SpawnTree(new Vector3(component.Anchor.x - 4.5f, component.worldPos.y, component.Anchor.z - 7.7f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.4f, component.worldPos.y, component.Anchor.z - 7.7f));
			component.SpawnTree(new Vector3(component.Anchor.x - 0.6f, component.worldPos.y, component.Anchor.z - 7.7f));
			component.SpawnTree(new Vector3(component.Anchor.x + 0.5f, component.worldPos.y, component.Anchor.z - 7.7f));
			component.SpawnTree(new Vector3(component.Anchor.x + 3.5f, component.worldPos.y, component.Anchor.z - 7.7f));
			component.SpawnTree(new Vector3(component.Anchor.x + 4.5f, component.worldPos.y, component.Anchor.z - 7.7f));
		}
		gameObject.transform.Rotate(0f, Orientation, 0f, Space.Self);
		gameObject.transform.parent = ServiceBuildings.transform;
		component.CreateTrees(bi.world);
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
		Indexer.Index = 0;
	}

	private void AirportPlopper14x8()
	{
		if (Orientation == 0 || Orientation == 180)
		{
			for (int i = (int)pos.z - 14; i <= (int)pos.z + 13; i++)
			{
				for (int j = (int)pos.x - 8; j <= (int)pos.x + 7; j++)
				{
					if (bi.world.GetIfNotNull(j, i) == null)
					{
						sm.Play("Impractical");
						return;
					}
				}
			}
		}
		else
		{
			for (int k = (int)pos.z - 8; k <= (int)pos.z + 7; k++)
			{
				for (int l = (int)pos.x - 14; l <= (int)pos.x + 13; l++)
				{
					if (bi.world.GetIfNotNull(l, k) == null)
					{
						sm.Play("Impractical");
						return;
					}
				}
			}
		}
		GameObject gameObject = Object.Instantiate(serviceUnit);
		ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
		((GameObject)Object.Instantiate(Resources.Load("Services/Airport_EvoZero"))).transform.parent = gameObject.transform;
		Center.y = roadAccess.h + 0.505f;
		gameObject.transform.position = Center;
		gameObject.name = pos.x + "_gov_" + pos.z;
		component.worldPos = new Vector3(pos.x, Center.y, pos.z);
		component.Orientation = Orientation;
		component.Anchor = new Vector3(pos.x - 0.5f, pos.y, pos.z - 0.5f);
		component.TreesParent = new GameObject("TreesParent");
		component.TreesParent.transform.parent = component.gameObject.transform;
		component.Capacity = 150;
		component.Maintenance = 100f;
		component.MinEmployees = 1;
		component.MaxEmployees = 3;
		component.FixedEmployees = 20;
		component.HiredEmployees = 2;
		component.type = ServiceCreator.Type.Airport;
		component.MaxStage = 4;
		component.Stage = 0;
		costHelper.PayForIt();
		sm.Play("Airport");
		sm.Play("PlopBuild");
		component.Entry = roadAccess.wP;
		roadAccess.o.services.Add(component);
		roading.w.Services.Add(component);
		if (Orientation == 0 || Orientation == 180)
		{
			for (int m = (int)pos.z - 14; m <= (int)pos.z + 13; m++)
			{
				for (int n = (int)pos.x - 8; n <= (int)pos.x + 7; n++)
				{
					BlockData blockData = bi.world.blockData[n, m];
					blockData.t = BlockData.Type.BUILDING;
					blockData.sO = component;
					blockData.FlatHeight(roadAccess.h);
					blockData.b = true;
					component.blockPos.Add(new Vector2Int(n, m));
					CheckChunks(blockData);
				}
			}
		}
		else
		{
			for (int num = (int)pos.z - 8; num <= (int)pos.z + 7; num++)
			{
				for (int num2 = (int)pos.x - 14; num2 <= (int)pos.x + 13; num2++)
				{
					BlockData blockData2 = bi.world.blockData[num2, num];
					blockData2.t = BlockData.Type.BUILDING;
					blockData2.sO = component;
					blockData2.FlatHeight(roadAccess.h);
					blockData2.b = true;
					component.blockPos.Add(new Vector2Int(num2, num));
					CheckChunks(blockData2);
				}
			}
		}
		component.CustomSpawnCar(new Vector2(0.91f, 10.1f), 180);
		component.CustomSpawnCar(new Vector2(1.87f, 11f), 0);
		component.CustomSpawnCar(new Vector2(1.87f, 10.05f), 0);
		component.SpawnTree(new Vector3(component.Anchor.x - 3.8f, component.worldPos.y, component.Anchor.z + 12.7f));
		component.SpawnTree(new Vector3(component.Anchor.x - 5.7f, component.worldPos.y, component.Anchor.z + 10.3f));
		component.SpawnTree(new Vector3(component.Anchor.x - 5.8f, component.worldPos.y, component.Anchor.z - 0.3f));
		component.SpawnTree(new Vector3(component.Anchor.x - 4.2f, component.worldPos.y, component.Anchor.z - 2.6f));
		component.SpawnTree(new Vector3(component.Anchor.x - 7f, component.worldPos.y, component.Anchor.z + 0.4f));
		component.SpawnTree(new Vector3(component.Anchor.x - 7.2f, component.worldPos.y, component.Anchor.z - 5.6f));
		component.SpawnTree(new Vector3(component.Anchor.x - 4.1f, component.worldPos.y, component.Anchor.z - 12.6f));
		component.SpawnTree(new Vector3(component.Anchor.x - 6.1f, component.worldPos.y, component.Anchor.z - 11.4f));
		gameObject.transform.Rotate(0f, Orientation, 0f, Space.Self);
		gameObject.transform.parent = ServiceBuildings.transform;
		component.CreateVehicles(bi.world);
		component.CreateTrees(bi.world);
		StartCoroutine(UpdateChunks());
		costHelper.gameObject.SetActive(value: false);
		Indexer.Index = 0;
		if (Messages.step == 28)
		{
			Messages.counter++;
		}
	}

	private void ParkPlopper1x1()
	{
		BlockData ifNotNull = bi.world.GetIfNotNull((int)pos.x, (int)pos.z);
		if (ifNotNull != null)
		{
			string pT = "none";
			if (Indexer.Index == 90)
			{
				pT = "Parks/SmallFountain";
			}
			else if (Indexer.Index == 91)
			{
				pT = "Parks/SmallFlowers";
			}
			else if (Indexer.Index == 92)
			{
				pT = "Parks/SmallStatue";
			}
			else if (Indexer.Index == 93)
			{
				pT = "Parks/SmallPlaza_B";
			}
			else if (Indexer.Index == 94)
			{
				pT = "Parks/SmallPlaza_C";
			}
			else if (Indexer.Index == 95)
			{
				pT = "Parks/SmallPlaza_D";
			}
			else if (Indexer.Index == 96)
			{
				pT = "Parks/SmallPlaza_E";
			}
			else if (Indexer.Index == 97)
			{
				pT = "Parks/SmallPlaza_F";
			}
			GameObject gameObject = Object.Instantiate(serviceUnit);
			ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
			gameObject.transform.position = Center;
			gameObject.transform.parent = ServiceBuildings.transform;
			component.worldPos = new Vector3(pos.x, Center.y, pos.z);
			component.Orientation = Orientation;
			component.Anchor = new Vector3(pos.x - 0.5f, pos.y, pos.z - 0.5f);
			roading.w.Services.Add(component);
			component.Capacity = 6;
			component.Cost = 10;
			component.type = ServiceCreator.Type.Park;
			component.Maintenance = 10f;
			costHelper.PayForIt();
			sm.Play("Park");
			ifNotNull.t = BlockData.Type.BUILDING;
			ifNotNull.sO = component;
			component.blockPos.Add(ifNotNull.wP);
			ifNotNull.b = true;
			ifNotNull.g = false;
			ifNotNull.pT = pT;
			Chunk o = ifNotNull.o;
			o.services.Add(component);
			updates.Add(o);
			StartCoroutine(UpdateChunks());
		}
	}

	private void BusPlopper()
	{
		BlockData blockData = bi.world.blockData[(int)pos.x, (int)pos.z];
		if (blockData == null)
		{
			Debug.Log("Block is null, can't build park");
			return;
		}
		RoadCreator roadCreator = roading.rD[(int)pos.x, 1, (int)pos.z];
		if (!(roadCreator.tile == "straight_bus") && !(roadCreator.tile == "straight_sub") && isFree && isConnected && !(roadCreator == null))
		{
			roadCreator.tile = "straight_bus";
			roadCreator.StopCapacity = 50000;
			roadCreator.stopType = RoadCreator.StopType.BUS;
			roadCreator.maintenance = 75f;
			roading.busStops.Add(roadCreator);
			costHelper.PayForIt();
			sm.Play("RoadBuilt");
			blockData.o.UpdateRoads();
		}
	}

	private void SubwayPlopper()
	{
		BlockData ifNotNull = bi.world.GetIfNotNull((int)pos.x, (int)pos.z);
		if (ifNotNull == null)
		{
			return;
		}
		int num = 0;
		RoadCreator roadCreator = roading.rD[(int)pos.x, 1, (int)pos.z];
		if (isFree && isConnected && roadCreator != null && !(roadCreator.tile == "straight_sub") && !(roadCreator.path == "straight_bus"))
		{
			if (roadCreator.cc == 1)
			{
				num = 180;
			}
			else if (roadCreator.cc == 2)
			{
				num = 0;
			}
			else if (roadCreator.cc == 3)
			{
				num = 90;
			}
			else if (roadCreator.cc == 4)
			{
				num = 270;
			}
			roadCreator.tile = "straight_sub";
			roadCreator.StopCapacity = 100000;
			roadCreator.stopType = RoadCreator.StopType.TRAIN;
			roadCreator.maintenance = 125f;
			ifNotNull.o.UpdateRoads();
			Vector3 position = new Vector3(pos.x, Center.y - 1.5f, pos.z);
			GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Roads/SubwayStation"));
			gameObject.transform.position = position;
			gameObject.name = pos.x + "_subwayStation_" + pos.z;
			gameObject.transform.Rotate(0f, num, 0f, Space.Self);
			gameObject.isStatic = false;
			gameObject.layer = 14;
			gameObject.transform.parent = roadCreator.gameObject.transform;
			RoadCreator roadCreator2 = roading.rG[(int)pos.x, 0, (int)pos.z];
			roadCreator2.stopType = RoadCreator.StopType.TRAIN;
			roadCreator2.StopCapacity = 100000;
			roading.trainStops.Add(roadCreator2);
			costHelper.PayForIt();
			sm.Play("PlopBuild");
			sm.Play("TrainStation");
		}
	}

	private void VegetationPlopper()
	{
		BlockData blockData = bi.world.blockData[(int)pos.x, (int)pos.z];
		if (blockData.t != BlockData.Type.ROAD && blockData.t != BlockData.Type.BUILDING)
		{
			if (Indexer.Index == 55)
			{
				blockData.t = BlockData.Type.GRASSTREE;
				blockData.g = false;
			}
			else if (Indexer.Index == 56)
			{
				blockData.t = BlockData.Type.GRASS;
				blockData.g = true;
				blockData.bio = 68;
			}
			updates.Add(blockData.o);
			StartCoroutine(UpdateChunks());
			sm.Play("Tree");
		}
	}

	private void CheckChunks(BlockData b)
	{
		updates.Add(b.o);
		if (b.pos[0] == 0)
		{
			BlockData bounded = bi.world.GetBounded(b.wP.x - 1, b.wP.y);
			updates.Add(bounded.o);
		}
		else if (b.pos[0] == 15)
		{
			BlockData bounded2 = bi.world.GetBounded(b.wP.x + 1, b.wP.y);
			updates.Add(bounded2.o);
		}
		if (b.pos[1] == 0)
		{
			BlockData bounded3 = bi.world.GetBounded(b.wP.x, b.wP.y - 1);
			updates.Add(bounded3.o);
		}
		else if (b.pos[1] == 15)
		{
			BlockData bounded4 = bi.world.GetBounded(b.wP.x, b.wP.y + 1);
			updates.Add(bounded4.o);
		}
	}

	private IEnumerator UpdateChunks()
	{
		foreach (Chunk update in updates)
		{
			update.Redraw();
			yield return new WaitForEndOfFrame();
		}
		updates.Clear();
	}
}
