using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegionUI : MonoBehaviour
{
	public TMP_FontAsset cityFont;

	public Texture2D trafficTex;

	public Material projectorMat;

	public Color32 roadColor;

	public Color32 capitalColor;

	public TextMeshProUGUI nationName;

	public TextMeshProUGUI pop;

	public TextMeshProUGUI treasury;

	public TextMeshProUGUI power;

	public TextMeshProUGUI cr;

	public TextMeshProUGUI fi;

	public TextMeshProUGUI stab;

	public TextMeshProUGUI er;

	public Image FlagBanner;

	public Image FlagLoading;

	public int step;

	public GameObject arrow;

	public MessageBox TutoBox;

	public void CreateText(int pop, string name, Vector3 pos, bool isCapital)
	{
		GameObject gameObject = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Materials/CityName"));
		gameObject.transform.position = pos;
		TextMeshPro component = gameObject.GetComponent<TextMeshPro>();
		component.text = name + Environment.NewLine + pop.ToString("N0");
		if (isCapital)
		{
			component.color = Color.red;
		}
		else
		{
			component.color = Color.white;
		}
	}

	public void ProjectRoads()
	{
		int num = WorldRegion.chunkSize * WorldRegion.worldSize * 8;
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				BlockLight blockLight = WorldRegion.blockData[i, j];
				if (blockLight == null)
				{
					trafficTex.SetPixel(i, j, new Color32(0, 0, 0, 0));
				}
				else if (blockLight.t[0])
				{
					trafficTex.SetPixel(i, j, roadColor);
				}
				else
				{
					trafficTex.SetPixel(i, j, new Color32(0, 0, 0, 0));
				}
			}
		}
		trafficTex.wrapMode = TextureWrapMode.Clamp;
		trafficTex.Apply();
		projectorMat.SetTexture("_MainTex", trafficTex);
	}

	public void SetFlags()
	{
		if (DataRegion.FlagTexture != null)
		{
			Sprite sprite = Sprite.Create(DataRegion.FlagTexture, new Rect(0f, 0f, DataRegion.FlagTexture.width, DataRegion.FlagTexture.height), new Vector2(0.5f, 0.5f));
			FlagBanner.sprite = sprite;
			FlagLoading.sprite = sprite;
		}
	}

	public void ExitToDesktop()
	{
		Application.Quit();
	}

	public void SetArrowStep(int s)
	{
		if (SettingsStatic.tutorial)
		{
			if (step == 1 && s == 0)
			{
				arrow.SetActive(value: true);
				TutoBox.gameObject.SetActive(value: true);
				TutoBox.PopTitle.text = LOCSYS.Translate("RT1");
				TutoBox.PopText.text = LOCSYS.Translate("RM1");
				TutoBox.PopGoal.text = LOCSYS.Translate("RG1");
			}
			else if (step == 1 && s == 1)
			{
				arrow.SetActive(value: false);
			}
			else if (step == 1 && s == 2)
			{
				TutoBox.PopTitle.text = LOCSYS.Translate("RT2");
				TutoBox.PopText.text = LOCSYS.Translate("RM2");
				TutoBox.PopGoal.text = LOCSYS.Translate("RG2");
			}
		}
	}
}
