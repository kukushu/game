using UnityEngine;
using UnityEngine.UI;

public class SubMenu : MonoBehaviour
{
	public GameObject R_Icons;

	public GameObject C_Icons;

	public GameObject I_Icons;

	public GameObject M_Icons;

	public GameObject Leisure_Icons;

	public GameObject Parks_Icons;

	public GameObject StateOwned_Icons;

	private int panel_L;

	private int panel_R;

	public GameObject subLeft;

	public GameObject subRight;

	public Indexer indexer;

	public Button B_Res;

	public Button B_Com;

	public Button B_Ind;

	public Button B_Mix;

	public Button B_Park;

	public Button B_Leisure;

	public Button B_State;

	public Button B_Buldoze;

	public Sprite Res_On;

	public Sprite Res_Off;

	public Sprite Com_On;

	public Sprite Com_Off;

	public Sprite Ind_On;

	public Sprite Ind_Off;

	public Sprite Mix_On;

	public Sprite Mix_Off;

	public Sprite Park_On;

	public Sprite Park_Off;

	public Sprite Leisure_On;

	public Sprite Leisure_Off;

	public Sprite State_On;

	public Sprite State_Off;

	public Sprite Buldoze_On;

	public Sprite Buldoze_Off;

	public Button B_Roads;

	public Button B_Trans;

	public Button B_Pow;

	public Button B_Serv;

	public Button B_Rel;

	public Button B_Gov;

	public Button B_Spec;

	public Button B_Terra;

	public Sprite Roads_On;

	public Sprite Roads_Off;

	public Sprite Trans_On;

	public Sprite Trans_Off;

	public Sprite Pow_On;

	public Sprite Pow_Off;

	public Sprite Serv_On;

	public Sprite Serv_Off;

	public Sprite Rel_On;

	public Sprite Rel_Off;

	public Sprite Gov_On;

	public Sprite Gov_Off;

	public Sprite Spec_On;

	public Sprite Spec_Off;

	public Sprite Terra_On;

	public Sprite Terra_Off;

	public GameObject Terrain_Icons;

	public GameObject Services_Icons;

	public GameObject Roads_Icons;

	public GameObject Gov_Icons;

	public GameObject Facilities_Icons;

	public GameObject Special_Icons;

	public GameObject Religion_Icons;

	public GameObject Transport_Icons;

	public void ShowResidential()
	{
		if (panel_L == 1)
		{
			subLeft.SetActive(value: false);
			panel_L = 0;
			indexer.HideAll();
			return;
		}
		subLeft.SetActive(value: true);
		R_Icons.SetActive(value: true);
		indexer.superSub = 1;
		C_Icons.SetActive(value: false);
		I_Icons.SetActive(value: false);
		Parks_Icons.SetActive(value: false);
		StateOwned_Icons.SetActive(value: false);
		M_Icons.SetActive(value: false);
		Leisure_Icons.SetActive(value: false);
		panel_L = 1;
		if (Indexer.Sub_Index == 1)
		{
			indexer.super.transform.position = indexer.residentialLow.transform.position;
		}
		else if (Indexer.Sub_Index == 2)
		{
			indexer.super.transform.position = indexer.residentialMid.transform.position;
		}
		else if (Indexer.Sub_Index == 3)
		{
			indexer.super.transform.position = indexer.residentialHi.transform.position;
		}
		else if (Indexer.Sub_Index == 4)
		{
			indexer.super.transform.position = indexer.residentialCus.transform.position;
		}
		SetButtons();
	}

