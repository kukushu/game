using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NationMenu : MonoBehaviour
{
	public GameObject NationUI;

	public GameObject newNationUI;

	public GameObject loadNationUI;

	public GameObject selectorGeo;

	public GameObject selectorPol;

	public GameObject selectorMap;

	public GameObject selectorDiff;

	public GameObject selectorSize;

	public TMP_Dropdown dropdownNations;

	public TMP_InputField inputName;

	public List<GameObject> geoGOs;

	public List<GameObject> polGOs;

	public List<GameObject> mapGOs;

	public List<GameObject> diffGOs;

	public List<GameObject> sizeGOs;

	public Button newButton;

	public Button loadButton;

	public Sprite Tab_Active;

	public Sprite Tab_Inactive;

	public SoundManager sm;

	private int geoType = 2;

	private int mapType;

	private int polType;

	private int diffType = 2;

	private int sizeType = 1;

	public GameObject flagCreatorGO;

	public Image mapPreview;

	public RTS_Camera camScript;

	public FlagCapture flagCapture;

	public WorldRegion worldRegion;

	public RegionInteraction ri;

	public TextMeshProUGUI Bal;

	public TextMeshProUGUI Inf;

	public TextMeshProUGUI LCdem;

	public TextMeshProUGUI Rdem;

	public TextMeshProUGUI Bonus;

	public TextLocaliser difficultyDetails;

	public TextMeshProUGUI difficultyText;

	public Image gridPreview;

	public Sprite preview320x;

	public Sprite preview512x;

	public TextMeshProUGUI deleteTip;

	public void NewNation()
	{
		flagCreatorGO.SetActive(value: false);
		NationUI.SetActive(value: true);
		newNationUI.SetActive(value: true);
		loadNationUI.SetActive(value: false);
		newButton.image.sprite = Tab_Active;
		loadButton.image.sprite = Tab_Inactive;
		Inf.text = LOCSYS.Translate("CVhigh");
		SetGeography(6);
		StartCoroutine(worldRegion.BlurrIn());
		camScript.BlockCamera();
		ri.censusOpen = true;
	}

	public void LoadNation()
	{
		NationUI.SetActive(value: true);
		newNationUI.SetActive(value: false);
		loadNationUI.SetActive(value: true);
		loadButton.image.sprite = Tab_Active;
		newButton.image.sprite = Tab_Inactive;
		StartCoroutine(worldRegion.BlurrIn());
		camScript.BlockCamera();
		ri.censusOpen = true;
		ListSavedNations();
	}

	public void TabLoadNation()
	{
		NationUI.SetActive(value: true);
		newNationUI.SetActive(value: false);
		loadNationUI.SetActive(value: true);
		loadButton.image.sprite = Tab_Active;
		newButton.image.sprite = Tab_Inactive;
		ListSavedNations();
	}

	public void TabNewNation()
	{
		NationUI.SetActive(value: true);
		newNationUI.SetActive(value: true);
		loadNationUI.SetActive(value: false);
		loadButton.image.sprite = Tab_Inactive;
		newButton.image.sprite = Tab_Active;
		SetGeography(6);
		camScript.BlockCamera();
		ri.censusOpen = true;
	}

	public void CloseAll()
	{
		if (DataRegion.savedNations.Count >= 1)
		{
			NationUI.SetActive(value: false);
			worldRegion.BlurrOff();
			camScript.UnblockCamera();
			ri.censusOpen = false;
		}
	}

	public void SetGeography(int type)
	{
		geoType = type;
		selectorGeo.transform.position = geoGOs[type].transform.position;
		SetMap(0);
	}

	public void SetMap(int type)
	{
		mapType = type;
		selectorMap.transform.position = mapGOs[type].transform.position;
		if (geoType < 8)
		{
			mapPreview.sprite = Resources.Load<Sprite>("Maps/Preview/" + Dico.Map_Geography[geoType] + Dico.Map_Variation[mapType]);
			return;
		}
		string text = Application.persistentDataPath + "/Map " + type + ".png";
		if (!File.Exists(text))
		{
			Debug.Log("No image found at " + text);
			mapType = 1;
			SetGeography(6);
		}
		else
		{
			byte[] data = File.ReadAllBytes(text);
			Texture2D texture2D = new Texture2D(1, 1);
			texture2D.LoadImage(data);
			Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
			mapPreview.sprite = sprite;
		}
	}

	public void SetPol(int type)
	{
		polType = type;
		selectorPol.transform.position = polGOs[type].transform.position;
	}

	public void SetDiff(int type)
	{
		diffType = type;
		selectorDiff.transform.position = diffGOs[type].transform.position;
		switch (type)
		{
		case 0:
			difficultyDetails.key = "easyDD";
			difficultyText.text = LOCSYS.Translate("easyDD");
			break;
		case 1:
			difficultyDetails.key = "norDD";
			difficultyText.text = LOCSYS.Translate("norDD");
			break;
		default:
			difficultyDetails.key = "realDD";
			difficultyText.text = LOCSYS.Translate("realDD");
			break;
		}
	}

	public void SetSize(int type)
	{
		sizeType = type;
		selectorSize.transform.position = sizeGOs[type].transform.position;
		if (type == 0)
		{
			gridPreview.sprite = preview320x;
		}
		else
		{
			gridPreview.sprite = preview512x;
		}
	}

	public void Randomize()
	{
		int index = UnityEngine.Random.Range(0, Dico.nationName.Count);
		inputName.text = Dico.nationName[index];
	}

	public void CreateNation()
	{
		worldRegion.transition.gameObject.SetActive(value: true);
		worldRegion.transition.alpha = 1f;
		worldRegion.loadingBar.value = 0f;
		worldRegion.loadingLoc.key = "Load0";
		StartCoroutine(GoToNewNation());
	}

	private IEnumerator GoToNewNation()
	{
		if (inputName.text.Length < 3 || inputName.text.EndsWith(" "))
		{
			Randomize();
		}
		foreach (string savedNation in DataRegion.savedNations)
		{
			if (savedNation == inputName.text)
			{
				Randomize();
				Debug.Log("NATION WITH SAME NAME EXISTS! NEW NAME SET!");
			}
		}
		yield return new WaitForSecondsRealtime(0.4f);
		string value = (DataRegion.mapFile = ((geoType >= 8) ? (Dico.Map_Geography[geoType] + " " + mapType) : (Dico.Map_Geography[geoType] + " " + Dico.Map_Variation[mapType])));
		DataRegion.nationName = inputName.text;
		DataRegion.ecoSettings[0] = diffType;
		if (sizeType == 0)
		{
			DataRegion.ecoSettings[1] = 8f;
		}
		else
		{
			DataRegion.ecoSettings[1] = 5f;
		}
		Debug.Log("New Nation with difficulty " + diffType + ", Number of cities= " + DataRegion.ecoSettings[1]);
		if (diffType == 0)
		{
			DataRegion.ecoSettings[4] = 0f;
		}
		else if (diffType == 1)
		{
			DataRegion.ecoSettings[4] = 10f;
		}
		else
		{
			DataRegion.ecoSettings[4] = 25f;
		}
		DataRegion.ecoSettings[5] = polType;
		DataRegion.savedNations.Add(inputName.text);
		ES3.Save(DataRegion.nationName + "ecoSettings", DataRegion.ecoSettings);
		ES3.Save(inputName.text + "MapFile", value);
		ES3.Save("nationName", DataRegion.nationName);
		ES3.Save("savedNations", DataRegion.savedNations);
		worldRegion.regionUI.step = 1;
		CloseAll();
		DataRegion.ResetValues();
		flagCapture.CreateDefaultFlag();
		SceneManager.LoadScene("RegionView");
	}

	public void DeleteNation()
	{
		string text = dropdownNations.options[dropdownNations.value].text;
		Debug.Log("Try to erase= " + text);
		if (DataRegion.nationName == text)
		{
			deleteTip.text = LOCSYS.Translate("cantdel");
			sm.Play("Impractical");
			return;
		}
		DataRegion.savedNations.Remove(text);
		ES3.DeleteKey(text + "cities");
		ES3.DeleteKey(text + "treasury");
		ES3.DeleteKey(text + "policies");
		ES3.DeleteKey(text + "capital");
		ES3.DeleteKey(text + "built");
		ES3.DeleteKey(text + "HQstage");
		ES3.DeleteKey(text + "MapFile");
		ES3.DeleteKey(text + "LandPrice");
		ES3.DeleteKey(text + "ExchangeRate");
		ES3.DeleteKey(text + "Stability");
		ES3.DeleteKey(text + "FI");
		ES3.DeleteKey(text + "CR");
		ES3.DeleteKey(text + "Version");
		ES3.DeleteDirectory(text);
		ES3.DeleteFile(text + "blockData.es3");
		ES3.DeleteFile(text + "Flag.png");
		ES3.Save("savedNations", DataRegion.savedNations);
		deleteTip.text = text + ": " + LOCSYS.Translate("nationRemoved");
		sm.Play("Option2");
		ListSavedNations();
	}

	private void ListSavedNations()
	{
		DataRegion.LoadListOfSavedNations();
		dropdownNations.ClearOptions();
		List<string> list = new List<string>();
		for (int i = 0; i < DataRegion.savedNations.Count; i++)
		{
			string item = DataRegion.savedNations[i];
			list.Add(item);
		}
		dropdownNations.AddOptions(list);
		dropdownNations.value = 0;
		dropdownNations.RefreshShownValue();
	}

	public void GoToNation()
	{
		Debug.Log("Going to Nation: " + dropdownNations.options[dropdownNations.value].text.ToString());
		StartCoroutine(SwitchNation());
	}

	private IEnumerator SwitchNation()
	{
		worldRegion.LoadingTask.text = LOCSYS.Translate("Load6");
		DataRegion.dataLoaded = false;
		StartCoroutine(worldRegion.FadeInScreen());
		yield return new WaitForSeconds(0.4f);
		DataRegion.SaveDataRegion();
		try
		{
			SceneManager.UnloadSceneAsync("Main_Scene_March2020");
		}
		catch (Exception)
		{
			Debug.Log("Unable to unload Main Scene");
		}
		yield return null;
		DataRegion.nationName = dropdownNations.options[dropdownNations.value].text;
		worldRegion.LoadingTask.text = LOCSYS.Translate("Start1") + " " + DataRegion.nationName;
		ES3.Save("nationName", DataRegion.nationName);
		SceneManager.LoadScene("RegionView");
	}
}
