using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WorldRegion : MonoBehaviour
{
	public static bool isNew = true;

	public RTS_Camera cameraRTS;

	public Material textureAtlas;

	public Material buildingTexture;

	public Material fluidTexture;

	public Material treeTexture;

	public Heightmapper heightmapper;

	public static int chunkSize = 40;

	public static int worldSize = 8;

	public static int worldHeight = 64;

	public static ConcurrentDictionary<string, ChunkRegion> coroutineChunks;

	public static List<string> toRemove = new List<string>();

	public static int realWorldSize = 0;

	public static CoroutineQueue queue;

	public static uint maxCoroutines = 50u;

	public static bool isReady = false;

	public static BlockLight[,] blockData;

	public static List<ChunkRegion> chunkList = new List<ChunkRegion>();

	public PolicyArray rawPolicyArray;

	public RegionUI regionUI;

	public SettingsUI settingsUI;

	public NationMenu nationMenu;

	public TextMeshProUGUI LoadingTask;

	public TextLocaliser loadingLoc;

	public Slider loadingBar;

	public CanvasGroup transition;

	public PostProcessVolume ppv;

	public AsyncOperation asyncLoad;

	public Image transitionImage;

	public List<Sprite> loadingImages;

	public static int imageIndex = 0;

	public GameObject oneStar;

	public GameObject twoStars;

	public GameObject threeStars;

	public CrashHandler crashHandler;

	public Projector selectProjector;

	public bool allGood;

	public static string BuildChunkName(Vector3 v)
	{
		return (int)v.x + "_" + (int)v.y + "_" + (int)v.z;
	}

	public static BlockRegion GetWorldBlock(Vector3 pos)
	{
		int num = ((!(pos.x < 0f)) ? ((int)(Mathf.Round(pos.x) / (float)chunkSize) * chunkSize) : ((int)(Mathf.Round(pos.x - (float)chunkSize) / (float)chunkSize) * chunkSize));
		int num2 = ((!(pos.y < 0f)) ? ((int)(Mathf.Round(pos.y) / (float)worldHeight) * worldHeight) : ((int)(Mathf.Round(pos.y - (float)worldHeight) / (float)worldHeight) * worldHeight));
		int num3 = ((!(pos.z < 0f)) ? ((int)(Mathf.Round(pos.z) / (float)chunkSize) * chunkSize) : ((int)(Mathf.Round(pos.z - (float)chunkSize) / (float)chunkSize) * chunkSize));
		int num4 = (int)Mathf.Abs(Mathf.Round(pos.x) - (float)num);
		int num5 = (int)Mathf.Abs(Mathf.Round(pos.y) - (float)num2);
		int num6 = (int)Mathf.Abs(Mathf.Round(pos.z) - (float)num3);
		string key = BuildChunkName(new Vector3(num, num2, num3));
		if (coroutineChunks.TryGetValue(key, out var value))
		{
			return value.chunkData[num4, num5, num6];
		}
		return null;
	}

	private void Awake()
	{
		transition.gameObject.SetActive(value: true);
		transition.alpha = 1f;
		imageIndex = UnityEngine.Random.Range(0, loadingImages.Count);
		transitionImage.sprite = loadingImages[imageIndex];
	}

	private void Start()
	{
		SettingsStatic.firstTime = false;
		StartCoroutine(crashHandler.CrashCountdown());
		try
		{
			if (ES3.KeyExists("nationName"))
			{
				DataRegion.nationName = ES3.LoadString("nationName", DataRegion.nationName);
			}
		}
		catch (Exception)
		{
			Debug.Log("Unable to open last saved nation!");
			DataRegion.nationName = "Default2021";
		}
		if (DataRegion.nationName == "Default2021")
		{
			SettingsStatic.firstTime = true;
			ES3.DeleteFile("settings.es3");
		}
		if (SteamLanguage.script.gameObject.activeSelf)
		{
			SteamLanguage.script.GetSteamLanguageCode();
		}
		SettingsStatic.LoadSettings();
		settingsUI.gameObject.SetActive(value: true);
		if (SettingsStatic.firstTime)
		{
			SettingsStatic.resolutionWidth = Screen.currentResolution.width;
			SettingsStatic.reolutionHeight = Screen.currentResolution.height;
			nationMenu.NewNation();
			SettingsStatic.graphicsOption[0] = 2;
			settingsUI.ApplyPreset(2);
		}
		else
		{
			settingsUI.StartCoroutine(settingsUI.LoadSettings());
		}
		if (DataRegion.nationName != "Default2021" && ES3.KeyExists(DataRegion.nationName + "cities"))
		{
			isNew = false;
			DataRegion.LoadDataRegion(inRegion: true);
			regionUI.SetFlags();
			SetRegionSize();
			if (blockData == null)
			{
				nationMenu.NewNation();
				settingsUI.CloseSettings();
				StartCoroutine(FadeOutScreen());
				Debug.Log("Missing blockData");
				return;
			}
		}
		else
		{
			Debug.Log("Region is new. Initializing Data Arrays...");
			if (DataRegion.nationName != "Default2021")
			{
				Debug.Log(DataRegion.nationName);
				regionUI.step = 1;
				regionUI.SetArrowStep(0);
				DataRegion.ecoSettings = ES3.Load(DataRegion.nationName + "ecoSettings", DataRegion.ecoSettings);
				DataRegion.savedNations = ES3.Load("savedNations", DataRegion.savedNations);
				if (DataRegion.ecoSettings[0] == 0f)
				{
					DataRegion.Treasury = 200000;
					DataRegion.MoneyPrinted = 0f;
				}
				else if (DataRegion.ecoSettings[0] == 1f)
				{
					DataRegion.Treasury = 150000;
					DataRegion.MoneyPrinted = 0f;
				}
				else
				{
					DataRegion.Treasury = 100000;
					DataRegion.MoneyPrinted = 20000f;
				}
				SetRegionSize();
			}
			isNew = true;
			DataRegion.dataLoaded = false;
			DataRegion.cities = new DataCity[worldSize, worldSize];
			DataRegion.policyArray = rawPolicyArray;
			blockData = new BlockLight[worldSize * chunkSize * 8, worldSize * chunkSize * 8];
			if (ES3.KeyExists(DataRegion.nationName + "MapFile"))
			{
				DataRegion.mapFile = ES3.LoadString(DataRegion.nationName + "MapFile", DataRegion.mapFile);
			}
			else
			{
				Debug.Log("Map Name Was Not Saved. Nation name= " + DataRegion.nationName);
			}
			for (int i = 0; i < worldSize; i++)
			{
				for (int j = 0; j < worldSize; j++)
				{
					DataRegion.cities[i, j] = new DataCity
					{
						codeName = i.ToString() + j + DataRegion.nationName,
						rX = i,
						rZ = j
					};
				}
			}
		}
		if (DataRegion.mapFile.StartsWith("Map"))
		{
			string path = Application.persistentDataPath + "/" + DataRegion.mapFile + ".png";
			if (!File.Exists(path))
			{
				Debug.LogError("Heightmap Introuvable!! Loading Default Map...");
				Heightmapper.Land = (Texture2D)Resources.Load("Maps/Costal Isa");
			}
			else
			{
				Debug.Log("Loading Custom Map! Wouhou.");
				byte[] data = File.ReadAllBytes(path);
				Texture2D texture2D = new Texture2D(1, 1);
				texture2D.LoadImage(data);
				Heightmapper.Land = texture2D;
			}
		}
		else
		{
			Heightmapper.Land = (Texture2D)Resources.Load("Maps/" + DataRegion.mapFile);
		}
		DataRegion.UpdatePop();
		regionUI.pop.text = DataRegion.Population.ToString("N0");
		regionUI.treasury.text = "$" + DataRegion.Treasury.ToString("N0");
		regionUI.nationName.text = DataRegion.nationName;
		regionUI.power.text = (DataRegion.PowerCapacity - DataRegion.PowerUse).ToString("N0") + "MWh";
		regionUI.stab.text = DataRegion.Stability.ToString("N0");
		regionUI.fi.text = DataRegion.FreedomIndex.ToString("N0");
		regionUI.cr.text = DataRegion.CivilRights.ToString("N0");
		regionUI.er.text = (DataRegion.TradeUse / 100f).ToString("F0") + "k / " + (DataRegion.TradeCapacity / 100).ToString("F0") + "k";
		if (DataRegion.ecoSettings[0] == 0f)
		{
			oneStar.SetActive(value: true);
			twoStars.SetActive(value: false);
			threeStars.SetActive(value: false);
		}
		else if (DataRegion.ecoSettings[0] == 1f)
		{
			oneStar.SetActive(value: false);
			twoStars.SetActive(value: true);
			threeStars.SetActive(value: false);
		}
		else if (DataRegion.ecoSettings[0] == 2f)
		{
			oneStar.SetActive(value: false);
			twoStars.SetActive(value: false);
			threeStars.SetActive(value: true);
		}
		cameraRTS.transform.position = new Vector3(200f, 100f, 200f);
		cameraRTS.follower.transform.position = new Vector3(200f, 100f, 200f);
		realWorldSize = worldSize * chunkSize;
		cameraRTS.gameObject.SetActive(value: false);
		coroutineChunks = new ConcurrentDictionary<string, ChunkRegion>();
		transform.position = Vector3.zero;
		transform.rotation = Quaternion.identity;
		queue = new CoroutineQueue(maxCoroutines, StartCoroutine);
		for (int k = 0; k < worldSize; k++)
		{
			for (int l = 0; l < worldSize; l++)
			{
				BuildChunkAt(k, l);
			}
		}
		queue.Run(DrawChunks());
		isReady = true;
		if (!cameraRTS.gameObject.activeSelf)
		{
			cameraRTS.gameObject.SetActive(value: true);
		}
		loadingBar.value = 5f;
		if (!isNew)
		{
			LoadingTask.text = LOCSYS.Translate("Start1") + " " + DataRegion.nationName;
		}
		else
		{
			LoadingTask.text = LOCSYS.Translate("Start2");
		}
	}

	private void SetRegionSize()
	{
		if (DataRegion.ecoSettings[1] == 5f)
		{
			worldSize = 5;
			chunkSize = 64;
			World.worldSize = 32;
			selectProjector.orthographicSize = 31.45f;
		}
		else
		{
			worldSize = 8;
			chunkSize = 40;
			World.worldSize = 20;
			selectProjector.orthographicSize = 19.45f;
		}
	}

	private void BuildChunkAt(int x, int z)
	{
		Vector3 position = new Vector3(x * chunkSize, 0f, z * chunkSize);
		DataCity city = DataRegion.cities[x, z];
		ChunkRegion chunkRegion = new ChunkRegion(position, this, x, z, city);
		chunkRegion.chunk.transform.parent = transform;
		coroutineChunks.TryAdd(chunkRegion.chunk.name, chunkRegion);
	}

	private IEnumerator DrawChunks()
	{
		LoadingTask.text = LOCSYS.Translate("Start3");
		int count = 0;
		foreach (KeyValuePair<string, ChunkRegion> coroutineChunk in coroutineChunks)
		{
			if (coroutineChunk.Value.status == ChunkRegion.ChunkStatus.DRAW)
			{
				coroutineChunk.Value.DrawChunk();
			}
			count++;
			if (count >= 8)
			{
				count = 0;
				loadingBar.value += 5f;
				yield return new WaitForFixedUpdate();
			}
			yield return null;
		}
		LoadingTask.text = LOCSYS.Translate("Start4");
		Time.timeScale = 1f;
		yield return new WaitForFixedUpdate();
		loadingBar.value = 50f;
		regionUI.ProjectRoads();
		yield return new WaitForEndOfFrame();
		loadingBar.value += 5f;
		settingsUI.CloseSettings();
		StartCoroutine(PreLoadCityScene());
	}

	private IEnumerator PreLoadCityScene()
	{
		yield return new WaitForSeconds(0.1f);
		Application.backgroundLoadingPriority = ThreadPriority.High;
		asyncLoad = SceneManager.LoadSceneAsync("Main_Scene_March2020", LoadSceneMode.Single);
		asyncLoad.allowSceneActivation = false;
		yield return new WaitForFixedUpdate();
		allGood = true;
		yield return new WaitForFixedUpdate();
		StartCoroutine(FadeOutScreen());
	}

	public IEnumerator FadeOutScreen()
	{
		yield return new WaitForEndOfFrame();
		loadingBar.value = 100f;
		float alpha = 1f;
		for (int i = 0; i < 11; i++)
		{
			alpha -= 0.1f;
			transition.alpha = alpha;
			yield return new WaitForEndOfFrame();
		}
		yield return new WaitForFixedUpdate();
		transition.gameObject.SetActive(value: false);
	}

	public IEnumerator FadeInScreen()
	{
		transition.alpha = 0f;
		transition.gameObject.SetActive(value: true);
		loadingBar.value = 0f;
		LoadingTask.text = LOCSYS.Translate("Load6");
		float alpha = 0.2f;
		for (int i = 0; i < 11; i++)
		{
			alpha += 0.1f;
			transition.alpha = alpha;
			yield return new WaitForEndOfFrame();
		}
	}

	public IEnumerator BlurrIn()
	{
		ppv.profile.TryGetSettings<DepthOfField>(out var dof);
		dof.focusDistance.value = 70f;
		for (int i = 160; i > 100; i--)
		{
			dof.focusDistance.value--;
			yield return new WaitForEndOfFrame();
		}
		yield return new WaitForEndOfFrame();
	}

	public void BlurrOff()
	{
		ppv.profile.TryGetSettings<DepthOfField>(out var outSetting);
		Debug.Log("Blurr Off");
		outSetting.focusDistance.value = 100f;
	}
}