	public void ShowCommercial()
	{
		if (panel_L == 2)
		{
			subLeft.SetActive(value: false);
			panel_L = 0;
			indexer.HideAll();
			return;
		}
		subLeft.SetActive(value: true);
		R_Icons.SetActive(value: false);
		C_Icons.SetActive(value: true);
		indexer.superSub = 2;
		I_Icons.SetActive(value: false);
		Parks_Icons.SetActive(value: false);
		StateOwned_Icons.SetActive(value: false);
		M_Icons.SetActive(value: false);
		Leisure_Icons.SetActive(value: false);
		panel_L = 2;
		if (Indexer.Sub_Index == 1)
		{
			indexer.super.transform.position = indexer.commercialLow.transform.position;
		}
		else if (Indexer.Sub_Index == 2)
		{
			indexer.super.transform.position = indexer.commercialMid.transform.position;
		}
		else if (Indexer.Sub_Index == 3)
		{
			indexer.super.transform.position = indexer.commercialHi.transform.position;
		}
		else if (Indexer.Sub_Index == 4)
		{
			indexer.super.transform.position = indexer.residentialCus.transform.position;
		}
		SetButtons();
	}

	public void ShowIndustrial()
	{
		if (panel_L == 3)
		{
			subLeft.SetActive(value: false);
			panel_L = 0;
			indexer.HideAll();
			return;
		}
		subLeft.SetActive(value: true);
		R_Icons.SetActive(value: false);
		C_Icons.SetActive(value: false);
		I_Icons.SetActive(value: true);
		indexer.superSub = 3;
		Parks_Icons.SetActive(value: false);
		StateOwned_Icons.SetActive(value: false);
		M_Icons.SetActive(value: false);
		Leisure_Icons.SetActive(value: false);
		panel_L = 3;
		if (Indexer.Sub_Index == 1)
		{
			indexer.super.transform.position = indexer.industrialLow.transform.position;
		}
		else if (Indexer.Sub_Index == 2)
		{
			indexer.super.transform.position = indexer.industrialMid.transform.position;
		}
		else if (Indexer.Sub_Index == 3)
		{
			indexer.super.transform.position = indexer.industrialHi.transform.position;
		}
		else if (Indexer.Sub_Index == 4)
		{
			indexer.super.transform.position = indexer.residentialCus.transform.position;
		}
		SetButtons();
	}

	public void ShowMixed()
	{
		if (panel_L == -1)
		{
			subLeft.SetActive(value: false);
			panel_L = -1;
			indexer.HideAll();
			return;
		}
		subLeft.SetActive(value: true);
		R_Icons.SetActive(value: false);
		C_Icons.SetActive(value: false);
		M_Icons.SetActive(value: true);
		indexer.superSub = -1;
		I_Icons.SetActive(value: false);
		Parks_Icons.SetActive(value: false);
		StateOwned_Icons.SetActive(value: false);
		Leisure_Icons.SetActive(value: false);
		panel_L = -1;
		if (Indexer.Sub_Index == 1)
		{
			indexer.super.transform.position = indexer.mixLow.transform.position;
		}
		else if (Indexer.Sub_Index == 2)
		{
			indexer.super.transform.position = indexer.mixMid.transform.position;
		}
		else if (Indexer.Sub_Index == 3)
		{
			indexer.super.transform.position = indexer.mixHi.transform.position;
		}
		else if (Indexer.Sub_Index == 4)
		{
			indexer.super.transform.position = indexer.residentialCus.transform.position;
		}
		SetButtons();
	}

	public void ShowParks()
	{
		if (panel_L == 5)
		{
			subLeft.SetActive(value: false);
			panel_L = 0;
			if (indexer.superSub == 5)
			{
				indexer.HideAll();
			}
			else
			{
				SetButtons();
			}
			return;
		}
		indexer.HideAll();
		subLeft.SetActive(value: true);
		R_Icons.SetActive(value: false);
		C_Icons.SetActive(value: false);
		I_Icons.SetActive(value: false);
		Parks_Icons.SetActive(value: true);
		StateOwned_Icons.SetActive(value: false);
		M_Icons.SetActive(value: false);
		Leisure_Icons.SetActive(value: false);
		panel_L = 5;
		if (indexer.superSub < 7)
		{
			indexer.HideAll();
		}
		else
		{
			SetButtons();
		}
	}

