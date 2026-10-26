using System.Collections;
using System.Collections.Generic;
using EntroPi;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using VolumetricFogAndMist;

public class SettingsUI : MonoBehaviour
{
	public GameObject graphicsPanel;

	public GameObject gameplayPanel;

	public Button graphicsButton;

	public Button gameplayButton;

	public Sprite Tab_Active;

	public Sprite Tab_Inactive;

	public List<GameObject> selectors;

	public List<GameObject> B_Presets;

	public List<GameObject> B_Fullscreen;

	public List<GameObject> B_Shadows;

	public List<GameObject> B_Textures;

	public List<GameObject> B_LOD;

	public List<GameObject> B_Vehicles;

	public List<GameObject> B_Peds;

	public List<GameObject> B_SSR;

	public List<GameObject> B_AO;

	public List<GameObject> B_Bloom;

	public List<GameObject> B_Scattering;

	public List<GameObject> B_CloudsNation;

	public List<GameObject> B_CloudsCity;

	public List<GameObject> B_CloudShadows;

	public List<GameObject> B_Vsync;

	public List<GameObject> B_MaxFPS;

	public List<GameObject> B_Anti;

	public TMP_Dropdown resolutionDropdown;

	private Resolution[] resolutions;

	private int line;

	private int option;

	public bool isCity;

	public PostProcessVolume ppv;

	public PostProcessLayer ppl;

	public Camera cam;

	public GameObject directionalLight;

	public Light lightlight;

	public CTAA_PC ctaa;

	public VolumetricFogPosT cityFog;

	public VolumetricFogPosT nationFog;

	public ReflectionProbe reflectionProbe;

	public WorldRegion worldRegion;

	public Indexer indexer;

	public Slider UISlider;

	public Slider musicSlider;

	public Slider citySlider;

	public Slider natureSlider;

	public Slider vehiclesSlider;

	public Slider mouseSlider;

	public Slider keyboardSlider;

	public GameObject tutoButtonON;

	public GameObject tutoButtonOFF;

	public GameObject alertButtonON;

	public GameObject alertButtonOFF;

	public GameObject infoButtonON;

	public GameObject infoButtonOFF;

	public GameObject scrollButtonON;

	public GameObject scrollButtonOFF;

	public GameObject azertyON;

	public GameObject azertyOFF;

	public GameObject smoothON;

	public GameObject smoothOFF;

	public GameObject tutoSelector;

	public GameObject alertSelector;

	public GameObject infoSelector;

	public GameObject scrollSelector;

	public GameObject azertySelector;

	public GameObject smoothSelector;

	public Messages messages;

	public TextMeshProUGUI forward;

	public TextMeshProUGUI left;

	public TextMeshProUGUI zoomIn;

	public TextMeshProUGUI rotateLeft;

	public PlaylistRegion playlist;

	public RTS_Camera cameraScript;

	private void RefreshResolutions()
	{
		resolutions = Screen.resolutions;
		resolutionDropdown.ClearOptions();
		List<string> list = new List<string>();
		int value = 0;
		for (int i = 0; i < resolutions.Length; i++)
		{
			string item = resolutions[i].width + " x " + resolutions[i].height + " " + resolutions[i].refreshRate + "Hz";
			list.Add(item);
			if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
			{
				value = i;
			}
		}
		resolutionDropdown.AddOptions(list);
		resolutionDropdown.value = value;
		resolutionDropdown.RefreshShownValue();
	}

	public void OpenSettingsPanel()
	{
		if (gameObject.activeInHierarchy)
		{
			CloseSettings();
			return;
		}
		gameObject.SetActive(value: true);
		Indexer.sliderOnScreen = true;
		OpenGameplayPanel();
		StartCoroutine(LoadSettings());
		RefreshResolutions();
	}

	public void OpenGameplayPanel()
	{
		graphicsPanel.SetActive(value: false);
		gameplayPanel.SetActive(value: true);
		gameplayButton.image.sprite = Tab_Active;
		graphicsButton.image.sprite = Tab_Inactive;
	}

