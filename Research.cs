using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Research : MonoBehaviour
{
	public Economy eco;

	public Budget budget;

	public SoundManager sm;

	public CostHelper costHelper;

	public Legislation_UI legislation;

	public TextMeshProUGUI HQ_Req1;

	public TextMeshProUGUI HQ_Req2;

	public TextMeshProUGUI HQ_Unlock;

	public TextMeshProUGUI Immi_Req1;

	public TextMeshProUGUI Immi_Req2;

	public TextMeshProUGUI Immi_Unlock;

	public TextMeshProUGUI Comm_Req1;

	public TextMeshProUGUI Comm_Req2;

	public TextMeshProUGUI Comm_Unlock;

	public TextMeshProUGUI Soc_Req1;

	public TextMeshProUGUI Soc_Req2;

	public TextMeshProUGUI Soc_Unlock;

	public TextMeshProUGUI CB_Req1;

	public TextMeshProUGUI CB_Req2;

	public TextMeshProUGUI CB_Unlock;

	public TextMeshProUGUI Con_Req1;

	public TextMeshProUGUI Con_Req2;

	public TextMeshProUGUI Con_Unlock;

	public TextMeshProUGUI ArcoA_Req1;

	private bool arcoAr1;

	public TextMeshProUGUI ArcoA_Req2;

	private bool arcoAr2;

	public TextMeshProUGUI ArcoA_Unlock;

	public Button ArcoA_B;

	private readonly int ArcoA_price = 75000;

	public TextMeshProUGUI ArcoB_Req1;

	private bool arcoBr1;

	public TextMeshProUGUI ArcoB_Req2;

	private bool arcoBr2;

	public TextMeshProUGUI ArcoB_Unlock;

	public Button ArcoB_B;

	private readonly int ArcoB_price = 225000;

	public TextMeshProUGUI ArcoC_Req1;

	private bool arcoCr1;

	public TextMeshProUGUI ArcoC_Req2;

	private bool arcoCr2;

	public TextMeshProUGUI ArcoC_Unlock;

	public Button ArcoC_B;

	private readonly int ArcoC_price = 600000;

	public TextMeshProUGUI Uni_Req1;

	private bool unir1;

	public TextMeshProUGUI Uni_Req2;

	private bool unir2;

	public TextMeshProUGUI Uni_Unlock;

	public Button Uni_B;

	private readonly int Uni_price = 20000;

	public TextMeshProUGUI Fusion_Req1;

	private bool Fusionr1;

	public TextMeshProUGUI Fusion_Req2;

	private bool Fusionr2;

	public TextMeshProUGUI Fusion_Unlock;

	public Button Fusion_B;

	private readonly int Fusion_price = 175000;

	public TextMeshProUGUI Media_Req1;

	private bool Mediar1;

	public TextMeshProUGUI Media_Req2;

	private bool Mediar2;

	public TextMeshProUGUI Media_Unlock;

	public Button Media_B;

	private readonly int Media_price = 1250;

	public TextMeshProUGUI Red_Req1;

	private bool Redr1;

	public TextMeshProUGUI Red_Req2;

	private bool Redr2;

	public TextMeshProUGUI Red_Unlock;

	public Button Red_B;

	private readonly int Red_price = 50;

	public TextMeshProUGUI Eden_Req1;

	private bool Edenr1;

	public TextMeshProUGUI Eden_Req2;

	private bool Edenr2;

	public TextMeshProUGUI Eden_Unlock;

	public Button Eden_B;

	private readonly int Eden_price = 50;

	public TextMeshProUGUI Carbon_Req1;

	private bool Carbonr1;

	public TextMeshProUGUI Carbon_Req2;

	private bool Carbonr2;

	public TextMeshProUGUI Carbon_Unlock;

	public Button Carbon_B;

	private readonly int Carbon_price = 50;

	public TextMeshProUGUI Gulag_Req1;

	private bool Gulagr1;

	public TextMeshProUGUI Gulag_Req2;

	private bool Gulagr2;

	public TextMeshProUGUI Gulag_Unlock;

	public Button Gulag_B;

	private readonly int Gulag_price = 1000;

	public TextMeshProUGUI Rehab_Req1;

	private bool Rehabr1;

	public TextMeshProUGUI Rehab_Req2;

	private bool Rehabr2;

	public TextMeshProUGUI Rehab_Unlock;

	public Button Rehab_B;

	private readonly int Rehab_price = 100000;

	public TextMeshProUGUI Drone_Req1;

	private bool Droner1;

	public TextMeshProUGUI Drone_Req2;

	private bool Droner2;

	public TextMeshProUGUI Drone_Unlock;

	public Button Drone_B;

	private readonly int Drone_price = 2500000;

	private void Start()
	{
		HQ_Unlock.text = LOCSYS.Translate("Stage") + ": " + DataRegion.HQstage + " / 5";
		Immi_Unlock.text = LOCSYS.Translate("Locked");
		Comm_Unlock.text = LOCSYS.Translate("Locked");
		Soc_Unlock.text = LOCSYS.Translate("Locked");
		CB_Unlock.text = LOCSYS.Translate("Locked");
		Con_Unlock.text = LOCSYS.Translate("Locked");
		ArcoA_Unlock.text = LOCSYS.Translate("Locked");
		ArcoB_Unlock.text = LOCSYS.Translate("Locked");
		ArcoC_Unlock.text = LOCSYS.Translate("Locked");
		Uni_Unlock.text = LOCSYS.Translate("Locked");
		Fusion_Unlock.text = LOCSYS.Translate("Locked");
		Media_Unlock.text = LOCSYS.Translate("Locked");
		Red_Unlock.text = LOCSYS.Translate("Locked");
		Eden_Unlock.text = LOCSYS.Translate("Locked");
		Carbon_Unlock.text = LOCSYS.Translate("Locked");
		Gulag_Unlock.text = LOCSYS.Translate("Locked");
		Rehab_Unlock.text = LOCSYS.Translate("Locked");
		Drone_Unlock.text = LOCSYS.Translate("Locked");
	}

	public void UnlockSpecialBuildings()
	{
		if (!DataRegion.alreadyBuilt[10])
		{
			if (eco.NationPop > 120000f)
			{
				ArcoA_Req1.color = budget.green;
				arcoAr1 = true;
			}
			if (eco.EducationLevel > 89f)
			{
				ArcoA_Req2.color = budget.green;
				arcoAr2 = true;
			}
			if (arcoAr1 && arcoAr2)
			{
				ArcoA_Unlock.text = LOCSYS.Translate("Research") + " ($" + ((float)ArcoA_price * Budget.ExchangeRate).ToString("N0") + ")";
				ArcoA_Unlock.color = budget.white;
			}
		}
		else
		{
			ArcoA_Unlock.text = LOCSYS.Translate("Available");
			ArcoA_Unlock.color = budget.green;
			ArcoA_B.GetComponent<TooltipTrigger>().header = "";
			ArcoA_B.interactable = true;
		}
		if (!DataRegion.alreadyBuilt[11])
		{
			if (eco.NationPop > 240000f)
			{
				ArcoB_Req1.color = budget.green;
				arcoBr1 = true;
			}
			if (DataRegion.alreadyBuilt[10])
			{
				ArcoB_Req2.color = budget.green;
				arcoBr2 = true;
			}
			if (arcoBr1 && arcoBr2)
			{
				ArcoB_Unlock.text = LOCSYS.Translate("Research") + " ($" + ((float)ArcoB_price * Budget.ExchangeRate).ToString("N0") + ")";
				ArcoB_Unlock.color = budget.white;
			}
		}
		else
		{
			ArcoB_Unlock.text = LOCSYS.Translate("Available");
			ArcoB_Unlock.color = budget.green;
			ArcoB_B.GetComponent<TooltipTrigger>().header = "";
			ArcoB_B.interactable = true;
		}
		if (!DataRegion.alreadyBuilt[12])
		{
			if (eco.NationPop > 480000f)
			{
				ArcoC_Req1.color = budget.green;
				arcoCr1 = true;
			}
			if (DataRegion.alreadyBuilt[11])
			{
				ArcoC_Req2.color = budget.green;
				arcoCr2 = true;
			}
			if (arcoCr1 && arcoCr2)
			{
				ArcoC_Unlock.text = LOCSYS.Translate("Research") + " ($" + ((float)ArcoC_price * Budget.ExchangeRate).ToString("N0") + ")";
				ArcoC_Unlock.color = budget.white;
			}
		}
		else
		{
			ArcoC_Unlock.text = LOCSYS.Translate("Available");
			ArcoC_Unlock.color = budget.green;
			ArcoC_B.GetComponent<TooltipTrigger>().header = "";
			ArcoC_B.interactable = true;
		}
		if (!DataRegion.alreadyBuilt[5])
		{
			if (eco.NationPop > 100000f)
			{
				Uni_Req1.color = budget.green;
				unir1 = true;
			}
			if (eco.EducationLevel > 64f)
			{
				Uni_Req2.color = budget.green;
				unir2 = true;
			}
			else
			{
				Uni_Req2.color = budget.red;
				unir2 = false;
			}
			if (unir1 && unir2)
			{
				Uni_Unlock.text = LOCSYS.Translate("Research") + " ($" + ((float)Uni_price * Budget.ExchangeRate).ToString("N0") + ")";
				Uni_Unlock.color = budget.white;
			}
		}
		else
		{
			Uni_Unlock.text = LOCSYS.Translate("Available");
			Uni_Unlock.color = budget.green;
			Uni_B.GetComponent<TooltipTrigger>().header = "";
			Uni_B.interactable = true;
		}
		if (!DataRegion.alreadyBuilt[14])
		{
			if (int.Parse(legislation.Array.grid[2, 885]) == 3)
			{
				Media_Req1.color = budget.green;
				Mediar1 = true;
			}
			if (eco.CivilRights > 89f)
			{
				Media_Req2.color = budget.green;
				Mediar2 = true;
			}
			else
			{
				Media_Req2.color = budget.red;
				Mediar2 = false;
			}
			if (Mediar1 && Mediar2)
			{
				Media_Unlock.text = LOCSYS.Translate("Research") + " ($" + ((float)Media_price * Budget.ExchangeRate).ToString("N0") + ")";
				Media_Unlock.color = budget.white;
			}
		}
		else
		{
			Media_Unlock.text = LOCSYS.Translate("Available");
			Media_Unlock.color = budget.green;
			Media_B.GetComponent<TooltipTrigger>().header = "";
			Media_B.interactable = true;
		}
		if (!DataRegion.alreadyBuilt[13])
		{
			if (int.Parse(legislation.Array.grid[2, 305]) < 2)
			{
				Fusion_Req2.color = budget.green;
				Fusionr2 = true;
			}
			if (eco.EducationLevel > 99f)
			{
				Fusion_Req1.color = budget.green;
				Fusionr1 = true;
			}
			else
			{
				Fusion_Req1.color = budget.red;
				Fusionr1 = false;
			}
			if (Fusionr1 && Fusionr2)
			{
				Fusion_Unlock.text = LOCSYS.Translate("Research") + " ($" + ((float)Fusion_price * Budget.ExchangeRate).ToString("N0") + ")";
				Fusion_Unlock.color = budget.white;
			}
		}
		else
		{
			Fusion_Unlock.text = LOCSYS.Translate("Available");
			Fusion_Unlock.color = budget.green;
			Fusion_B.GetComponent<TooltipTrigger>().header = "";
			Fusion_B.interactable = true;
		}
		if (!DataRegion.alreadyBuilt[15])
		{
			if (int.Parse(legislation.Array.grid[2, 525]) < 2)
			{
				Red_Req1.color = budget.green;
				Redr1 = true;
			}
			if (eco.FreedomIndex > 59f)
			{
				Red_Req2.color = budget.green;
				Redr2 = true;
			}
			else
			{
				Red_Req2.color = budget.red;
				Redr2 = false;
			}
			if (Redr1 && Redr2)
			{
				Red_Unlock.text = LOCSYS.Translate("Research") + " ($" + ((float)Red_price * Budget.ExchangeRate).ToString("N0") + ")";
				Red_Unlock.color = budget.white;
			}
		}
		else
		{
			Red_Unlock.text = LOCSYS.Translate("Available");
			Red_Unlock.color = budget.green;
			Red_B.GetComponent<TooltipTrigger>().header = "";
			Red_B.interactable = true;
		}
		if (!DataRegion.alreadyBuilt[16])
		{
			int num = int.Parse(legislation.Array.grid[2, 45]);
			if (eco.CivilRights > 69f)
			{
				Eden_Req1.color = budget.green;
				Edenr1 = true;
			}
			else
			{
				Eden_Req1.color = budget.red;
				Edenr1 = false;
			}
			if (num < 2)
			{
				Eden_Req2.color = budget.green;
				Edenr2 = true;
			}
			if (Edenr1 && Edenr2)
			{
				Eden_Unlock.text = LOCSYS.Translate("Research") + " ($" + ((float)Eden_price * Budget.ExchangeRate).ToString("N0") + ")";
				Eden_Unlock.color = budget.white;
			}
		}
		else
		{
			Eden_Unlock.text = LOCSYS.Translate("Available");
			Eden_Unlock.color = budget.green;
			Eden_B.GetComponent<TooltipTrigger>().header = "";
			Eden_B.interactable = true;
		}
		if (!DataRegion.alreadyBuilt[17])
		{
			if (eco.EducationLevel > 89f)
			{
				Carbon_Req1.color = budget.green;
				Carbonr1 = true;
			}
			else
			{
				Carbon_Req1.color = budget.red;
				Carbonr1 = false;
			}
			Carbonr2 = true;
			if (Carbonr1 && Carbonr2)
			{
				Carbon_Unlock.text = LOCSYS.Translate("Research") + " ($" + ((float)Carbon_price * Budget.ExchangeRate).ToString("N0") + ")";
				Carbon_Unlock.color = budget.white;
			}
		}
		else
		{
			Carbon_Unlock.text = LOCSYS.Translate("Available");
			Carbon_Unlock.color = budget.green;
			Carbon_B.GetComponent<TooltipTrigger>().header = "";
			Carbon_B.interactable = true;
		}
		if (!DataRegion.alreadyBuilt[18])
		{
			int num2 = int.Parse(legislation.Array.grid[2, 705]);
			if (eco.FreedomIndex < 45f)
			{
				Gulag_Req1.color = budget.green;
				Gulagr1 = true;
			}
			else
			{
				Gulag_Req1.color = budget.red;
				Gulagr1 = false;
			}
			if (num2 == 3)
			{
				Gulag_Req2.color = budget.green;
				Gulagr2 = true;
			}
			if (Gulagr1 && Gulagr2)
			{
				Gulag_Unlock.text = LOCSYS.Translate("Research") + " ($" + ((float)Gulag_price * Budget.ExchangeRate).ToString("N0") + ")";
				Gulag_Unlock.color = budget.white;
			}
		}
		else
		{
			Gulag_Unlock.text = LOCSYS.Translate("Available");
			Gulag_Unlock.color = budget.green;
			Gulag_B.GetComponent<TooltipTrigger>().header = "";
			Gulag_B.interactable = true;
		}
		if (!DataRegion.alreadyBuilt[19])
		{
			if (eco.FreedomIndex > 70f)
			{
				Rehab_Req1.color = budget.green;
				Rehabr1 = true;
			}
			if (eco.Unemployment < 4f)
			{
				Rehab_Req2.color = budget.green;
				Rehabr2 = true;
			}
			else
			{
				Rehab_Req2.color = budget.red;
				Rehabr2 = false;
			}
			if (Rehabr1 && Rehabr2)
			{
				Rehab_Unlock.text = LOCSYS.Translate("Research") + " ($" + ((float)Rehab_price * Budget.ExchangeRate).ToString("N0") + ")";
				Rehab_Unlock.color = budget.white;
			}
		}
		else
		{
			Rehab_Unlock.text = LOCSYS.Translate("Available");
			Rehab_Unlock.color = budget.green;
			Rehab_B.GetComponent<TooltipTrigger>().header = "";
			Rehab_B.interactable = true;
		}
		if (!DataRegion.alreadyBuilt[20])
		{
			if (eco.FreedomIndex > 80f)
			{
				Drone_Req2.color = budget.green;
				Droner2 = true;
			}
			else
			{
				Drone_Req2.color = budget.red;
				Droner2 = false;
			}
			if ((float)eco.GDP / eco.Population > 9999f)
			{
				Drone_Req1.color = budget.green;
				Droner1 = true;
			}
			else
			{
				Drone_Req1.color = budget.red;
				Droner1 = false;
			}
			if (Droner1 && Droner2)
			{
				Drone_Unlock.text = LOCSYS.Translate("Research") + " ($" + ((float)Drone_price * Budget.ExchangeRate).ToString("N0") + ")";
				Drone_Unlock.color = budget.white;
			}
		}
		else
		{
			Drone_Unlock.text = LOCSYS.Translate("Available");
			Drone_Unlock.color = budget.green;
			Drone_B.GetComponent<TooltipTrigger>().header = "";
			Drone_B.interactable = true;
		}
	}

	public void UpdateGovTexts()
	{
		if (eco.NationPop > 2000f)
		{
			Immi_Req1.color = budget.green;
		}
		if (eco.NationPop > 75000f)
		{
			CB_Req1.color = budget.green;
		}
		if (eco.enactedPolicies >= 12)
		{
			CB_Req2.color = budget.green;
		}
		if (eco.NationPop > 25000f)
		{
			Comm_Req1.color = budget.green;
		}
		if (eco.enactedPolicies >= 3)
		{
			Comm_Req2.color = budget.green;
		}
		if (eco.NationPop > 50000f)
		{
			Soc_Req1.color = budget.green;
		}
		if (eco.enactedPolicies >= 6)
		{
			Soc_Req2.color = budget.green;
		}
		if (eco.NationPop > 300000f)
		{
			Con_Req1.color = budget.green;
		}
		if (eco.enactedPolicies >= 30)
		{
			Con_Req2.color = budget.green;
		}
	}

	public void UnlockBuilding(int index)
	{
		switch (index)
		{
		case 10:
			if (budget.Balance < (float)ArcoA_price * Budget.ExchangeRate)
			{
				sm.Play("Impractical");
			}
			else if (!arcoAr1 || !arcoAr2)
			{
				sm.Play("Impractical");
			}
			else if (!DataRegion.alreadyBuilt[10])
			{
				DataRegion.alreadyBuilt[10] = true;
				ArcoA_Unlock.text = LOCSYS.Translate("Available");
				ArcoA_B.interactable = true;
				sm.Play("Purchase");
				costHelper.PayAmount(Mathf.RoundToInt((float)ArcoA_price * Budget.ExchangeRate));
			}
			break;
		case 11:
			if (budget.Balance < (float)ArcoB_price * Budget.ExchangeRate)
			{
				sm.Play("Impractical");
			}
			else if (!arcoBr1 || !arcoBr2)
			{
				sm.Play("Impractical");
			}
			else if (!DataRegion.alreadyBuilt[11])
			{
				DataRegion.alreadyBuilt[11] = true;
				ArcoB_Unlock.text = LOCSYS.Translate("Available");
				ArcoB_B.interactable = true;
				sm.Play("Purchase");
				costHelper.PayAmount(Mathf.RoundToInt((float)ArcoB_price * Budget.ExchangeRate));
			}
			break;
		case 12:
			if (budget.Balance < (float)ArcoC_price * Budget.ExchangeRate)
			{
				sm.Play("Impractical");
			}
			else if (!arcoCr1 || !arcoCr2)
			{
				sm.Play("Impractical");
			}
			else if (!DataRegion.alreadyBuilt[12])
			{
				DataRegion.alreadyBuilt[12] = true;
				ArcoC_Unlock.text = LOCSYS.Translate("Available");
				ArcoC_B.interactable = true;
				sm.Play("Purchase");
				costHelper.PayAmount(Mathf.RoundToInt((float)ArcoC_price * Budget.ExchangeRate));
			}
			break;
		case 5:
			if (budget.Balance < (float)Uni_price * Budget.ExchangeRate)
			{
				sm.Play("Impractical");
			}
			else if (!unir1 || !unir2)
			{
				sm.Play("Impractical");
			}
			else if (!DataRegion.alreadyBuilt[5])
			{
				DataRegion.alreadyBuilt[5] = true;
				Uni_Unlock.text = LOCSYS.Translate("Available");
				Uni_B.interactable = true;
				sm.Play("Purchase");
				costHelper.PayAmount(Mathf.RoundToInt((float)Uni_price * Budget.ExchangeRate));
			}
			break;
		case 14:
			if (budget.Balance < (float)Media_price * Budget.ExchangeRate)
			{
				sm.Play("Impractical");
			}
			else if (!Mediar1 || !Mediar2)
			{
				sm.Play("Impractical");
			}
			else if (!DataRegion.alreadyBuilt[14])
			{
				DataRegion.alreadyBuilt[14] = true;
				Media_Unlock.text = LOCSYS.Translate("Available");
				Media_B.interactable = true;
				sm.Play("Purchase");
				costHelper.PayAmount(Mathf.RoundToInt((float)Media_price * Budget.ExchangeRate));
			}
			break;
		case 13:
			if (budget.Balance < (float)Fusion_price * Budget.ExchangeRate)
			{
				sm.Play("Impractical");
			}
			else if (!Fusionr1 || !Fusionr2)
			{
				sm.Play("Impractical");
			}
			else if (!DataRegion.alreadyBuilt[13])
			{
				DataRegion.alreadyBuilt[13] = true;
				Fusion_Unlock.text = LOCSYS.Translate("Available");
				Fusion_B.interactable = true;
				sm.Play("Purchase");
				costHelper.PayAmount(Mathf.RoundToInt((float)Fusion_price * Budget.ExchangeRate));
			}
			break;
		case 15:
			if (budget.Balance < (float)Red_price * Budget.ExchangeRate)
			{
				sm.Play("Impractical");
			}
			else if (!Redr1 || !Redr2)
			{
				sm.Play("Impractical");
			}
			else if (!DataRegion.alreadyBuilt[15])
			{
				DataRegion.alreadyBuilt[15] = true;
				Red_Unlock.text = LOCSYS.Translate("Available");
				Red_B.interactable = true;
				sm.Play("Purchase");
				costHelper.PayAmount(Mathf.RoundToInt((float)Red_price * Budget.ExchangeRate));
			}
			break;
		case 16:
			if (budget.Balance < (float)Eden_price * Budget.ExchangeRate)
			{
				sm.Play("Impractical");
			}
			else if (!Edenr1 || !Edenr2)
			{
				sm.Play("Impractical");
			}
			else if (!DataRegion.alreadyBuilt[16])
			{
				DataRegion.alreadyBuilt[16] = true;
				Eden_Unlock.text = LOCSYS.Translate("Available");
				Eden_B.interactable = true;
				sm.Play("Purchase");
				costHelper.PayAmount(Mathf.RoundToInt((float)Eden_price * Budget.ExchangeRate));
			}
			break;
		case 17:
			if (budget.Balance < (float)Carbon_price * Budget.ExchangeRate)
			{
				sm.Play("Impractical");
			}
			else if (!Carbonr1 || !Carbonr2)
			{
				sm.Play("Impractical");
			}
			else if (!DataRegion.alreadyBuilt[17])
			{
				DataRegion.alreadyBuilt[17] = true;
				Carbon_Unlock.text = LOCSYS.Translate("Available");
				Carbon_B.interactable = true;
				sm.Play("Purchase");
				costHelper.PayAmount(Mathf.RoundToInt((float)Carbon_price * Budget.ExchangeRate));
			}
			break;
		case 18:
			if (budget.Balance < (float)Gulag_price * Budget.ExchangeRate)
			{
				sm.Play("Impractical");
			}
			else if (!Gulagr1 || !Gulagr2)
			{
				sm.Play("Impractical");
			}
			else if (!DataRegion.alreadyBuilt[18])
			{
				DataRegion.alreadyBuilt[18] = true;
				Gulag_Unlock.text = LOCSYS.Translate("Available");
				Gulag_B.interactable = true;
				sm.Play("Purchase");
				costHelper.PayAmount(Mathf.RoundToInt((float)Gulag_price * Budget.ExchangeRate));
			}
			break;
		case 19:
			if (budget.Balance < (float)Rehab_price * Budget.ExchangeRate)
			{
				sm.Play("Impractical");
			}
			else if (!Rehabr1 || !Rehabr2)
			{
				sm.Play("Impractical");
			}
			else if (!DataRegion.alreadyBuilt[19])
			{
				DataRegion.alreadyBuilt[19] = true;
				Rehab_Unlock.text = LOCSYS.Translate("Available");
				Rehab_B.interactable = true;
				sm.Play("Purchase");
				costHelper.PayAmount(Mathf.RoundToInt((float)Rehab_price * Budget.ExchangeRate));
			}
			break;
		case 20:
			if (budget.Balance < (float)Drone_price * Budget.ExchangeRate)
			{
				sm.Play("Impractical");
			}
			else if (!Droner1 || !Droner2)
			{
				sm.Play("Impractical");
			}
			else if (!DataRegion.alreadyBuilt[20])
			{
				DataRegion.alreadyBuilt[20] = true;
				Drone_Unlock.text = LOCSYS.Translate("Available");
				Drone_B.interactable = true;
				sm.Play("Purchase");
				costHelper.PayAmount(Mathf.RoundToInt((float)Drone_price * Budget.ExchangeRate));
			}
			break;
		}
	}

	public void UnlockArcoA(int i)
	{
		if (budget.Balance >= (float)i)
		{
			sm.Play("Purchase");
			ArcoA_Unlock.text = LOCSYS.Translate("Ready");
			budget.Balance -= i;
		}
		else
		{
			sm.Play("Impractical");
		}
	}

	public void ClosePanel()
	{
		gameObject.SetActive(value: false);
		Indexer.sliderOnScreen = false;
	}
}
