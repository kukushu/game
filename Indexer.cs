using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AzureSky;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Indexer : MonoBehaviour
{
	public Color blue;

	public Color green;

	public Color yellow;

	public Color red;

	public Color csWhite;

	public Color purple;

	public Vector3 lastCamPos;

	public static int Index = 0;

	public int debugIndex;

	public static int Sub_Index = 1;

	public static bool isZero = true;

	public static bool sliderOnScreen = false;

	public static int cycle = 0;

	private float doubleClickTimeLimit = 1f;

	public static float Hover_X = 0f;

	public static float Hover_Z = 0f;

	public static string roadSubType;

	public static string roadStyle;

	public Zoning zoning;

	public BlockInteraction bi;

	public Economy eco;

	public News news;

	public Messages messages;

	public Plopping plopping;

	public Research research;

	public CostHelper costHelper;

	public Gamespeed gamespeed;

	public Slider Demand_LC_P;

	public TextMeshProUGUI Tot_LC;

	public TextMeshProUGUI Growth_LC;

	public Slider Demand_LC_N;

	public Slider Demand_MC_P;

	public TextMeshProUGUI Tot_MC;

	public TextMeshProUGUI Growth_MC;

	public Slider Demand_MC_N;

	public Slider Demand_UC_P;

	public TextMeshProUGUI Tot_UC;

	public TextMeshProUGUI Growth_UC;

	public Slider Demand_UC_N;

	public Slider Demand_M_P;

	public TextMeshProUGUI Tot_M;

	public TextMeshProUGUI Growth_M;

	public Slider Demand_M_N;

	public Slider Demand_HT_P;

	public TextMeshProUGUI Tot_HT;

	public TextMeshProUGUI Growth_HT;

	public Slider Demand_HT_N;

	public Slider Demand_R_P;

	public TextMeshProUGUI Tot_R;

	public TextMeshProUGUI Growth_R;

	public Slider Demand_R_N;

	public Slider Demand_O_P;

	public TextMeshProUGUI Tot_O;

	public TextMeshProUGUI Growth_O;

	public Slider Demand_O_N;

	public Slider Demand_S_P;

	public TextMeshProUGUI Tot_S;

	public TextMeshProUGUI Growth_S;

	public Slider Demand_S_N;

	public Slider Demand_Res_P;

	public Slider Demand_Res_N;

	public Slider Demand_Com_P;

	public Slider Demand_Com_N;

	public Slider Demand_Ind_P;

	public Slider Demand_Ind_N;

	public Text IndexInfo;

	public TextMeshProUGUI PopInfo;

	public TextMeshProUGUI GovType;

	public TextMeshProUGUI Unemployment;

	public TextMeshProUGUI ApprovalRate;

	public TextMeshProUGUI StabilityRate;

	public TextMeshProUGUI FreedomRate;

	public TextMeshProUGUI CivilRightsRate;

	public TextMeshProUGUI SocialLadder;

	public GameObject CensusPanel;

	public GameObject leftDetailsPanel;

	public GameObject rightDetailsPanel;

	public TextMeshProUGUI LCpop;

	public TextMeshProUGUI LCpercent;

	public TextMeshProUGUI MCpop;

	public TextMeshProUGUI MCpercent;

	public TextMeshProUGUI UCpop;

	public TextMeshProUGUI UCpercent;

	public TextMeshProUGUI Imports;

	public TextMeshProUGUI Exports;

	public Slider AffiliationSlider;

	public GameObject SchoolPlacer;

	public GameObject HighSchoolPlacer;

	public GameObject UniversityPlacer;

	public GameObject HQPlacer;

	public GameObject ImmigrationPlacer;

	public GameObject CommercePlacer;

	public GameObject SocialPlacer;

	public GameObject BankPlacer;

	public GameObject HospitalPlacer;

	public GameObject FirePlacer;

	public GameObject PolicePlacer;

	public GameObject AirportPlacer;

	public GameObject BusPlacer;

	public GameObject StationPlacer;

	public GameObject ConnectionPlacer;

	public GameObject Universal1x;

	public GameObject CoalPlant;

	public GameObject Generators;

	public GameObject SolarPlant;

	public GameObject NuclearPlant;

	public GameObject HydrogenPlant;

	public GameObject Turbine;

	public GameObject Pumping;

	public GameObject WaterTower;

	public GameObject HelipadPlacer;

	public GameObject HLM1;

	public GameObject HLM2;

	public GameObject HLM3;

	public GameObject Empty4x;

	public GameObject Empty8x;

	public GameObject ramps;

	public GameObject aveRamps;

	public GameObject ArcoA;

	public GameObject ArcoB;

	public GameObject ArcoC;

	public GameObject Marina;

	public GameObject Pier;

	public GameObject Congress;

	public GameObject Media;

	public GameObject Carbon;

	public GameObject Empty6x;

	public GameObject Drone;

	public GameObject PortPlacer;

	public GameObject Universal16x;

	public GameObject gridRoad;

	public GameObject gridAve;

	public GameObject gridRail;

	public GameObject gridBus;

	public GameObject gridTrain;

	public Material avenueGridMat;

	public Texture avenueTexture;

	public Texture highwayTexture;

	public Button R_low;

	public Button R_mid;

	public Button R_hi;

	public Button C_low;

	public Button C_mid;

	public Button C_hi;

	public Button I_low;

	public Button I_mid;

	public Button I_hi;

	public Button B_Immi;

	public Button B_ImmiPanel;

	public Button B_Bank;

	public Button B_BuyDollars;

	public Button B_SellDollars;

	public Button B_Comm;

	public Button B_EcoPolTab;

	public Button B_Social;

	public Button B_SocPolTab;

	public Button B_Congress;

	public Button B_CusPolTab;

	public Button B_Research;

	public Button B_ResearchPanel;

	public Button B_HQ;

	public Button B_Legislation;

	public Button B_Univ;

	public Button B_Mixed;

	public Button B_Stadium;

	public Button B_Museum;

	public Button B_Pool;

	public Button B_Holo;

	public Sprite B_Mixed_Active;

	public Image EduLogo;

	public Image SecuLogo;

	public Image HealthLogo;

	public Image TrafficLogo;

	public Image PollutionLogo;

	public TextMeshProUGUI EduRate;

	public TextMeshProUGUI HealthRate;

	public TextMeshProUGUI SecuRate;

	public Slider powerSlider;

	public Image powerArrow;

	public Slider waterSlider;

	public Image waterArrow;

	public MouseOver powerOver;

	public MouseOver waterOver;

	public TextMeshProUGUI Inflation;

	public TextMeshProUGUI Corruption;

	public Minimap minimap;

	public RTS_Camera rtsCam;

	public GameObject UndergroundFolder;

	public GameObject BioFolder;

	public GameObject WaterPlane;

	public GameObject WorldFolder;

	public Light Sun;

	public static bool undergroundDestroy = false;

	public ServiceTool serviceTool;

	public Legislation_UI legislation;

	public GameObject cityCamera;

	public GameObject copterCamera;

	public GameObject copterModel;

	public static bool copterMode = false;

	public static Vector3 padPos;

	public TooltipTrigger copterTooltip;

	public GameObject heliControls;

	public HelicopterController heliController;

	public GameObject carModel;

	public Rigidbody carRigidBody;

	public GameObject carCamera;

	public Camera cameraCar;

	public GameObject carControls;

	public Image FlagRight;

	public Image FlagLegislation;

	public Image FlagBudget;

	public Image FlagLoading;

	public Material FlagMaterial;

	public Material HelicoFlagMaterial;

	public PostProcessVolume ppv;

	private Bloom bloom;

	private AutoExposure autoExposure;

	public GameObject immiPanel;

	public GameObject lesiglationPanel;

	public GameObject budgetPanel;

	public GameObject statsPanel;

	public GameObject settingsPanel;

	public GameObject researchPanel;

	public GameObject layerButtons;

	public GameObject optionButtons;

	public GameObject upperUI;

	public GameObject lowerUI;

	public GameObject super;

	public int superSub;

	public GameObject residentialLow;

	public GameObject residentialMid;

	public GameObject residentialHi;

	public GameObject residentialCus;

	public GameObject commercialLow;

	public GameObject commercialMid;

	public GameObject commercialHi;

	public GameObject industrialLow;

	public GameObject industrialMid;

	public GameObject industrialHi;

	public GameObject mixLow;

	public GameObject mixMid;

	public GameObject mixHi;

	public GameObject oneStar;

	public GameObject twoStars;

	public GameObject threeStars;

	public bool secuCapExceeded;

	public TooltipTrigger Power_W;

	public TooltipTrigger Water_W;

	public TooltipTrigger Budget_W;

	public TooltipTrigger Unemployment_W;

	public TooltipTrigger Layer_W;

	public TooltipTrigger Edu_W;

	public TooltipTrigger Health_W;

	public TooltipTrigger Secu_W;

	public SubMenu subMenu;

	public Image dayNightImage;

	public Sprite full;

	public Sprite day;

	public Sprite night;

	public AzureTimeController timeController;

	public TooltipTrigger dayNightTT;

	public Slider timeSlider;

	private bool cycling;

	private bool userChangedTime;

	public static bool quit = false;

	public static int resStage = 1;

	public static int comStage = 1;

	public static int indStage = 1;

	public static int mixStage = 1;

	public TextMeshProUGUI ResDensity;

	public TextMeshProUGUI MixDensity;

	public TextMeshProUGUI ComDensity;

	public TextMeshProUGUI IndDensity;

	private bool isSaving;

	private void Start()
	{
		ppv.profile.TryGetSettings<Bloom>(out bloom);
		ppv.profile.TryGetSettings<AutoExposure>(out autoExposure);
		StartCoroutine(UIupdate());
		StartCoroutine(InputListener());
		if (DataRegion.FlagTexture == null)
		{
			Debug.Log("FLAG IS NULL, DEFAULT NOT SAVED");
			DataRegion.FlagTexture = bi.world.defaultFlag;
		}
		Sprite sprite = Sprite.Create(DataRegion.FlagTexture, new Rect(0f, 0f, DataRegion.FlagTexture.width, DataRegion.FlagTexture.height), new Vector2(0.5f, 0.5f));
		FlagRight.sprite = sprite;
		FlagLegislation.sprite = sprite;
		FlagLoading.sprite = sprite;
		FlagBudget.sprite = sprite;
		FlagMaterial.mainTexture = DataRegion.FlagTexture;
		HelicoFlagMaterial.mainTexture = DataRegion.FlagTexture;
		sliderOnScreen = false;
		cameraCar = carCamera.GetComponentInChildren<Camera>();
	}

	private void HidePlacers()
	{
		if (Index == 17 || Index == 191)
		{
			rtsCam.GoSurface();
			BioFolder.SetActive(value: true);
			WaterPlane.SetActive(value: true);
			WorldFolder.transform.position = new Vector3(0f, 0f, 0f);
			Sun.gameObject.SetActive(value: true);
		}
		Roading.sideType = "straight";
		Avenueing.sideType = "side";
		Avenueing.subType = "Avenue/";
		Avenueing.level = 1;
		Index = 0;
		bi.One_one.transform.position = new Vector3(-1000f, 1000f, -1000f);
		SchoolPlacer.SetActive(value: false);
		HighSchoolPlacer.SetActive(value: false);
		HospitalPlacer.SetActive(value: false);
		FirePlacer.SetActive(value: false);
		PolicePlacer.SetActive(value: false);
		HQPlacer.SetActive(value: false);
		ImmigrationPlacer.SetActive(value: false);
		CommercePlacer.SetActive(value: false);
		SocialPlacer.SetActive(value: false);
		Media.SetActive(value: false);
		AirportPlacer.SetActive(value: false);
		PortPlacer.SetActive(value: false);
		Universal1x.SetActive(value: false);
		StationPlacer.SetActive(value: false);
		ConnectionPlacer.SetActive(value: false);
		BankPlacer.SetActive(value: false);
		Generators.SetActive(value: false);
		CoalPlant.SetActive(value: false);
		Pumping.SetActive(value: false);
		Turbine.SetActive(value: false);
		WaterTower.SetActive(value: false);
		HelipadPlacer.SetActive(value: false);
		Empty4x.SetActive(value: false);
		Empty8x.SetActive(value: false);
		ramps.SetActive(value: false);
		aveRamps.SetActive(value: false);
		HLM1.SetActive(value: false);
		HLM2.SetActive(value: false);
		HLM3.SetActive(value: false);
		ArcoA.SetActive(value: false);
		ArcoB.SetActive(value: false);
		ArcoC.SetActive(value: false);
		UniversityPlacer.SetActive(value: false);
		bi.cursorManager.SetActive(value: false);
		HydrogenPlant.SetActive(value: false);
		NuclearPlant.SetActive(value: false);
		SolarPlant.SetActive(value: false);
		Congress.SetActive(value: false);
		Marina.SetActive(value: false);
		Pier.SetActive(value: false);
		Carbon.SetActive(value: false);
		Empty6x.SetActive(value: false);
		Drone.SetActive(value: false);
		Universal16x.SetActive(value: false);
	}

	private void HideCursors()
	{
		bi.roading.wSquare_1.transform.position = new Vector3(-1000f, 1000f, -1000f);
		bi.roading.wSquare_2.transform.position = new Vector3(-1000f, 1000f, -1000f);
		bi.roading.wSquare_3.transform.position = new Vector3(-1000f, 1000f, -1000f);
		bi.roading.wSquare_4.transform.position = new Vector3(-1000f, 1000f, -1000f);
		zoning.One_one.transform.position = new Vector3(-1000f, 1000f, -1000f);
		gridRoad.SetActive(value: false);
		gridAve.SetActive(value: false);
		gridRail.SetActive(value: false);
		gridBus.SetActive(value: false);
		gridTrain.SetActive(value: false);
	}

	private void Update()
	{
		if (Index != 0 && isZero)
		{
			isZero = false;
		}
		if (rtsCam.freeze)
		{
			return;
		}
		debugIndex = Index;
		if ((Input.GetKeyDown(KeyCode.Escape) && bi.tamere == 0) || (Index == 0 && !isZero))
		{
			HideAll();
			minimap.CloseLayers();
		}
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			eco.stats.CloseStats();
			eco.immi.ClosePanel();
			research.ClosePanel();
			eco.budget.ClosePanel();
			legislation.CloseLegislation();
			bi.serviceTool.CloseServiceEditor();
			bi.superTool.CloseSuperTool();
			BlockInteraction.following = false;
			bi.sm.Play("Close");
		}
		if (Input.GetKeyDown(KeyCode.Equals))
		{
			GoStateCar();
		}
		if (Input.GetKeyDown(KeyCode.Space) && !copterMode)
		{
			if (Time.timeScale == 0f)
			{
				if (gamespeed.previousSpeed == 1)
				{
					gamespeed.Speed_1();
				}
				else if (gamespeed.previousSpeed == 2)
				{
					gamespeed.Speed_2();
				}
				else
				{
					gamespeed.Speed_3();
				}
				bi.sm.Play("OpenPanel");
			}
			else
			{
				gamespeed.Speed_0();
				bi.sm.Play("OpenPanel");
			}
		}
		if (Input.GetKeyDown(KeyCode.Backspace))
		{
			if (Time.timeScale == 0f)
			{
				if (gamespeed.previousSpeed == 1)
				{
					gamespeed.Speed_1();
				}
				else if (gamespeed.previousSpeed == 2)
				{
					gamespeed.Speed_2();
				}
				else
				{
					gamespeed.Speed_3();
				}
				bi.sm.Play("OpenPanel");
			}
			else
			{
				if (gamespeed.previousSpeed == 1)
				{
					gamespeed.Speed_2();
				}
				else if (gamespeed.previousSpeed == 2)
				{
					gamespeed.Speed_3();
				}
				else
				{
					gamespeed.Speed_1();
				}
				bi.sm.Play("OpenPanel");
			}
		}
		if (Input.GetKeyDown(KeyCode.Escape) && copterMode)
		{
			ExitCopterMode();
			ExitStateCar();
		}
		if (Input.GetKeyDown(KeyCode.V))
		{
			HideShowUI();
		}
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			ResidentialClick();
		}
		if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			CommercialClick();
		}
		if (Input.GetKeyDown(KeyCode.Alpha3))
		{
			IndustrialClick();
		}
		if (Input.GetKeyDown(KeyCode.Alpha4))
		{
			LowDensityClick();
		}
		if (Input.GetKeyDown(KeyCode.Alpha5))
		{
			MiddleDensityClick();
		}
		if (Input.GetKeyDown(KeyCode.Alpha6))
		{
			HighDensityClick();
		}
		if (Input.GetKeyDown(KeyCode.B) && !copterMode)
		{
			DestroyClick();
		}
		if (Input.GetKeyDown(KeyCode.N))
		{
			CensusClick();
		}
		if (!userChangedTime)
		{
			timeSlider.value = timeController.m_timeline;
		}
		else
		{
			userChangedTime = false;
		}
	}

	private IEnumerator UIupdate()
	{
		while (true)
		{
			if (eco.LC_Demand >= 0f)
			{
				Demand_LC_N.value = 0f;
				Demand_LC_P.value = eco.LC_Demand;
			}
			else
			{
				Demand_LC_P.value = 0f;
				Demand_LC_N.value = Mathf.Abs(eco.LC_Demand);
			}
			Tot_LC.text = eco.LC_Demand.ToString("N0");
			if (eco.MC_Demand >= 0f)
			{
				Demand_MC_N.value = 0f;
				Demand_MC_P.value = eco.MC_Demand;
			}
			else
			{
				Demand_MC_P.value = 0f;
				Demand_MC_N.value = Mathf.Abs(eco.MC_Demand);
			}
			Tot_MC.text = eco.MC_Demand.ToString("N0");
			if (eco.UC_Demand >= 0f)
			{
				Demand_UC_N.value = 0f;
				Demand_UC_P.value = eco.UC_Demand;
			}
			else
			{
				Demand_UC_P.value = 0f;
				Demand_UC_N.value = Mathf.Abs(eco.UC_Demand);
			}
			Tot_UC.text = eco.UC_Demand.ToString("N0");
			if (eco.M_Demand >= 0f)
			{
				Demand_M_N.value = 0f;
				Demand_M_P.value = eco.M_Demand;
			}
			else
			{
				Demand_M_P.value = 0f;
				Demand_M_N.value = Mathf.Abs(eco.M_Demand);
			}
			Tot_M.text = eco.M_Demand.ToString("N0");
			if (eco.HT_Demand >= 0f)
			{
				Demand_HT_N.value = 0f;
				Demand_HT_P.value = eco.HT_Demand;
			}
			else
			{
				Demand_HT_P.value = 0f;
				Demand_HT_N.value = Mathf.Abs(eco.HT_Demand);
			}
			Tot_HT.text = eco.HT_Demand.ToString("N0");
			if (eco.R_Demand >= 0f)
			{
				Demand_R_N.value = 0f;
				Demand_R_P.value = eco.R_Demand;
			}
			else
			{
				Demand_R_P.value = 0f;
				Demand_R_N.value = Mathf.Abs(eco.R_Demand);
			}
			Tot_R.text = eco.R_Demand.ToString("N0");
			if (eco.O_Demand >= 0f)
			{
				Demand_O_N.value = 0f;
				Demand_O_P.value = eco.O_Demand;
			}
			else
			{
				Demand_O_P.value = 0f;
				Demand_O_N.value = Mathf.Abs(eco.O_Demand);
			}
			Tot_O.text = eco.O_Demand.ToString("N0");
			if (eco.Slum_Demand >= 0f)
			{
				Demand_S_N.value = 0f;
				Demand_S_P.value = eco.Slum_Demand;
			}
			else
			{
				Demand_S_P.value = 0f;
				Demand_S_N.value = Mathf.Abs(eco.Slum_Demand);
			}
			Tot_S.text = eco.Slum_Demand.ToString("N0");
			float num = eco.LC_Demand + eco.MC_Demand + eco.UC_Demand;
			float num2 = eco.R_Demand + eco.O_Demand;
			float num3 = eco.M_Demand + eco.HT_Demand;
			if (num >= 0f)
			{
				Demand_Res_N.value = 0f;
				Demand_Res_P.value = num;
			}
			else
			{
				Demand_Res_P.value = 0f;
				Demand_Res_N.value = Mathf.Abs(num);
			}
			if (num2 >= 0f)
			{
				Demand_Com_N.value = 0f;
				Demand_Com_P.value = num2;
			}
			else
			{
				Demand_Com_P.value = 0f;
				Demand_Com_N.value = Mathf.Abs(num2);
			}
			if (num3 >= 0f)
			{
				Demand_Ind_N.value = 0f;
				Demand_Ind_P.value = num3;
			}
			else
			{
				Demand_Ind_P.value = 0f;
				Demand_Ind_N.value = Mathf.Abs(num3);
			}
			LCpop.text = eco.LCPop.ToString("N0");
			LCpercent.text = eco.LCPartPercent.ToString("N0") + "%";
			MCpop.text = eco.MCPop.ToString("N0");
			MCpercent.text = eco.MCPartPercent.ToString("N0") + "%";
			UCpop.text = eco.UCPop.ToString("N0");
			UCpercent.text = eco.UCPartPercent.ToString("N0") + "%";
			yield return new WaitForEndOfFrame();
			IndexInfo.text = "x=" + Hover_X + " z=" + Hover_Z;
			if (eco.InformalPop > 500f)
			{
				PopInfo.text = "≈" + eco.Population.ToString("N0");
			}
			else
			{
				PopInfo.text = eco.Population.ToString("N0");
			}
			yield return new WaitForEndOfFrame();
			if (eco.Population < 400f)
			{
				Unemployment.text = "≈10%";
			}
			else
			{
				Unemployment.text = eco.Unemployment.ToString("F0") + "%";
			}
			if (eco.Unemployment <= -1f)
			{
				Unemployment.color = red;
			}
			else if (eco.Unemployment <= 1f)
			{
				Unemployment.color = yellow;
			}
			else if (eco.Unemployment <= 6f)
			{
				Unemployment.color = green;
			}
			else if (eco.Unemployment < 15f)
			{
				Unemployment.color = yellow;
			}
			else
			{
				Unemployment.color = red;
			}
			StabilityRate.text = eco.Stability.ToString("N0");
			SetTextColor(StabilityRate, (int)eco.Stability);
			FreedomRate.text = eco.FreedomIndex.ToString("N0");
			CivilRightsRate.text = eco.CivilRights.ToString("N0");
			EduLogo.color = new Color32((byte)(181f - eco.EducationLevel * 0.32f), (byte)(118f + eco.EducationLevel * 0.73f), 118, byte.MaxValue);
			EduRate.text = eco.EducationLevel.ToString("N0");
			HealthLogo.color = new Color32((byte)(181f - (float)eco.HealthScore * 0.32f), (byte)(118f + (float)eco.HealthScore * 0.73f), 118, byte.MaxValue);
			HealthRate.text = eco.HealthScore.ToString("N0");
			SecuLogo.color = new Color32((byte)(181f - (float)eco.SecurityScore * 0.32f), (byte)(118f + (float)eco.SecurityScore * 0.73f), 118, byte.MaxValue);
			SecuRate.text = eco.SecurityScore.ToString("N0");
			TrafficLogo.color = new Color32((byte)(181f - (100f - eco.Congestion) * 0.32f), (byte)(118f + (100f - eco.Congestion) * 0.73f), 118, byte.MaxValue);
			PollutionLogo.color = new Color32((byte)(181f - eco.Environment * 0.32f), (byte)(118f + eco.Environment * 0.73f), 118, byte.MaxValue);
			ApprovalRate.text = eco.ApprovalRate + "%";
			SetTextColor(ApprovalRate, eco.ApprovalRate);
			float num4 = eco.PowerUse * 100 / (eco.PowerCapacity + 1);
			float num5 = eco.WaterUse * 100 / (eco.WaterCapacity + 1);
			powerSlider.value = num4;
			waterSlider.value = num5;
			powerOver.data = num4;
			waterOver.data = num5;
			if (num4 > 96f)
			{
				powerArrow.color = red;
			}
			else if (num4 > 93f)
			{
				powerArrow.color = yellow;
			}
			else
			{
				powerArrow.color = Color.white;
			}
			if (num5 > 96f)
			{
				waterArrow.color = red;
			}
			else if (num5 > 93f)
			{
				waterArrow.color = yellow;
			}
			else
			{
				waterArrow.color = Color.white;
			}
			Imports.text = eco.budget.Imports.ToString("N0");
			Exports.text = eco.budget.Exports.ToString("N0");
			AffiliationSlider.value = eco.GlobalLeaning;
			yield return new WaitForEndOfFrame();
			UpdateGovType();
			yield return new WaitForEndOfFrame();
			CheckToUnlockFeature();
			yield return new WaitForEndOfFrame();
			research.UnlockSpecialBuildings();
			yield return new WaitForEndOfFrame();
			research.UpdateGovTexts();
			yield return new WaitForEndOfFrame();
			UpdateWarnings();
			yield return new WaitForSecondsRealtime(1f);
		}
	}

	private void UpdateWarnings()
	{
		bool flag = false;
		float num = eco.PowerCapacity;
		float num2 = eco.WaterCapacity;
		if (num == 0f)
		{
			num = 1f;
		}
		if (num2 == 0f)
		{
			num2 = 1f;
		}
		if (eco.Population > 1750f && eco.Population < 100000f && (float)(eco.PowerUse * 100) / num < 40f)
		{
			Power_W.gameObject.SetActive(value: true);
			Power_W.header = "n0";
		}
		else if (eco.Population > 25f && (float)(eco.PowerUse * 100) / num > 98f)
		{
			Power_W.gameObject.SetActive(value: true);
			Power_W.header = "n1";
		}
		else
		{
			Power_W.gameObject.SetActive(value: false);
		}
		if (eco.Population > 2000f && eco.Population < 25000f && (float)(eco.WaterUse * 100) / num2 < 40f)
		{
			Water_W.gameObject.SetActive(value: true);
			Water_W.header = "n2";
		}
		else if (eco.Population > 25f && (float)(eco.WaterUse * 100) / num2 > 98f)
		{
			Water_W.gameObject.SetActive(value: true);
			Water_W.header = "n3";
		}
		else
		{
			Water_W.gameObject.SetActive(value: false);
		}
		if (eco.Population > 750f && eco.Unemployment < 1f)
		{
			Unemployment_W.gameObject.SetActive(value: true);
			Unemployment_W.header = "n5";
		}
		else if (eco.Unemployment > 20f)
		{
			Unemployment_W.gameObject.SetActive(value: true);
			Unemployment_W.header = "n7";
		}
		else
		{
			Unemployment_W.gameObject.SetActive(value: false);
		}
		if ((Budget.CashFlow < -200f && eco.budget.Balance < 2000f) || eco.budget.Balance < 0f || (Budget.CashFlow < -2000f && eco.budget.Balance < 40000f))
		{
			Budget_W.gameObject.SetActive(value: true);
			Budget_W.header = "n14";
		}
		else
		{
			Budget_W.gameObject.SetActive(value: false);
		}
		if (eco.Population > 4000f && eco.TotalPupils * 100 / (eco.SchoolCapacity + 1) > 99)
		{
			Edu_W.gameObject.SetActive(value: true);
			Edu_W.header = "n8";
			flag = true;
		}
		else if (eco.Population > 20000f && eco.TotalStudents * 100 / (eco.HighSchoolCapacity + 1) > 99)
		{
			Edu_W.gameObject.SetActive(value: true);
			Edu_W.header = "n8";
			flag = true;
		}
		else if (eco.Population > 4000f && (float)eco.SchoolCapacity > (float)eco.TotalPupils * 2f)
		{
			Edu_W.gameObject.SetActive(value: true);
			Edu_W.header = "n9";
			flag = true;
		}
		else
		{
			Edu_W.gameObject.SetActive(value: false);
		}
		if (eco.Population > 10000f && eco.HospitalCapacity < eco.HospitalUse)
		{
			Health_W.gameObject.SetActive(value: true);
			Health_W.header = "n10";
			flag = true;
		}
		else if (eco.HospitalCapacity > 5 && eco.HospitalCapacity > eco.HospitalUse * 2)
		{
			Health_W.gameObject.SetActive(value: true);
			Health_W.header = "n11";
			flag = true;
		}
		else
		{
			Health_W.gameObject.SetActive(value: false);
		}
		if (eco.Population > 20000f && secuCapExceeded)
		{
			Secu_W.gameObject.SetActive(value: true);
			Secu_W.header = "n19";
			flag = true;
		}
		else
		{
			Secu_W.gameObject.SetActive(value: false);
		}
		if (flag && eco.Population < 150000f && !layerButtons.activeSelf)
		{
			Layer_W.gameObject.SetActive(value: true);
		}
		else
		{
			Layer_W.gameObject.SetActive(value: false);
		}
	}

	private void UpdateGovType()
	{
		string text = "";
		string text2 = "";
		float freedomIndex = eco.FreedomIndex;
		float civilRights = eco.CivilRights;
		bool flag = false;
		if (civilRights < 20f)
		{
			if (freedomIndex < 20f)
			{
				text = "";
				text2 = "Dystopia";
				flag = true;
			}
			else if (freedomIndex >= 20f && freedomIndex < 40f)
			{
				text = "Fascist";
				text2 = "Dictatorship";
			}
			else if (freedomIndex >= 40f && freedomIndex < 60f)
			{
				text = "Corrupt";
				text2 = "Dictatorship";
			}
			else if (freedomIndex >= 60f && freedomIndex < 80f)
			{
				text = "Capitalist";
				text2 = "Dictatorship";
			}
			else if (freedomIndex >= 80f)
			{
				text = "Free Market";
				text2 = "Dictatorship";
			}
		}
		else if (civilRights >= 20f && civilRights < 40f)
		{
			if (freedomIndex < 20f)
			{
				text = "Communist";
				text2 = "Dictatorship";
				if (!messages.achievements[11] && eco.Population > 10000f)
				{
					SteamAchievements.script.UnlockAchievement("commu");
					messages.achievements[11] = true;
				}
			}
			else if (freedomIndex >= 20f && freedomIndex < 40f)
			{
				text = "Fascist";
				text2 = "Oligarchy";
			}
			else if (freedomIndex >= 40f && freedomIndex < 60f)
			{
				text = "Corrupt";
				text2 = "Oligarchy";
			}
			else if (freedomIndex >= 60f && freedomIndex < 80f)
			{
				text = "Capitalist";
				text2 = "Oligarchy";
			}
			else if (freedomIndex >= 80f)
			{
				text = "Free Market";
				text2 = "Oligarchy";
			}
		}
		else if (civilRights >= 40f && civilRights < 60f)
		{
			if (freedomIndex < 20f)
			{
				text = "Communist";
				text2 = "Oligarchy";
			}
			else if (freedomIndex >= 20f && freedomIndex < 40f)
			{
				text = "Socialist";
				text2 = "Oligarchy";
			}
			else if (freedomIndex >= 40f && freedomIndex < 60f)
			{
				text = "Elitist";
				text2 = "Republic";
			}
			else if (freedomIndex >= 60f && freedomIndex < 80f)
			{
				text = "Capitalist";
				text2 = "Republic";
			}
			else if (freedomIndex >= 80f)
			{
				text = "Free Market";
				text2 = "Republic";
			}
		}
		else if (civilRights >= 60f && civilRights < 80f)
		{
			if (freedomIndex < 20f)
			{
				text = "Communist";
				text2 = "Democracy";
			}
			else if (freedomIndex >= 20f && freedomIndex < 40f)
			{
				text = "Socialist";
				text2 = "Democracy";
			}
			else if (freedomIndex >= 40f && freedomIndex < 60f)
			{
				text = "Liberal";
				text2 = "Democracy";
			}
			else if (freedomIndex >= 60f && freedomIndex < 80f)
			{
				text = "Libertarian";
				text2 = "Republic";
			}
			else if (freedomIndex >= 80f)
			{
				text = "Anarchist";
				text2 = "Republic";
				if (!messages.achievements[12] && eco.Population > 10000f)
				{
					SteamAchievements.script.UnlockAchievement("anarch");
					messages.achievements[12] = true;
				}
			}
		}
		else if (civilRights >= 80f)
		{
			if (freedomIndex < 20f)
			{
				text = "Communist";
				text2 = "Utopia";
			}
			else if (freedomIndex >= 20f && freedomIndex < 40f)
			{
				text = "Socialist";
				text2 = "Utopia";
			}
			else if (freedomIndex >= 40f && freedomIndex < 60f)
			{
				text = "Liberal";
				text2 = "Utopia";
			}
			else if (freedomIndex >= 60f && freedomIndex < 80f)
			{
				text = "Libertarian";
				text2 = "Utopia";
			}
			else if (freedomIndex >= 80f)
			{
				text = "";
				text2 = "Utopia";
				flag = true;
			}
		}
		string text3 = LOCSYS.Translate(text + " " + text2);
		if (flag)
		{
			text3 = LOCSYS.Translate(text2);
		}
		_ = text3 != GovType.text;
		GovType.text = text3;
	}

	public void CheckToUnlockFeature()
	{
		if (DataRegion.alreadyBuilt[9])
		{
			B_HQ.interactable = false;
			B_Legislation.interactable = true;
			B_HQ.GetComponent<TooltipTrigger>().header = "";
			B_Legislation.GetComponent<TooltipTrigger>().header = "";
			if (DataRegion.selectedCity.isCapital && eco.HQbuilding != null)
			{
				if (eco.HQbuilding.Stage == 1)
				{
					research.HQ_Unlock.text = LOCSYS.Translate("Stage") + ": " + eco.HQbuilding.Stage + " / 5";
					research.HQ_Req1.text = LOCSYS.Translate("HQS1R1");
					research.HQ_Req2.text = LOCSYS.Translate("HQS1R2");
				}
				else if (eco.HQbuilding.Stage == 2)
				{
					research.HQ_Unlock.text = LOCSYS.Translate("Stage") + ": " + eco.HQbuilding.Stage + " / 5";
					research.HQ_Req1.text = LOCSYS.Translate("HQS2R1");
					research.HQ_Req2.text = LOCSYS.Translate("HQS2R2");
				}
				else if (eco.HQbuilding.Stage == 3)
				{
					research.HQ_Unlock.text = LOCSYS.Translate("Stage") + ": " + eco.HQbuilding.Stage + " / 5";
					research.HQ_Req1.text = LOCSYS.Translate("HQS3R1");
					research.HQ_Req2.text = LOCSYS.Translate("HQS3R2");
				}
				else if (eco.HQbuilding.Stage == 4)
				{
					research.HQ_Unlock.text = LOCSYS.Translate("Stage") + ": " + eco.HQbuilding.Stage + " / 5";
					research.HQ_Req1.text = LOCSYS.Translate("HQS4R1");
					research.HQ_Req2.text = LOCSYS.Translate("HQS4R2");
				}
			}
			else if (DataRegion.selectedCity.isCapital && eco.HQbuilding == null)
			{
				Debug.Log("HQ IS NULL");
				research.HQ_Unlock.text = LOCSYS.Translate("Stage") + ": 0 / 5";
				research.HQ_Req1.text = LOCSYS.Translate("HQr1");
				research.HQ_Req2.text = LOCSYS.Translate("HQr2");
			}
		}
		else if (DataRegion.selectedCity.isCapital && eco.Population > 750f && !bi.world.alreadyUnlocked[9])
		{
			B_HQ.interactable = true;
			bi.world.alreadyUnlocked[9] = true;
			news.goodNews.Add(new Vector3Int(49, 0, 1));
			messages.NewInfoMessage(49);
			B_HQ.GetComponent<TooltipTrigger>().header = "";
		}
		if (DataRegion.selectedCity.isCapital && eco.HQbuilding != null)
		{
			if (eco.HQbuilding.Stage == 1 && eco.NationPop > 22500f && eco.enactedPolicies > 1)
			{
				serviceTool.HQUpdate(2, eco.HQbuilding);
				legislation.UnlockNewPolicies(2);
				messages.NewInfoMessage(100);
			}
			else if (eco.HQbuilding.Stage == 2 && eco.NationPop > 100000f && eco.enactedPolicies > 9)
			{
				serviceTool.HQUpdate(3, eco.HQbuilding);
				legislation.UnlockNewPolicies(3);
				messages.NewInfoMessage(100);
			}
			else if (eco.HQbuilding.Stage == 3 && eco.NationPop > 400000f && eco.enactedPolicies > 20)
			{
				serviceTool.HQUpdate(4, eco.HQbuilding);
				legislation.UnlockNewPolicies(4);
				messages.NewInfoMessage(100);
			}
			else if (eco.HQbuilding.Stage == 4 && eco.NationPop > 1200000f && eco.enactedPolicies > 39)
			{
				serviceTool.HQUpdate(5, eco.HQbuilding);
				legislation.UnlockNewPolicies(5);
				messages.NewInfoMessage(100);
				if (!messages.achievements[26])
				{
					SteamAchievements.script.UnlockAchievement("hq");
					messages.achievements[26] = true;
				}
			}
		}
		if (DataRegion.alreadyBuilt[0])
		{
			B_Immi.interactable = false;
			B_ImmiPanel.interactable = true;
			B_Immi.GetComponent<TooltipTrigger>().header = "";
			B_ImmiPanel.GetComponent<TooltipTrigger>().header = "";
			research.Immi_Unlock.text = LOCSYS.Translate("AlreadyBuilt");
		}
		else if (bi.world.alreadyUnlocked[0] || eco.NationPop > 2000f)
		{
			B_Immi.interactable = true;
			B_Immi.GetComponent<TooltipTrigger>().header = "";
			bi.world.alreadyUnlocked[0] = true;
			news.goodNews.Add(new Vector3Int(52, 0, 1));
			messages.NewInfoMessage(52);
			research.Immi_Unlock.text = LOCSYS.Translate("Available");
			research.Immi_Unlock.color = green;
		}
		if (DataRegion.alreadyBuilt[1])
		{
			B_Bank.interactable = false;
			B_BuyDollars.interactable = true;
			B_SellDollars.interactable = true;
			B_Bank.GetComponent<TooltipTrigger>().header = "";
			B_BuyDollars.GetComponent<TooltipTrigger>().header = "";
			B_SellDollars.GetComponent<TooltipTrigger>().header = "";
			research.CB_Unlock.text = LOCSYS.Translate("AlreadyBuilt");
		}
		else if ((eco.NationPop > 75000f && eco.enactedPolicies >= 12) || bi.world.alreadyUnlocked[1])
		{
			B_Bank.interactable = true;
			B_Bank.GetComponent<TooltipTrigger>().header = "";
			bi.world.alreadyUnlocked[1] = true;
			news.goodNews.Add(new Vector3Int(53, 0, 1));
			messages.NewInfoMessage(53);
			research.CB_Unlock.text = LOCSYS.Translate("Available");
			research.CB_Unlock.color = green;
		}
		if (DataRegion.alreadyBuilt[2])
		{
			B_Comm.interactable = false;
			B_EcoPolTab.interactable = true;
			B_Comm.GetComponent<TooltipTrigger>().header = "";
			B_EcoPolTab.GetComponent<TooltipTrigger>().header = "";
			research.Comm_Unlock.text = LOCSYS.Translate("AlreadyBuilt");
		}
		else if ((eco.NationPop > 25000f && eco.enactedPolicies >= 3) || bi.world.alreadyUnlocked[2])
		{
			B_Comm.interactable = true;
			B_Comm.GetComponent<TooltipTrigger>().header = "";
			bi.world.alreadyUnlocked[2] = true;
			news.goodNews.Add(new Vector3Int(54, 0, 1));
			messages.NewInfoMessage(54);
			research.Comm_Unlock.text = LOCSYS.Translate("Available");
			research.Comm_Unlock.color = green;
		}
		if (DataRegion.alreadyBuilt[3])
		{
			B_Social.interactable = false;
			B_SocPolTab.interactable = true;
			B_Social.GetComponent<TooltipTrigger>().header = "";
			B_SocPolTab.GetComponent<TooltipTrigger>().header = "";
			research.Soc_Unlock.text = LOCSYS.Translate("AlreadyBuilt");
		}
		else if ((eco.NationPop > 50000f && eco.enactedPolicies >= 6) || bi.world.alreadyUnlocked[3])
		{
			B_Social.interactable = true;
			B_Social.GetComponent<TooltipTrigger>().header = "";
			bi.world.alreadyUnlocked[3] = true;
			news.goodNews.Add(new Vector3Int(55, 0, 1));
			messages.NewInfoMessage(55);
			research.Soc_Unlock.text = LOCSYS.Translate("Available");
			research.Soc_Unlock.color = green;
		}
		if (DataRegion.alreadyBuilt[6])
		{
			B_Congress.interactable = false;
			B_CusPolTab.interactable = true;
			B_Congress.GetComponent<TooltipTrigger>().header = "";
			B_CusPolTab.GetComponent<TooltipTrigger>().header = "";
			research.Con_Unlock.text = LOCSYS.Translate("AlreadyBuilt");
		}
		else if ((eco.NationPop > 300000f && eco.enactedPolicies >= 30) || bi.world.alreadyUnlocked[6])
		{
			B_Congress.interactable = true;
			B_Congress.GetComponent<TooltipTrigger>().header = "";
			bi.world.alreadyUnlocked[6] = true;
			news.goodNews.Add(new Vector3Int(58, 0, 1));
			messages.NewInfoMessage(58);
			research.Con_Unlock.text = LOCSYS.Translate("Available");
			research.Con_Unlock.color = green;
		}
		if (eco.MCPop >= 3500f)
		{
			B_Mixed.interactable = true;
			B_Mixed.image.sprite = B_Mixed_Active;
			subMenu.Mix_Off = B_Mixed_Active;
			B_Mixed.GetComponent<TooltipTrigger>().header = "";
		}
		if (eco.MCPop >= 150000f)
		{
			B_Stadium.interactable = true;
			B_Stadium.GetComponent<TooltipTrigger>().header = "";
		}
		if (eco.UCPop >= 40000f)
		{
			B_Museum.interactable = true;
			B_Museum.GetComponent<TooltipTrigger>().header = "";
		}
		if (eco.MCPop >= 15000f)
		{
			B_Pool.interactable = true;
			B_Pool.GetComponent<TooltipTrigger>().header = "";
		}
		if (eco.hightechJobs >= 100000f)
		{
			B_Holo.interactable = true;
			B_Holo.GetComponent<TooltipTrigger>().header = "";
		}
	}

	private Color SetLogoColor(float usage)
	{
		if (usage < 50f)
		{
			return blue;
		}
		if (usage >= 50f && usage < 90f)
		{
			return green;
		}
		if (usage >= 90f && usage < 99f)
		{
			return yellow;
		}
		return red;
	}

	private void SetTextColor(TextMeshProUGUI t, int i)
	{
		t.color = new Color32((byte)(181f - (float)i * 0.32f), (byte)(118f + (float)i * 0.73f), 118, byte.MaxValue);
	}

	private IEnumerator InputListener()
	{
		while (enabled)
		{
			if (Input.GetMouseButtonDown(1))
			{
				yield return ClickEvent();
			}
			yield return null;
		}
	}

	private IEnumerator ClickEvent()
	{
		yield return new WaitForEndOfFrame();
		float count = 0f;
		while (count < doubleClickTimeLimit)
		{
			if (Input.GetMouseButtonDown(1) && bi.tamere == 0)
			{
				HideAll();
				minimap.CloseLayers();
				break;
			}
			count += Time.fixedDeltaTime;
			yield return null;
		}
	}

	public void HideSuper()
	{
		super.transform.position = new Vector3(4000f, 4000f, 0f);
		superSub = 0;
	}

	public void HideAll()
	{
		HideSuper();
		HidePlacers();
		Index = 0;
		HideCursors();
		bi.x1 = 0;
		bi.z1 = 0;
		bi.x2 = 0;
		bi.z2 = 0;
		bi.tamere = 0;
		if (Index == 10)
		{
			bi.roading.StopVisualizeRoad();
		}
		else if (Index == 11)
		{
			bi.roading.ave.StopVisualizeRoad();
		}
		else if (Index == 24 || Index == 9)
		{
			bi.buldoze.StopVisualizeDestruction();
		}
		zoning.ClearFriendly();
		zoning.StopVisualize();
		subMenu.SetButtons();
		CensusPanel.SetActive(value: false);
		costHelper.gameObject.SetActive(value: false);
		TooltipSystem.Hide();
		isZero = true;
		BlockInteraction.following = false;
		if (RTS_Camera.showingUnderground)
		{
			rtsCam.GoSurface();
			BioFolder.SetActive(value: true);
			WaterPlane.SetActive(value: true);
			WorldFolder.transform.position = new Vector3(0f, 0f, 0f);
			Sun.gameObject.SetActive(value: true);
			Debug.Log("Forcing surface view!");
		}
	}

	public void RaiseTerrainClick(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 11;
		Index = 1;
	}

	public void LowerTerrainClick(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 11;
		Index = 2;
	}

	public void LevelTerrainClick(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 11;
		Index = 3;
	}

	public void SmoothTerrainClick(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 11;
		Index = 4;
	}

	public void StreetClick(GameObject go)
	{
		HideAll();
		Index = 10;
		super.transform.position = go.transform.position;
		superSub = 10;
		roadSubType = "Street/";
		roadStyle = "Light/";
		gridRoad.SetActive(value: true);
	}

	public void DirtClick(GameObject go)
	{
		HideAll();
		Index = 10;
		super.transform.position = go.transform.position;
		superSub = 10;
		roadSubType = "Street/";
		roadStyle = "Dirt/";
		gridRoad.SetActive(value: true);
	}

	public void StreeTreeClick(GameObject go)
	{
		HideAll();
		Index = 10;
		super.transform.position = go.transform.position;
		superSub = 10;
		roadSubType = "Street/";
		roadStyle = "Light/";
		Roading.sideType = "straighttree";
		gridRoad.SetActive(value: true);
	}

	public void RoadClick(GameObject go)
	{
		HideAll();
		Index = 10;
		super.transform.position = go.transform.position;
		superSub = 10;
		roadSubType = "Road/";
		roadStyle = "Light/";
		gridRoad.SetActive(value: true);
	}

	public void AvenueClick(GameObject go)
	{
		HideAll();
		Index = 11;
		super.transform.position = go.transform.position;
		superSub = 10;
		roadStyle = "Light/";
		Avenueing.subType = "Avenue/";
		Avenueing.level = 1;
		gridAve.SetActive(value: true);
		avenueGridMat.mainTexture = avenueTexture;
	}

	public void AvenueTreeClick(GameObject go)
	{
		HideAll();
		Index = 11;
		super.transform.position = go.transform.position;
		superSub = 10;
		roadStyle = "Light/";
		Avenueing.subType = "Avenue/";
		Avenueing.level = 1;
		Avenueing.sideType = "sidetree";
		gridAve.SetActive(value: true);
		avenueGridMat.mainTexture = avenueTexture;
	}

	public void HighwayClick(GameObject go)
	{
		HideAll();
		Index = 11;
		super.transform.position = go.transform.position;
		superSub = 10;
		Avenueing.subType = "Hway/";
		Avenueing.level = 2;
		gridAve.SetActive(value: true);
		avenueGridMat.mainTexture = highwayTexture;
	}

	public void HighwayExitClick(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 10;
		bi.pos = new Vector3(-100f, 0f, -100f);
		ramps.SetActive(value: true);
		aveRamps.SetActive(value: true);
		Index = 120;
	}

	public void RailClick(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 10;
		Railing.style = "Ground/";
		Index = 17;
		bi.railing.grid.SetActive(value: true);
	}

	public void StationClick(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 10;
		StationPlacer.SetActive(value: true);
		gridTrain.SetActive(value: true);
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 18;
	}

	public void SubwayClick(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 10;
		Railing.style = "Underground/";
		rtsCam.GoUnderground();
		BioFolder.SetActive(value: false);
		WaterPlane.SetActive(value: false);
		WorldFolder.transform.position = new Vector3(0f, -1.5f, 0f);
		Sun.gameObject.SetActive(value: false);
		Index = 17;
	}

	public void SubwayStationClick(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 10;
		rtsCam.GoUnderground();
		BioFolder.SetActive(value: false);
		WaterPlane.SetActive(value: false);
		WorldFolder.transform.position = new Vector3(0f, -1.5f, 0f);
		Sun.gameObject.SetActive(value: false);
		gridTrain.SetActive(value: true);
		Index = 191;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Roads/straight_sub"));
	}

	public void SubwayConnectionClick(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 10;
		ConnectionPlacer.SetActive(value: true);
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 192;
	}

	public void BusClick(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 10;
		Index = 15;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Roads/straight_bus"));
		gridBus.SetActive(value: true);
	}

	public void AirportClick(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 10;
		AirportPlacer.SetActive(value: true);
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 16;
	}

	public void PortClick(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 10;
		PortPlacer.SetActive(value: true);
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 19;
	}

	public void ResidentialClick()
	{
		if (Index == 20)
		{
			HideAll();
			return;
		}
		HideAll();
		Index = 20;
		zoning.series = 1;
	}

	public void CommercialClick()
	{
		if (Index == 21)
		{
			HideAll();
			return;
		}
		HideAll();
		Index = 21;
		zoning.series = 1;
	}

	public void IndustrialClick()
	{
		if (Index == 22)
		{
			HideAll();
			return;
		}
		HideAll();
		Index = 22;
		zoning.series = 1;
	}

	public void MixedClick()
	{
		if (Index == 24)
		{
			HideAll();
			return;
		}
		HideAll();
		Index = 24;
		zoning.series = 1;
	}

	public void FarmClick(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 3;
		Index = 23;
	}

	public void ExploreClick(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 3;
		Index = 26;
		if (minimap.displayed != "Resource")
		{
			minimap.ToggleResource();
		}
	}

	public void MineClick(GameObject go)
	{
		HideAll();
		Empty8x.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 3;
		Index = 25;
		if (minimap.displayed != "Resource")
		{
			minimap.ToggleResource();
		}
	}

	public void HLM1Click(GameObject go)
	{
		HideAll();
		HLM1.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 6;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 70;
	}

	public void HLM2Click(GameObject go)
	{
		HideAll();
		HLM2.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 6;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 71;
	}

	public void HLM3Click(GameObject go)
	{
		HideAll();
		HLM3.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 6;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 72;
	}

	public void FactorySClick(GameObject go)
	{
		HideAll();
		Empty4x.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 6;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 73;
	}

	public void FactoryMClick(GameObject go)
	{
		HideAll();
		Empty4x.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 6;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 74;
	}

	public void RetailSClick(GameObject go)
	{
		HideAll();
		Empty4x.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 6;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 75;
	}

	public void ChristianClick(GameObject go)
	{
		HideAll();
		Empty4x.SetActive(value: true);
		bi.pos = new Vector3(-100f, 0f, -100f);
		super.transform.position = go.transform.position;
		superSub = 4;
		Index = 240;
	}

	public void MosqueClick(GameObject go)
	{
		HideAll();
		Empty4x.SetActive(value: true);
		bi.pos = new Vector3(-100f, 0f, -100f);
		super.transform.position = go.transform.position;
		superSub = 4;
		Index = 241;
	}

	public void PagodaClick(GameObject go)
	{
		HideAll();
		Empty4x.SetActive(value: true);
		bi.pos = new Vector3(-100f, 0f, -100f);
		super.transform.position = go.transform.position;
		superSub = 4;
		Index = 242;
	}

	public void SchoolClick(GameObject go)
	{
		HideAll();
		SchoolPlacer.SetActive(value: true);
		bi.pos = new Vector3(-100f, 0f, -100f);
		super.transform.position = go.transform.position;
		superSub = 7;
		Index = 30;
	}

	public void HospitalClick(GameObject go)
	{
		HideAll();
		HospitalPlacer.SetActive(value: true);
		bi.pos = new Vector3(-100f, 0f, -100f);
		super.transform.position = go.transform.position;
		superSub = 7;
		Index = 31;
	}

	public void FireClick(GameObject go)
	{
		HideAll();
		FirePlacer.SetActive(value: true);
		bi.pos = new Vector3(-100f, 0f, -100f);
		super.transform.position = go.transform.position;
		superSub = 7;
		Index = 32;
	}

	public void PoliceClick(GameObject go)
	{
		HideAll();
		PolicePlacer.SetActive(value: true);
		bi.pos = new Vector3(-100f, 0f, -100f);
		super.transform.position = go.transform.position;
		superSub = 7;
		Index = 33;
	}

	public void HighSchoolClick(GameObject go)
	{
		HideAll();
		HighSchoolPlacer.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 7;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 34;
	}

	public void UniversityClick(GameObject go)
	{
		HideAll();
		UniversityPlacer.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 7;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 35;
	}

	public void LibraryClick(GameObject go)
	{
		HideAll();
		Empty4x.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 7;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 36;
	}

	public void PrisonClick(GameObject go)
	{
		HideAll();
		Empty8x.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 7;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 37;
	}

	public void HQClick(GameObject go)
	{
		HideAll();
		HQPlacer.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 9;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 40;
	}

	public void ImmigrationClick(GameObject go)
	{
		HideAll();
		ImmigrationPlacer.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 9;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 41;
	}

	public void BankClick(GameObject go)
	{
		HideAll();
		BankPlacer.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 9;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 42;
	}

	public void CommerceClick(GameObject go)
	{
		HideAll();
		CommercePlacer.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 9;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 43;
	}

	public void SocialClick(GameObject go)
	{
		HideAll();
		SocialPlacer.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 9;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 44;
	}

	public void HelipadClick(GameObject go)
	{
		HideAll();
		HelipadPlacer.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 9;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 46;
	}

	public void CongressClick(GameObject go)
	{
		HideAll();
		Congress.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 9;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 47;
	}

	public void ArcoAClick(GameObject go)
	{
		HideAll();
		ArcoA.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 12;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 80;
	}

	public void ArcoBClick(GameObject go)
	{
		HideAll();
		ArcoB.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 12;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 81;
	}

	public void ArcoCClick(GameObject go)
	{
		HideAll();
		ArcoC.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 12;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 82;
	}

	public void MediaClick(GameObject go)
	{
		HideAll();
		Media.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 12;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 83;
	}

	public void RedClick(GameObject go)
	{
		HideAll();
		Empty8x.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 12;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 84;
	}

	public void EedeniumClick(GameObject go)
	{
		HideAll();
		Empty8x.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 12;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 85;
	}

	public void PathClick(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 5;
		Index = 50;
	}

	public void SmallFountain(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 5;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Index = 90;
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Parks/SmallFountain"));
	}

	public void SmallFlowers(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 5;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Index = 91;
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Parks/SmallFlowers"));
	}

	public void SmallStatue(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 5;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Index = 92;
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Parks/SmallStatue"));
	}

	public void SmallPlazaB(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 5;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Index = 93;
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Parks/SmallPlaza_B"));
	}

	public void SmallPlazaC(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 5;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Index = 94;
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Parks/SmallPlaza_C"));
	}

	public void SmallPlazaD(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 5;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Index = 95;
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Parks/SmallPlaza_D"));
	}

	public void SmallPlazaE(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 5;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Index = 96;
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Parks/SmallPlaza_E"));
	}

	public void SmallPlazaF(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 5;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Index = 97;
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Parks/SmallPlaza_F"));
	}

	public void Basket(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 5;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Index = 98;
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Parks/Basket"));
	}

	public void Tennis(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 5;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Index = 107;
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Parks/Tennis"));
	}

	public void LargePlazaA(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 5;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Index = 99;
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Parks/LargePlaza_A"));
	}

	public void LargePlazaB(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 5;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Index = 100;
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Parks/LargePlaza_B"));
	}

	public void LargePlazaC(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 5;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Index = 101;
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Parks/LargePlaza_C"));
	}

	public void MarinaClick(GameObject go)
	{
		HideAll();
		Marina.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 5;
		Index = 102;
	}

	public void PierClick(GameObject go)
	{
		HideAll();
		Pier.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 5;
		Index = 106;
	}

	public void StadiumClick(GameObject go)
	{
		HideAll();
		Universal16x.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 5;
		Index = 110;
	}

	public void MuseumClick(GameObject go)
	{
		HideAll();
		Empty8x.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 5;
		Index = 111;
	}

	public void PoolClick(GameObject go)
	{
		HideAll();
		Empty6x.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 5;
		Index = 112;
	}

	public void BeachClick(GameObject go)
	{
		HideAll();
		Universal1x.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 5;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Index = 113;
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Parks/BeachD"));
	}

	public void HoloClick(GameObject go)
	{
		HideAll();
		Empty8x.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 5;
		Index = 114;
	}

	public void LargePlazaD(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 5;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Index = 103;
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Parks/LargePlaza_D"));
	}

	public void LargePlazaE(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 5;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Index = 104;
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Parks/LargePlaza_E"));
	}

	public void LargePlazaF(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 5;
		UnityEngine.Object.Destroy(Universal1x.gameObject);
		Index = 105;
		Universal1x = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Parks/LargePlaza_F"));
	}

	public void TreeClick(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 11;
		Index = 55;
	}

	public void GrassClick(GameObject go)
	{
		HideAll();
		super.transform.position = go.transform.position;
		superSub = 11;
		Index = 56;
	}

	public void TurbineClick(GameObject go)
	{
		HideAll();
		Turbine.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 8;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 58;
	}

	public void PumpingClick(GameObject go)
	{
		HideAll();
		Pumping.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 8;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 59;
	}

	public void CoalClick(GameObject go)
	{
		HideAll();
		CoalPlant.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 8;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 60;
	}

	public void GeneratorsClick(GameObject go)
	{
		HideAll();
		Generators.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 8;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 61;
	}

	public void WaterTowerClick(GameObject go)
	{
		HideAll();
		WaterTower.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 8;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 62;
	}

	public void SolarClick(GameObject go)
	{
		HideAll();
		SolarPlant.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 8;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 63;
	}

	public void NuclearClick(GameObject go)
	{
		HideAll();
		NuclearPlant.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 8;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 64;
	}

	public void HydrogenClick(GameObject go)
	{
		HideAll();
		HydrogenPlant.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 8;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 65;
	}

	public void CarbonClick(GameObject go)
	{
		HideAll();
		Carbon.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 8;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 66;
	}

	public void GulagClick(GameObject go)
	{
		HideAll();
		Empty6x.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 8;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 67;
	}

	public void RehabClick(GameObject go)
	{
		HideAll();
		Empty6x.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 8;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 68;
	}

	public void DroneClick(GameObject go)
	{
		HideAll();
		Drone.SetActive(value: true);
		super.transform.position = go.transform.position;
		superSub = 8;
		bi.pos = new Vector3(-100f, 0f, -100f);
		Index = 69;
	}

	public void GoRegion()
	{
		StartCoroutine(SaveCity(0));
	}

	public void GoStateCar()
	{
		if (World.staticWorld.Roads.Count < 2)
		{
			bi.sm.Play("Impractical");
			return;
		}
		if (copterMode && !carCamera.activeSelf)
		{
			bi.sm.Play("Impractical");
			return;
		}
		cityCamera.SetActive(value: false);
		carCamera.SetActive(value: true);
		copterCamera.SetActive(value: false);
		carControls.SetActive(value: true);
		carRigidBody.isKinematic = false;
		carModel.SetActive(value: true);
		copterMode = true;
		carModel.transform.position = World.staticWorld.Roads[UnityEngine.Random.Range(0, World.staticWorld.Roads.Count)].wPos + new Vector3(0f, 0.25f, 0f);
		gamespeed.Speed_1();
	}

	public void ExitStateCar()
	{
		carModel.SetActive(value: false);
		cityCamera.SetActive(value: true);
		copterCamera.SetActive(value: false);
		carCamera.SetActive(value: false);
		carControls.SetActive(value: false);
		carRigidBody.isKinematic = true;
		copterMode = false;
		carModel.transform.position = Vector3.zero;
		if (SettingsStatic.graphicsOption[14] == 0)
		{
			Application.targetFrameRate = 60;
			OnDemandRendering.renderFrameInterval = 2;
		}
		else if (SettingsStatic.graphicsOption[14] == 1)
		{
			Application.targetFrameRate = -1;
			OnDemandRendering.renderFrameInterval = 2;
		}
		else if (SettingsStatic.graphicsOption[14] == 2)
		{
			Application.targetFrameRate = -1;
			OnDemandRendering.renderFrameInterval = 1;
		}
	}

	public void GoStateCopter()
	{
		if (padPos == Vector3.zero)
		{
			copterTooltip.header = "noHelipad";
			bi.sm.Play("Impractical");
			return;
		}
		if (copterMode)
		{
			bi.sm.Play("Impractical");
			return;
		}
		copterModel.SetActive(value: true);
		heliController.HelicopterModel.isKinematic = false;
		cityCamera.SetActive(value: false);
		carCamera.SetActive(value: false);
		copterCamera.SetActive(value: true);
		copterModel.transform.position = padPos + new Vector3(0f, 0.1f, 0f);
		copterMode = true;
		heliControls.SetActive(value: true);
		heliController.BaseHeight = padPos.y;
		heliController.HelicopterSound.volume = SettingsStatic.Carsvolume * 2f;
		if (SettingsStatic.graphicsOption[14] == 0)
		{
			Application.targetFrameRate = 60;
			OnDemandRendering.renderFrameInterval = 3;
		}
		else if (SettingsStatic.graphicsOption[14] == 1)
		{
			Application.targetFrameRate = 60;
			OnDemandRendering.renderFrameInterval = 2;
		}
		else if (SettingsStatic.graphicsOption[14] == 2)
		{
			Application.targetFrameRate = 60;
			OnDemandRendering.renderFrameInterval = 1;
		}
		gamespeed.Speed_1();
	}

	public void ExitCopterMode()
	{
		cityCamera.SetActive(value: true);
		copterCamera.SetActive(value: false);
		heliController.HelicopterModel.isKinematic = true;
		heliController.EngineForce = 0f;
		copterMode = false;
		heliControls.SetActive(value: false);
		copterModel.transform.position = padPos + new Vector3(0f, 0.1f, 0f);
		if (SettingsStatic.graphicsOption[14] == 0)
		{
			Application.targetFrameRate = 60;
			OnDemandRendering.renderFrameInterval = 2;
		}
		else if (SettingsStatic.graphicsOption[14] == 1)
		{
			Application.targetFrameRate = -1;
			OnDemandRendering.renderFrameInterval = 2;
		}
		else if (SettingsStatic.graphicsOption[14] == 2)
		{
			Application.targetFrameRate = -1;
			OnDemandRendering.renderFrameInterval = 1;
		}
	}

	public void ExitButton()
	{
		StartCoroutine(SaveCity(1));
	}

	public void SaveButton()
	{
		StartCoroutine(SaveCity(2));
	}

	public void DayNightButton()
	{
		if (!cycling)
		{
			cycling = true;
			StartCoroutine(ChangeDayNightCycle());
		}
	}

	public void SetTime()
	{
		timeController.m_timeline = timeSlider.value;
		userChangedTime = true;
	}

	private IEnumerator ChangeDayNightCycle()
	{
		if (cycle == 0)
		{
			cycle = 1;
			timeController.m_timeline = 9f;
			yield return new WaitForSecondsRealtime(0.1f);
			dayNightImage.sprite = day;
			timeSlider.gameObject.SetActive(value: false);
			yield return new WaitForSecondsRealtime(0.5f);
			timeController.enabled = false;
			bloom.intensity.value = 0.6f;
			autoExposure.minLuminance.value = -0.65f;
		}
		else if (cycle == 1)
		{
			cycle = 2;
			timeController.m_timeline = 23f;
			timeController.enabled = true;
			yield return new WaitForSecondsRealtime(0.1f);
			dayNightImage.sprite = night;
			yield return new WaitForSecondsRealtime(0.5f);
			timeController.enabled = false;
		}
		else
		{
			cycle = 0;
			timeController.enabled = true;
			timeController.m_timeline = 4f;
			dayNightImage.sprite = full;
			timeSlider.gameObject.SetActive(value: true);
			timeController.enabled = true;
			bloom.intensity.value = 0.6f;
			autoExposure.minLuminance.value = -0.65f;
		}
		yield return new WaitForEndOfFrame();
		cycling = false;
	}

	public void ShowUIFromOptions()
	{
		if (!upperUI.activeSelf)
		{
			upperUI.SetActive(value: true);
		}
		if (!lowerUI.activeSelf)
		{
			lowerUI.SetActive(value: true);
		}
	}

	public void HideShowUI()
	{
		bool activeSelf = upperUI.activeSelf;
		upperUI.SetActive(!activeSelf);
		bool activeSelf2 = lowerUI.activeSelf;
		lowerUI.SetActive(!activeSelf2);
	}

	public void HideShowLayers()
	{
		bool activeSelf = layerButtons.activeSelf;
		layerButtons.SetActive(!activeSelf);
		if (activeSelf)
		{
			minimap.CloseLayers();
		}
	}

	public void HideShowOptions()
	{
		bool activeSelf = optionButtons.activeSelf;
		optionButtons.SetActive(!activeSelf);
	}

	public void HideShowLeftDetails()
	{
		bool activeSelf = leftDetailsPanel.activeSelf;
		leftDetailsPanel.SetActive(!activeSelf);
	}

	public void HideShowRightDetails()
	{
		bool activeSelf = rightDetailsPanel.activeSelf;
		rightDetailsPanel.SetActive(!activeSelf);
	}

	public void LowDensityClick()
	{
		HideAll();
		Sub_Index = 1;
		super.transform.position = residentialLow.transform.position;
	}

	public void MiddleDensityClick()
	{
		HideAll();
		Sub_Index = 2;
		super.transform.position = residentialMid.transform.position;
	}

	public void HighDensityClick()
	{
		HideAll();
		Sub_Index = 3;
		super.transform.position = residentialHi.transform.position;
	}

	public void CustomDensityClick()
	{
		HideAll();
		Sub_Index = 4;
		super.transform.position = residentialCus.transform.position;
	}

	public void ForceIndex(int i)
	{
		Index = i;
	}

	public void DestroyClick()
	{
		if ((Index == 17 && Railing.style == "Underground/") || Index == 191)
		{
			Index = 9;
			undergroundDestroy = true;
			return;
		}
		undergroundDestroy = false;
		rtsCam.GoSurface();
		BioFolder.SetActive(value: true);
		WaterPlane.SetActive(value: true);
		WorldFolder.transform.position = new Vector3(0f, 0f, 0f);
		Sun.gameObject.SetActive(value: true);
		if (Index == 9)
		{
			HideAll();
			return;
		}
		HideAll();
		Index = 9;
		subMenu.SetButtons();
	}

	public void CensusClick()
	{
		if (Index == 8)
		{
			HideAll();
			return;
		}
		HideAll();
		Index = 8;
		CensusPanel.SetActive(value: true);
	}

	public static void ToggleFullScreen()
	{
		Screen.fullScreen = !Screen.fullScreen;
	}

	public static void ToggleMaps()
	{
		Screen.fullScreen = !Screen.fullScreen;
	}

	public void ResPlus()
	{
		resStage++;
		if (resStage > 7)
		{
			resStage = 7;
		}
		ResDensity.text = resStage.ToString();
	}

	public void ResLess()
	{
		resStage--;
		if (resStage < 1)
		{
			resStage = 1;
		}
		ResDensity.text = resStage.ToString();
	}

	public void MixPlus()
	{
		mixStage++;
		if (mixStage > 4)
		{
			mixStage = 4;
		}
		MixDensity.text = mixStage.ToString();
	}

	public void MixLess()
	{
		mixStage--;
		if (mixStage < 1)
		{
			mixStage = 1;
		}
		MixDensity.text = mixStage.ToString();
	}

	public void IndPlus()
	{
		indStage++;
		if (indStage > 4)
		{
			indStage = 4;
		}
		IndDensity.text = indStage.ToString();
	}

	public void IndLess()
	{
		indStage--;
		if (indStage < 1)
		{
			indStage = 1;
		}
		IndDensity.text = indStage.ToString();
	}

	public void ComPlus()
	{
		comStage++;
		if (comStage > 6)
		{
			comStage = 6;
		}
		ComDensity.text = comStage.ToString();
	}

	public void ComLess()
	{
		comStage--;
		if (comStage < 1)
		{
			comStage = 1;
		}
		ComDensity.text = comStage.ToString();
	}

	public void OpenImmigrationPanel()
	{
		bool activeSelf = immiPanel.activeSelf;
		immiPanel.SetActive(!activeSelf);
		lesiglationPanel.SetActive(value: false);
		budgetPanel.SetActive(value: false);
		statsPanel.SetActive(value: false);
		settingsPanel.SetActive(value: false);
		researchPanel.SetActive(value: false);
		serviceTool.CloseServiceEditor();
		bi.superTool.CloseSuperTool();
		BlockInteraction.following = false;
		if (immiPanel.activeSelf)
		{
			sliderOnScreen = true;
		}
		else
		{
			sliderOnScreen = false;
		}
	}

	public void OpenLegislationPanel()
	{
		bool activeSelf = lesiglationPanel.activeSelf;
		lesiglationPanel.SetActive(!activeSelf);
		immiPanel.SetActive(value: false);
		budgetPanel.SetActive(value: false);
		statsPanel.SetActive(value: false);
		settingsPanel.SetActive(value: false);
		researchPanel.SetActive(value: false);
		serviceTool.CloseServiceEditor();
		bi.superTool.CloseSuperTool();
		BlockInteraction.following = false;
	}

	public void OpenBudgetPanel()
	{
		bool activeSelf = budgetPanel.activeSelf;
		budgetPanel.SetActive(!activeSelf);
		eco.budget.CheckTaxRatex();
		immiPanel.SetActive(value: false);
		lesiglationPanel.SetActive(value: false);
		statsPanel.SetActive(value: false);
		settingsPanel.SetActive(value: false);
		researchPanel.SetActive(value: false);
		serviceTool.CloseServiceEditor();
		bi.superTool.CloseSuperTool();
		BlockInteraction.following = false;
	}

	public void OpenStatsPanel()
	{
		bool activeSelf = statsPanel.activeSelf;
		statsPanel.SetActive(!activeSelf);
		immiPanel.SetActive(value: false);
		lesiglationPanel.SetActive(value: false);
		budgetPanel.SetActive(value: false);
		settingsPanel.SetActive(value: false);
		researchPanel.SetActive(value: false);
		serviceTool.CloseServiceEditor();
		bi.superTool.CloseSuperTool();
		BlockInteraction.following = false;
	}

	public void QuickApproval()
	{
		bool activeSelf = statsPanel.activeSelf;
		statsPanel.SetActive(!activeSelf);
		immiPanel.SetActive(value: false);
		lesiglationPanel.SetActive(value: false);
		budgetPanel.SetActive(value: false);
		settingsPanel.SetActive(value: false);
		researchPanel.SetActive(value: false);
		eco.stats.OpenPol();
		serviceTool.CloseServiceEditor();
		bi.superTool.CloseSuperTool();
		BlockInteraction.following = false;
	}

	public void QuickPop()
	{
		bool activeSelf = statsPanel.activeSelf;
		statsPanel.SetActive(!activeSelf);
		immiPanel.SetActive(value: false);
		lesiglationPanel.SetActive(value: false);
		budgetPanel.SetActive(value: false);
		settingsPanel.SetActive(value: false);
		researchPanel.SetActive(value: false);
		eco.stats.OpenData();
		serviceTool.CloseServiceEditor();
		bi.superTool.CloseSuperTool();
		BlockInteraction.following = false;
	}

	public void QuickEco()
	{
		bool activeSelf = statsPanel.activeSelf;
		statsPanel.SetActive(!activeSelf);
		immiPanel.SetActive(value: false);
		lesiglationPanel.SetActive(value: false);
		budgetPanel.SetActive(value: false);
		settingsPanel.SetActive(value: false);
		researchPanel.SetActive(value: false);
		eco.stats.OpenEco();
		serviceTool.CloseServiceEditor();
		bi.superTool.CloseSuperTool();
		BlockInteraction.following = false;
	}

	public void OpenResearchPanel()
	{
		bool activeSelf = researchPanel.activeSelf;
		researchPanel.SetActive(!activeSelf);
		immiPanel.SetActive(value: false);
		lesiglationPanel.SetActive(value: false);
		budgetPanel.SetActive(value: false);
		settingsPanel.SetActive(value: false);
		statsPanel.SetActive(value: false);
		serviceTool.CloseServiceEditor();
		bi.superTool.CloseSuperTool();
		BlockInteraction.following = false;
	}

	private IEnumerator SaveCity(int next)
	{
		Time.timeScale = 1f;
		if (!isSaving)
		{
			isSaving = true;
			Debug.Log("Saving type: " + next);
			StartCoroutine(bi.world.FadeInScreen());
			HideAll();
			bi.world.loadingLoc.gameObject.SetActive(value: false);
			bi.world.loadingBar.value = 10f;
			bi.world.LoadingTask.text = LOCSYS.Translate("Save1") + " " + World.cityName;
			yield return new WaitForSecondsRealtime(0.8f);
			yield return null;
			yield return new WaitForEndOfFrame();
			Resources.UnloadUnusedAssets();
			yield return new WaitForEndOfFrame();
			GC.Collect();
			yield return new WaitForEndOfFrame();
			bi.world.loadingBar.value = 30f;
			lastCamPos = bi.world.RTScam.LookAt;
			bi.world.CleanUpForSaving();
			yield return new WaitForEndOfFrame();
			bi.world.loadingBar.value = 40f;
			DataRegion.Stability = (int)eco.Stability;
			DataRegion.FreedomIndex = (int)eco.FreedomIndex;
			DataRegion.CivilRights = (int)eco.CivilRights;
			DataRegion.Treasury += eco.budget.sharedTillNow;
			DataRegion.Treasury = Mathf.Clamp(DataRegion.Treasury, 0, 1000000000);
			DataRegion.cities[DataRegion.selectedCity.rX, DataRegion.selectedCity.rZ].balance = (int)eco.budget.Balance;
			if (eco.HQbuilding != null)
			{
				DataRegion.HQstage = eco.HQbuilding.Stage;
			}
			ES3AutoSaveMgr.Current.Save();
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
			bi.world.loadingBar.value = 50f;
			string text = DataRegion.nationName + bi.world.rX + bi.world.rZ;
			ES3.Save(text, bi.world.blockData, DataRegion.nationName + "/" + text + ".es3");
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
			bi.world.loadingBar.value = 70f;
			DataRegion.SaveDataCity(bi.world);
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
			bi.world.loadingBar.value = 90f;
			DataRegion.SaveDataRegion();
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
			bi.world.loadingBar.value = 100f;
			switch (next)
			{
			case 0:
				bi.world.LoadingTask.text = LOCSYS.Translate("Load6");
				SceneManager.LoadScene("RegionView");
				yield break;
			case 1:
				Application.Quit();
				yield break;
			}
			eco.budget.sharedTillNow = 0;
			bi.world.LoadingTask.text = LOCSYS.Translate("Save2");
			StartCoroutine(bi.world.FadeOutScreen());
			gamespeed.Speed_1();
			isSaving = false;
		}
	}
}