	public void OpenGraphicsPanel()
	{
		gameplayPanel.SetActive(value: false);
		graphicsPanel.SetActive(value: true);
		graphicsButton.image.sprite = Tab_Active;
		gameplayButton.image.sprite = Tab_Inactive;
	}

	public void SetLine(int l)
	{
		line = l;
	}

	public void SetOption(int o)
	{
		option = o;
		if (line == 0)
		{
			SettingsStatic.graphicsOption[0] = option;
			selectors[0].SetActive(value: true);
			ApplyPreset(o);
		}
		else
		{
			SettingsStatic.graphicsOption[line] = 4;
			selectors[0].SetActive(value: false);
		}
	}

	public void ApplyPreset(int preset)
	{
		if (preset == 0)
		{
			SettingsStatic.graphicsOption[1] = 0;
			SettingsStatic.graphicsOption[2] = 1;
			SettingsStatic.graphicsOption[3] = 0;
			SettingsStatic.graphicsOption[4] = 0;
			SettingsStatic.graphicsOption[5] = 0;
			SettingsStatic.graphicsOption[6] = 0;
			SettingsStatic.graphicsOption[7] = 1;
			SettingsStatic.graphicsOption[8] = 0;
			SettingsStatic.graphicsOption[9] = 0;
			SettingsStatic.graphicsOption[10] = 0;
			SettingsStatic.graphicsOption[11] = 0;
			SettingsStatic.graphicsOption[12] = 0;
			SettingsStatic.graphicsOption[13] = 0;
			SettingsStatic.graphicsOption[14] = 1;
			SettingsStatic.graphicsOption[15] = 1;
			SettingsStatic.graphicsOption[16] = 1;
		}
		if (preset == 1)
		{
			SettingsStatic.graphicsOption[1] = 1;
			SettingsStatic.graphicsOption[2] = 2;
			SettingsStatic.graphicsOption[3] = 1;
			SettingsStatic.graphicsOption[4] = 0;
			SettingsStatic.graphicsOption[5] = 0;
			SettingsStatic.graphicsOption[6] = 1;
			SettingsStatic.graphicsOption[7] = 2;
			SettingsStatic.graphicsOption[8] = 1;
			SettingsStatic.graphicsOption[9] = 1;
			SettingsStatic.graphicsOption[10] = 1;
			SettingsStatic.graphicsOption[11] = 0;
			SettingsStatic.graphicsOption[12] = 1;
			SettingsStatic.graphicsOption[13] = 0;
			SettingsStatic.graphicsOption[14] = 1;
			SettingsStatic.graphicsOption[15] = 1;
			SettingsStatic.graphicsOption[16] = 1;
		}
		if (preset == 2)
		{
			SettingsStatic.graphicsOption[1] = 2;
			SettingsStatic.graphicsOption[2] = 3;
			SettingsStatic.graphicsOption[3] = 2;
			SettingsStatic.graphicsOption[4] = 1;
			SettingsStatic.graphicsOption[5] = 1;
			SettingsStatic.graphicsOption[6] = 2;
			SettingsStatic.graphicsOption[7] = 2;
			SettingsStatic.graphicsOption[8] = 1;
			SettingsStatic.graphicsOption[9] = 1;
			SettingsStatic.graphicsOption[10] = 1;
			SettingsStatic.graphicsOption[11] = 1;
			SettingsStatic.graphicsOption[12] = 1;
			SettingsStatic.graphicsOption[13] = 0;
			SettingsStatic.graphicsOption[14] = 0;
			SettingsStatic.graphicsOption[15] = 1;
			SettingsStatic.graphicsOption[16] = 2;
		}
		if (preset == 3)
		{
			SettingsStatic.graphicsOption[1] = 2;
			SettingsStatic.graphicsOption[2] = 3;
			SettingsStatic.graphicsOption[3] = 3;
			SettingsStatic.graphicsOption[4] = 3;
			SettingsStatic.graphicsOption[5] = 3;
			SettingsStatic.graphicsOption[6] = 3;
			SettingsStatic.graphicsOption[7] = 2;
			SettingsStatic.graphicsOption[8] = 1;
			SettingsStatic.graphicsOption[9] = 3;
			SettingsStatic.graphicsOption[10] = 1;
			SettingsStatic.graphicsOption[11] = 1;
			SettingsStatic.graphicsOption[12] = 1;
			SettingsStatic.graphicsOption[13] = 0;
			SettingsStatic.graphicsOption[14] = 1;
			SettingsStatic.graphicsOption[15] = 1;
			SettingsStatic.graphicsOption[16] = 2;
		}
		StartCoroutine(LoadSettings());
	}

