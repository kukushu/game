using UnityEngine;

public class LeftSmartPanel : MonoBehaviour
{
	public GameObject Panel;

	private int Type;

	private bool isActive;

	public void OpenPanelResidential()
	{
		if (Type != 1 && isActive)
		{
			isActive = true;
			Type = 1;
		}
		else if (Type == 1 && isActive)
		{
			isActive = false;
			Panel.SetActive(value: false);
			Type = 0;
			Indexer.Index = 0;
		}
		else if (!isActive)
		{
			isActive = true;
			Panel.SetActive(value: true);
			Type = 1;
		}
	}

	public void OpenPanelCommercial()
	{
		if (Type != 2 && isActive)
		{
			isActive = true;
			Type = 2;
		}
		else if (Type == 2 && isActive)
		{
			isActive = false;
			Panel.SetActive(value: false);
			Type = 0;
			Indexer.Index = 0;
		}
		else if (!isActive)
		{
			isActive = true;
			Panel.SetActive(value: true);
			Type = 2;
		}
	}

	public void OpenPanelIndustrial()
	{
		if (Type != 3 && isActive)
		{
			isActive = true;
			Type = 3;
		}
		else if (Type == 3 && isActive)
		{
			isActive = false;
			Panel.SetActive(value: false);
			Type = 0;
			Indexer.Index = 0;
		}
		else if (!isActive)
		{
			isActive = true;
			Panel.SetActive(value: true);
			Type = 3;
		}
	}

	public void OpenPanelAgriculture()
	{
		if (Type != 4 && isActive)
		{
			isActive = true;
			Type = 4;
		}
		else if (Type == 4 && isActive)
		{
			isActive = false;
			Panel.SetActive(value: false);
			Type = 0;
			Indexer.Index = 0;
		}
		else if (!isActive)
		{
			isActive = true;
			Panel.SetActive(value: true);
			Type = 4;
		}
	}

	public void OpenPanelReligion()
	{
		if (Type != 5 && isActive)
		{
			isActive = true;
			Type = 5;
		}
		else if (Type == 5 && isActive)
		{
			isActive = false;
			Panel.SetActive(value: false);
			Type = 0;
			Indexer.Index = 0;
		}
		else if (!isActive)
		{
			isActive = true;
			Panel.SetActive(value: true);
			Type = 5;
		}
	}
}
