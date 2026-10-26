using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Messages : MonoBehaviour
{
	public GameObject PopUpsGO;

	public GameObject arrow;

	public MessageBox TutoBox;

	public MessageBox RedBox;

	public GameObject RiotBox;

	public MessageBox BlueBox;

	public MessageBox PurpleBox;

	public SoundManager sm;

	public Rioting rioting;

	public TextMeshProUGUI RiotLine1;

	public TextMeshProUGUI RiotLine2;

	public Button Option2;

	public Button Option3;

	public Economy eco;

	public Budget budget;

	public TrafficManager trafficManager;

	public RTS_Camera cam;

	public Indexer indexer;

	public static Messages messages;

	public static int step;

	public static int counter;

	public static Vector3 arrowPos;

	private int lastAlert;

	private int lastInfo;

	public static bool purpleON;

	public GameObject sOUT;

	public GameObject s10;

	public GameObject s11;

	public GameObject s2;

	public GameObject s3;

	public GameObject s20;

	public GameObject s21;

	public GameObject s30;

	public GameObject s31;

	public GameObject s32;

	public GameObject s40;

	public GameObject s41;

	public GameObject s50;

	public GameObject s51;

	public GameObject s60;

	public GameObject s61;

	public GameObject s80;

	public GameObject s81;

	public GameObject s100;

	public GameObject s120;

	public GameObject s129;

	public GameObject s131;

	public GameObject s140;

	public GameObject s141;

	public GameObject s161;

	public GameObject s191;

	public GameObject s200;

	public GameObject s201;

	public GameObject s210;

	public GameObject s220;

	public GameObject s230;

	public GameObject s231;

	public GameObject s290;

	public GameObject LayerGO;

	public bool[] achievements = new bool[30];

	private int count;

	private int tries;

	private void Start()
	{
		messages = this;
		if (DataRegion.Population < 2 && SettingsStatic.tutorial && DataRegion.selectedCity.isCapital)
		{
			TutoBox.gameObject.SetActive(value: true);
			arrow.transform.position = sOUT.transform.position;
			step = -1;
			arrow.SetActive(value: true);
			StartCoroutine(CoroutineTutorial());
		}
		else if (step > 0 && SettingsStatic.tutorial && DataRegion.selectedCity.isCapital)
		{
			TutoBox.gameObject.SetActive(value: true);
			StartCoroutine(CoroutineTutorial());
		}
		StartCoroutine(CheckForMessages());
	}

	private void Update()
	{
		if (SettingsStatic.tutorial)
		{
			arrow.transform.position = Vector3.Lerp(arrow.transform.position, arrowPos, 0.1f);
		}
	}

	private IEnumerator CoroutineTutorial()
	{
		while (SettingsStatic.tutorial)
		{
			if (step < 17 && eco.Population > 2000f)
			{
				eco.LC_Demand = -5f;
			}
			else if (step < 22 && eco.Population > 4000f)
			{
				eco.LC_Demand = -5f;
			}
			UpdateTutorial(step);
			yield return new WaitForSecondsRealtime(0.5f);
		}
	}

	private IEnumerator CheckForMessages()
	{
		while (true)
		{
			if (World.dataReady)
			{
				if ((Budget.CashFlow < 0f && budget.Balance < Mathf.Abs(Budget.CashFlow * 6f) && !eco.isBankrupted) || (budget.Balance < 0f && !eco.isBankrupted))
				{
					NewAlertMessage(4);
				}
				else if (eco.PowerUse * 100 / (eco.PowerCapacity + 1) > 99 && eco.Population > 200f)
				{
					NewAlertMessage(1);
				}
				else if (eco.WaterUse * 100 / (eco.WaterCapacity + 1) > 99 && eco.Population > 200f)
				{
					NewAlertMessage(2);
				}
				else if (eco.TotalPupils * 100 / (eco.SchoolCapacity + 1) > 150 && eco.TotalPupils * 100 / (eco.SchoolCapacity + 1) < 200)
				{
					NewAlertMessage(3);
				}
				else if (eco.Congestion > 75f && eco.Population < 100000f)
				{
					NewAlertMessage(5);
				}
				else if (eco.LC_Demand > 1750f && eco.Slum_Demand > 100f && eco.InformalPop > 250f && eco.Population < 150000f && budget.LC_Tax < 17f)
				{
					NewAlertMessage(6);
				}
				else if (eco.LC_Demand > 2000f && eco.Slum_Demand > 100f && eco.InformalPop > 250f && eco.Population < 175000f && budget.LC_Tax > 18f)
				{
					NewAlertMessage(9);
				}
				else if (eco.Unemployment < 0f && eco.Population > 5000f)
				{
					NewAlertMessage(11);
				}
				if (!achievements[0] && eco.Population >= 10000f)
				{
					SteamAchievements.script.UnlockAchievement("Pop10k");
					achievements[0] = true;
				}
				else if (!achievements[1] && eco.Population >= 50000f)
				{
					SteamAchievements.script.UnlockAchievement("Pop50k");
					achievements[1] = true;
				}
				else if (!achievements[2] && eco.Population >= 100000f)
				{
					SteamAchievements.script.UnlockAchievement("Pop100k");
					achievements[2] = true;
				}
				else if (!achievements[3] && eco.Population >= 250000f)
				{
					SteamAchievements.script.UnlockAchievement("Pop250k");
					achievements[3] = true;
				}
				else if (!achievements[4] && eco.Population >= 500000f)
				{
					SteamAchievements.script.UnlockAchievement("Pop500k");
					achievements[4] = true;
				}
				else if (!achievements[5] && eco.NationPop >= 1000000f)
				{
					SteamAchievements.script.UnlockAchievement("Pop1000k");
					achievements[5] = true;
				}
				if (!achievements[7] && budget.Balance >= 1000000f)
				{
					SteamAchievements.script.UnlockAchievement("swiss");
					achievements[7] = true;
				}
				if (!achievements[9] && eco.Unemployment < 1f && eco.Population > 10000f)
				{
					SteamAchievements.script.UnlockAchievement("hiring");
					achievements[9] = true;
				}
				if (!achievements[10] && eco.hightechJobs > 100000f)
				{
					SteamAchievements.script.UnlockAchievement("silicon");
					achievements[10] = true;
				}
				if (!achievements[13] && eco.ApprovalRate < 20 && eco.Population > 10000f)
				{
					SteamAchievements.script.UnlockAchievement("unpop");
					achievements[13] = true;
				}
				if (!achievements[14] && eco.ApprovalRate > 80 && eco.Population > 10000f)
				{
					SteamAchievements.script.UnlockAchievement("popu");
					achievements[14] = true;
				}
				if (!achievements[15] && eco.GlobalLeaning < 20f && eco.Population > 10000f)
				{
					SteamAchievements.script.UnlockAchievement("cali");
					achievements[15] = true;
				}
				if (!achievements[16] && eco.GlobalLeaning > 80f && eco.Population > 10000f)
				{
					SteamAchievements.script.UnlockAchievement("notcali");
					achievements[16] = true;
				}
				if (!achievements[17] && eco.BirthRate > 6f && eco.Population > 10000f)
				{
					SteamAchievements.script.UnlockAchievement("pregnant");
					achievements[17] = true;
				}
				if (!achievements[18] && eco.enactedPolicies > 49)
				{
					SteamAchievements.script.UnlockAchievement("policy");
					achievements[18] = true;
				}
				if (!achievements[21] && eco.Environment > 85f && eco.Population > 200000f)
				{
					SteamAchievements.script.UnlockAchievement("eco");
					achievements[21] = true;
				}
				if (!achievements[22] && eco.EducationLevel > 99f)
				{
					SteamAchievements.script.UnlockAchievement("edu");
					achievements[22] = true;
				}
				if (!achievements[24] && DataRegion.cityCount > 9)
				{
					SteamAchievements.script.UnlockAchievement("nation");
					achievements[24] = true;
				}
				if (!achievements[27] && budget.UC_Tax < budget.LC_Tax - 5f && eco.Population > 20000f)
				{
					SteamAchievements.script.UnlockAchievement("capital");
					achievements[27] = true;
				}
				if (!achievements[28] && CheckIfAllUnlock())
				{
					SteamAchievements.script.UnlockAchievement("future");
					achievements[28] = true;
				}
				if (!achievements[29] && (float)eco.GDP / eco.Population > 9999f && eco.Population > 10000f)
				{
					SteamAchievements.script.UnlockAchievement("singapore");
					achievements[29] = true;
				}
			}
			float num = Mathf.Clamp(Time.timeScale, 1f, 6f);
			yield return new WaitForSeconds(3f * num);
			count++;
			if (count >= 24)
			{
				CloseAlertMessage();
				count = 0;
			}
		}
	}

	private bool CheckIfAllUnlock()
	{
		int num = 0;
		for (int i = 0; i < 25; i++)
		{
			if (DataRegion.alreadyBuilt[i])
			{
				num++;
			}
		}
		if (num >= 18)
		{
			return true;
		}
		return false;
	}

	public void NewAlertMessage(int LOCSYNref)
	{
		if (LOCSYNref == 8)
		{
			eco.Stability -= 8f;
			eco.LC_Demand -= 100f;
			eco.MC_Demand -= 100f;
			eco.UC_Demand -= 100f;
		}
		if (!SettingsStatic.redAlerts || !World.dataReady)
		{
			return;
		}
		if (Time.timeScale == 0f || (step > 1 && step < 22) || LOCSYNref == lastAlert)
		{
			tries++;
			if (tries < 24)
			{
				return;
			}
			tries = 0;
		}
		RedBox.gameObject.SetActive(value: true);
		lastAlert = LOCSYNref;
		string key = "AT" + LOCSYNref;
		string key2 = "AM" + LOCSYNref;
		RedBox.PopTitle.text = LOCSYS.Translate(key);
		RedBox.PopText.text = LOCSYS.Translate(key2);
		sm.Play("Alert");
	}

	public void NewRiotMessage(int rioters, string LOCpol, int LOCreason, float violence)
	{
		RiotBox.SetActive(value: true);
		string text;
		if (violence == 0.5f)
		{
			text = LOCSYS.Translate("AY3");
		}
		else
		{
			text = ((violence != 0.1f) ? LOCSYS.Translate("AY1") : LOCSYS.Translate("AY2"));
		}
		RiotLine1.text = rioters + " " + LOCSYS.Translate(LOCpol) + " " + LOCSYS.Translate("AYmain");
		RiotLine2.text = LOCSYS.Translate("AX" + LOCreason) + " " + text;
		sm.Play("Alert");
	}

	public void NewInfoMessage(int LOCSYNref)
	{
		if (lastInfo != LOCSYNref && (step <= 1 || step >= 30) && !BlueBox.gameObject.activeSelf)
		{
			lastInfo = LOCSYNref;
			BlueBox.gameObject.SetActive(value: true);
			string key = "NT" + LOCSYNref;
			string key2 = "NM" + LOCSYNref;
			BlueBox.PopTitle.text = LOCSYS.Translate(key);
			BlueBox.PopText.text = LOCSYS.Translate(key2);
			sm.Play("NewMessage");
		}
	}

	private void CloseAlertMessage()
	{
		if (RedBox.gameObject.activeInHierarchy)
		{
			sm.Play("Close");
		}
		RedBox.gameObject.SetActive(value: false);
	}

	public void RiotOption1()
	{
		sm.Play("Close");
		rioting.response = 0;
		RiotBox.SetActive(value: false);
		eco.riotCivilRights++;
		eco.riotSecurity -= 5f;
		TooltipSystem.Hide();
	}

	public void RiotOption2()
	{
		sm.Play("Option3");
		rioting.response = 1;
		rioting.riotDuration -= 50;
		rioting.Violence -= 0.5f;
		RiotBox.SetActive(value: false);
		eco.riotCivilRights--;
		eco.riotSecurity++;
		budget.Balance -= Rioting.Rioters * 5;
		budget.UI_Balance.text = "$" + budget.Balance.ToString("N0");
		TooltipSystem.Hide();
	}

	public void RiotOption3()
	{
		sm.Play("Option4");
		rioting.response = 2;
		rioting.riotDuration -= 100;
		rioting.Violence -= 0.9f;
		RiotBox.SetActive(value: false);
		eco.riotCivilRights -= 5f;
		eco.riotSecurity += 5f;
		budget.Balance -= Rioting.Rioters * 20;
		budget.UI_Balance.text = "$" + budget.Balance.ToString("N0");
		TooltipSystem.Hide();
	}

	public void SeeRiot()
	{
		if (Rioting.riotPosition != Vector3.zero)
		{
			cam.LookAt = Rioting.riotPosition;
		}
		else
		{
			sm.Play("Impractical");
		}
	}

	private void UpdateTutorial(int s)
	{
		if (s == -1)
		{
			TutoBox.gameObject.SetActive(value: true);
		}
		else if (s == 1 && counter >= 150)
		{
			NextStep();
		}
		else if (s == 3 && counter >= 25)
		{
			NextStep();
		}
		else if (s == 4 && counter >= 2)
		{
			NextStep();
			indexer.gamespeed.Speed_1();
		}
		else if (s == 5 && counter >= 6)
		{
			NextStep();
		}
		else if (s == 6 && budget.R_Tax < 4f)
		{
			NextStep();
		}
		else if (s == 7 && counter >= 6)
		{
			NextStep();
		}
		else if (s == 8 && eco.Population >= 755f)
		{
			NextStep();
		}
		else if (s == 9 && counter >= 1)
		{
			NextStep();
		}
		else if (s == 12 && counter >= 3)
		{
			NextStep();
		}
		else if (s == 15 && counter >= 1)
		{
			NextStep();
		}
		else if (s == 16 && counter >= 1)
		{
			NextStep();
		}
		else if (s == 17 && eco.Population >= 2000f)
		{
			NextStep();
		}
		else if (s == 18 && counter >= 1)
		{
			NextStep();
		}
		else if (s == 19 && eco.immi.LA_SliderUI.value >= 8f)
		{
			NextStep();
		}
		else if (s == 20 && counter >= 1)
		{
			NextStep();
		}
		else if (s == 21 && counter >= 1)
		{
			NextStep();
		}
		else if (s == 22 && eco.Population >= 4000f)
		{
			if (budget.Balance < 10000f)
			{
				budget.Balance = 10000f;
				budget.UI_Balance.text = "$" + budget.Balance.ToString("N0");
			}
			NextStep();
		}
		else if (s == 28 && counter >= 1)
		{
			NextStep();
		}
		else if (s == 32)
		{
			TutoBox.gameObject.SetActive(value: false);
			SettingsStatic.tutorial = false;
			step = -1;
			arrow.SetActive(value: false);
			if (!achievements[6] && Budget.Expenses < Budget.Income)
			{
				SteamAchievements.script.UnlockAchievement("grad");
				achievements[6] = true;
			}
		}
		TutoBox.PopTitle.text = LOCSYS.Translate("TT" + step);
		TutoBox.PopText.text = LOCSYS.Translate("TM" + step);
		TutoBox.PopGoal.text = LOCSYS.Translate("TG" + step);
	}

	private void NextStep()
	{
		indexer.HideAll();
		counter = 0;
		step++;
		SetArrowStep(step);
		UpdateTutorial(step);
		sm.Play("ClickButton");
	}

	public void ClickContinue()
	{
		if (step == 17 || step == 22 || step == 8)
		{
			sm.Play("Impractical");
			return;
		}
		arrow.SetActive(value: true);
		sm.Play("ClickButtonB");
		NextStep();
	}

	public void CloseMessage(GameObject box)
	{
		box.SetActive(value: false);
		sm.Play("Close");
	}

	public void SetArrowStep(float s)
	{
		if (!SettingsStatic.tutorial)
		{
			return;
		}
		if (step == 0)
		{
			arrowPos = sOUT.transform.position;
		}
		else if (step == 1)
		{
			if (s == 1f)
			{
				arrowPos = s10.transform.position;
			}
			else if (s == 1.1f)
			{
				arrowPos = s11.transform.position;
			}
			else if (s == 1.2f)
			{
				arrowPos = sOUT.transform.position;
			}
		}
		else if (step == 2)
		{
			if (s == 2f)
			{
				arrowPos = s2.transform.position;
				arrow.transform.rotation = new Quaternion(0f, 0f, 180f, 0f);
			}
			else if (s == 2.1f)
			{
				arrowPos = s3.transform.position;
			}
		}
		else if (step == 3)
		{
			if (s == 3f)
			{
				arrowPos = s20.transform.position;
				arrow.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
			}
			else if (s == 3.1f)
			{
				arrowPos = s21.transform.position;
			}
			else if (s == 3.2f)
			{
				arrowPos = sOUT.transform.position;
			}
		}
		else if (step == 4)
		{
			if (s == 4f)
			{
				arrowPos = s30.transform.position;
			}
			else if (s == 4.1f)
			{
				arrowPos = s31.transform.position;
			}
			else if (s == 4.2f)
			{
				arrowPos = sOUT.transform.position;
			}
			else if (s == 4.3f)
			{
				arrowPos = s32.transform.position;
			}
			else if (s == 4.4f)
			{
				arrowPos = sOUT.transform.position;
			}
		}
		else if (step == 5)
		{
			if (s == 5f)
			{
				arrowPos = s40.transform.position;
			}
			else if (s == 5.1f)
			{
				arrowPos = s41.transform.position;
			}
			else if (s == 5.2f)
			{
				arrowPos = sOUT.transform.position;
			}
		}
		else if (step == 6)
		{
			if (s == 6f)
			{
				arrowPos = s50.transform.position;
				arrow.transform.rotation = new Quaternion(0f, 0f, 180f, 0f);
			}
			else if (s == 6.1f)
			{
				arrowPos = s51.transform.position;
				arrow.transform.rotation = new Quaternion(0f, 0f, 225f, 0f);
				eco.Stability = 100f;
			}
			else if (s == 6.2f)
			{
				arrowPos = sOUT.transform.position;
			}
		}
		else if (step == 7)
		{
			if (s == 7f)
			{
				arrowPos = s60.transform.position;
				arrow.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
			}
			else if (s == 7.1f)
			{
				arrowPos = s61.transform.position;
			}
			else if (s == 7.2f)
			{
				arrowPos = sOUT.transform.position;
			}
		}
		else if (step == 8)
		{
			arrowPos = sOUT.transform.position;
		}
		else if (step == 9)
		{
			if (s == 9f)
			{
				arrowPos = s80.transform.position;
			}
			else if (s == 9.1f)
			{
				arrowPos = s81.transform.position;
			}
			else if (s == 9.2f)
			{
				arrowPos = sOUT.transform.position;
			}
		}
		else if (step == 11)
		{
			if (s == 11f)
			{
				arrowPos = s100.transform.position;
				arrow.transform.rotation = new Quaternion(0f, 0f, 180f, 0f);
			}
			else if (s == 11.1f)
			{
				arrowPos = sOUT.transform.position;
				NextStep();
			}
		}
		else if (step == 13)
		{
			if (s == 13f)
			{
				arrowPos = s120.transform.position;
				arrow.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
			}
			else if (s == 13.1f)
			{
				arrowPos = sOUT.transform.position;
				NextStep();
			}
		}
		else if (step == 14)
		{
			if (s == 14f)
			{
				if (!LayerGO.activeSelf)
				{
					arrowPos = s140.transform.position;
					arrow.transform.rotation = new Quaternion(0f, 0f, 180f, 0f);
				}
				else
				{
					arrowPos = sOUT.transform.position;
				}
			}
			if (s == 14.1f)
			{
				arrowPos = sOUT.transform.position;
			}
		}
		else if (step == 15)
		{
			if (s == 15f)
			{
				arrowPos = s129.transform.position;
				arrow.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
			}
			else if (s == 15.1f)
			{
				arrowPos = s131.transform.position;
			}
			else if (s == 15.2f)
			{
				arrowPos = sOUT.transform.position;
			}
		}
		else if (step == 18)
		{
			if (s == 18f)
			{
				arrowPos = s80.transform.position;
			}
			else if (s == 9.1f)
			{
				arrowPos = s191.transform.position;
			}
			else if (s == 18.2f)
			{
				arrowPos = sOUT.transform.position;
			}
		}
		else if (step == 19)
		{
			if (s == 19f)
			{
				arrowPos = s200.transform.position;
				arrow.transform.rotation = new Quaternion(0f, 0f, 180f, 0f);
			}
			else if (s == 19.1f)
			{
				arrowPos = s201.transform.position;
				arrow.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
			}
			else if (s == 19.2f)
			{
				arrowPos = sOUT.transform.position;
			}
		}
		else if (step == 20)
		{
			if (s == 20f)
			{
				arrowPos = s210.transform.position;
				arrow.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
			}
			else if (s == 20.1f)
			{
				arrowPos = sOUT.transform.position;
			}
		}
		else if (step == 22)
		{
			if (s == 22f)
			{
				arrowPos = s220.transform.position;
			}
			else if (s == 22.1f)
			{
				arrowPos = sOUT.transform.position;
			}
		}
		else if (step == 23)
		{
			if (s == 23f)
			{
				arrowPos = s230.transform.position;
				arrow.transform.rotation = new Quaternion(0f, 0f, 180f, 0f);
			}
			else if (s == 23.1f)
			{
				arrowPos = s231.transform.position;
			}
			else if (s == 23.2f)
			{
				arrowPos = sOUT.transform.position;
				NextStep();
			}
		}
		else if (step == 28)
		{
			if (s == 28f)
			{
				arrowPos = s10.transform.position;
				arrow.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
			}
			else if (s == 1.1f)
			{
				arrowPos = s161.transform.position;
			}
			else if (s == 28.2f)
			{
				arrowPos = sOUT.transform.position;
			}
		}
		else if (step == 29)
		{
			if (s == 29f)
			{
				arrowPos = s290.transform.position;
				arrow.transform.rotation = new Quaternion(0f, 0f, 180f, 0f);
			}
			else if (s == 29.1f)
			{
				arrowPos = sOUT.transform.position;
			}
		}
	}

	public void SetHierarchy(bool enter)
	{
		if (enter)
		{
			PopUpsGO.transform.SetSiblingIndex(18);
		}
		else
		{
			PopUpsGO.transform.SetSiblingIndex(6);
		}
	}

	public void AddCounter()
	{
		counter++;
	}

	public void CheckForEvents()
	{
		if (eco.Population > 40000f && eco.w.CommercialBuildings.Count > 1 && (eco.LCPartPercent - eco.fix + (eco.Unemployment - 5f) + (float)((40 - eco.SecurityScore) * 2) - eco.fix) / 3f + (float)Random.Range(-100, 10) > 50f)
		{
			NewAlertMessage(10);
			LotCreator lotCreator = eco.w.CommercialBuildings[Random.Range(0, eco.w.CommercialBuildings.Count)];
			if (lotCreator.isDecayed || lotCreator.Entry == Vector2Int.zero || lotCreator.Stage == 0)
			{
				return;
			}
			lotCreator.decayReason = "VictimOf";
			lotCreator.DecayLot(toParking: true);
			lotCreator.negatives = 20f;
			eco.Stability -= 2f;
			((GameObject)Object.Instantiate(Resources.Load("Vehicles/HelicoCrime"))).transform.position = new Vector3(lotCreator.Entry.x, lotCreator.Anchor.y + 2f, lotCreator.Entry.y);
		}
		if (!(eco.Population > 5000f) || eco.w.VacantResLots.Count <= 0 || !(((float)(100 - eco.SecurityScore * 2) - eco.fix) / 2f + (float)Random.Range(-150, 0) + (float)eco.w.VacantResLots.Count > 50f))
		{
			return;
		}
		LotCreator lotCreator2 = eco.w.VacantResLots[Random.Range(0, eco.w.VacantResLots.Count)];
		if (!lotCreator2.isDecayed || lotCreator2.isRubles || lotCreator2.Stage == 0)
		{
			return;
		}
		lotCreator2.negatives = 20f;
		eco.Stability--;
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Construction/Fire"));
		gameObject.transform.position = new Vector3(lotCreator2.Anchor.x, lotCreator2.wP.y + 0.504f, lotCreator2.Anchor.z);
		FireCoroutine component = gameObject.GetComponent<FireCoroutine>();
		component.lotOnFire = lotCreator2;
		foreach (ServiceCreator service in eco.w.Services)
		{
			if (service.type == ServiceCreator.Type.Fire && service.carsOut < service.Stage)
			{
				trafficManager.SendSpecialVehicleOnPath(service, lotCreator2, "Vehicles/FireEngine", component);
				break;
			}
		}
	}
}