	public void ShowLeisure()
	{
		if (panel_L == -2)
		{
			subLeft.SetActive(value: false);
			panel_L = 0;
			if (indexer.superSub == -2)
			{
				indexer.HideAll();
			}
			else
			{
				SetButtons();
			}
			return;
		}
		indexer.HideAll();
		subLeft.SetActive(value: true);
		R_Icons.SetActive(value: false);
		C_Icons.SetActive(value: false);
		I_Icons.SetActive(value: false);
		M_Icons.SetActive(value: false);
		Leisure_Icons.SetActive(value: true);
		Parks_Icons.SetActive(value: false);
		StateOwned_Icons.SetActive(value: false);
		panel_L = -2;
		if (indexer.superSub < 7)
		{
			indexer.HideAll();
		}
		else
		{
			SetButtons();
		}
	}

	public void ShowStateOwned()
	{
		if (panel_L == 6)
		{
			subLeft.SetActive(value: false);
			panel_L = 0;
			if (indexer.superSub == 6)
			{
				indexer.HideAll();
			}
			else
			{
				SetButtons();
			}
			return;
		}
		indexer.HideAll();
		subLeft.SetActive(value: true);
		R_Icons.SetActive(value: false);
		C_Icons.SetActive(value: false);
		I_Icons.SetActive(value: false);
		Parks_Icons.SetActive(value: false);
		StateOwned_Icons.SetActive(value: true);
		M_Icons.SetActive(value: false);
		Leisure_Icons.SetActive(value: false);
		panel_L = 6;
		if (indexer.superSub < 7)
		{
			indexer.HideAll();
		}
		else
		{
			SetButtons();
		}
	}

	public void ShowRoads()
	{
		if (panel_R == 10)
		{
			subRight.SetActive(value: false);
			panel_R = 0;
			if (indexer.superSub == 10)
			{
				indexer.HideAll();
			}
			else
			{
				SetButtons();
			}
			return;
		}
		subRight.SetActive(value: true);
		Terrain_Icons.SetActive(value: false);
		Services_Icons.SetActive(value: false);
		Roads_Icons.SetActive(value: true);
		Gov_Icons.SetActive(value: false);
		Facilities_Icons.SetActive(value: false);
		Special_Icons.SetActive(value: false);
		Religion_Icons.SetActive(value: false);
		Transport_Icons.SetActive(value: false);
		panel_R = 10;
		if (indexer.superSub > 6)
		{
			indexer.HideAll();
		}
		else
		{
			SetButtons();
		}
	}

	public void ShowServices()
	{
		if (panel_R == 7)
		{
			subRight.SetActive(value: false);
			panel_R = 0;
			if (indexer.superSub == 7)
			{
				indexer.HideAll();
			}
			else
			{
				SetButtons();
			}
			return;
		}
		subRight.SetActive(value: true);
		Terrain_Icons.SetActive(value: false);
		Services_Icons.SetActive(value: true);
		Roads_Icons.SetActive(value: false);
		Gov_Icons.SetActive(value: false);
		Facilities_Icons.SetActive(value: false);
		Special_Icons.SetActive(value: false);
		Religion_Icons.SetActive(value: false);
		Transport_Icons.SetActive(value: false);
		panel_R = 7;
		if (indexer.superSub > 6)
		{
			indexer.HideAll();
		}
		else
		{
			SetButtons();
		}
	}

	public void ShowFacilities()
	{
		if (panel_R == 8)
		{
			subRight.SetActive(value: false);
			panel_R = 0;
			if (indexer.superSub == 8)
			{
				indexer.HideAll();
			}
			else
			{
				SetButtons();
			}
			return;
		}
		subRight.SetActive(value: true);
		Terrain_Icons.SetActive(value: false);
		Services_Icons.SetActive(value: false);
		Roads_Icons.SetActive(value: false);
		Gov_Icons.SetActive(value: false);
		Facilities_Icons.SetActive(value: true);
		Special_Icons.SetActive(value: false);
		Religion_Icons.SetActive(value: false);
		Transport_Icons.SetActive(value: false);
		panel_R = 8;
		if (indexer.superSub > 6)
		{
			indexer.HideAll();
		}
		else
		{
			SetButtons();
		}
	}

