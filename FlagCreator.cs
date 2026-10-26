using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlagCreator : MonoBehaviour
{
	private string selectedColor = "A";

	private string selectedEmblemPosition = "C";

	public Material FlagA;

	public Material FlagB;

	public Material FlagC;

	public List<Button> allFieldsButtons;

	public List<GameObject> allFieldsGO;

	public GameObject FieldTypeSelector;

	public List<Image> allEmblemsImage;

	public List<Button> allEmblemsGO;

	public GameObject EmblemTypeSelector;

	public List<GameObject> emblemPositions;

	public Button B_EmblemLeft;

	public Image I_EmblemLeft;

	private int selectedLeft;

	private float scaleLeft = 0.75f;

	public Button B_EmblemRight;

	public Image I_EmblemRight;

	private int selectedRight;

	private float scaleRight = 0.75f;

	public Button B_EmblemCenter;

	public Image I_EmblemCenter;

	private int selectedCenter;

	private float scaleCenter = 0.75f;

	public GameObject EmblemPositionSelector;

	public Button B_ColorA;

	private int selectedColorA;

	public Button B_ColorB;

	private int selectedColorB;

	public Button B_ColorC;

	private int selectedColorC;

	public GameObject ColorSelector;

	public List<Button> allColorsGO;

	public GameObject ColorTypeSelector;

	public List<Color> allColorsRGB;

	public FlagCapture flagCapture;

	public WorldRegion worldRegion;

	public int width;

	public int height;

	public void SetField(int fieldInt)
	{
		foreach (GameObject item in allFieldsGO)
		{
			item.gameObject.SetActive(value: false);
		}
		allFieldsGO[fieldInt].gameObject.SetActive(value: true);
		FieldTypeSelector.transform.position = allFieldsButtons[fieldInt].transform.position;
	}

	public void ClickEmblemLeft()
	{
		EmblemPositionSelector.transform.position = B_EmblemLeft.transform.position;
		selectedEmblemPosition = "L";
		EmblemTypeSelector.transform.position = allEmblemsGO[selectedLeft].transform.position;
	}

	public void ClickEmblemCenter()
	{
		EmblemPositionSelector.transform.position = B_EmblemCenter.transform.position;
		selectedEmblemPosition = "C";
		EmblemTypeSelector.transform.position = allEmblemsGO[selectedCenter].transform.position;
	}

	public void ClickEmblemRight()
	{
		EmblemPositionSelector.transform.position = B_EmblemRight.transform.position;
		selectedEmblemPosition = "R";
		EmblemTypeSelector.transform.position = allEmblemsGO[selectedRight].transform.position;
	}

	public void SetEmblem(int emblemInt)
	{
		if (selectedEmblemPosition == "L")
		{
			selectedLeft = emblemInt;
			EmblemTypeSelector.transform.position = allEmblemsGO[emblemInt].transform.position;
			I_EmblemLeft.sprite = allEmblemsImage[emblemInt].sprite;
		}
		else if (selectedEmblemPosition == "C")
		{
			selectedCenter = emblemInt;
			EmblemTypeSelector.transform.position = allEmblemsGO[emblemInt].transform.position;
			I_EmblemCenter.sprite = allEmblemsImage[emblemInt].sprite;
		}
		else if (selectedEmblemPosition == "R")
		{
			selectedRight = emblemInt;
			EmblemTypeSelector.transform.position = allEmblemsGO[emblemInt].transform.position;
			I_EmblemRight.sprite = allEmblemsImage[emblemInt].sprite;
		}
	}

	public void ClickColorA()
	{
		ColorSelector.transform.position = B_ColorA.transform.position;
		selectedColor = "A";
		ColorTypeSelector.transform.position = allColorsGO[selectedColorA].transform.position;
	}

	public void ClickColorB()
	{
		ColorSelector.transform.position = B_ColorB.transform.position;
		selectedColor = "B";
		ColorTypeSelector.transform.position = allColorsGO[selectedColorB].transform.position;
	}

	public void ClickColorC()
	{
		ColorSelector.transform.position = B_ColorC.transform.position;
		selectedColor = "C";
		ColorTypeSelector.transform.position = allColorsGO[selectedColorC].transform.position;
	}

	public void SetColor(int colorInt)
	{
		if (selectedColor == "A")
		{
			selectedColorA = colorInt;
			ColorTypeSelector.transform.position = allColorsGO[colorInt].transform.position;
			FlagA.color = allColorsRGB[colorInt];
		}
		else if (selectedColor == "B")
		{
			selectedColorB = colorInt;
			ColorTypeSelector.transform.position = allColorsGO[colorInt].transform.position;
			FlagB.color = allColorsRGB[colorInt];
		}
		else if (selectedColor == "C")
		{
			selectedColorC = colorInt;
			ColorTypeSelector.transform.position = allColorsGO[colorInt].transform.position;
			FlagC.color = allColorsRGB[colorInt];
		}
	}

	public void ScaleUp()
	{
		if (selectedEmblemPosition == "L")
		{
			scaleLeft += 0.05f;
			UpdateScale("L");
		}
		else if (selectedEmblemPosition == "C")
		{
			scaleCenter += 0.05f;
			UpdateScale("C");
		}
		else if (selectedEmblemPosition == "R")
		{
			scaleRight += 0.05f;
			UpdateScale("R");
		}
	}

	public void ScaleDown()
	{
		if (selectedEmblemPosition == "L")
		{
			scaleLeft -= 0.05f;
			UpdateScale("L");
		}
		else if (selectedEmblemPosition == "C")
		{
			scaleCenter -= 0.05f;
			UpdateScale("C");
		}
		else if (selectedEmblemPosition == "R")
		{
			scaleRight -= 0.05f;
			UpdateScale("R");
		}
	}

	private void UpdateScale(string emblem)
	{
		switch (emblem)
		{
		case "L":
			if (scaleLeft < 0.1f)
			{
				scaleLeft = 0.1f;
			}
			else if (scaleLeft > 0.5f)
			{
				scaleLeft = 0.5f;
			}
			I_EmblemLeft.GetComponent<RectTransform>().localScale = new Vector3(scaleLeft, scaleLeft, 1f);
			break;
		case "C":
			if (scaleCenter < 0.1f)
			{
				scaleCenter = 0.1f;
			}
			else if (scaleCenter > 0.75f)
			{
				scaleCenter = 0.75f;
			}
			I_EmblemCenter.GetComponent<RectTransform>().localScale = new Vector3(scaleCenter, scaleCenter, 1f);
			break;
		case "R":
			if (scaleRight < 0.1f)
			{
				scaleRight = 0.1f;
			}
			else if (scaleRight > 0.5f)
			{
				scaleRight = 0.5f;
			}
			I_EmblemRight.GetComponent<RectTransform>().localScale = new Vector3(scaleRight, scaleRight, 1f);
			break;
		}
	}

	public void SetEmblemPosition(int position)
	{
		if (selectedEmblemPosition == "L")
		{
			I_EmblemLeft.transform.position = new Vector3(I_EmblemLeft.transform.position.x, emblemPositions[position].transform.position.y, 0f);
		}
		else if (selectedEmblemPosition == "C")
		{
			I_EmblemCenter.transform.position = new Vector3(I_EmblemCenter.transform.position.x, emblemPositions[position].transform.position.y, 0f);
		}
		else if (selectedEmblemPosition == "R")
		{
			I_EmblemRight.transform.position = new Vector3(I_EmblemRight.transform.position.x, emblemPositions[position].transform.position.y, 0f);
		}
	}

	public void SaveFlag()
	{
		worldRegion.BlurrOff();
		flagCapture.Capture();
	}

	public void Open()
	{
		gameObject.SetActive(value: true);
		StartCoroutine(worldRegion.BlurrIn());
	}

	public void Close()
	{
		worldRegion.BlurrOff();
		gameObject.SetActive(value: false);
	}
}