	public IEnumerator LoadSettings()
	{
		UISlider.value = SettingsStatic.UIVolume;
		musicSlider.value = SettingsStatic.MusicVolume;
		natureSlider.value = SettingsStatic.NatureVolume;
		citySlider.value = SettingsStatic.CityVolume;
		vehiclesSlider.value = SettingsStatic.Carsvolume;
		mouseSlider.value = SettingsStatic.mouseSense;
		keyboardSlider.value = SettingsStatic.keyboardSense;
		SetTutorial(SettingsStatic.tutorial);
		SetAlert(SettingsStatic.redAlerts);
		SetInfo(SettingsStatic.blueInfo);
		SetScroll(SettingsStatic.edgeScroll);
		SetAzerty(SettingsStatic.azerty);
		if (SettingsStatic.smoothLevel < 100)
		{
			SetSmooth(active: false);
		}
		else
		{
			SetSmooth(active: true);
		}
		for (int i = 0; i <= 16; i++)
		{
			yield return new WaitForEndOfFrame();
			line = i;
			option = SettingsStatic.graphicsOption[i];
			ChangeGraphicsSetting();
		}
		yield return new WaitForEndOfFrame();
	}

	public void SetResolution()
	{
		int value = resolutionDropdown.value;
		bool fullscreen = SettingsStatic.graphicsOption[15] == 1;
		Resolution resolution = resolutions[value];
		UnityEngine.Debug.Log("Setting and Saving new resolution: " + resolution.width + " x " + resolution.height);
		Screen.SetResolution(resolution.width, resolution.height, fullscreen);
		SettingsStatic.resolutionWidth = resolution.width;
		SettingsStatic.reolutionHeight = resolution.height;
	}

