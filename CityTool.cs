using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CityTool : MonoBehaviour
{
	public TextMeshProUGUI Title;

	public TextMeshProUGUI Line1;

	public TextMeshProUGUI Unemp;

	public TextMeshProUGUI GdpCap;

	public TextMeshProUGUI Balance;

	public TextMeshProUGUI Line5;

	public TextMeshProUGUI Power;

	public TextMeshProUGUI LCpart;

	public TextMeshProUGUI MCpart;

	public TextMeshProUGUI UCpart;

	public TextMeshProUGUI NeiJobs;

	public TextMeshProUGUI TradeCap;

	public GameObject buyLand;

	public GameObject openCity;

	public GameObject cityBox;

	public GameObject landBox;

	public GameObject panel;

	public RegionInteraction ri;

	public RegionUI regionUI;

	public WorldRegion region;

	public PlaylistRegion playlist;

	public Slider loadingBar;

	public TextMeshProUGUI LoadingTask;

	public TMP_InputField inputName;

	public GameObject namingPanel;

	public RTS_Camera camScript;

	public SoundManager sm;

	public int toTransfer = 5000;

	private string cityName = "";

	private readonly int[] amounts = new int[14]
	{
		-50000, -20000, -10000, -5000, -2000, -1000, 1000, 2000, 5000, 10000,
		20000, 50000, 100000, 200000
	};

	private int amPos = 6;

	private void Awake()
	{
		if (DataRegion.Treasury < 5000)
		{
			amPos = 6;
		}
		else if (DataRegion.Treasury < 20000)
		{
			amPos = 8;
		}
		else if (DataRegion.Treasury < 100000)
		{
			amPos = 10;
		}
		else if (DataRegion.Treasury < 1000000)
		{
			amPos = 12;
		}
		else
		{
			amPos = 13;
		}
		toTransfer = amounts[amPos];
	}

	public void UpdateTexts()
	{
		regionUI.treasury.text = "$" + DataRegion.Treasury.ToString("N0");
		if (DataRegion.selectedCity.owned)
		{
			openCity.SetActive(value: true);
			buyLand.SetActive(value: false);
			cityBox.SetActive(value: true);
			landBox.SetActive(value: false);
			Title.text = DataRegion.selectedCity.cityName;
			Line1.text = DataRegion.selectedCity.population.ToString("N0");
			Unemp.text = DataRegion.selectedCity.unemployment.ToString("F0") + "%";
			GdpCap.text = " $" + (DataRegion.selectedCity.GDPcap * 1000).ToString("N0");
			LCpart.text = DataRegion.selectedCity.LCpart.ToString("F0") + "%";
			MCpart.text = DataRegion.selectedCity.MCpart.ToString("F0") + "%";
			UCpart.text = DataRegion.selectedCity.UCpart.ToString("F0") + "%";
			Power.text = (DataRegion.selectedCity.powerCapacity - DataRegion.selectedCity.powerUse).ToString("F0") + "MWh";
			Balance.text = LOCSYS.Translate("Balance") + " : $" + DataRegion.selectedCity.balance.ToString("N0");
			Line5.text = "$" + toTransfer.ToString("N0");
			cityName = DataRegion.selectedCity.cityName;
			NeiJobs.text = DataRegion.selectedCity.jobsShared.ToString("F0");
			TradeCap.text = (DataRegion.selectedCity.TradeUse / 100).ToString("F0") + "k / " + (DataRegion.selectedCity.TradeCapacity / 100).ToString("F0") + "k";
		}
		else
		{
			openCity.SetActive(value: false);
			buyLand.SetActive(value: true);
			cityBox.SetActive(value: false);
			landBox.SetActive(value: true);
			Title.text = LOCSYS.Translate("LAND FOR SALE");
			Line1.text = LOCSYS.Translate("Price") + " : $" + DataRegion.LandPrice.ToString("N0");
			Unemp.text = "";
			GdpCap.text = "";
			LCpart.text = "";
			MCpart.text = "";
			UCpart.text = "";
			Power.text = "";
			cityName = "";
			NeiJobs.text = "";
			TradeCap.text = "";
		}
	}

	public void NameCity()
	{
		namingPanel.SetActive(value: true);
		camScript.BlockCamera();
	}

	public void AcceptName()
	{
		if (inputName.text.Length < 2 || inputName.text.EndsWith(" "))
		{
			Randomize();
			Debug.Log("Incorrect Name");
		}
		DataRegion.selectedCity.cityName = inputName.text;
		PurchaseLand();
		camScript.UnblockCamera();
		ri.censusOpen = false;
	}

	private void PurchaseLand()
	{
		namingPanel.SetActive(value: false);
		DataCity selectedCity = DataRegion.selectedCity;
		if (DataRegion.LandPrice > DataRegion.Treasury)
		{
			sm.Play("Impractical");
			return;
		}
		sm.Play("Purchase");
		if (DataRegion.capital == null)
		{
			DataRegion.capital = selectedCity;
			selectedCity.isCapital = true;
		}
		else
		{
			Debug.Log("Existing Capital is= " + DataRegion.capital.cityName);
		}
		selectedCity.owned = true;
		DataRegion.Treasury -= DataRegion.LandPrice;
		if (DataRegion.ecoSettings[0] == 0f)
		{
			DataRegion.selectedCity.balance = 100000;
		}
		else if (DataRegion.ecoSettings[0] == 1f)
		{
			DataRegion.selectedCity.balance = 50000;
		}
		else
		{
			DataRegion.selectedCity.balance = 20000;
		}
		UpdateTexts();
		DataRegion.LandPrice *= 2;
		if (DataRegion.LandPrice > 500000)
		{
			DataRegion.LandPrice = 500000;
		}
		Vector3 pos = new Vector3(selectedCity.rX * WorldRegion.chunkSize + WorldRegion.chunkSize / 2, 50f, selectedCity.rZ * WorldRegion.chunkSize + WorldRegion.chunkSize / 2);
		region.regionUI.CreateText(selectedCity.population, selectedCity.cityName, pos, selectedCity.isCapital);
	}

	public void OpenCity()
	{
		int num = (DataRegion.tip = Random.Range(0, Dico.Tips.Length));
		region.loadingLoc.gameObject.SetActive(value: true);
		region.loadingLoc.key = Dico.Tips[num];
		StartCoroutine(playlist.FadeOutVol());
		StartCoroutine(region.FadeInScreen());
		loadingBar.value = 5f;
		LoadingTask.text = LOCSYS.Translate("Load2") + " " + cityName;
		StartCoroutine(LoadingCity());
	}

	private IEnumerator LoadingCity()
	{
		yield return new WaitForSecondsRealtime(0.5f);
		loadingBar.value = 10f;
		DataRegion.SaveDataRegion();
		yield return new WaitForSecondsRealtime(0.1f);
		Application.backgroundLoadingPriority = ThreadPriority.High;
		loadingBar.value = 20f;
		region.asyncLoad.allowSceneActivation = true;
	}

	public void MoreFund()
	{
		amPos++;
		if (amPos >= amounts.Length)
		{
			amPos = amounts.Length - 1;
		}
		if (amounts[amPos] <= DataRegion.Treasury)
		{
			toTransfer = amounts[amPos];
		}
		else
		{
			sm.Play("Impractical");
			toTransfer = DataRegion.Treasury;
			amPos = 6;
		}
		UpdateTexts();
	}

	public void LessFund()
	{
		amPos--;
		if (amPos < 0)
		{
			amPos = 0;
		}
		toTransfer = amounts[amPos];
		UpdateTexts();
	}

	public void SendFund()
	{
		if (toTransfer > DataRegion.Treasury)
		{
			sm.Play("Impractical");
			return;
		}
		if (toTransfer < 0 && -toTransfer > DataRegion.selectedCity.balance)
		{
			sm.Play("Impractical");
			return;
		}
		DataRegion.selectedCity.balance += toTransfer;
		DataRegion.Treasury -= toTransfer;
		DataRegion.Treasury = Mathf.Clamp(DataRegion.Treasury, 0, 1000000000);
		UpdateTexts();
		sm.Play("Money");
	}

	public void Randomize()
	{
		int index = Random.Range(0, Dico.cityName.Count);
		inputName.text = Dico.cityName[index];
	}

	public void CloseTool()
	{
		panel.gameObject.SetActive(value: false);
		ri.censusOpen = false;
		namingPanel.SetActive(value: false);
		camScript.UnblockCamera();
	}
}