	public void ShowGovernment()
	{
		if (panel_R == 9)
		{
			subRight.SetActive(value: false);
			panel_R = 0;
			if (indexer.superSub == 9)
			{
				indexer.HideAll();
			}
			else
			{
				SetButtons();
			}
			return;
		}
		subRight.SetActive(value: true);
		Terrain_Icons.SetActive(value: false);
		Services_Icons.SetActive(value: false);
		Roads_Icons.SetActive(value: false);
		Gov_Icons.SetActive(value: true);
		Facilities_Icons.SetActive(value: false);
		Special_Icons.SetActive(value: false);
		Religion_Icons.SetActive(value: false);
		Transport_Icons.SetActive(value: false);
		panel_R = 9;
		if (indexer.superSub > 6)
		{
			indexer.HideAll();
		}
		else
		{
			SetButtons();
		}
	}

	public void ShowReligion()
	{
		if (panel_R == 13)
		{
			subRight.SetActive(value: false);
			panel_R = 0;
			if (indexer.superSub == 13)
			{
				indexer.HideAll();
			}
			else
			{
				SetButtons();
			}
			return;
		}
		subRight.SetActive(value: true);
		Terrain_Icons.SetActive(value: false);
		Services_Icons.SetActive(value: false);
		Roads_Icons.SetActive(value: false);
		Gov_Icons.SetActive(value: false);
		Facilities_Icons.SetActive(value: false);
		Special_Icons.SetActive(value: false);
		Religion_Icons.SetActive(value: true);
		Transport_Icons.SetActive(value: false);
		panel_R = 13;
		if (indexer.superSub < 13)
		{
			indexer.HideAll();
		}
		else
		{
			SetButtons();
		}
	}

	public void ShowTerrain()
	{
		if (panel_R == 11)
		{
			subRight.SetActive(value: false);
			panel_R = 0;
			if (indexer.superSub == 11)
			{
				indexer.HideAll();
			}
			else
			{
				SetButtons();
			}
			return;
		}
		subRight.SetActive(value: true);
		Terrain_Icons.SetActive(value: true);
		Services_Icons.SetActive(value: false);
		Roads_Icons.SetActive(value: false);
		Gov_Icons.SetActive(value: false);
		Facilities_Icons.SetActive(value: false);
		Special_Icons.SetActive(value: false);
		Religion_Icons.SetActive(value: false);
		Transport_Icons.SetActive(value: false);
		panel_R = 11;
		if (indexer.superSub > 6)
		{
			indexer.HideAll();
		}
		else
		{
			SetButtons();
		}
	}

	public void ShowSpecial()
	{
		if (panel_R == 12)
		{
			subRight.SetActive(value: false);
			panel_R = 0;
			if (indexer.superSub == 12)
			{
				indexer.HideAll();
			}
			else
			{
				SetButtons();
			}
			return;
		}
		subRight.SetActive(value: true);
		Terrain_Icons.SetActive(value: false);
		Services_Icons.SetActive(value: false);
		Roads_Icons.SetActive(value: false);
		Gov_Icons.SetActive(value: false);
		Facilities_Icons.SetActive(value: false);
		Special_Icons.SetActive(value: true);
		Religion_Icons.SetActive(value: false);
		Transport_Icons.SetActive(value: false);
		panel_R = 12;
		if (indexer.superSub > 6)
		{
			indexer.HideAll();
		}
		else
		{
			SetButtons();
		}
	}

