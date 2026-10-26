using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Immigration : MonoBehaviour
{
	public Economy eco;

	public GameObject immiPanel;

	public Color red;

	public Color green;

	public Color cream;

	public int AirportConnections;

	public TextMeshProUGUI LA_Arrivals;

	public TextMeshProUGUI LA_Airport_Bonus;

	public TextMeshProUGUI LA_PR_Bonus;

	public TextMeshProUGUI LA_CR_Bonus;

	public TextMeshProUGUI LA_FI_Bonus;

	public TextMeshProUGUI LA_QoL_Bonus;

	public TextMeshProUGUI LA_BorderPercent;

	public float LA_Arriv;

	private float LA_PR_Bo;

	private float LA_CR_Bo;

	private float LA_FI_Bo;

	private float LA_QoL_Bo;

	public float LA_Border;

	private readonly float LA_LCpart = 0.92f;

	private readonly float LA_MCpart = 0.07f;

	private readonly float LA_UCpart = 0.01f;

	public float ImmiLeaning = 50f;

	public static int connectionCapacity = 75;

	public TextMeshProUGUI A_Arrivals;

	public TextMeshProUGUI A_Airport_Bonus;

	public TextMeshProUGUI A_PR_Bonus;

	public TextMeshProUGUI A_CR_Bonus;

	public TextMeshProUGUI A_FI_Bonus;

	public TextMeshProUGUI A_QoL_Bonus;

	public TextMeshProUGUI A_BorderPercent;

	public float A_Arriv;

	private float A_PR_Bo;

	private float A_CR_Bo;

	private float A_FI_Bo;

	private float A_QoL_Bo;

	public float A_Border = 1f;

	private readonly float A_LCpart = 0.74f;

	private readonly float A_MCpart = 0.24f;

	private readonly float A_UCpart = 0.02f;

	public TextMeshProUGUI SEA_Arrivals;

	public TextMeshProUGUI SEA_Airport_Bonus;

	public TextMeshProUGUI SEA_PR_Bonus;

	public TextMeshProUGUI SEA_CR_Bonus;

	public TextMeshProUGUI SEA_FI_Bonus;

	public TextMeshProUGUI SEA_QoL_Bonus;

	public TextMeshProUGUI SEA_BorderPercent;

	public float SEA_Arriv;

	private float SEA_PR_Bo;

	private float SEA_CR_Bo;

	private float SEA_FI_Bo;

	private float SEA_QoL_Bo;

	public float SEA_Border;

	private readonly float SEA_LCpart = 0.14f;

	private readonly float SEA_MCpart = 0.72f;

	private readonly float SEA_UCpart = 0.14f;

	public TextMeshProUGUI Arriv_LC;

	public TextMeshProUGUI Arriv_MC;

	public TextMeshProUGUI Arriv_UC;

	public float Arrivals_LC;

	public float Arrivals_MC;

	public float Arrivals_UC;

	public Slider LA_SliderUI;

	public Slider A_SliderUI;

	public Slider SEA_SliderUI;

	public float capacityNews;

	public float libs;

	public float reps;

	private void Start()
	{
		StartCoroutine(UpdateImmigration());
	}

	public void A_Change(Slider slider)
	{
		A_Border = slider.value / 10f;
		A_BorderPercent.text = Mathf.RoundToInt(slider.value * 10f) + "%";
	}

	public void LA_Change(Slider slider)
	{
		LA_Border = slider.value / 10f;
		LA_BorderPercent.text = Mathf.RoundToInt(slider.value * 10f) + "%";
	}

	public void SEA_Change(Slider slider)
	{
		SEA_Border = slider.value / 10f;
		SEA_BorderPercent.text = Mathf.RoundToInt(slider.value * 10f) + "%";
	}

	private IEnumerator UpdateImmigration()
	{
		while (true)
		{
			float x = (float)((int)eco.Population + 5000) + 0.1f * eco.otherCitiesPop;
			float cr = (int)eco.CivilRights;
			float fi = (int)eco.FreedomIndex;
			float qol = eco.QualityOfLife;
			float u = (int)eco.Unemployment;
			float num = DataRegion.RegionalConnections / 2f - (float)AirportConnections / 2f;
			float connectionsBonus = Mathf.Clamp(((float)AirportConnections + num) / 50f, 0f, 1f);
			float airCapacity = (capacityNews = ((!((float)AirportConnections + num <= 0f)) ? (((float)AirportConnections + num) * (float)connectionCapacity + 40f) : 125f));
			LA_Arriv = (int)(Mathf.Log(0.0005f * x) * 80f);
			LA_CR_Bo = (cr - 20f) / 400f;
			LA_FI_Bo = (fi - 30f) / 300f;
			LA_PR_Bo = (25f - u) / 15f;
			LA_QoL_Bo = (qol - 30f) / 250f;
			LA_Arriv = LA_Arriv * (connectionsBonus + LA_CR_Bo + LA_FI_Bo + LA_PR_Bo + LA_QoL_Bo) * LA_Border;
			if (LA_Arriv < 0f)
			{
				LA_Arriv = 0f;
			}
			float lA_Arriv = LA_Arriv;
			if (airCapacity < LA_Arriv)
			{
				LA_Arriv = airCapacity;
				LA_Airport_Bonus.color = red;
				LA_Airport_Bonus.text = (100f - lA_Arriv * 100f / (LA_Arriv + 1f)).ToString("+0;-#") + "%";
			}
			else
			{
				LA_Airport_Bonus.text = (connectionsBonus * 100f).ToString("+0;-#") + "%";
				LA_Airport_Bonus.color = green;
			}
			SetColor(LA_CR_Bo, LA_CR_Bonus);
			LA_Arrivals.text = LA_Arriv.ToString("+0;-#");
			LA_CR_Bonus.text = (LA_CR_Bo * 100f).ToString("+0;-#") + "%";
			SetColor(LA_CR_Bo, LA_CR_Bonus);
			LA_FI_Bonus.text = (LA_FI_Bo * 100f).ToString("+0;-#") + "%";
			SetColor(LA_FI_Bo, LA_FI_Bonus);
			LA_PR_Bonus.text = (LA_PR_Bo * 100f).ToString("+0;-#") + "%";
			SetColor(LA_PR_Bo, LA_PR_Bonus);
			LA_QoL_Bonus.text = (LA_QoL_Bo * 100f).ToString("+0;-#") + "%";
			SetColor(LA_QoL_Bo, LA_QoL_Bonus);
			yield return new WaitForEndOfFrame();
			A_Arriv = (int)(Mathf.Log(0.0005f * x) * 80f);
			A_CR_Bo = (cr - 40f) / 150f;
			A_FI_Bo = (fi - 50f) / 50f;
			A_PR_Bo = (10f - u) / 35f;
			A_QoL_Bo = (qol - 50f) / 75f;
			A_Arriv = A_Arriv * (connectionsBonus + A_CR_Bo + A_FI_Bo + A_PR_Bo + A_QoL_Bo) * A_Border;
			if (A_Arriv < 0f)
			{
				A_Arriv = 0f;
			}
			float a_Arriv = A_Arriv;
			if (airCapacity < A_Arriv)
			{
				A_Arriv = airCapacity;
				A_Airport_Bonus.color = red;
				A_Airport_Bonus.text = (100f - a_Arriv * 100f / (A_Arriv + 1f)).ToString("+0;-#") + "%";
			}
			else
			{
				A_Airport_Bonus.text = (connectionsBonus * 100f).ToString("+0;-#") + "%";
				A_Airport_Bonus.color = green;
			}
			A_Arrivals.text = A_Arriv.ToString("+0;-#");
			A_CR_Bonus.text = (A_CR_Bo * 100f).ToString("+0;-#") + "%";
			SetColor(A_CR_Bo, A_CR_Bonus);
			A_FI_Bonus.text = (A_FI_Bo * 100f).ToString("+0;-#") + "%";
			SetColor(A_FI_Bo, A_FI_Bonus);
			A_PR_Bonus.text = (A_PR_Bo * 100f).ToString("+0;-#") + "%";
			SetColor(A_PR_Bo, A_PR_Bonus);
			A_QoL_Bonus.text = (A_QoL_Bo * 100f).ToString("+0;-#") + "%";
			SetColor(A_QoL_Bo, A_QoL_Bonus);
			yield return new WaitForEndOfFrame();
			SEA_Arriv = (int)(Mathf.Log(0.0005f * x) * 80f);
			SEA_CR_Bo = (cr - 70f) / 75f;
			SEA_FI_Bo = (fi - 40f) / 150f;
			SEA_PR_Bo = (10f - u) / 100f;
			SEA_QoL_Bo = (qol - 80f) / 25f;
			SEA_Arriv = SEA_Arriv * (connectionsBonus + SEA_CR_Bo + SEA_FI_Bo + SEA_PR_Bo + SEA_QoL_Bo) * SEA_Border;
			if (SEA_Arriv < 0f)
			{
				SEA_Arriv = 0f;
			}
			float sEA_Arriv = SEA_Arriv;
			if (airCapacity < SEA_Arriv)
			{
				SEA_Arriv = airCapacity;
				SEA_Airport_Bonus.color = red;
				SEA_Airport_Bonus.text = (100f - sEA_Arriv * 100f / (SEA_Arriv + 1f)).ToString("+0;-#") + "%";
			}
			else
			{
				SEA_Airport_Bonus.text = (connectionsBonus * 100f).ToString("+0;-#") + "%";
				SEA_Airport_Bonus.color = green;
			}
			SEA_Arrivals.text = SEA_Arriv.ToString("+0;-#");
			SEA_CR_Bonus.text = (SEA_CR_Bo * 100f).ToString("+0;-#") + "%";
			SetColor(SEA_CR_Bo, SEA_CR_Bonus);
			SEA_FI_Bonus.text = (SEA_FI_Bo * 100f).ToString("+0;-#") + "%";
			SetColor(SEA_FI_Bo, SEA_FI_Bonus);
			SEA_PR_Bonus.text = (SEA_PR_Bo * 100f).ToString("+0;-#") + "%";
			SetColor(SEA_PR_Bo, SEA_PR_Bonus);
			SEA_QoL_Bonus.text = (SEA_QoL_Bo * 100f).ToString("+0;-#") + "%";
			SetColor(SEA_QoL_Bo, SEA_QoL_Bonus);
			yield return new WaitForEndOfFrame();
			Arrivals_LC = A_LCpart * A_Arriv + LA_LCpart * LA_Arriv + SEA_LCpart * SEA_Arriv;
			Arrivals_MC = A_MCpart * A_Arriv + LA_MCpart * LA_Arriv + SEA_MCpart * SEA_Arriv;
			Arrivals_UC = A_UCpart * A_Arriv + LA_UCpart * LA_Arriv + SEA_UCpart * SEA_Arriv;
			if (Arrivals_LC < 0f)
			{
				Arrivals_LC = 0f;
			}
			if (Arrivals_MC < 0f)
			{
				Arrivals_MC = 0f;
			}
			if (Arrivals_UC < 0f)
			{
				Arrivals_UC = 0f;
			}
			libs = (A_LCpart * A_Arriv + A_MCpart * A_Arriv + A_UCpart * A_Arriv) * 0.2f + (LA_LCpart * LA_Arriv + LA_MCpart * LA_Arriv + LA_UCpart * LA_Arriv) * 0.5f + (SEA_LCpart * SEA_Arriv + SEA_MCpart * SEA_Arriv + SEA_UCpart * SEA_Arriv) * 0.8f;
			reps = (A_LCpart * A_Arriv + A_MCpart * A_Arriv + A_UCpart * A_Arriv) * 0.8f + (LA_LCpart * LA_Arriv + LA_MCpart * LA_Arriv + LA_UCpart * LA_Arriv) * 0.5f + (SEA_LCpart * SEA_Arriv + SEA_MCpart * SEA_Arriv + SEA_UCpart * SEA_Arriv) * 0.2f;
			ImmiLeaning = Mathf.Round(reps * 100f / (libs + reps + 1f));
			Arriv_LC.text = Arrivals_LC.ToString("+0;-#");
			Arriv_MC.text = Arrivals_MC.ToString("+0;-#");
			Arriv_UC.text = Arrivals_UC.ToString("+0;-#");
			if (immiPanel.activeInHierarchy)
			{
				yield return new WaitForSeconds(0.5f);
			}
			else
			{
				yield return new WaitForSeconds(5f);
			}
		}
	}

	private void SetColor(float x, TextMeshProUGUI tm)
	{
		if (x < 0f)
		{
			tm.color = red;
		}
		else if (x == 0f)
		{
			tm.color = cream;
		}
		else
		{
			tm.color = green;
		}
	}

	public void ClosePanel()
	{
		immiPanel.SetActive(value: false);
		Indexer.sliderOnScreen = false;
	}

	public void RetrieveSliderPositions()
	{
		A_SliderUI.value = A_Border * 10f;
		A_Change(A_SliderUI);
		LA_SliderUI.value = LA_Border * 10f;
		LA_Change(LA_SliderUI);
		SEA_SliderUI.value = SEA_Border * 10f;
		SEA_Change(SEA_SliderUI);
	}
}