	public void ChangeGraphicsSetting()
	{
		Vector2Int vector2Int = new Vector2Int(line, option);
		SettingsStatic.graphicsOption[line] = option;
		if (vector2Int.x == 0)
		{
			if (vector2Int.y == 4)
			{
				selectors[line].SetActive(value: false);
				return;
			}
			selectors[line].SetActive(value: true);
			selectors[line].transform.position = B_Presets[option].transform.position;
		}
		else if (vector2Int.x == 15)
		{
			selectors[line].transform.position = B_Fullscreen[option].transform.position;
			if (vector2Int.y == 0)
			{
				Screen.SetResolution(SettingsStatic.resolutionWidth, SettingsStatic.reolutionHeight, fullscreen: false);
			}
			else if (vector2Int.y == 1)
			{
				Screen.SetResolution(SettingsStatic.resolutionWidth, SettingsStatic.reolutionHeight, fullscreen: true);
			}
		}
		else if (vector2Int.x == 1)
		{
			selectors[line].transform.position = B_Shadows[option].transform.position;
			if (vector2Int.y == 0)
			{
				QualitySettings.shadows = ShadowQuality.Disable;
				lightlight.shadowCustomResolution = 2048;
			}
			else if (vector2Int.y == 1)
			{
				QualitySettings.shadowCascades = 4;
				QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
				QualitySettings.shadows = ShadowQuality.HardOnly;
				lightlight.shadowCustomResolution = 4096;
			}
			else if (vector2Int.y == 2)
			{
				QualitySettings.shadowCascades = 4;
				QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
				QualitySettings.shadows = ShadowQuality.HardOnly;
				lightlight.shadowCustomResolution = 8192;
			}
		}
		else if (vector2Int.x == 2)
		{
			selectors[line].transform.position = B_Textures[option].transform.position;
			if (vector2Int.y == 0)
			{
				QualitySettings.masterTextureLimit = 3;
			}
			else if (vector2Int.y == 1)
			{
				QualitySettings.masterTextureLimit = 2;
			}
			else if (vector2Int.y == 2)
			{
				QualitySettings.masterTextureLimit = 1;
			}
			else if (vector2Int.y == 3)
			{
				QualitySettings.masterTextureLimit = 0;
			}
		}
		else if (vector2Int.x == 3)
		{
			selectors[line].transform.position = B_LOD[option].transform.position;
			if (vector2Int.y == 0)
			{
				SettingsStatic.carsDistance = 90;
				SettingsStatic.propsDistance = 75;
				SettingsStatic.pedsDistance = 12;
				QualitySettings.lodBias = 0.5f;
			}
			else if (vector2Int.y == 1)
			{
				SettingsStatic.carsDistance = 150;
				SettingsStatic.propsDistance = 125;
				SettingsStatic.pedsDistance = 24;
				QualitySettings.lodBias = 1f;
			}
			else if (vector2Int.y == 2)
			{
				SettingsStatic.carsDistance = 250;
				SettingsStatic.propsDistance = 225;
				SettingsStatic.pedsDistance = 40;
				QualitySettings.lodBias = 2f;
			}
			else if (vector2Int.y == 3)
			{
				SettingsStatic.carsDistance = 350;
				SettingsStatic.propsDistance = 500;
				SettingsStatic.pedsDistance = 50;
				QualitySettings.lodBias = 5f;
			}
			cameraScript.UpdateSettings();
		}
		else if (vector2Int.x == 4)
		{
			selectors[line].transform.position = B_Vehicles[option].transform.position;
			if (vector2Int.y == 0)
			{
				SettingsStatic.maxCars = 500;
				SettingsStatic.carDensity = 10;
			}
			else if (vector2Int.y == 1)
			{
				SettingsStatic.maxCars = 1500;
				SettingsStatic.carDensity = 6;
			}
			else if (vector2Int.y == 2)
			{
				SettingsStatic.maxCars = 3000;
				SettingsStatic.carDensity = 4;
			}
			else if (vector2Int.y == 3)
			{
				SettingsStatic.maxCars = 9000;
				SettingsStatic.carDensity = 3;
			}
		}
		else if (vector2Int.x == 5)
		{
			selectors[line].transform.position = B_Peds[option].transform.position;
			if (vector2Int.y == 0)
			{
				SettingsStatic.maxPeds = 200;
				SettingsStatic.pedsDensity = 6f;
			}
			else if (vector2Int.y == 1)
			{
				SettingsStatic.maxPeds = 1000;
				SettingsStatic.pedsDensity = 5f;
			}
			else if (vector2Int.y == 2)
			{
				SettingsStatic.maxPeds = 2000;
				SettingsStatic.pedsDensity = 2f;
			}
			else if (vector2Int.y == 3)
			{
				SettingsStatic.maxPeds = 4000;
				SettingsStatic.pedsDensity = 1f;
			}
		}
		else if (vector2Int.x == 6)
		{
			selectors[line].transform.position = B_SSR[option].transform.position;
			if (isCity)
			{
				ppv.profile.TryGetSettings<ScreenSpaceReflections>(out var outSetting);
				if (vector2Int.y == 0)
				{
					outSetting.active = false;
				}
				else if (vector2Int.y == 1)
				{
					outSetting.active = true;
					outSetting.preset.value = ScreenSpaceReflectionPreset.Medium;
				}
				else if (vector2Int.y == 2)
				{
					outSetting.active = true;
					outSetting.preset.value = ScreenSpaceReflectionPreset.Custom;
				}
				else if (vector2Int.y == 3)
				{
					outSetting.active = true;
					outSetting.preset.value = ScreenSpaceReflectionPreset.Ultra;
				}
			}
		}
		else if (vector2Int.x == 7)
		{
			selectors[line].transform.position = B_AO[option].transform.position;
			if (isCity)
			{
				ppv.profile.TryGetSettings<AmbientOcclusion>(out var outSetting2);
				if (vector2Int.y == 0)
				{
					outSetting2.active = false;
				}
				else if (vector2Int.y == 1)
				{
					outSetting2.active = true;
					outSetting2.intensity.value = 0.75f;
					outSetting2.thicknessModifier.value = 1.5f;
				}
				else if (vector2Int.y == 2)
				{
					outSetting2.active = true;
					outSetting2.intensity.value = 1f;
					outSetting2.thicknessModifier.value = 2.25f;
				}
			}
		}
		else if (vector2Int.x == 8)
		{
			selectors[line].transform.position = B_Bloom[option].transform.position;
			if (isCity)
			{
				ppv.profile.TryGetSettings<Bloom>(out var outSetting3);
				if (vector2Int.y == 0)
				{
					outSetting3.active = false;
				}
				else if (vector2Int.y == 1)
				{
					outSetting3.active = true;
				}
			}
		}
		else if (vector2Int.x == 9)
		{
			selectors[line].transform.position = B_Scattering[option].transform.position;
			if (isCity)
			{
				if (vector2Int.y == 0)
				{
					reflectionProbe.resolution = 32;
				}
				else if (vector2Int.y == 1)
				{
					reflectionProbe.resolution = 128;
				}
				else if (vector2Int.y == 2)
				{
					reflectionProbe.resolution = 512;
				}
				else if (vector2Int.y == 3)
				{
					reflectionProbe.resolution = 1024;
				}
			}
		}
		else if (vector2Int.x == 10)
		{
			selectors[line].transform.position = B_CloudsNation[option].transform.position;
			if (!isCity)
			{
				if (vector2Int.y == 0)
				{
					nationFog.enabled = false;
				}
				else if (vector2Int.y == 1)
				{
					nationFog.enabled = true;
				}
			}
		}
		else if (vector2Int.x == 11)
		{
			selectors[line].transform.position = B_CloudsCity[option].transform.position;
			if (isCity)
			{
				if (vector2Int.y == 0)
				{
					cityFog.enabled = false;
				}
				else if (vector2Int.y == 1)
				{
					cityFog.enabled = true;
				}
			}
		}
		else if (vector2Int.x == 12)
		{
			selectors[line].transform.position = B_CloudShadows[option].transform.position;
			CloudShadows component = directionalLight.GetComponent<CloudShadows>();
			if (vector2Int.y == 0)
			{
				component.enabled = false;
			}
			else if (vector2Int.y == 1)
			{
				component.enabled = true;
			}
		}
		else if (vector2Int.x == 13)
		{
			selectors[line].transform.position = B_Vsync[option].transform.position;
			if (vector2Int.y == 0)
			{
				QualitySettings.vSyncCount = 0;
			}
			else if (vector2Int.y == 1)
			{
				QualitySettings.vSyncCount = 1;
			}
		}
		else if (vector2Int.x == 14)
		{
			selectors[line].transform.position = B_MaxFPS[option].transform.position;
			if (vector2Int.y == 0)
			{
				Application.targetFrameRate = 60;
				OnDemandRendering.renderFrameInterval = 2;
			}
			else if (vector2Int.y == 1)
			{
				Application.targetFrameRate = 120;
				OnDemandRendering.renderFrameInterval = 2;
			}
			else if (vector2Int.y == 2)
			{
				Application.targetFrameRate = -1;
				OnDemandRendering.renderFrameInterval = 1;
			}
		}
		else if (vector2Int.x == 16)
		{
			selectors[line].transform.position = B_Anti[option].transform.position;
			if (vector2Int.y == 0)
			{
				ppl.antialiasingMode = PostProcessLayer.Antialiasing.None;
				ctaa.enabled = false;
			}
			else if (vector2Int.y == 1)
			{
				ppl.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
				ppl.subpixelMorphologicalAntialiasing.quality = SubpixelMorphologicalAntialiasing.Quality.High;
				ctaa.enabled = false;
			}
			else if (vector2Int.y == 2)
			{
				ppl.antialiasingMode = PostProcessLayer.Antialiasing.None;
				ctaa.enabled = true;
			}
		}
	}

