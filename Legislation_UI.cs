using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Legislation_UI : MonoBehaviour
{
	public GameObject legislationPanel;

	public GameObject policyPanel;

	public GameObject customGOs;

	public GameObject classicGOs;

	public Economy eco;

	public Button Economy;

	public Button Government;

	public Button Society;

	public Button Custom;

	private int Tab = 2;

	private int Policy;

	private int Choice;

	public Sprite Tab_Active;

	public Sprite Tab_Inactive;

	public Sprite Button_Active;

	public Sprite Button_Inactive;

	public Image PolicyImage;

	public Sprite ClassicPanel;

	public Sprite CustomPanel;

	public PolicyArray Array;

	public Button[] buttons;

	public TextMeshProUGUI[] titles;

	public TextMeshProUGUI[] subTitles;

	public TextMeshProUGUI[] costs;

	public Button Option1;

	public TextMeshProUGUI O1;

	public Button Option2;

	public TextMeshProUGUI O2;

	public Button Option3;

	public TextMeshProUGUI O3;

	public Button Option4;

	public TextMeshProUGUI O4;

	public GameObject ChoiceHover;

	public TextMeshProUGUI Title;

	public TextMeshProUGUI SubTitle;

	public TextMeshProUGUI Gentrification;

	public TextMeshProUGUI FreedomIndex;

	public TextMeshProUGUI CivilRights;

	public TextMeshProUGUI FertilityRate;

	public TextMeshProUGUI Security;

	public TextMeshProUGUI Environment;

	public TextMeshProUGUI SchoolEfficiency;

	public TextMeshProUGUI HospitalEfficiency;

	public TextMeshProUGUI Corruption;

	public TextMeshProUGUI Cost;

	public TextMeshProUGUI Religiousness;

	public TextMeshProUGUI Retail;

	public TextMeshProUGUI Office;

	public TextMeshProUGUI Manufacturing;

	public TextMeshProUGUI HighTech;

	public GameObject L1;

	public GameObject L2;

	public GameObject L3;

	public GameObject R1;

	public GameObject R2;

	public GameObject R3;

	public TextMeshProUGUI LeftSupport;

	public TextMeshProUGUI RightSupport;

	public Color Green;

	public Color Red;

	public Color Zero;

	public GameObject Dropdown;

	public TextMeshProUGUI R_Drop;

	public TextMeshProUGUI O_Drop;

	public TextMeshProUGUI BM_Drop;

	public TextMeshProUGUI AM_Drop;

	public TextMeshProUGUI CR_Drop;

	public TextMeshProUGUI SL_Drop;

	public TextMeshProUGUI FI_Drop;

	public TextMeshProUGUI He_Drop;

	public TextMeshProUGUI Ed_Drop;

	public TextMeshProUGUI Se_Drop;

	public TextMeshProUGUI Br_Drop;

	public TextMeshProUGUI Rel_Drop;

	public TextMeshProUGUI Pol_Drop;

	public TextMeshProUGUI Co_Drop;

	public TextMeshProUGUI R_Res;

	public TextMeshProUGUI O_Res;

	public TextMeshProUGUI BM_Res;

	public TextMeshProUGUI AM_Res;

	public TextMeshProUGUI CR_Res;

	public TextMeshProUGUI SL_Res;

	public TextMeshProUGUI FI_Res;

	public TextMeshProUGUI He_Res;

	public TextMeshProUGUI Ed_Res;

	public TextMeshProUGUI Se_Res;

	public TextMeshProUGUI Br_Res;

	public TextMeshProUGUI Rel_Res;

	public TextMeshProUGUI Polu_Res;

	public TextMeshProUGUI Co_Res;

	public TextMeshProUGUI Cons_Res;

	public TextMeshProUGUI Libs_Res;

	public TextMeshProUGUI PointAvail;

	public TextMeshProUGUI stabWarning;

	private double points;

	private double[] tValue = new double[19];

	public TextMeshProUGUI inputDescription;

	private void Awake()
	{
		Array = DataRegion.policyArray;
		UnlockNewPolicies(DataRegion.HQstage);
	}

	public void LegislationCLick()
	{
		if (Tab == 1)
		{
			EconomyClick();
		}
		else if (Tab == 2)
		{
			GovernmentClick();
		}
		else if (Tab == 3)
		{
			SocietyClick();
		}
		else if (Tab == 4)
		{
			CustomClick();
		}
	}

	public void UpdatePolicyButtons()
	{
		for (int i = 1; i < 18; i++)
		{
			int num = PolicyLines(i);
			if (num == 0)
			{
				titles[i].text = "";
				subTitles[i].text = "-";
				costs[i].text = "-";
				buttons[i].image.sprite = Button_Inactive;
				continue;
			}
			titles[i].text = LOCSYS.Translate(Array.getValueAtIndex(1, num));
			if (num < 1024)
			{
				subTitles[i].text = GetSubTitle(num);
				float cost = GetCost(num);
				if (cost < 0f)
				{
					costs[i].color = Green;
				}
				else if (cost == 0f)
				{
					costs[i].color = Zero;
				}
				else
				{
					costs[i].color = Red;
				}
				costs[i].text = "$" + cost.ToString("N0");
			}
			else
			{
				subTitles[i].text = Array.grid[4, num].ToString();
				float num2 = float.Parse(Array.grid[4, num + 11], CultureInfo.InvariantCulture) * eco.polBudgetDivider * eco.Population;
				costs[i].text = "$" + num2.ToString("N0");
				if (num2 > 0f)
				{
					costs[i].color = Red;
				}
				else
				{
					costs[i].color = Green;
				}
			}
			buttons[i].image.sprite = GetSprite(num);
		}
	}

	public void EconomyClick()
	{
		Economy.image.sprite = Tab_Active;
		Government.image.sprite = Tab_Inactive;
		Society.image.sprite = Tab_Inactive;
		Custom.image.sprite = Tab_Inactive;
		Tab = 1;
		HideShowRightColumn(show: true);
		UpdatePolicyButtons();
	}

	public void GovernmentClick()
	{
		Economy.image.sprite = Tab_Inactive;
		Government.image.sprite = Tab_Active;
		Society.image.sprite = Tab_Inactive;
		Custom.image.sprite = Tab_Inactive;
		Tab = 2;
		HideShowRightColumn(show: true);
		UpdatePolicyButtons();
	}

	public void SocietyClick()
	{
		Economy.image.sprite = Tab_Inactive;
		Government.image.sprite = Tab_Inactive;
		Society.image.sprite = Tab_Active;
		Custom.image.sprite = Tab_Inactive;
		Tab = 3;
		HideShowRightColumn(show: true);
		UpdatePolicyButtons();
	}

	public void CustomClick()
	{
		Economy.image.sprite = Tab_Inactive;
		Government.image.sprite = Tab_Inactive;
		Society.image.sprite = Tab_Inactive;
		Custom.image.sprite = Tab_Active;
		Tab = 4;
		HideShowRightColumn(show: false);
		UpdatePolicyButtons();
	}

	private string GetSubTitle(int line)
	{
		if (int.Parse(Array.grid[2, line + 1]) == 4)
		{
			return "?";
		}
		int num = int.Parse(Array.grid[2, line + 1]);
		return LOCSYS.Translate(Array.getValueAtIndex(4 + num, line));
	}

	private float GetCost(int line)
	{
		int num = int.Parse(Array.grid[2, line + 1]);
		if (num == 4)
		{
			return 0f;
		}
		return float.Parse(Array.grid[4 + num, line + 11], CultureInfo.InvariantCulture) * eco.polBudgetDivider * eco.Population;
	}

	private Sprite GetSprite(int line)
	{
		if (int.Parse(Array.grid[2, line + 1]) == 4)
		{
			return Button_Inactive;
		}
		return Button_Active;
	}

	public void ClosePolicy()
	{
		policyPanel.SetActive(value: false);
		legislationPanel.SetActive(value: true);
		LegislationCLick();
	}

	public void CloseLegislation()
	{
		legislationPanel.SetActive(value: false);
		Indexer.sliderOnScreen = false;
	}

	public void GetPolicy(int y)
	{
		legislationPanel.SetActive(value: false);
		policyPanel.SetActive(value: true);
		Policy = PolicyLines(y);
		if (Policy == 0)
		{
			Debug.Log("ERROR IN POLICY");
			return;
		}
		if (Policy < 1024)
		{
			customGOs.SetActive(value: false);
			classicGOs.SetActive(value: true);
			PolicyImage.sprite = ClassicPanel;
			OpenPolicy();
		}
		else
		{
			customGOs.SetActive(value: true);
			classicGOs.SetActive(value: false);
			PolicyImage.sprite = CustomPanel;
			OpenCustom();
		}
		if (eco.Stability < 10f)
		{
			stabWarning.text = LOCSYS.Translate("enactStab");
		}
		else
		{
			stabWarning.text = "";
		}
	}

	private int PolicyLines(int y)
	{
		if (Tab == 1)
		{
			return y switch
			{
				1 => 4, 
				2 => 64, 
				3 => 164, 
				4 => 304, 
				5 => 324, 
				6 => 344, 
				7 => 464, 
				8 => 484, 
				9 => 624, 
				10 => 724, 
				11 => 364, 
				12 => 864, 
				13 => 904, 
				14 => 924, 
				15 => 944, 
				16 => 964, 
				17 => 984, 
				_ => 0, 
			};
		}
		if (Tab == 2)
		{
			return y switch
			{
				1 => 24, 
				2 => 84, 
				3 => 124, 
				4 => 144, 
				5 => 184, 
				6 => 204, 
				7 => 224, 
				8 => 384, 
				9 => 404, 
				10 => 424, 
				11 => 564, 
				12 => 584, 
				13 => 644, 
				14 => 664, 
				15 => 744, 
				16 => 784, 
				17 => 884, 
				_ => 0, 
			};
		}
		if (Tab == 3)
		{
			return y switch
			{
				1 => 44, 
				2 => 104, 
				3 => 244, 
				4 => 264, 
				5 => 284, 
				6 => 444, 
				7 => 504, 
				8 => 524, 
				9 => 544, 
				10 => 604, 
				11 => 684, 
				12 => 764, 
				13 => 704, 
				14 => 824, 
				15 => 844, 
				16 => 804, 
				17 => 1004, 
				_ => 0, 
			};
		}
		if (Tab == 4)
		{
			return y switch
			{
				1 => 1024, 
				2 => 1044, 
				3 => 1064, 
				4 => 1084, 
				5 => 1104, 
				6 => 1124, 
				7 => 1144, 
				8 => 1164, 
				9 => 1184, 
				10 => 1204, 
				_ => 0, 
			};
		}
		Debug.Log("No policy assigned to this button");
		return 0;
	}

	private void OpenPolicy()
	{
		O1.text = LOCSYS.Translate(Array.getValueAtIndex(4, Policy));
		O2.text = LOCSYS.Translate(Array.getValueAtIndex(5, Policy));
		O3.text = LOCSYS.Translate(Array.getValueAtIndex(6, Policy));
		O4.text = LOCSYS.Translate(Array.getValueAtIndex(7, Policy));
		Title.text = LOCSYS.Translate(Array.getValueAtIndex(1, Policy));
		SubTitle.text = LOCSYS.Translate(Array.getValueAtIndex(2, Policy));
		Choice = Convert.ToInt32(Array.getValueAtIndex(2, Policy + 1));
		if (Choice == 4)
		{
			Choice = 1;
			Option2_Click();
		}
		else if (Choice == 0)
		{
			Option1_Click();
		}
		else if (Choice == 1)
		{
			Option2_Click();
		}
		else if (Choice == 2)
		{
			Option3_Click();
		}
		else
		{
			Option4_Click();
		}
	}

	private void ChangeOption()
	{
		Gentrification.text = (float.Parse(Array.grid[4 + Choice, Policy + 1], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		Gentrification.color = SetColor(1);
		Religiousness.text = (float.Parse(Array.grid[4 + Choice, Policy + 18], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		Religiousness.color = SetColor(18);
		Retail.text = (float.Parse(Array.grid[4 + Choice, Policy + 3], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		Retail.color = SetColor(3);
		Office.text = (float.Parse(Array.grid[4 + Choice, Policy + 4], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		Office.color = SetColor(4);
		Manufacturing.text = (float.Parse(Array.grid[4 + Choice, Policy + 5], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		Manufacturing.color = SetColor(5);
		HighTech.text = (float.Parse(Array.grid[4 + Choice, Policy + 6], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		HighTech.color = SetColor(6);
		SchoolEfficiency.text = (float.Parse(Array.grid[4 + Choice, Policy + 8], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		SchoolEfficiency.color = SetColor(8);
		HospitalEfficiency.text = (float.Parse(Array.grid[4 + Choice, Policy + 9], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		HospitalEfficiency.color = SetColor(9);
		float num = float.Parse(Array.grid[4 + Choice, Policy + 10], CultureInfo.InvariantCulture) * 10f;
		Corruption.text = num.ToString("+0.0;-0.0") + "%";
		if (num < 0f)
		{
			Corruption.color = Green;
		}
		else if (num == 0f)
		{
			Corruption.color = Zero;
		}
		else
		{
			Corruption.color = Red;
		}
		float num2 = float.Parse(Array.grid[4 + Choice, Policy + 11], CultureInfo.InvariantCulture) * eco.polBudgetDivider * eco.Population;
		Cost.text = "$" + num2.ToString("N0");
		if (num2 < 0f)
		{
			Cost.color = Green;
		}
		else if (num2 == 0f)
		{
			Cost.color = Zero;
		}
		else
		{
			Cost.color = Red;
		}
		FreedomIndex.text = (float.Parse(Array.grid[4 + Choice, Policy + 12], CultureInfo.InvariantCulture) * 10f).ToString("+0.#;-0.#") + "pt.";
		FreedomIndex.color = SetColor(12);
		CivilRights.text = (float.Parse(Array.grid[4 + Choice, Policy + 13], CultureInfo.InvariantCulture) * 10f).ToString("+0.#;-0.#") + "pt.";
		CivilRights.color = SetColor(13);
		FertilityRate.text = (float.Parse(Array.grid[4 + Choice, Policy + 15], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		FertilityRate.color = SetColor(15);
		Security.text = (float.Parse(Array.grid[4 + Choice, Policy + 16], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		Security.color = SetColor(16);
		float num3 = (0f - float.Parse(Array.grid[4 + Choice, Policy + 17], CultureInfo.InvariantCulture)) * 10f;
		Environment.text = num3.ToString("+0.0;-0.0") + "%";
		if (num3 < 0f)
		{
			Environment.color = Green;
		}
		else if (num3 == 0f)
		{
			Environment.color = Zero;
		}
		else
		{
			Environment.color = Red;
		}
		LeftSupport.text = (float.Parse(Array.getValueAtIndex(4 + Choice, Policy + 14), CultureInfo.InvariantCulture) * 100f).ToString("N0") + "%";
		LeftSupport.color = SetColor(14);
		RightSupport.text = (float.Parse(Array.getValueAtIndex(4 + Choice, Policy + 7), CultureInfo.InvariantCulture) * 100f).ToString("N0") + "%";
		RightSupport.color = SetColor(7);
		float num4 = float.Parse(Array.getValueAtIndex(4 + Choice, Policy + 2), CultureInfo.InvariantCulture);
		if (num4 < -0.2f)
		{
			L3.SetActive(value: true);
			L2.SetActive(value: true);
			L1.SetActive(value: true);
			R3.SetActive(value: false);
			R2.SetActive(value: false);
			R1.SetActive(value: false);
		}
		else if (num4 < -0.1f)
		{
			L3.SetActive(value: false);
			L2.SetActive(value: true);
			L1.SetActive(value: true);
			R3.SetActive(value: false);
			R2.SetActive(value: false);
			R1.SetActive(value: false);
		}
		else if (num4 < 0f)
		{
			L3.SetActive(value: false);
			L2.SetActive(value: false);
			L1.SetActive(value: true);
			R3.SetActive(value: false);
			R2.SetActive(value: false);
			R1.SetActive(value: false);
		}
		else if (num4 < 0.1f)
		{
			L3.SetActive(value: false);
			L2.SetActive(value: false);
			L1.SetActive(value: false);
			R3.SetActive(value: false);
			R2.SetActive(value: false);
			R1.SetActive(value: false);
		}
		else if (num4 < 0.2f)
		{
			L3.SetActive(value: false);
			L2.SetActive(value: false);
			L1.SetActive(value: false);
			R3.SetActive(value: true);
			R2.SetActive(value: false);
			R1.SetActive(value: false);
		}
		else if (num4 < 0.3f)
		{
			L3.SetActive(value: false);
			L2.SetActive(value: false);
			L1.SetActive(value: false);
			R3.SetActive(value: true);
			R2.SetActive(value: true);
			R1.SetActive(value: false);
		}
		else
		{
			L3.SetActive(value: false);
			L2.SetActive(value: false);
			L1.SetActive(value: false);
			R3.SetActive(value: true);
			R2.SetActive(value: true);
			R1.SetActive(value: true);
		}
	}

	private Color SetColor(int effect)
	{
		if (float.Parse(Array.grid[4 + Choice, Policy + effect], CultureInfo.InvariantCulture) < 0f)
		{
			return Red;
		}
		if (float.Parse(Array.grid[4 + Choice, Policy + effect], CultureInfo.InvariantCulture) == 0f)
		{
			return Zero;
		}
		return Green;
	}

	private Color SetColorDirect(double color)
	{
		if (color < 0.0)
		{
			return Red;
		}
		if (color == 0.0)
		{
			return Zero;
		}
		return Green;
	}

	private Color SetColorDirectIn(double color)
	{
		if (color < 0.0)
		{
			return Green;
		}
		if (color == 0.0)
		{
			return Zero;
		}
		return Red;
	}

	public void Option1_Click()
	{
		ChoiceHover.gameObject.transform.position = Option1.transform.position;
		Choice = 0;
		ChangeOption();
	}

	public void Option2_Click()
	{
		ChoiceHover.gameObject.transform.position = Option2.transform.position;
		Choice = 1;
		ChangeOption();
	}

	public void Option3_Click()
	{
		ChoiceHover.gameObject.transform.position = Option3.transform.position;
		Choice = 2;
		ChangeOption();
	}

	public void Option4_Click()
	{
		ChoiceHover.gameObject.transform.position = Option4.transform.position;
		Choice = 3;
		ChangeOption();
	}

	public void Enact()
	{
		if (eco.Stability < 10f)
		{
			eco.budget.sm.Play("Impractical");
			return;
		}
		Array.setValueAtIndex(2, Policy + 1, Choice);
		eco.Stability -= 10f;
		if (Policy > 1020)
		{
			EnactCustom();
			return;
		}
		eco.ApprovalLeft += float.Parse(Array.grid[4 + Choice, Policy + 14], CultureInfo.InvariantCulture) * 100f;
		eco.ApprovalRight += float.Parse(Array.grid[4 + Choice, Policy + 7], CultureInfo.InvariantCulture) * 100f;
		eco.ApprovalLeft = Mathf.Clamp(eco.ApprovalLeft, -50f, 100f);
		eco.ApprovalRight = Mathf.Clamp(eco.ApprovalRight, -50f, 100f);
		ClosePolicy();
		legislationPanel.SetActive(value: true);
		LegislationCLick();
	}

	public void DropdownPolicy(int policy)
	{
		int num = PolicyLines(policy);
		if (!int.TryParse(Array.grid[2, num + 1], out var result))
		{
			result = 4;
		}
		if (result == 4)
		{
			Dropdown.SetActive(value: false);
			return;
		}
		SL_Drop.text = (float.Parse(Array.grid[4 + result, num + 1], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		Rel_Drop.text = (float.Parse(Array.grid[4 + result, num + 18], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		R_Drop.text = (float.Parse(Array.grid[4 + result, num + 3], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		O_Drop.text = (float.Parse(Array.grid[4 + result, num + 4], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		BM_Drop.text = (float.Parse(Array.grid[4 + result, num + 5], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		AM_Drop.text = (float.Parse(Array.grid[4 + result, num + 6], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		Ed_Drop.text = (float.Parse(Array.grid[4 + result, num + 8], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		He_Drop.text = (float.Parse(Array.grid[4 + result, num + 9], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		Co_Drop.text = (float.Parse(Array.grid[4 + result, num + 10], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		FI_Drop.text = (float.Parse(Array.grid[4 + result, num + 12], CultureInfo.InvariantCulture) * 10f).ToString("+0.#;-0.#") + "pt.";
		CR_Drop.text = (float.Parse(Array.grid[4 + result, num + 13], CultureInfo.InvariantCulture) * 10f).ToString("+0.#;-0.#") + "pt.";
		Br_Drop.text = (float.Parse(Array.grid[4 + result, num + 15], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		Se_Drop.text = (float.Parse(Array.grid[4 + result, num + 16], CultureInfo.InvariantCulture) * 10f).ToString("+0.0;-0.0") + "%";
		Pol_Drop.text = (float.Parse(Array.grid[4 + result, num + 17], CultureInfo.InvariantCulture) * -10f).ToString("+0.0;-0.0") + "%";
		MoveDropdown(policy);
	}

	private void MoveDropdown(int policy)
	{
		Vector3 vector = new Vector3(0f, -100f, 0f);
		Dropdown.transform.position = buttons[policy].transform.position + vector;
	}

	public void ShowDropdown(int policy)
	{
		Dropdown.SetActive(value: true);
		DropdownPolicy(policy);
	}

	public void HideDropdown()
	{
		Dropdown.SetActive(value: false);
	}

	public void UnlockNewPolicies(int HQstage)
	{
		if (HQstage >= 1)
		{
			buttons[1].interactable = true;
			buttons[2].interactable = true;
			buttons[3].interactable = true;
			buttons[4].interactable = true;
		}
		if (HQstage >= 2)
		{
			buttons[5].interactable = true;
			buttons[6].interactable = true;
			buttons[7].interactable = true;
		}
		if (HQstage >= 3)
		{
			buttons[8].interactable = true;
			buttons[9].interactable = true;
			buttons[10].interactable = true;
			buttons[11].interactable = true;
		}
		if (HQstage >= 4)
		{
			buttons[12].interactable = true;
			buttons[13].interactable = true;
			buttons[14].interactable = true;
		}
		if (HQstage >= 5)
		{
			buttons[15].interactable = true;
			buttons[16].interactable = true;
			buttons[17].interactable = true;
		}
	}

	public void OpenCustom()
	{
		Choice = 0;
		for (int i = 1; i < 19; i++)
		{
			tValue[i] = float.Parse(Array.grid[4, Policy + i], CultureInfo.InvariantCulture);
		}
		inputDescription.text = Array.grid[4, Policy].ToString();
		Title.text = Array.getValueAtIndex(1, Policy);
		SubTitle.text = LOCSYS.Translate("Executive Order");
		UpdatePoints();
	}

	public void PlusCustom(int index)
	{
		double num = tValue[index];
		num += 0.019999999552965164;
		tValue[index] = ClampValue(index, num);
		UpdatePoints();
	}

	public void LessCustom(int index)
	{
		double num = tValue[index];
		num -= 0.019999999552965164;
		tValue[index] = ClampValue(index, num);
		UpdatePoints();
	}

	private double ClampValue(int index, double v)
	{
		double num;
		double num2;
		if (index < 5)
		{
			num = -0.1;
			num2 = 0.1;
		}
		else if (index < 7)
		{
			num = 0.0;
			num2 = 0.1;
		}
		else if (index < 11)
		{
			num = -0.2;
			num2 = 0.2;
		}
		else if (index == 11)
		{
			num = -0.5;
			num2 = 0.5;
		}
		else if (index < 14)
		{
			num = -0.1;
			num2 = 0.1;
		}
		else
		{
			num = -0.2;
			num2 = 0.2;
		}
		if (v < num)
		{
			return num;
		}
		if (v > num2)
		{
			return num2;
		}
		return v;
	}

	private void UpdatePoints()
	{
		points = 0.0;
		if (tValue[1] > 0.0)
		{
			points -= (0.01 * tValue[1] + tValue[1] * tValue[1] * tValue[1]) * 40000.0;
		}
		else
		{
			points -= (0.01 * tValue[1] + tValue[1] * tValue[1] * tValue[1]) * 40000.0;
		}
		if (tValue[2] > 0.0)
		{
			points -= (0.01 * tValue[2] + tValue[2] * tValue[2] * tValue[2]) * 30000.0;
		}
		else
		{
			points -= (0.01 * tValue[2] + tValue[2] * tValue[2] * tValue[2]) * 30000.0;
		}
		if (tValue[3] > 0.0)
		{
			points -= (0.01 * tValue[3] + tValue[3] * tValue[3] * tValue[3]) * 50000.0;
		}
		else
		{
			points -= (0.01 * tValue[3] + tValue[3] * tValue[3] * tValue[3]) * 50000.0;
		}
		if (tValue[4] > 0.0)
		{
			points -= (0.01 * tValue[4] + tValue[4] * tValue[4] * tValue[4]) * 40000.0;
		}
		else
		{
			points -= (0.01 * tValue[4] + tValue[4] * tValue[4] * tValue[4]) * 40000.0;
		}
		if (tValue[5] > 0.0)
		{
			points -= (0.01 * tValue[5] + tValue[5] * tValue[5] * tValue[5]) * 40000.0;
		}
		else
		{
			points -= (0.01 * tValue[5] + tValue[5] * tValue[5] * tValue[5]) * 40000.0;
		}
		if (tValue[6] > 0.0)
		{
			points -= (0.01 * tValue[6] + tValue[6] * tValue[6] * tValue[6]) * 40000.0;
		}
		else
		{
			points -= (0.006 * tValue[6] + tValue[6] * tValue[6] * tValue[6]) * 40000.0;
		}
		if (tValue[8] > 0.0)
		{
			points -= (0.01 * tValue[8] + tValue[8] * tValue[8] * tValue[8]) * 30000.0;
		}
		else
		{
			points -= (0.01 * tValue[8] + tValue[8] * tValue[8] * tValue[8]) * 30000.0;
		}
		if (tValue[9] > 0.0)
		{
			points -= (0.01 * tValue[9] + tValue[9] * tValue[9] * tValue[9]) * 20000.0;
		}
		else
		{
			points -= (0.01 * tValue[9] + tValue[9] * tValue[9] * tValue[9]) * 20000.0;
		}
		if (tValue[10] > 0.0)
		{
			points += (0.01 * tValue[10] + tValue[10] * tValue[10] * tValue[10]) * 20000.0;
		}
		else
		{
			points += (0.01 * tValue[10] + tValue[10] * tValue[10] * tValue[10]) * 20000.0;
		}
		if (tValue[11] > 0.0)
		{
			points += tValue[11] * tValue[11] * 1000.0;
		}
		else
		{
			points -= tValue[11] * tValue[11] * 1000.0;
		}
		if (tValue[12] > 0.0)
		{
			points -= (0.01 * tValue[12] + tValue[12] * tValue[12] * tValue[12]) * 40000.0;
		}
		else
		{
			points -= (0.01 * tValue[12] + tValue[12] * tValue[12] * tValue[12]) * 40000.0;
		}
		if (tValue[13] > 0.0)
		{
			points -= (0.01 * tValue[13] + tValue[13] * tValue[13] * tValue[13]) * 40000.0;
		}
		else
		{
			points -= (0.01 * tValue[13] + tValue[13] * tValue[13] * tValue[13]) * 40000.0;
		}
		if (tValue[15] > 0.0)
		{
			points -= (0.01 * tValue[15] + tValue[15] * tValue[15] * tValue[15]) * 20000.0;
		}
		else
		{
			points -= (0.01 * tValue[15] + tValue[15] * tValue[15] * tValue[15]) * 20000.0;
		}
		if (tValue[16] > 0.0)
		{
			points -= (0.01 * tValue[16] + tValue[16] * tValue[16] * tValue[16]) * 30000.0;
		}
		else
		{
			points -= (0.01 * tValue[16] + tValue[16] * tValue[16] * tValue[16]) * 30000.0;
		}
		if (tValue[17] > 0.0)
		{
			points -= (0.01 * tValue[17] + tValue[17] * tValue[17] * tValue[17]) * 20000.0;
		}
		else
		{
			points -= (0.01 * tValue[17] + tValue[17] * tValue[17] * tValue[17]) * 20000.0;
		}
		if (tValue[18] > 0.0)
		{
			points -= (0.01 * tValue[18] + tValue[18] * tValue[18] * tValue[18]) * 20000.0;
		}
		else
		{
			points -= (0.01 * tValue[18] + tValue[18] * tValue[18] * tValue[18]) * 20000.0;
		}
		points = (int)points;
		PointAvail.text = points.ToString("N0");
		if (points < 0.0)
		{
			eco.budget.sm.Play("NoMoney");
		}
		Gentrification.text = (tValue[1] * 10.0).ToString("+0.0;-0.0") + "%";
		Gentrification.color = SetColorDirect(tValue[1]);
		Retail.text = (tValue[3] * 10.0).ToString("+0.0;-0.0") + "%";
		Retail.color = SetColorDirect(tValue[3]);
		Office.text = (tValue[4] * 10.0).ToString("+0.0;-0.0") + "%";
		Office.color = SetColorDirect(tValue[4]);
		Manufacturing.text = (tValue[5] * 10.0).ToString("+0.0;-0.0") + "%";
		Manufacturing.color = SetColorDirect(tValue[5]);
		HighTech.text = (tValue[6] * 10.0).ToString("+0.0;-0.0") + "%";
		HighTech.color = SetColorDirect(tValue[6]);
		SchoolEfficiency.text = (tValue[8] * 10.0).ToString("+0.0;-0.0") + "%";
		SchoolEfficiency.color = SetColorDirect(tValue[8]);
		HospitalEfficiency.text = (tValue[9] * 10.0).ToString("+0.0;-0.0") + "%";
		HospitalEfficiency.color = SetColorDirect(tValue[9]);
		Corruption.text = (tValue[10] * 10.0).ToString("+0.0;-0.0") + "%";
		Corruption.color = SetColorDirectIn(tValue[10]);
		Cost.text = "$" + (tValue[11] * (double)eco.polBudgetDivider * (double)eco.Population).ToString("N0");
		Cost.color = SetColorDirectIn(tValue[11]);
		FreedomIndex.text = (tValue[12] * 10.0).ToString("+0.#;-0.#") + "pt.";
		FreedomIndex.color = SetColorDirect(tValue[12]);
		CivilRights.text = (tValue[13] * 10.0).ToString("+0.#;-0.#") + "pt.";
		CivilRights.color = SetColorDirect(tValue[13]);
		FertilityRate.text = (tValue[15] * 10.0).ToString("+0.0;-0.0") + "%";
		FertilityRate.color = SetColorDirect(tValue[15]);
		Security.text = (tValue[16] * 10.0).ToString("+0.0;-0.0") + "%";
		Security.color = SetColorDirect(tValue[16]);
		Environment.text = (tValue[17] * -10.0).ToString("+0.0;-0.0") + "%";
		Environment.color = SetColorDirect(tValue[17]);
		Religiousness.text = (tValue[18] * 10.0).ToString("+0.0;-0.0") + "%";
		Religiousness.color = SetColorDirect(tValue[18]);
		LeftSupport.text = (tValue[14] * 10.0).ToString("N0") + "%";
		LeftSupport.color = SetColorDirect(tValue[14]);
		RightSupport.text = (tValue[7] * 10.0).ToString("N0") + "%";
		RightSupport.color = SetColorDirect(tValue[7]);
	}

	public void EnactCustom()
	{
		Debug.Log("Enact Custom");
		if (points < 0.0)
		{
			eco.budget.sm.Play("Impractical");
			return;
		}
		for (int i = 1; i < 19; i++)
		{
			Array.recordValue(4, Policy + i, (float)tValue[i]);
		}
		Array.grid[4, Policy] = inputDescription.text;
		eco.Stability -= 4f;
		ClosePolicy();
		legislationPanel.SetActive(value: true);
		LegislationCLick();
	}

	private void HideShowRightColumn(bool show)
	{
		for (int i = 11; i < 18; i++)
		{
			buttons[i].gameObject.SetActive(show);
		}
	}
}
