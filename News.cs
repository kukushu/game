using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class News : MonoBehaviour
{
	public Economy eco;

	public Budget budget;

	public World w;

	public Messages messages;

	public GameObject TextPrefab;

	public Button buttonLink;

	public TextMeshProUGUI topLine;

	public GameObject defaultLine;

	public GameObject content;

	public GameObject scrollView;

	private List<TextMeshProUGUI> allLines = new List<TextMeshProUGUI>();

	private string line1;

	private string line2;

	private string line3;

	public Color32 red;

	public Color32 white;

	public Color32 green;

	private List<Vector3Int> severeNews = new List<Vector3Int>();

	private List<Vector3Int> neutralNews = new List<Vector3Int>();

	public List<Vector3Int> goodNews = new List<Vector3Int>();

	private List<Vector3Int> allNews = new List<Vector3Int>();

	private Vector3Int topLineData;

	private int extraTime;

	private void Start()
	{
		StartCoroutine(ChangeTopLine());
	}

	private void UpdateNews()
	{
		extraTime++;
		severeNews.Clear();
		neutralNews.Clear();
		allNews.Clear();
		if (eco.Population > 500f && eco.PowerUse * 100 / (eco.PowerCapacity + 1) < 50)
		{
			neutralNews.Add(new Vector3Int(0, 1, 0));
		}
		else if (eco.Population > 500f && eco.PowerUse * 100 / (eco.PowerCapacity + 1) > 99)
		{
			severeNews.Add(new Vector3Int(1, 2, 0));
		}
		if (eco.Population > 500f && eco.WaterUse * 100 / (eco.WaterCapacity + 1) < 50)
		{
			neutralNews.Add(new Vector3Int(2, 1, 0));
		}
		else if (eco.Population > 500f && eco.WaterUse * 100 / (eco.WaterCapacity + 1) > 99)
		{
			severeNews.Add(new Vector3Int(3, 2, 0));
		}
		if (eco.LC_Demand > 500f)
		{
			neutralNews.Add(new Vector3Int(4, 1, 0));
		}
		if (eco.Population > 500f && eco.Unemployment < 2f)
		{
			severeNews.Add(new Vector3Int(5, 2, 0));
		}
		else if (eco.Population > 2500f && eco.Unemployment > 15f && eco.Unemployment <= 22f)
		{
			neutralNews.Add(new Vector3Int(6, 1, 1));
		}
		else if (eco.Unemployment > 22f)
		{
			severeNews.Add(new Vector3Int(7, 2, 1));
		}
		if (eco.Population > 5000f && eco.EducationQuality < 15f)
		{
			neutralNews.Add(new Vector3Int(8, 1, 0));
		}
		else if (eco.Population > 1000f && eco.SchoolCapacity > eco.TotalPupils * 2)
		{
			neutralNews.Add(new Vector3Int(9, 1, 0));
		}
		if (eco.immi.LA_Arriv >= eco.immi.capacityNews)
		{
			neutralNews.Add(new Vector3Int(46, 1, 0));
		}
		if (eco.HospitalCapacity < eco.HospitalUse - 100)
		{
			neutralNews.Add(new Vector3Int(10, 1, 0));
		}
		else if (eco.Population > 2000f && eco.HospitalCapacity > eco.HospitalUse * 2)
		{
			neutralNews.Add(new Vector3Int(11, 1, 0));
		}
		if (eco.Population < 3000f && eco.LC_Demand + eco.MC_Demand > 30f && w.VacantResLots.Count < 2)
		{
			neutralNews.Add(new Vector3Int(12, 1, 0));
		}
		if (eco.Population < 5000f && eco.R_Demand > 20f)
		{
			neutralNews.Add(new Vector3Int(13, 1, 1));
		}
		if (Budget.CashFlow < 50f && budget.Balance < Mathf.Abs(Budget.CashFlow * 12f))
		{
			severeNews.Add(new Vector3Int(14, 2, 1));
		}
		if (eco.ApprovalRate < 30)
		{
			severeNews.Add(new Vector3Int(16, 2, 1));
		}
		else if (eco.ApprovalRate > 75)
		{
			neutralNews.Add(new Vector3Int(25, 0, 0));
		}
		if (eco.Population > 3000f && eco.Stability < 20f)
		{
			neutralNews.Add(new Vector3Int(17, 1, 0));
		}
		if (eco.ladderHealth < -10f)
		{
			neutralNews.Add(new Vector3Int(18, 1, 1));
		}
		if (eco.SecurityScore < 20 && eco.Population > 8000f)
		{
			neutralNews.Add(new Vector3Int(19, 1, 0));
		}
		else if (eco.SecurityScore > 95)
		{
			neutralNews.Add(new Vector3Int(20, 0, 0));
		}
		if ((eco.Population < 5000f && eco.HT_Demand + eco.M_Demand > 50f) || eco.HT_Demand + eco.M_Demand > 1000f)
		{
			neutralNews.Add(new Vector3Int(22, 1, 0));
		}
		if (eco.HealthScore > 80)
		{
			neutralNews.Add(new Vector3Int(23, 0, 0));
		}
		if (budget.churchCount < 1 && eco.Population > 1000f && eco.Population < 15000f)
		{
			neutralNews.Add(new Vector3Int(24, 1, 0));
		}
		if (eco.Unemployment > 3f && eco.Unemployment < 6f)
		{
			neutralNews.Add(new Vector3Int(27, 0, 0));
		}
		if (eco.BirthRate < 1.5f && eco.Population > 15000f)
		{
			neutralNews.Add(new Vector3Int(28, 1, 0));
		}
		else if (eco.BirthRate > 4.5f && eco.Population > 15000f)
		{
			neutralNews.Add(new Vector3Int(29, 0, 0));
		}
		if (eco.ReligionScore > 80f)
		{
			neutralNews.Add(new Vector3Int(30, 1, 0));
		}
		else if (eco.ReligionScore < 10f && eco.Population > 25000f)
		{
			neutralNews.Add(new Vector3Int(45, 1, 0));
		}
		if (eco.PopGrowthNominal < 10f)
		{
			neutralNews.Add(new Vector3Int(31, 1, 0));
		}
		if (eco.Slum_Demand > 10f)
		{
			neutralNews.Add(new Vector3Int(32, 1, 1));
		}
		if (eco.Retail < 1f && eco.Unemployment > 12f)
		{
			neutralNews.Add(new Vector3Int(33, 2, 0));
		}
		if (eco.HT_Demand > 750f)
		{
			neutralNews.Add(new Vector3Int(34, 1, 0));
		}
		if (eco.Environment < 30f)
		{
			neutralNews.Add(new Vector3Int(35, 1, 0));
		}
		if (eco.Inequality > 75 && eco.Population > 14000f)
		{
			neutralNews.Add(new Vector3Int(36, 2, 1));
		}
		else if (eco.Inequality < 30)
		{
			neutralNews.Add(new Vector3Int(37, 0, 0));
		}
		if (eco.ongoingDecay)
		{
			neutralNews.Add(new Vector3Int(38, 1, 1));
		}
		if (eco.GlobalLeaning < 15f)
		{
			neutralNews.Add(new Vector3Int(39, 1, 0));
		}
		else if (eco.GlobalLeaning > 85f)
		{
			neutralNews.Add(new Vector3Int(40, 1, 0));
		}
		if (eco.Corruption > 1.5f)
		{
			neutralNews.Add(new Vector3Int(41, 1, 0));
		}
		else if (eco.Corruption <= 0f)
		{
			neutralNews.Add(new Vector3Int(42, 1, 0));
		}
		if (budget.Inflation > 3.5f)
		{
			neutralNews.Add(new Vector3Int(43, 2, 0));
		}
		else if (budget.Inflation < 1f)
		{
			neutralNews.Add(new Vector3Int(44, 0, 0));
		}
		if (eco.HighSchools < 1 && eco.Population > 20000f)
		{
			neutralNews.Add(new Vector3Int(47, 2, 0));
		}
		if ((float)eco.PrisonCapacity * 1.2f < (float)eco.PrisonUse && eco.Population > 40000f)
		{
			neutralNews.Add(new Vector3Int(48, 2, 0));
		}
		if (eco.EducationQuality > 80f)
		{
			neutralNews.Add(new Vector3Int(21, 0, 0));
		}
		if (Budget.CashFlow > 2500f && budget.Balance > 50000f && budget.Balance < 500000f)
		{
			neutralNews.Add(new Vector3Int(15, 0, 0));
		}
		if (eco.Population > 10000f && eco.Stability > 98f)
		{
			neutralNews.Add(new Vector3Int(26, 0, 0));
		}
		if (goodNews.Count > 0)
		{
			allNews.Add(goodNews[0]);
		}
		while (severeNews.Count > 0 && allNews.Count < 6)
		{
			int index = Random.Range(0, severeNews.Count);
			allNews.Add(severeNews[index]);
			severeNews.RemoveAt(index);
		}
		while (neutralNews.Count > 0 && allNews.Count < 6)
		{
			int index2 = Random.Range(0, neutralNews.Count);
			allNews.Add(neutralNews[index2]);
			neutralNews.RemoveAt(index2);
		}
		if (allNews.Count < 1)
		{
			line1 = CreateStatsNews(0);
			allNews.Add(new Vector3Int(0, 1, 0));
		}
		else
		{
			line1 = LOCSYS.Translate("n" + allNews[0].x);
		}
		if (allNews.Count < 2)
		{
			line2 = CreateStatsNews(1);
			allNews.Add(new Vector3Int(0, 1, 0));
		}
		else
		{
			line2 = LOCSYS.Translate("n" + allNews[1].x);
		}
		if (allNews.Count < 3)
		{
			line3 = CreateStatsNews(2);
			allNews.Add(new Vector3Int(0, 1, 0));
		}
		else
		{
			line3 = LOCSYS.Translate("n" + allNews[2].x);
		}
		foreach (TextMeshProUGUI item in allLines.ToList())
		{
			allLines.Remove(item);
			Object.Destroy(item.gameObject);
		}
		for (int i = 0; i < allNews.Count; i++)
		{
			if (i <= 7)
			{
				GameObject gameObject = Object.Instantiate(defaultLine);
				gameObject.transform.SetParent(content.transform);
				gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
				gameObject.SetActive(value: true);
				TextMeshProUGUI component = gameObject.GetComponent<TextMeshProUGUI>();
				component.text = LOCSYS.Translate("n" + allNews[i].x);
				gameObject.GetComponent<NewsClick>().line = allNews[i];
				SetColorAndUnderlineBox(component, allNews[i]);
				allLines.Add(component);
			}
		}
		if (extraTime > 3)
		{
			extraTime = 0;
			goodNews.Clear();
		}
	}

	private string CreateStatsNews(int emptyLine)
	{
		float value = Random.value;
		switch (emptyLine)
		{
		case 2:
			if (value < 0.25f)
			{
				return LOCSYS.Translate("n100") + " " + eco.GDPgrowth.ToString("+#;-#;0") + "%";
			}
			if (value < 0.5f)
			{
				return LOCSYS.Translate("n101") + " " + eco.ApprovalRate + "%";
			}
			if (value < 0.75f)
			{
				return (eco.immi.Arrivals_LC + eco.immi.Arrivals_MC + eco.immi.Arrivals_UC).ToString("N0") + " " + LOCSYS.Translate("n102");
			}
			return LOCSYS.Translate("n103") + " " + eco.Unemployment.ToString("N1") + "%";
		case 1:
			if (value < 0.5f)
			{
				return (eco.PowerUse * 100 / (eco.PowerCapacity + 1)).ToString("F0") + "% " + LOCSYS.Translate("n104");
			}
			return LOCSYS.Translate("n105") + " " + eco.EducationLevel.ToString("F0") + "%";
		case 0:
			if (value < 0.25f)
			{
				return LOCSYS.Translate("n106") + " " + eco.NationPop.ToString("N0");
			}
			if (value < 0.5f)
			{
				return LOCSYS.Translate("n107") + " " + eco.QualityOfLife.ToString("N0") + "%";
			}
			if (value < 0.75f)
			{
				return LOCSYS.Translate("n108") + " " + (eco.GDP / 100000000).ToString("F") + "%";
			}
			return LOCSYS.Translate("n109") + " " + (eco.UCnaturalGrowth / 2f).ToString("F0");
		default:
			return "";
		}
	}

	private IEnumerator ChangeTopLine()
	{
		while (true)
		{
			UpdateNews();
			topLine.text = line1;
			topLineData = allNews[0];
			topLine.color = SetColorAndUnderline();
			yield return new WaitForSecondsRealtime(8f);
			while (topLine.color.a >= 0f)
			{
				topLine.color = new Color(topLine.color.r, topLine.color.g, topLine.color.b, topLine.color.a - 0.05f);
				yield return new WaitForFixedUpdate();
			}
			yield return new WaitForSecondsRealtime(1f);
			topLine.text = line2;
			topLineData = allNews[1];
			topLine.color = SetColorAndUnderline();
			yield return new WaitForSecondsRealtime(8f);
			while (topLine.color.a >= 0f)
			{
				topLine.color = new Color(topLine.color.r, topLine.color.g, topLine.color.b, topLine.color.a - 0.05f);
				yield return new WaitForFixedUpdate();
			}
			yield return new WaitForSecondsRealtime(1f);
			topLine.text = line3;
			topLineData = allNews[2];
			topLine.color = SetColorAndUnderline();
			yield return new WaitForSecondsRealtime(8f);
			while (topLine.color.a >= 0f)
			{
				topLine.color = new Color(topLine.color.r, topLine.color.g, topLine.color.b, topLine.color.a - 0.05f);
				yield return new WaitForFixedUpdate();
			}
			yield return new WaitForSecondsRealtime(1f);
		}
	}

	private Color SetColorAndUnderline()
	{
		if (topLineData.z > 0)
		{
			topLine.fontStyle = FontStyles.Underline;
		}
		else
		{
			topLine.fontStyle = FontStyles.Normal;
		}
		if (topLineData.y == 0)
		{
			return green;
		}
		if (topLineData.y == 1)
		{
			return white;
		}
		return red;
	}

	private void SetColorAndUnderlineBox(TextMeshProUGUI t, Vector3Int data)
	{
		if (data.z > 0)
		{
			t.fontStyle = FontStyles.Underline;
		}
		else
		{
			t.fontStyle = FontStyles.Normal;
		}
		if (data.y == 0)
		{
			t.color = green;
		}
		else if (data.y == 1)
		{
			t.color = white;
		}
		else
		{
			t.color = red;
		}
	}

	public void ClickLinkable()
	{
		if (topLineData.z == 1)
		{
			int num = topLineData.x;
			if (num == 7)
			{
				num = 6;
			}
			messages.BlueBox.gameObject.SetActive(value: false);
			messages.NewInfoMessage(num);
		}
	}

	public void ClickBoxLinkable(NewsClick click)
	{
		if (click.line.z == 1)
		{
			int num = click.line.x;
			if (num == 7)
			{
				num = 6;
			}
			messages.BlueBox.gameObject.SetActive(value: false);
			messages.NewInfoMessage(num);
		}
	}

	public void OpenScrollView()
	{
		scrollView.SetActive(!scrollView.activeSelf);
	}
}