	public void CloseSettings()
	{
		SettingsStatic.SaveSettings();
		cameraScript.SetAzerty();
		gameObject.SetActive(value: false);
		Indexer.sliderOnScreen = false;
	}

	public void SetSlider(int slider)
	{
		switch (slider)
		{
		case 0:
			SettingsStatic.MusicVolume = musicSlider.value;
			playlist.source.volume = musicSlider.value;
			break;
		case 2:
			SettingsStatic.NatureVolume = natureSlider.value;
			if (isCity)
			{
				cameraScript.audioSourceHigh.volume = natureSlider.value;
				cameraScript.audioSourceLow.volume = natureSlider.value;
			}
			break;
		case 1:
			SettingsStatic.CityVolume = citySlider.value;
			break;
		case 3:
			SettingsStatic.Carsvolume = vehiclesSlider.value;
			break;
		case 4:
			SettingsStatic.mouseSense = mouseSlider.value;
			cameraScript.MouseRotateSensity = mouseSlider.value;
			cameraScript.mouseMovementSpeed = mouseSlider.value * 1.66f;
			cameraScript.scrollwheelSensity = mouseSlider.value / 1.2f;
			break;
		case 5:
			SettingsStatic.keyboardSense = keyboardSlider.value;
			cameraScript.KeyboardRotateSensity = keyboardSlider.value;
			cameraScript.keyboardMovementSpeed = keyboardSlider.value * 0.0175f;
			cameraScript.keyboardZoomingSensity = keyboardSlider.value * 75f;
			break;
		case 6:
			SettingsStatic.UIVolume = UISlider.value;
			break;
		}
	}

