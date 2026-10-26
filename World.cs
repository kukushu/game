using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class World : MonoBehaviour
{
	public TextMeshProUGUI CityNameInfo;

	public GameObject cam;

	public Material terrainT;

	public Material fluidTexture;

	public Material trunkTexture;

	public Material leafTexture;

	public Material grassTexture;

	public Material roadTexture;

	public Material avenueTexture;

	public Material propTexture;

	public Material vehicleTexture;

	public List<Material> decayMat = new List<Material>();

	public static int chunkSize = 16;

	public static int worldSize = 20;

	private List<Chunk> chunks = new List<Chunk>();

	public static int realWorldSize = 8;

	public GameObject Biosphere;

	public PhysicMaterial asphalt;

	public PhysicMaterial dirt;

	public ReflectionProbe reflectionProbe;

	public List<LotCreator> VacantResLots = new List<LotCreator>();

	public List<LotCreator> VacantIndusLots = new List<LotCreator>();

	public List<LotCreator> VacantCommLots = new List<LotCreator>();

	public List<LotCreator> VacantPrimaryLots = new List<LotCreator>();

	public List<LotCreator> VacantMixedLots = new List<LotCreator>();

	public List<LotCreator> ResidentialBuildings = new List<LotCreator>();

	public List<LotCreator> IndustrialBuildings = new List<LotCreator>();

	public List<LotCreator> CommercialBuildings = new List<LotCreator>();

	public List<LotCreator> MixedBuildings = new List<LotCreator>();

	public List<SlumCreator> SlumBuildings = new List<SlumCreator>();

	public List<ServiceCreator> Arcologies = new List<ServiceCreator>();

	public List<RoadCreator> Roads = new List<RoadCreator>();

	public List<RoadCreator> Rails = new List<RoadCreator>();

	public List<RoadCreator> Underground = new List<RoadCreator>();

	public List<ServiceCreator> Services = new List<ServiceCreator>();

	public bool[] alreadyUnlocked = new bool[50];

	public Vector3 lastbuildPos;

	private BlockData fou = new BlockData();

	public LotAudio la;

	public Economy eco;

	public Indexer indexer;

	public Immigration immi;

	public Buldoze buldoze;

	public RTS_Camera RTScam;

	public AnimationCurve distanceCurve;

	public BlockData[,] blockData;

	public static HashSet<Chunk> updates = new HashSet<Chunk>();

	public int rX;

	public int rZ;

	public static string cityName = "none";

	public string nameCode;

	public string RegionMap = "Papua";

	public bool loaded;

	public Vector3 pos = Vector3.zero;

	public bool x_con;

	public bool _xcon;

	public bool z_con;

	public bool _zcon;

	public static List<RoadCreator> exitRoads = new List<RoadCreator>();

	public static List<RoadCreator> entranceRoads = new List<RoadCreator>();

	public float LCbirthNeighbors;

	public float MCbirthNeighbors;

	public float UCbirthNeighbors;

	public float neighborJobsAvailable;

	public TextLocaliser loadingLoc;

	public TextMeshProUGUI LoadingTask;

	public Slider loadingBar;

	public CanvasGroup transition;

	public Image transitionImage;

	public List<Sprite> loadingImages;

	public static bool dataReady = false;

	public static bool commuteReady = false;

	public float incomeMultiplier = 1f;

	public float approvalBonus;

	public bool unemployStabilizer;

	public float pollutionMultiplier = 1f;

	public float decayMultiplier = 1f;

	public SettingsUI settingsUI;

	public static World staticWorld;

	public GameObject waterPlane;

	public GameObject frame512x;

	public GameObject frame320x;

	public Texture2D defaultFlag;

	public GameObject grassCliff;

	public List<GameObject> stoneCliffs = new List<GameObject>();

	public GameObject stoneCliffBuilt;

	private void Awake()
	{
		staticWorld = this;
	}

	public static string BuildChunkName(Vector3 v)
	{
		return (int)v.x + "_" + (int)v.y + "_" + (int)v.z;
	}

	public BlockData Get(int x, int z)
	{
		if (x < 0 || z < 0 || x >= worldSize * chunkSize || z >= worldSize * chunkSize)
		{
			return fou;
		}
		return blockData[x, z];
	}

	public BlockData GetIfNotNull(int x, int z)
	{
		if (x < 0 || z < 0 || x >= worldSize * chunkSize || z >= worldSize * chunkSize)
		{
			return null;
		}
		return blockData[x, z];
	}

	public BlockData GetBounded(int x, int z)
	{
		if (x < 0)
		{
			x = 0;
		}
		else if (x >= worldSize * chunkSize)
		{
			x = worldSize * chunkSize - 1;
		}
		if (z < 0)
		{
			z = 0;
		}
		else if (z >= worldSize * chunkSize)
		{
			z = worldSize * chunkSize - 1;
		}
		return blockData[x, z];
	}

	private void Start()
	{
		transition.gameObject.SetActive(value: true);
		transition.alpha = 1f;
		transitionImage.sprite = loadingImages[WorldRegion.imageIndex];
		loadingLoc.key = Dico.Tips[Random.Range(0, Dico.Tips.Length)];
		loadingLoc.gameObject.SetActive(value: true);
		realWorldSize = worldSize * chunkSize;
		cam.transform.position = new Vector3(realWorldSize / 2, 100f, realWorldSize / 2 / 2);
		settingsUI.gameObject.SetActive(value: true);
		settingsUI.StartCoroutine(settingsUI.LoadSettings());
		dataReady = false;
		commuteReady = false;
		QualitySettings.shadowDistance = 250f;
		DataRegion.LoadDataRegion(inRegion: false);
		eco.FreedomIndex = DataRegion.FreedomIndex;
		eco.CivilRights = DataRegion.CivilRights;
		eco.Stability = DataRegion.Stability;
		rX = DataRegion.selectedCity.rX;
		rZ = DataRegion.selectedCity.rZ;
		cityName = DataRegion.selectedCity.cityName;
		nameCode = DataRegion.selectedCity.codeName;
		GameObject.Find("Easy Save 3 Manager").GetComponent<ES3AutoSaveMgr>().settings.path = DataRegion.nationName + "/" + cityName + "/" + cityName + rX + rZ + ".es3";
		loadingBar.value = 40f;
		eco.budget.Balance = DataRegion.cities[DataRegion.selectedCity.rX, DataRegion.selectedCity.rZ].balance;
		if (DataRegion.selectedCity.built)
		{
			string text = DataRegion.nationName + rX + rZ;
			blockData = ES3.Load(text, DataRegion.nationName + "/" + text + ".es3", blockData);
			LoadingTask.text = LOCSYS.Translate("Load7");
			loaded = true;
			if (blockData == null)
			{
				blockData = new BlockData[worldSize * 16, worldSize * 16];
				loaded = false;
				Debug.Log("Corrupted city data.");
				LoadingTask.text = "CORRUPTED CITY DATA. FORCING TO OPEN...";
			}
		}
		else
		{
			blockData = new BlockData[worldSize * 16, worldSize * 16];
			eco.LC_Demand = 800f;
			eco.R_Demand = 0f;
			LoadingTask.text = LOCSYS.Translate("Load8");
		}
		chunks = new List<Chunk>();
		transform.position = Vector3.zero;
		transform.rotation = Quaternion.identity;
		for (int i = 0; i < worldSize; i++)
		{
			for (int j = 0; j < worldSize; j++)
			{
				BuildChunkAt(i, j);
			}
		}
		StartCoroutine(LaunchSimulation());
		CityNameInfo.text = cityName;
		eco.budget.UI_Balance.text = "$" + eco.budget.Balance.ToString("N0");
		RTScam.MouseRotateSensity = SettingsStatic.mouseSense;
		RTScam.mouseMovementSpeed = SettingsStatic.mouseSense * 1.66f;
		RTScam.scrollwheelSensity = SettingsStatic.mouseSense;
		RTScam.KeyboardRotateSensity = SettingsStatic.keyboardSense;
		RTScam.keyboardMovementSpeed = SettingsStatic.keyboardSense * 0.0175f;
		RTScam.keyboardZoomingSensity = SettingsStatic.keyboardSense * 75f;
		RTScam.edgeInput_bool = SettingsStatic.edgeScroll;
		RTScam.boundary = chunkSize * worldSize + 10;
		if (DataRegion.ecoSettings[0] == 0f)
		{
			incomeMultiplier = 1.3f;
			approvalBonus = 20f;
			unemployStabilizer = true;
			pollutionMultiplier = 0.5f;
			decayMultiplier = 0.5f;
			eco.immiDivider = 12f;
			eco.birthBase = 2.1f;
			indexer.oneStar.SetActive(value: true);
			indexer.twoStars.SetActive(value: false);
			indexer.threeStars.SetActive(value: false);
		}
		else if (DataRegion.ecoSettings[0] == 1f)
		{
			Debug.Log("Normal Mode");
			incomeMultiplier = 1.15f;
			approvalBonus = 5f;
			unemployStabilizer = true;
			pollutionMultiplier = 1f;
			decayMultiplier = 0.8f;
			eco.immiDivider = 11f;
			eco.birthBase = 2f;
			indexer.oneStar.SetActive(value: false);
			indexer.twoStars.SetActive(value: true);
			indexer.threeStars.SetActive(value: false);
		}
		else
		{
			Debug.Log("Realistic Mode");
			incomeMultiplier = 1f;
			approvalBonus = 0f;
			unemployStabilizer = false;
			pollutionMultiplier = 1f;
			decayMultiplier = 1f;
			eco.immiDivider = 10f;
			eco.birthBase = 1.8f;
			indexer.oneStar.SetActive(value: false);
			indexer.twoStars.SetActive(value: false);
			indexer.threeStars.SetActive(value: true);
		}
	}

	private void BuildChunkAt(int x, int z)
	{
		Chunk chunk = new Chunk(new Vector3(x * chunkSize, 0f, z * chunkSize), this);
		chunk.chunk.transform.parent = transform;
		chunks.Add(chunk);
	}

	private IEnumerator DrawChunks()
	{
		loadingBar.value = 50f;
		LoadingTask.text = LOCSYS.Translate("Load3");
		int count = 0;
		int valueAdd = 3;
		if (worldSize == 32)
		{
			valueAdd = 2;
		}
		foreach (Chunk chunk in chunks)
		{
			chunk.DrawChunk();
			count++;
			if (count > worldSize)
			{
				count = 0;
				loadingBar.value += valueAdd;
				yield return new WaitForEndOfFrame();
			}
		}
		LoadingTask.text = LOCSYS.Translate("Load5");
		eco.leg.legislationPanel.SetActive(value: true);
		yield return new WaitForEndOfFrame();
		eco.leg.legislationPanel.SetActive(value: false);
		yield return new WaitForEndOfFrame();
		Resources.UnloadUnusedAssets();
		yield return new WaitForEndOfFrame();
		CheckUpMissingMaterials();
		yield return new WaitForEndOfFrame();
		settingsUI.CloseSettings();
		StartCoroutine(FadeOutScreen());
		BlockData blockData = this.blockData[worldSize * 16 / 2, worldSize * 16 / 2];
		reflectionProbe.transform.position = new Vector3(worldSize * 16 / 2, blockData.h + 5f, worldSize * 16 / 2);
		reflectionProbe.size = new Vector3(worldSize * 16, 50f, worldSize * 16);
		loadingBar.value = 100f;
		yield return new WaitForEndOfFrame();
		reflectionProbe.RenderProbe();
		fou.v[0] = (short)eco.AverageValue;
		fou.v[1] = 1;
		cam.GetComponent<RTS_Camera>().GoSurface();
		immi.RetrieveSliderPositions();
		StartCoroutine(ContinuousChunkUpdate());
		yield return new WaitForEndOfFrame();
		UpdateConnections();
	}

	private void UpdateConnections()
	{
		Debug.Log("There are " + DataRegion.selectedCity.connections.Count + " connections in this city.");
		Roading component = GetComponent<Roading>();
		Avenueing component2 = GetComponent<Avenueing>();
		int num = worldSize * chunkSize;
		int num2 = DataRegion.selectedCity.rX;
		int num3 = DataRegion.selectedCity.rZ;
		DataCity city = DataRegion.GetCity(num2 - 1, num3);
		DataCity city2 = DataRegion.GetCity(num2 + 1, num3);
		DataCity city3 = DataRegion.GetCity(num2, num3 - 1);
		DataCity city4 = DataRegion.GetCity(num2, num3 + 1);
		eco.neighborsJobs = 0f;
		if (city != null)
		{
			foreach (Connection connection in city.connections)
			{
				if (Get(0, connection.entrancePos.y).r || Get(0, connection.entrancePos.y - 1).r)
				{
					_xcon = true;
				}
				else
				{
					if (connection.entrancePos.x != num - 1)
					{
						continue;
					}
					if (connection.roadType == 0)
					{
						Get(0, connection.entrancePos.y).r = true;
						Get(0, connection.entrancePos.y - 1).r = true;
						Get(1, connection.entrancePos.y).r = true;
						Get(1, connection.entrancePos.y - 1).r = true;
						Get(2, connection.entrancePos.y).r = true;
						Get(2, connection.entrancePos.y - 1).r = true;
						Get(3, connection.entrancePos.y).r = true;
						Get(3, connection.entrancePos.y - 1).r = true;
						component.FlatNewLineX(new Vector2Int(0, connection.entrancePos.y));
						component.BuildNewLineX(new Vector2Int(0, connection.entrancePos.y));
						component.FlatNewLineX(new Vector2Int(1, connection.entrancePos.y));
						component.BuildNewLineX(new Vector2Int(1, connection.entrancePos.y));
						component.FlatNewLineX(new Vector2Int(2, connection.entrancePos.y));
						component.BuildNewLineX(new Vector2Int(2, connection.entrancePos.y));
						component.FlatNewLineX(new Vector2Int(3, connection.entrancePos.y));
						component.BuildNewLineX(new Vector2Int(3, connection.entrancePos.y));
						_xcon = true;
					}
					else if (connection.roadType == 1 || connection.roadType == 2)
					{
						if (connection.roadType == 1)
						{
							Avenueing.subType = "Avenue/";
						}
						else
						{
							Avenueing.subType = "Hway/";
						}
						Get(0, connection.entrancePos.y).r = true;
						Get(0, connection.entrancePos.y - 1).r = true;
						Get(0, connection.entrancePos.y + 1).r = true;
						Get(1, connection.entrancePos.y).r = true;
						Get(1, connection.entrancePos.y - 1).r = true;
						Get(1, connection.entrancePos.y + 1).r = true;
						Get(2, connection.entrancePos.y).r = true;
						Get(2, connection.entrancePos.y - 1).r = true;
						Get(2, connection.entrancePos.y + 1).r = true;
						Get(3, connection.entrancePos.y).r = true;
						Get(3, connection.entrancePos.y - 1).r = true;
						Get(3, connection.entrancePos.y + 1).r = true;
						component2.FlatAndBuildLineX(new Vector3(0f, 0f, connection.entrancePos.y));
						component2.FlatAndBuildLineX(new Vector3(1f, 0f, connection.entrancePos.y));
						component2.FlatAndBuildLineX(new Vector3(2f, 0f, connection.entrancePos.y));
						component2.FlatAndBuildLineX(new Vector3(3f, 0f, connection.entrancePos.y));
						_xcon = true;
					}
				}
			}
		}
		if (city2 != null)
		{
			foreach (Connection connection2 in city2.connections)
			{
				if (Get(num - 1, connection2.exitPos.y).r || Get(num - 1, connection2.exitPos.y - 1).r)
				{
					x_con = true;
				}
				else
				{
					if (connection2.exitPos.x != 0)
					{
						continue;
					}
					if (connection2.roadType == 0)
					{
						Get(num - 4, connection2.exitPos.y).r = true;
						Get(num - 4, connection2.exitPos.y - 1).r = true;
						Get(num - 3, connection2.exitPos.y).r = true;
						Get(num - 3, connection2.exitPos.y - 1).r = true;
						Get(num - 2, connection2.exitPos.y).r = true;
						Get(num - 2, connection2.exitPos.y - 1).r = true;
						Get(num - 1, connection2.exitPos.y).r = true;
						Get(num - 1, connection2.exitPos.y - 1).r = true;
						component.FlatNewLineX(new Vector2Int(num - 4, connection2.exitPos.y));
						component.BuildNewLineX(new Vector2Int(num - 4, connection2.exitPos.y));
						component.FlatNewLineX(new Vector2Int(num - 3, connection2.exitPos.y));
						component.BuildNewLineX(new Vector2Int(num - 3, connection2.exitPos.y));
						component.FlatNewLineX(new Vector2Int(num - 2, connection2.exitPos.y));
						component.BuildNewLineX(new Vector2Int(num - 2, connection2.exitPos.y));
						component.FlatNewLineX(new Vector2Int(num - 1, connection2.exitPos.y));
						component.BuildNewLineX(new Vector2Int(num - 1, connection2.exitPos.y));
						x_con = true;
					}
					else if (connection2.roadType == 1 || connection2.roadType == 2)
					{
						if (connection2.roadType == 1)
						{
							Avenueing.subType = "Avenue/";
						}
						else
						{
							Avenueing.subType = "Hway/";
						}
						Get(num - 4, connection2.exitPos.y).r = true;
						Get(num - 4, connection2.exitPos.y - 1).r = true;
						Get(num - 4, connection2.exitPos.y + 1).r = true;
						Get(num - 3, connection2.exitPos.y).r = true;
						Get(num - 3, connection2.exitPos.y - 1).r = true;
						Get(num - 3, connection2.exitPos.y + 1).r = true;
						Get(num - 2, connection2.exitPos.y).r = true;
						Get(num - 2, connection2.exitPos.y - 1).r = true;
						Get(num - 2, connection2.exitPos.y + 1).r = true;
						Get(num - 1, connection2.exitPos.y).r = true;
						Get(num - 1, connection2.exitPos.y - 1).r = true;
						Get(num - 1, connection2.exitPos.y + 1).r = true;
						component2.FlatAndBuildLineX(new Vector3(num - 4, 0f, connection2.exitPos.y));
						component2.FlatAndBuildLineX(new Vector3(num - 3, 0f, connection2.exitPos.y));
						component2.FlatAndBuildLineX(new Vector3(num - 2, 0f, connection2.exitPos.y));
						component2.FlatAndBuildLineX(new Vector3(num - 1, 0f, connection2.exitPos.y));
						x_con = true;
					}
				}
			}
		}
		if (city3 != null)
		{
			foreach (Connection connection3 in city3.connections)
			{
				if (Get(connection3.entrancePos.x, 0).r || Get(connection3.entrancePos.x - 1, 0).r)
				{
					_zcon = true;
				}
				else
				{
					if (connection3.entrancePos.y != num - 1)
					{
						continue;
					}
					if (connection3.roadType == 0)
					{
						Get(connection3.entrancePos.x, 0).r = true;
						Get(connection3.entrancePos.x - 1, 0).r = true;
						Get(connection3.entrancePos.x, 1).r = true;
						Get(connection3.entrancePos.x - 1, 1).r = true;
						Get(connection3.entrancePos.x, 2).r = true;
						Get(connection3.entrancePos.x - 1, 2).r = true;
						Get(connection3.entrancePos.x, 3).r = true;
						Get(connection3.entrancePos.x - 1, 3).r = true;
						component.FlatNewLineZ(new Vector2Int(connection3.entrancePos.x, 0));
						component.BuildNewLineZ(new Vector2Int(connection3.entrancePos.x, 0));
						component.FlatNewLineZ(new Vector2Int(connection3.entrancePos.x, 1));
						component.BuildNewLineZ(new Vector2Int(connection3.entrancePos.x, 1));
						component.FlatNewLineZ(new Vector2Int(connection3.entrancePos.x, 2));
						component.BuildNewLineZ(new Vector2Int(connection3.entrancePos.x, 2));
						component.FlatNewLineZ(new Vector2Int(connection3.entrancePos.x, 3));
						component.BuildNewLineZ(new Vector2Int(connection3.entrancePos.x, 3));
						_zcon = true;
					}
					else if (connection3.roadType == 1 || connection3.roadType == 2)
					{
						if (connection3.roadType == 1)
						{
							Avenueing.subType = "Avenue/";
						}
						else
						{
							Avenueing.subType = "Hway/";
						}
						Get(connection3.entrancePos.x, 0).r = true;
						Get(connection3.entrancePos.x - 1, 0).r = true;
						Get(connection3.entrancePos.x + 1, 0).r = true;
						Get(connection3.entrancePos.x, 1).r = true;
						Get(connection3.entrancePos.x - 1, 1).r = true;
						Get(connection3.entrancePos.x + 1, 1).r = true;
						Get(connection3.entrancePos.x, 2).r = true;
						Get(connection3.entrancePos.x - 1, 2).r = true;
						Get(connection3.entrancePos.x + 1, 2).r = true;
						Get(connection3.entrancePos.x, 3).r = true;
						Get(connection3.entrancePos.x - 1, 3).r = true;
						Get(connection3.entrancePos.x + 1, 3).r = true;
						component2.FlatAndBuildLineZ(new Vector3(connection3.entrancePos.x, 0f, 0f));
						component2.FlatAndBuildLineZ(new Vector3(connection3.entrancePos.x, 0f, 1f));
						component2.FlatAndBuildLineZ(new Vector3(connection3.entrancePos.x, 0f, 2f));
						component2.FlatAndBuildLineZ(new Vector3(connection3.entrancePos.x, 0f, 3f));
						_zcon = true;
					}
				}
			}
		}
		if (city4 != null)
		{
			foreach (Connection connection4 in city4.connections)
			{
				if (Get(connection4.exitPos.x, num - 1).r || Get(connection4.exitPos.x - 1, num - 1).r)
				{
					z_con = true;
				}
				else
				{
					if (connection4.exitPos.y != 0)
					{
						continue;
					}
					if (connection4.roadType == 0)
					{
						Get(connection4.exitPos.x, num - 4).r = true;
						Get(connection4.exitPos.x - 1, num - 4).r = true;
						Get(connection4.exitPos.x, num - 3).r = true;
						Get(connection4.exitPos.x - 1, num - 3).r = true;
						Get(connection4.exitPos.x, num - 2).r = true;
						Get(connection4.exitPos.x - 1, num - 2).r = true;
						Get(connection4.exitPos.x, num - 1).r = true;
						Get(connection4.exitPos.x - 1, num - 1).r = true;
						component.FlatNewLineZ(new Vector2Int(connection4.exitPos.x, num - 4));
						component.BuildNewLineZ(new Vector2Int(connection4.exitPos.x, num - 4));
						component.FlatNewLineZ(new Vector2Int(connection4.exitPos.x, num - 3));
						component.BuildNewLineZ(new Vector2Int(connection4.exitPos.x, num - 3));
						component.FlatNewLineZ(new Vector2Int(connection4.exitPos.x, num - 2));
						component.BuildNewLineZ(new Vector2Int(connection4.exitPos.x, num - 2));
						component.FlatNewLineZ(new Vector2Int(connection4.exitPos.x, num - 1));
						component.BuildNewLineZ(new Vector2Int(connection4.exitPos.x, num - 1));
						z_con = true;
					}
					else if (connection4.roadType == 1 || connection4.roadType == 2)
					{
						if (connection4.roadType == 1)
						{
							Avenueing.subType = "Avenue/";
						}
						else
						{
							Avenueing.subType = "Hway/";
						}
						Get(connection4.exitPos.x, num - 4).r = true;
						Get(connection4.exitPos.x - 1, num - 4).r = true;
						Get(connection4.exitPos.x + 1, num - 4).r = true;
						Get(connection4.exitPos.x, num - 3).r = true;
						Get(connection4.exitPos.x - 1, num - 3).r = true;
						Get(connection4.exitPos.x + 1, num - 3).r = true;
						Get(connection4.exitPos.x, num - 2).r = true;
						Get(connection4.exitPos.x - 1, num - 2).r = true;
						Get(connection4.exitPos.x + 1, num - 2).r = true;
						Get(connection4.exitPos.x, num - 1).r = true;
						Get(connection4.exitPos.x - 1, num - 1).r = true;
						Get(connection4.exitPos.x + 1, num - 1).r = true;
						component2.FlatAndBuildLineZ(new Vector3(connection4.exitPos.x, 0f, num - 4));
						component2.FlatAndBuildLineZ(new Vector3(connection4.exitPos.x, 0f, num - 3));
						component2.FlatAndBuildLineZ(new Vector3(connection4.exitPos.x, 0f, num - 2));
						component2.FlatAndBuildLineZ(new Vector3(connection4.exitPos.x, 0f, num - 1));
						z_con = true;
					}
				}
			}
		}
		if (_xcon)
		{
			LCbirthNeighbors += city.LCbirth * 0.2f;
			MCbirthNeighbors += city.MCbirth * 0.2f;
			UCbirthNeighbors += city.UCbirth * 0.2f;
		}
		if (x_con)
		{
			LCbirthNeighbors += city2.LCbirth * 0.2f;
			MCbirthNeighbors += city2.MCbirth * 0.2f;
			UCbirthNeighbors += city2.UCbirth * 0.2f;
		}
		if (_zcon)
		{
			LCbirthNeighbors += city3.LCbirth * 0.2f;
			MCbirthNeighbors += city3.MCbirth * 0.2f;
			UCbirthNeighbors += city3.UCbirth * 0.2f;
		}
		if (z_con)
		{
			LCbirthNeighbors += city4.LCbirth * 0.2f;
			MCbirthNeighbors += city4.MCbirth * 0.2f;
			UCbirthNeighbors += city4.UCbirth * 0.2f;
		}
		UpdateNeighborJobs();
		StartCoroutine(component.UpdateChunks());
	}

	public void UpdateNeighborJobs()
	{
		int num = DataRegion.selectedCity.rX;
		int num2 = DataRegion.selectedCity.rZ;
		DataCity city = DataRegion.GetCity(num - 1, num2);
		DataCity city2 = DataRegion.GetCity(num + 1, num2);
		DataCity city3 = DataRegion.GetCity(num, num2 - 1);
		DataCity city4 = DataRegion.GetCity(num, num2 + 1);
		neighborJobsAvailable = 0f;
		if (_xcon)
		{
			neighborJobsAvailable += 0.2f * (float)city.jobsAvailable;
		}
		if (x_con)
		{
			neighborJobsAvailable += 0.2f * (float)city2.jobsAvailable;
		}
		if (_zcon)
		{
			neighborJobsAvailable += 0.2f * (float)city3.jobsAvailable;
		}
		if (z_con)
		{
			neighborJobsAvailable += 0.2f * (float)city4.jobsAvailable;
		}
		if (neighborJobsAvailable > eco.Population / 10f)
		{
			neighborJobsAvailable = eco.Population / 10f;
		}
		else if (neighborJobsAvailable < 0f)
		{
			neighborJobsAvailable = 0f;
		}
	}

	private IEnumerator ContinuousChunkUpdate()
	{
		while (true)
		{
			foreach (Chunk c in chunks)
			{
				if (c.updateTerrain && !c.updatingTerrain)
				{
					c.updatingTerrain = true;
					StartCoroutine(c.TerrainRedraw());
					for (int i = 0; i < 9; i++)
					{
						yield return null;
					}
				}
				if (c.updateProps && !c.updatingProps)
				{
					c.updatingProps = true;
					c.UpdateProps();
					yield return null;
				}
				if (c.updateFlora && !c.updatingFlora)
				{
					c.updatingFlora = true;
					c.UpdateGrass();
					c.UpdateTrees();
					yield return null;
					c.updatingFlora = false;
					c.updateFlora = false;
				}
				if (c.expiredRoads > 6 && !c.updatingRoads)
				{
					c.updatingRoads = true;
					c.UpdateRoads();
					yield return null;
				}
			}
			yield return new WaitForEndOfFrame();
		}
	}

	public void CleanUpForSaving()
	{
		foreach (LotCreator item in VacantCommLots.Concat(VacantResLots).Concat(VacantIndusLots).Concat(VacantMixedLots)
			.ToList())
		{
			item.reload = true;
			item.commuteLot = null;
			item.hasChanged = true;
		}
		foreach (LotCreator item2 in ResidentialBuildings.Concat(CommercialBuildings).Concat(IndustrialBuildings).Concat(MixedBuildings)
			.ToList())
		{
			item2.reload = true;
			item2.commuteLot = null;
			item2.hasChanged = true;
		}
		foreach (SlumCreator slumBuilding in SlumBuildings)
		{
			slumBuilding.reload = true;
		}
		foreach (ServiceCreator service in Services)
		{
			service.reload = true;
		}
		foreach (ServiceCreator arcology in Arcologies)
		{
			arcology.reload = true;
		}
		foreach (RoadCreator road in Roads)
		{
			road.reload = true;
			road.waypoint.occupers.Clear();
		}
		foreach (RoadCreator rail in Rails)
		{
			rail.reload = true;
			rail.waypoint.occupers.Clear();
		}
		foreach (RoadCreator item3 in Underground)
		{
			item3.reload = true;
			item3.waypoint.occupers.Clear();
		}
	}

	private IEnumerator LaunchSimulation()
	{
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		if (DataRegion.selectedCity.built)
		{
			ES3AutoSaveMgr.Current.Load();
		}
		yield return new WaitForEndOfFrame();
		StartCoroutine(DrawChunks());
		for (int i = 0; i < 60; i++)
		{
			yield return null;
		}
		indexer.gamespeed.Speed_0();
		_ = indexer.lastCamPos;
		RTScam.LookAt = indexer.lastCamPos;
		if (worldSize == 20)
		{
			waterPlane.transform.localScale = new Vector3(32f, 4f, 32f);
			waterPlane.transform.position = new Vector3(159.5f, 30.125f, 159.5f);
			frame320x.SetActive(value: true);
		}
		else
		{
			waterPlane.transform.localScale = new Vector3(51.2f, 4f, 51.2f);
			waterPlane.transform.position = new Vector3(255.5f, 30.125f, 255.5f);
			frame512x.SetActive(value: true);
		}
		int count = 0;
		foreach (Chunk update in updates)
		{
			update.UpdateRoads();
			count++;
			if (count > 7)
			{
				count = 0;
				yield return null;
			}
		}
		yield return new WaitForEndOfFrame();
		indexer.copterModel.transform.position = Indexer.padPos;
		if (Indexer.padPos != Vector3.zero)
		{
			indexer.copterModel.SetActive(value: true);
			indexer.copterTooltip.header = "";
		}
		else
		{
			indexer.copterTooltip.header = "noHelipad";
		}
		TrafficManager.cars = 0;
		TrafficManager.peds = 0;
		TrafficManager.nTrains = 0;
		updates.Clear();
		if (eco.Conservatives < 1f)
		{
			eco.Conservatives = 10f;
		}
		if (eco.Liberals < 1f)
		{
			eco.Liberals = 10f;
		}
		dataReady = true;
		eco.LaunchGrowth();
		indexer.zoning.lm.LaunchLotUpdates();
		yield return new WaitForSeconds(3f);
		commuteReady = true;
	}

	private void CheckUpMissingMaterials()
	{
		foreach (LotCreator item in ResidentialBuildings.ToList())
		{
			if (item != null)
			{
				item.FixMissingMaterials();
			}
		}
	}

	public IEnumerator FadeOutScreen()
	{
		yield return null;
		float alpha = 1f;
		for (int i = 0; i < 51; i++)
		{
			alpha -= 0.02f;
			transition.alpha = alpha;
			yield return null;
		}
		transition.gameObject.SetActive(value: false);
	}

	public IEnumerator FadeInScreen()
	{
		yield return null;
		transition.alpha = 0f;
		transitionImage.sprite = loadingImages[WorldRegion.imageIndex];
		transition.gameObject.SetActive(value: true);
		float alpha = 0f;
		for (int i = 0; i < 22; i++)
		{
			alpha += 0.05f;
			transition.alpha = alpha;
			yield return null;
		}
	}
}
