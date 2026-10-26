using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class Stats : MonoBehaviour
{
	public Economy eco;

	public Calendar calendar;

	public UILineRenderer lineRenderer;

	public Color redCity;

	public Color greenCity;

	public GameObject statsPanel;

	public GameObject Data;

	public GameObject Graph;

	public GameObject EcoTab;

	public GameObject PolTab;

	public GameObject Zero;

	public Button B_Data;

	public Button B_Graph;

	public Button B_Eco;

	public Button B_Pol;

	public Sprite Tab_Active;

	public Sprite Tab_Inactive;

	public Button B_weekly;

	public Button B_monthly;

	public Button B_yearly;

	public RectTransform[] pts = new RectTransform[26];

	private int timeFrame;

	private int dataN;

	private bool typePercentage;

	public GameObject displayed;

	public GameObject selectedTime;

	public float max = 10f;

	public float min;

	public float interval = 1f;

	public Slider LCpercent;

	public Slider MCpercent;

	public Slider UCpercent;

	public Slider Immigres;

	public Slider Undocumented;

	public Slider Locals;

	public Slider Conservatives;

	public Slider Liberals;

	public Slider Religious;

	public Slider NotReligious;

	public TextMeshProUGUI S_Tax;

	public TextMeshProUGUI S_FI;

	public TextMeshProUGUI S_CR;

	public TextMeshProUGUI S_Housing;

	public TextMeshProUGUI SlumDemand;

	public TextMeshProUGUI LC_SocialLadder;

	public TextMeshProUGUI LC_BirthRate;

	public TextMeshProUGUI LC_Immi;

	public TextMeshProUGUI LC_Neib;

	public TextMeshProUGUI LC_Total;

	public TextMeshProUGUI MC_Policies;

	public TextMeshProUGUI MC_Unempl;

	public TextMeshProUGUI MC_Edu;

	public TextMeshProUGUI MC_Inflation;

	public TextMeshProUGUI MC_Taxes;

	public TextMeshProUGUI MC_FromLCtoMC;

	public TextMeshProUGUI MC_SocialLadder;

	public TextMeshProUGUI MC_BirthRate;

	public TextMeshProUGUI MC_Immi;

	public TextMeshProUGUI MC_Neib;

	public TextMeshProUGUI MC_Total;

	public TextMeshProUGUI UC_Policies;

	public TextMeshProUGUI UC_Unempl;

	public TextMeshProUGUI UC_Corruption;

	public TextMeshProUGUI UC_Freedom;

	public TextMeshProUGUI UC_Taxes;

	public TextMeshProUGUI UC_FromMCtoUC;

	public TextMeshProUGUI UC_SocialLadder;

	public TextMeshProUGUI UC_BirthRate;

	public TextMeshProUGUI UC_Immi;

	public TextMeshProUGUI UC_Neib;

	public TextMeshProUGUI UC_Total;

	public TextMeshProUGUI J_WorkingPercent;

	public TextMeshProUGUI E_GDPCapita;

	public TextMeshProUGUI E_GDPGrowth;

	public TextMeshProUGUI I_Dollars;

	public TextMeshProUGUI I_GDP;

	public TextMeshProUGUI I_Printed;

	public TextMeshProUGUI ExchangeRate;

	public TextMeshProUGUI I_ExchangeRate;

	public TextMeshProUGUI I_Imports;

	public TextMeshProUGUI I_Ports;

	public TextMeshProUGUI TradeBalance;

	public TextMeshProUGUI I_TradeBalance;

	public TextMeshProUGUI I_Unemployment;

	public TextMeshProUGUI I_Housing;

	public TextMeshProUGUI Inflation;

	public Slider byWalk;

	public Slider byCar;

	public Slider byBus;

	public Slider byTrain;

	public Slider toNeighbor;

	public Slider byAirTaxi;

	public TextMeshProUGUI C_Congestion;

	public TextMeshProUGUI M_Corruption;

	public TextMeshProUGUI M_BirthRate;

	public TextMeshProUGUI M_Inequalities;

	public TextMeshProUGUI M_Incarceration;

	public TextMeshProUGUI Q_Parks;

	public TextMeshProUGUI Q_Security;

	public TextMeshProUGUI Q_Health;

	public TextMeshProUGUI Q_Environment;

	public TextMeshProUGUI Q_Total;

	public Button B_TotalPop;

	public Button B_LC;

	public Button B_MC;

	public Button B_UC;

	public Button B_Dwellers;

	public Button B_Unempl;

	public Button B_PopGrowth;

	public Button B_Inequal;

	public Button B_Approval;

	public Button B_GDPgrowth;

	public Button B_GDPnominal;

	public Button B_FI;

	public Button B_CR;

	public Button B_Edu;

	public Button B_Health;

	public Button B_Secu;

	public Button B_Reli;

	public Button B_Birth;

	public Button B_Corru;

	public TextMeshProUGUI twelveAgo;

	public TextMeshProUGUI twentyfourAgo;

	public TextMeshProUGUI textMax;

	public TextMeshProUGUI textMin;

	public TextMeshProUGUI textMiddle;

	public TextMeshProUGUI T_Retail;

	public TextMeshProUGUI P_Retail;

	public TextMeshProUGUI Tot_Retail;

	public TextMeshProUGUI FI_Office;

	public TextMeshProUGUI E_Office;

	public TextMeshProUGUI T_Office;

	public TextMeshProUGUI P_Office;

	public TextMeshProUGUI Tot_Office;

	public TextMeshProUGUI I_Man;

	public TextMeshProUGUI E_Man;

	public TextMeshProUGUI T_Man;

	public TextMeshProUGUI P_Man;

	public TextMeshProUGUI Tot_Man;

	public TextMeshProUGUI I_HT;

	public TextMeshProUGUI E_HT;

	public TextMeshProUGUI T_HT;

	public TextMeshProUGUI P_HT;

	public TextMeshProUGUI Tot_HT;

	public TextMeshProUGUI Arrow_LC;

	public TextMeshProUGUI Arrow_MC;

	public TextMeshProUGUI Arrow_UC;

	public TextMeshProUGUI Arrow_RtoM;

	public TextMeshProUGUI Arrow_MtoO;

	public TextMeshProUGUI Arrow_HTtoO;

	public TextMeshProUGUI Arrow_RtoHT;

	public Slider Rpercent;

	public Slider Opercent;

	public Slider Mpercent;

	public Slider HTpercent;

	public Slider Ppercent;

	public Slider GOVpercent;

	public Slider NeighbPercent;

	public Slider Upercent;

	public TextMeshProUGUI L_Policies;

	public TextMeshProUGUI L_Taxes;

	public TextMeshProUGUI L_Unemployment;

	public TextMeshProUGUI L_Inequality;

	public TextMeshProUGUI L_CR;

	public TextMeshProUGUI L_Enviro;

	public TextMeshProUGUI L_Health;

	public TextMeshProUGUI L_Edu;

	public TextMeshProUGUI R_Policies;

	public TextMeshProUGUI R_Taxes;

	public TextMeshProUGUI R_Unemployment;

	public TextMeshProUGUI R_Secu;

	public TextMeshProUGUI R_Border;

	public TextMeshProUGUI R_Stability;

	public TextMeshProUGUI R_Inflation;

	public TextMeshProUGUI R_FI;

	public TextMeshProUGUI R_Affiliations;

	public TextMeshProUGUI L_Affiliations;

	public TextMeshProUGUI R_Approval;

	public TextMeshProUGUI L_Approval;

	public TextMeshProUGUI R_Riot;

	public TextMeshProUGUI L_Riot;

	public TextMeshProUGUI TotalApproval;

	public GameObject Pointer;

	public GameObject ZeroZero;

	public GameObject FI100;

	public GameObject CR100;

	public Sprite aff100;

	public Sprite aff80;

	public Sprite aff60;

	public Sprite aff50;

	public Sprite aff40;

	public Sprite aff20;

	public Sprite aff0;

	public Image senate;

	public void OpenData()
	{
		Data.SetActive(value: true);
		Graph.SetActive(value: false);
		EcoTab.SetActive(value: false);
		PolTab.SetActive(value: false);
		B_Data.image.sprite = Tab_Active;
		B_Graph.image.sprite = Tab_Inactive;
		B_Eco.image.sprite = Tab_Inactive;
		B_Pol.image.sprite = Tab_Inactive;
	}

	public void OpenStats()
	{
		Data.SetActive(value: false);
		Graph.SetActive(value: true);
		EcoTab.SetActive(value: false);
		PolTab.SetActive(value: false);
		TotalPopClick();
		WeeklyClick();
		B_Data.image.sprite = Tab_Inactive;
		B_Graph.image.sprite = Tab_Active;
		B_Eco.image.sprite = Tab_Inactive;
		B_Pol.image.sprite = Tab_Inactive;
		StartCoroutine(UpdatePopStats());
	}

	public void OpenEco()
	{
		Data.SetActive(value: false);
		Graph.SetActive(value: false);
		EcoTab.SetActive(value: true);
		PolTab.SetActive(value: false);
		B_Data.image.sprite = Tab_Inactive;
		B_Graph.image.sprite = Tab_Inactive;
		B_Eco.image.sprite = Tab_Active;
		B_Pol.image.sprite = Tab_Inactive;
	}

	public void OpenPol()
	{
		Data.SetActive(value: false);
		Graph.SetActive(value: false);
		EcoTab.SetActive(value: false);
		PolTab.SetActive(value: true);
		B_Data.image.sprite = Tab_Inactive;
		B_Graph.image.sprite = Tab_Inactive;
		B_Eco.image.sprite = Tab_Inactive;
		B_Pol.image.sprite = Tab_Active;
		UpdateEtiquette();
		UpdateSenate();
	}

	public void CloseStats()
	{
		StopAllCoroutines();
		statsPanel.SetActive(value: false);
		Indexer.sliderOnScreen = false;
	}

	public void WeeklyClick()
	{
		timeFrame = 0;
		twelveAgo.text = LOCSYS.Translate("12W");
		twentyfourAgo.text = LOCSYS.Translate("24W");
		selectedTime.transform.position = B_weekly.transform.position;
		StartCoroutine(RedrawGraph());
	}

	public void MonthlyClick()
	{
		timeFrame = 1;
		twelveAgo.text = LOCSYS.Translate("12M");
		twentyfourAgo.text = LOCSYS.Translate("24M");
		selectedTime.transform.position = B_monthly.transform.position;
		StartCoroutine(RedrawGraph());
	}

	public void YearlyClick()
	{
		timeFrame = 2;
		twelveAgo.text = LOCSYS.Translate("12Y");
		twentyfourAgo.text = LOCSYS.Translate("24Y");
		selectedTime.transform.position = B_yearly.transform.position;
		StartCoroutine(RedrawGraph());
	}

	public void TotalPopClick()
	{
		displayed.transform.position = B_TotalPop.transform.position;
		dataN = 0;
		typePercentage = false;
		StartCoroutine(RedrawGraph());
	}

	public void LCClick()
	{
		displayed.transform.position = B_LC.transform.position;
		dataN = 1;
		typePercentage = false;
		StartCoroutine(RedrawGraph());
	}

	public void MCClick()
	{
		displayed.transform.position = B_MC.transform.position;
		dataN = 2;
		typePercentage = false;
		StartCoroutine(RedrawGraph());
	}

	public void UCClick()
	{
		displayed.transform.position = B_UC.transform.position;
		dataN = 3;
		typePercentage = false;
		StartCoroutine(RedrawGraph());
	}

	public void DwellersClick()
	{
		displayed.transform.position = B_Dwellers.transform.position;
		dataN = 4;
		typePercentage = false;
		StartCoroutine(RedrawGraph());
	}

	public void PopGrowthClick()
	{
		displayed.transform.position = B_PopGrowth.transform.position;
		dataN = 5;
		typePercentage = false;
		StartCoroutine(RedrawGraph());
	}

	public void UnemploymentClick()
	{
		displayed.transform.position = B_Unempl.transform.position;
		dataN = 6;
		typePercentage = false;
		StartCoroutine(RedrawGraph());
	}

	public void InequalClick()
	{
		displayed.transform.position = B_Inequal.transform.position;
		dataN = 7;
		typePercentage = true;
		StartCoroutine(RedrawGraph());
	}

	public void ApprovalClick()
	{
		displayed.transform.position = B_Approval.transform.position;
		dataN = 8;
		typePercentage = true;
		StartCoroutine(RedrawGraph());
	}

	public void GDPgrowthClick()
	{
		displayed.transform.position = B_GDPgrowth.transform.position;
		dataN = 9;
		typePercentage = false;
		StartCoroutine(RedrawGraph());
	}

	public void GDPnominalClick()
	{
		displayed.transform.position = B_GDPnominal.transform.position;
		dataN = 10;
		typePercentage = false;
		StartCoroutine(RedrawGraph());
	}

	public void FIClick()
	{
		displayed.transform.position = B_FI.transform.position;
		dataN = 11;
		typePercentage = true;
		StartCoroutine(RedrawGraph());
	}

	public void CRClick()
	{
		displayed.transform.position = B_CR.transform.position;
		dataN = 12;
		typePercentage = true;
		StartCoroutine(RedrawGraph());
	}

	public void EduClick()
	{
		displayed.transform.position = B_Edu.transform.position;
		dataN = 13;
		typePercentage = true;
		StartCoroutine(RedrawGraph());
	}

	public void HealthClick()
	{
		displayed.transform.position = B_Health.transform.position;
		dataN = 14;
		typePercentage = true;
		StartCoroutine(RedrawGraph());
	}

	public void SecuClick()
	{
		displayed.transform.position = B_Secu.transform.position;
		dataN = 15;
		typePercentage = true;
		StartCoroutine(RedrawGraph());
	}

	public void ReliClick()
	{
		displayed.transform.position = B_Reli.transform.position;
		dataN = 16;
		typePercentage = true;
		StartCoroutine(RedrawGraph());
	}

	public void BirthClick()
	{
		displayed.transform.position = B_Birth.transform.position;
		dataN = 17;
		typePercentage = false;
		StartCoroutine(RedrawGraph());
	}

	public void CorruClick()
	{
		displayed.transform.position = B_Corru.transform.position;
		dataN = 18;
		typePercentage = true;
		StartCoroutine(RedrawGraph());
	}

	private IEnumerator UpdatePopStats()
	{
		while (Graph.activeInHierarchy)
		{
			StartCoroutine(RedrawGraph());
			LCpercent.value = eco.LCPartPercent;
			MCpercent.value = eco.MCPartPercent;
			UCpercent.value = eco.UCPartPercent;
			yield return new WaitForSecondsRealtime(1f);
		}
	}

	private void UpdateEtiquette()
	{
		float x = ZeroZero.transform.localPosition.x + eco.CivilRights / 100f * (CR100.transform.localPosition.x - ZeroZero.transform.localPosition.x);
		float y = ZeroZero.transform.localPosition.y + eco.FreedomIndex / 100f * (FI100.transform.localPosition.y - ZeroZero.transform.localPosition.y);
		Pointer.transform.localPosition = new Vector3(x, y, 0f);
	}

	private void UpdateSenate()
	{
		float globalLeaning = eco.GlobalLeaning;
		if (globalLeaning > 90f)
		{
			senate.sprite = aff100;
		}
		else if (globalLeaning > 70f)
		{
			senate.sprite = aff80;
		}
		else if (globalLeaning > 55f)
		{
			senate.sprite = aff60;
		}
		else if (globalLeaning > 45f)
		{
			senate.sprite = aff50;
		}
		else if (globalLeaning > 30f)
		{
			senate.sprite = aff40;
		}
		else if (globalLeaning > 10f)
		{
			senate.sprite = aff20;
		}
		else
		{
			senate.sprite = aff0;
		}
	}

	private IEnumerator RedrawGraph()
	{
		if (!typePercentage)
		{
			max = eco.data[0, timeFrame, dataN] + 1;
			min = eco.data[0, timeFrame, dataN];
			for (int i = 0; i <= 25; i++)
			{
				int num = eco.data[i, timeFrame, dataN];
				if ((float)num > max)
				{
					max = num;
				}
				if ((float)num < min)
				{
					min = num;
				}
			}
			textMax.text = max.ToString("N0");
			textMin.text = min.ToString("N0");
			textMiddle.text = ((max + min) / 2f).ToString("N0");
		}
		else
		{
			max = 100f;
			min = 0f;
			textMax.text = "100%";
			textMin.text = "0%";
			textMiddle.text = "50%";
		}
		interval = (max - min) / 333f;
		if (interval < 0.03f)
		{
			interval = 0.03f;
		}
		int num2;
		if (timeFrame != 0)
		{
			num2 = ((timeFrame != 1) ? calendar.tYears : calendar.tMonths);
		}
		else
		{
			num2 = calendar.tWeeks;
		}
		for (int j = 0; j <= 25; j++)
		{
			int num3;
			if (j == 25)
			{
				num3 = num2;
			}
			else
			{
				num3 = num2 - (25 - j);
				if (num3 < 0)
				{
					num3 = 26 + num3;
				}
			}
			float y = ((float)eco.data[num3, timeFrame, dataN] - min) / interval;
			lineRenderer.Points[j] = new Vector2((float)j * 33.3f, y);
		}
		lineRenderer.SetAllDirty();
		yield return new WaitForEndOfFrame();
	}
}