	public void SetTutorial(bool active)
	{
		if (active)
		{
			tutoSelector.transform.position = tutoButtonON.transform.position;
			SettingsStatic.tutorial = true;
			return;
		}
		tutoSelector.transform.position = tutoButtonOFF.transform.position;
		SettingsStatic.tutorial = false;
		if (isCity)
		{
			messages.TutoBox.gameObject.SetActive(value: false);
			messages.arrow.SetActive(value: false);
		}
	}

	public void SetAlert(bool active)
	{
		if (active)
		{
			alertSelector.transform.position = alertButtonON.transform.position;
			SettingsStatic.redAlerts = true;
		}
		else
		{
			alertSelector.transform.position = alertButtonOFF.transform.position;
			SettingsStatic.redAlerts = false;
		}
	}

	public void SetInfo(bool active)
	{
		if (active)
		{
			infoSelector.transform.position = infoButtonON.transform.position;
			SettingsStatic.blueInfo = true;
		}
		else
		{
			infoSelector.transform.position = infoButtonOFF.transform.position;
			SettingsStatic.blueInfo = false;
		}
	}

	public void SetScroll(bool active)
	{
		if (active)
		{
			scrollSelector.transform.position = scrollButtonON.transform.position;
			SettingsStatic.edgeScroll = true;
		}
		else
		{
			scrollSelector.transform.position = scrollButtonOFF.transform.position;
			SettingsStatic.edgeScroll = false;
		}
		cameraScript.edgeInput_bool = SettingsStatic.edgeScroll;
	}

	public void SetSmooth(bool active)
	{
		if (!(smoothSelector == null))
		{
			if (active)
			{
				smoothSelector.transform.position = smoothON.transform.position;
				SettingsStatic.smoothLevel = 300;
			}
			else
			{
				smoothSelector.transform.position = smoothOFF.transform.position;
				SettingsStatic.smoothLevel = 30;
			}
		}
	}

	public void SetAzerty(bool active)
	{
		if (active)
		{
			azertySelector.transform.position = azertyON.transform.position;
			SettingsStatic.azerty = true;
			left.text = "A";
			forward.text = "W / +";
			zoomIn.text = "Z";
			rotateLeft.text = "Q";
		}
		else
		{
			azertySelector.transform.position = azertyOFF.transform.position;
			SettingsStatic.azerty = false;
			left.text = "Q";
			forward.text = "Z / +";
			zoomIn.text = "W";
			rotateLeft.text = "A";
		}
	}
}