	public void ShowTransport()
	{
		if (panel_R == 15)
		{
			subRight.SetActive(value: false);
			panel_R = 0;
			if (indexer.superSub == 15)
			{
				indexer.HideAll();
			}
			else
			{
				SetButtons();
			}
			return;
		}
		subRight.SetActive(value: true);
		Terrain_Icons.SetActive(value: false);
		Services_Icons.SetActive(value: false);
		Roads_Icons.SetActive(value: false);
		Gov_Icons.SetActive(value: false);
		Facilities_Icons.SetActive(value: false);
		Special_Icons.SetActive(value: false);
		Religion_Icons.SetActive(value: false);
		Transport_Icons.SetActive(value: true);
		panel_R = 15;
		if (indexer.superSub > 6)
		{
			indexer.HideAll();
		}
		else
		{
			SetButtons();
		}
	}

	public void SetButtons()
	{
		if (R_Icons.activeSelf && subLeft.activeSelf)
		{
			B_Res.image.sprite = Res_On;
		}
		else
		{
			B_Res.image.sprite = Res_Off;
		}
		if (C_Icons.activeSelf && subLeft.activeSelf)
		{
			B_Com.image.sprite = Com_On;
		}
		else
		{
			B_Com.image.sprite = Com_Off;
		}
		if (I_Icons.activeSelf && subLeft.activeSelf)
		{
			B_Ind.image.sprite = Ind_On;
		}
		else
		{
			B_Ind.image.sprite = Ind_Off;
		}
		if (Parks_Icons.activeSelf && subLeft.activeSelf)
		{
			B_Park.image.sprite = Park_On;
		}
		else
		{
			B_Park.image.sprite = Park_Off;
		}
		if (StateOwned_Icons.activeSelf && subLeft.activeSelf)
		{
			B_State.image.sprite = State_On;
		}
		else
		{
			B_State.image.sprite = State_Off;
		}
		if (M_Icons.activeSelf && subLeft.activeSelf)
		{
			B_Mix.image.sprite = Mix_On;
		}
		else
		{
			B_Mix.image.sprite = Mix_Off;
		}
		if (Leisure_Icons.activeSelf && subLeft.activeSelf)
		{
			B_Leisure.image.sprite = Leisure_On;
		}
		else
		{
			B_Leisure.image.sprite = Leisure_Off;
		}
		if (Indexer.Index == 9)
		{
			B_Buldoze.image.sprite = Buldoze_On;
		}
		else
		{
			B_Buldoze.image.sprite = Buldoze_Off;
		}
		if (Terrain_Icons.activeSelf && subRight.activeSelf)
		{
			B_Terra.image.sprite = Terra_On;
		}
		else
		{
			B_Terra.image.sprite = Terra_Off;
		}
		if (Services_Icons.activeSelf && subRight.activeSelf)
		{
			B_Serv.image.sprite = Serv_On;
		}
		else
		{
			B_Serv.image.sprite = Serv_Off;
		}
		if (Roads_Icons.activeSelf && subRight.activeSelf)
		{
			B_Roads.image.sprite = Roads_On;
		}
		else
		{
			B_Roads.image.sprite = Roads_Off;
		}
		if (Gov_Icons.activeSelf && subRight.activeSelf)
		{
			B_Gov.image.sprite = Gov_On;
		}
		else
		{
			B_Gov.image.sprite = Gov_Off;
		}
		if (Facilities_Icons.activeSelf && subRight.activeSelf)
		{
			B_Pow.image.sprite = Pow_On;
		}
		else
		{
			B_Pow.image.sprite = Pow_Off;
		}
		if (Special_Icons.activeSelf && subRight.activeSelf)
		{
			B_Spec.image.sprite = Spec_On;
		}
		else
		{
			B_Spec.image.sprite = Spec_Off;
		}
		if (Religion_Icons.activeSelf && subRight.activeSelf)
		{
			B_Rel.image.sprite = Rel_On;
		}
		else
		{
			B_Rel.image.sprite = Rel_Off;
		}
		if (Transport_Icons.activeSelf && subRight.activeSelf)
		{
			B_Trans.image.sprite = Trans_On;
		}
		else
		{
			B_Trans.image.sprite = Trans_Off;
		}
	}
}
