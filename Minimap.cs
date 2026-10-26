using System.Collections;
using UnityEngine;

public class Minimap : MonoBehaviour
{
	private int mapSize;

	public Texture2D value32x;

	public Texture2D value20x;

	public Texture2D traffic32x;

	public Texture2D traffic20x;

	public Texture2D zones32x;

	public Texture2D zones20x;

	public Texture2D power32x;

	public Texture2D power20x;

	public Texture2D water32x;

	public Texture2D water20x;

	public Texture2D edu32x;

	public Texture2D edu20x;

	public Texture2D birth32x;

	public Texture2D birth20x;

	public Texture2D secu32x;

	public Texture2D secu20x;

	public Texture2D reli32x;

	public Texture2D reli20x;

	public Texture2D polut32x;

	public Texture2D polut20x;

	public Texture2D health32x;

	public Texture2D health20x;

	public Texture2D resource32x;

	public Texture2D resource20x;

	public Projector projector;

	private Texture2D valueTex;

	private Texture2D trafficTex;

	private Texture2D zoneTex;

	private Texture2D powerTex;

	private Texture2D waterTex;

	private Texture2D eduTex;

	private Texture2D birthTex;

	private Texture2D secuTex;

	private Texture2D reliTex;

	private Texture2D polutTex;

	private Texture2D healthTex;

	private Texture2D resourceTex;

	public Material projectorMat;

	public World world;

	public Roading roading;

	public Color LC;

	public Color MC;

	public Color UC;

	public Color R;

	public Color O;

	public Color M;

	public Color HT;

	public Color P;

	public Color RMC;

	public Color OUC;

	public Color none;

	public string displayed = "none";

	public LayerMask buildingsOnly;

	public LayerMask roadsOnly;

	public LayerMask defaultAndRoads;

	public LayerMask defaultAndWater;

	private Color32[] resetColorArray;

	private void Start()
	{
		int worldSize = World.worldSize;
		mapSize = worldSize * 16;
		CreateMapTexture();
		StartCoroutine(UpdateAllLayers());
	}

	public void ToggleLandValue()
	{
		if (displayed == "LandValue")
		{
			projector.gameObject.SetActive(value: false);
			displayed = "none";
			return;
		}
		projector.gameObject.SetActive(value: true);
		projectorMat.SetTexture("_MainTex", valueTex);
		displayed = "LandValue";
		projector.ignoreLayers = defaultAndRoads;
	}

	public void ToggleTraffic()
	{
		if (displayed == "Traffic")
		{
			projector.gameObject.SetActive(value: false);
			displayed = "none";
			return;
		}
		projector.gameObject.SetActive(value: true);
		projectorMat.SetTexture("_MainTex", trafficTex);
		displayed = "Traffic";
		projector.ignoreLayers = roadsOnly;
	}

	public void ToggleZones()
	{
		if (displayed == "Land Use")
		{
			projector.gameObject.SetActive(value: false);
			displayed = "none";
			return;
		}
		projector.gameObject.SetActive(value: true);
		projectorMat.SetTexture("_MainTex", zoneTex);
		displayed = "Land Use";
		projector.ignoreLayers = buildingsOnly;
	}

	public void TogglePower()
	{
		if (displayed == "Power")
		{
			projector.gameObject.SetActive(value: false);
			displayed = "none";
			return;
		}
		projector.gameObject.SetActive(value: true);
		projectorMat.SetTexture("_MainTex", powerTex);
		displayed = "Power";
		projector.ignoreLayers = buildingsOnly;
	}

	public void ToggleWater()
	{
		if (displayed == "Water")
		{
			projector.gameObject.SetActive(value: false);
			displayed = "none";
			return;
		}
		projector.gameObject.SetActive(value: true);
		projectorMat.SetTexture("_MainTex", waterTex);
		displayed = "Water";
		projector.ignoreLayers = buildingsOnly;
	}

	public void ToggleEducation()
	{
		if (displayed == "Education Rating")
		{
			projector.gameObject.SetActive(value: false);
			displayed = "none";
			return;
		}
		projector.gameObject.SetActive(value: true);
		projectorMat.SetTexture("_MainTex", eduTex);
		displayed = "Education Rating";
		projector.ignoreLayers = buildingsOnly;
	}

	public void ToggleBirthRate()
	{
		if (displayed == "Birth Rate")
		{
			projector.gameObject.SetActive(value: false);
			displayed = "none";
			return;
		}
		projector.gameObject.SetActive(value: true);
		projectorMat.SetTexture("_MainTex", birthTex);
		displayed = "Birth Rate";
		projector.ignoreLayers = buildingsOnly;
	}

	public void ToggleSecurity()
	{
		if (displayed == "Sense of Security")
		{
			projector.gameObject.SetActive(value: false);
			displayed = "none";
			return;
		}
		projector.gameObject.SetActive(value: true);
		projectorMat.SetTexture("_MainTex", secuTex);
		displayed = "Sense of Security";
		projector.ignoreLayers = buildingsOnly;
	}

	public void ToggleReligion()
	{
		if (displayed == "Religiousness")
		{
			projector.gameObject.SetActive(value: false);
			displayed = "none";
			return;
		}
		projector.gameObject.SetActive(value: true);
		projectorMat.SetTexture("_MainTex", reliTex);
		displayed = "Religiousness";
		projector.ignoreLayers = buildingsOnly;
	}

	public void TogglePollution()
	{
		if (displayed == "Pollution")
		{
			projector.gameObject.SetActive(value: false);
			displayed = "none";
			return;
		}
		projector.gameObject.SetActive(value: true);
		projectorMat.SetTexture("_MainTex", polutTex);
		displayed = "Pollution";
		projector.ignoreLayers = defaultAndRoads;
	}

	public void ToggleHealth()
	{
		if (displayed == "Healthcare Rating")
		{
			projector.gameObject.SetActive(value: false);
			displayed = "none";
			return;
		}
		projector.gameObject.SetActive(value: true);
		projectorMat.SetTexture("_MainTex", healthTex);
		displayed = "Healthcare Rating";
		projector.ignoreLayers = buildingsOnly;
	}

	public void ToggleResource()
	{
		if (displayed == "Resource")
		{
			projector.gameObject.SetActive(value: false);
			displayed = "none";
			return;
		}
		projector.gameObject.SetActive(value: true);
		projectorMat.SetTexture("_MainTex", resourceTex);
		displayed = "Resource";
		projector.ignoreLayers = defaultAndWater;
	}

	public void CreateMapTexture()
	{
		if (mapSize == 320)
		{
			valueTex = value20x;
			trafficTex = traffic20x;
			zoneTex = zones20x;
			powerTex = power20x;
			waterTex = water20x;
			eduTex = edu20x;
			birthTex = birth20x;
			secuTex = secu20x;
			reliTex = reli20x;
			polutTex = polut20x;
			healthTex = health20x;
			resourceTex = resource20x;
			projectorMat.SetTexture("_MainTex", value20x);
		}
		else
		{
			valueTex = value32x;
			trafficTex = traffic32x;
			zoneTex = zones32x;
			powerTex = power32x;
			waterTex = water32x;
			eduTex = edu32x;
			birthTex = birth32x;
			secuTex = secu32x;
			reliTex = reli32x;
			polutTex = polut32x;
			healthTex = health32x;
			resourceTex = resource32x;
			projectorMat.SetTexture("_MainTex", value32x);
		}
		CreateClearTexture();
		UpdateAllResource();
		projector.transform.position = new Vector3((float)(mapSize / 2) - 0.5f, 25f, (float)(mapSize / 2) - 0.5f);
		projector.orthographicSize = mapSize / 2;
	}

	public void CloseLayers()
	{
		projector.gameObject.SetActive(value: false);
		displayed = "none";
	}

	private void CreateClearTexture()
	{
		Color32 color = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, 0);
		resetColorArray = valueTex.GetPixels32();
		for (int i = 0; i < resetColorArray.Length; i++)
		{
			resetColorArray[i] = color;
		}
	}

	public IEnumerator UpdateAllLayers()
	{
		while (true)
		{
			int _delay = 0;
			valueTex.SetPixels32(resetColorArray);
			trafficTex.SetPixels32(resetColorArray);
			zoneTex.SetPixels32(resetColorArray);
			powerTex.SetPixels32(resetColorArray);
			waterTex.SetPixels32(resetColorArray);
			eduTex.SetPixels32(resetColorArray);
			yield return null;
			birthTex.SetPixels32(resetColorArray);
			secuTex.SetPixels32(resetColorArray);
			reliTex.SetPixels32(resetColorArray);
			polutTex.SetPixels32(resetColorArray);
			healthTex.SetPixels32(resetColorArray);
			yield return null;
			for (int x = 0; x < mapSize; x++)
			{
				for (int z = 0; z < mapSize; z++)
				{
					_delay++;
					if (_delay > 255)
					{
						_delay = 0;
						yield return null;
					}
					BlockData blockData = world.blockData[x, z];
					if (world.blockData == null)
					{
						continue;
					}
					int num = Mathf.Clamp(blockData.v[0], 1, 600);
					Color32 color;
					if (num < 300)
					{
						color = new Color32((byte)(60.0 - 0.2 * (double)num), (byte)(0.333 * (double)num), (byte)(0.85 * (double)num), 235);
					}
					else
					{
						color = ((num >= 600) ? new Color32(0, byte.MaxValue, (byte)(160.0 + 0.15 * (double)(num - 970)), 235) : new Color32(0, (byte)(100.0 + 0.516 * (double)(num - 300)), (byte)(255.0 - 0.316 * (double)(num - 300)), 235));
					}
					valueTex.SetPixel(x, z, color);
					byte b = (byte)Mathf.Clamp(blockData.v[1] / 8, 0, 255);
					if (b > 0)
					{
						polutTex.SetPixel(x, z, new Color32(byte.MaxValue, 128, 0, b));
					}
					if (blockData.lO != null)
					{
						LotCreator lO = blockData.lO;
						if (lO.isDecayed)
						{
							color = Color.red;
						}
						else if (lO.lClass != LotCreator.LotClass.LOW)
						{
							if (lO.lClass == LotCreator.LotClass.MIDDLE)
							{
								color = MC;
							}
							else if (lO.lClass == LotCreator.LotClass.UPPER)
							{
								color = UC;
							}
							else if (lO.lClass == LotCreator.LotClass.MIXRETAIL)
							{
								color = RMC;
							}
							else if (lO.lClass == LotCreator.LotClass.MIXOFFICE)
							{
								color = OUC;
							}
							else if (lO.lClass == LotCreator.LotClass.MANUFACTURE)
							{
								color = M;
							}
							else if (lO.lClass == LotCreator.LotClass.HIGHTECH)
							{
								color = HT;
							}
							else if (lO.lClass == LotCreator.LotClass.OFFICE)
							{
								color = O;
							}
							else
							{
								color = ((lO.lClass != LotCreator.LotClass.RETAIL) ? ((Color32)P) : ((Color32)R));
							}
						}
						else if (lO.lType == LotCreator.LotType.INDUSTRIAL)
						{
							color = M;
						}
						else if (lO.lType == LotCreator.LotType.COMMERCIAL)
						{
							color = R;
						}
						else
						{
							color = ((lO.lType != LotCreator.LotType.MIXED) ? ((Color32)LC) : ((Color32)RMC));
						}
						zoneTex.SetPixel(x, z, color);
						color = ((!lO.powered) ? ((Color32)P) : ((Color32)M));
						powerTex.SetPixel(x, z, color);
						color = ((!lO.watered) ? ((Color32)P) : ((Color32)R));
						waterTex.SetPixel(x, z, color);
						if (lO.lType == LotCreator.LotType.RESIDENTIAL || lO.lType == LotCreator.LotType.MIXED)
						{
							color = new Color32((byte)(75.0 - 0.75 * (double)lO.eduLevel), (byte)lO.eduLevel, (byte)(2.55 * (double)lO.eduLevel), 235);
							eduTex.SetPixel(x, z, color);
							color = new Color32((byte)(75.0 - 0.75 * (double)lO.birthRate * 8.0), (byte)(2.55 * (double)lO.birthRate * 8.0), (byte)(lO.birthRate * 8f), 235);
							birthTex.SetPixel(x, z, color);
							color = new Color32(225, 0, 225, (byte)(2.5f * (float)lO.reliLevel));
							reliTex.SetPixel(x, z, color);
							color = new Color32((byte)lO.healthLevel, (byte)(2.1 * (double)lO.healthLevel), (byte)(75.0 - 0.75 * (double)lO.healthLevel), 235);
							healthTex.SetPixel(x, z, color);
						}
						color = new Color32((byte)(2.55 * (double)(100 - lO.secuLevel)), (byte)(2.55 * (double)lO.secuLevel), (byte)(2.55 * (double)lO.secuLevel), (byte)(100 + lO.secuLevel));
						secuTex.SetPixel(x, z, color);
					}
					else if (blockData.sO != null)
					{
						zoneTex.SetPixel(x, z, P);
						ServiceCreator sO = blockData.sO;
						if (sO.type == ServiceCreator.Type.Fire)
						{
							secuTex.SetPixel(x, z, Color.red);
						}
						else if (sO.type == ServiceCreator.Type.Police)
						{
							secuTex.SetPixel(x, z, Color.blue);
						}
						else if (sO.type == ServiceCreator.Type.Prison)
						{
							secuTex.SetPixel(x, z, Color.black);
						}
						else if (sO.type == ServiceCreator.Type.School)
						{
							eduTex.SetPixel(x, z, Color.cyan);
						}
						else if (sO.type == ServiceCreator.Type.Hospital)
						{
							healthTex.SetPixel(x, z, Color.magenta);
						}
						else if (sO.type == ServiceCreator.Type.Religion)
						{
							reliTex.SetPixel(x, z, Color.white);
						}
						else if (sO.type == ServiceCreator.Type.Power)
						{
							powerTex.SetPixel(x, z, Color.yellow);
						}
						else if (sO.type == ServiceCreator.Type.Water)
						{
							waterTex.SetPixel(x, z, Color.blue);
						}
						else if (sO.type == ServiceCreator.Type.Drone)
						{
							trafficTex.SetPixel(x, z, Color.cyan);
						}
					}
					if (blockData.r)
					{
						RoadCreator ifNotNull = roading.GetIfNotNull(x, 1, z);
						if (ifNotNull == null)
						{
							ifNotNull = roading.GetIfNotNull(x, 2, z);
						}
						Color32 color2;
						if (ifNotNull == null)
						{
							color2 = none;
						}
						else if (blockData.a)
						{
							color2 = Color.white;
						}
						else if (ifNotNull.StopCapacity > 1)
						{
							color2 = ((ifNotNull.stopType != RoadCreator.StopType.BUS) ? ((Color32)Color.blue) : ((Color32)Color.cyan));
						}
						else
						{
							float num2 = Mathf.Clamp(ifNotNull.TrafficPercent, 0, 100);
							float num3 = 0f;
							if (ifNotNull.TrafficPercent > 100)
							{
								num3 = Mathf.Clamp((float)(ifNotNull.TrafficPercent - 100) * 2.55f, 0f, 100f);
							}
							color2 = new Color32((byte)(num2 * 2.55f), (byte)(255f - num2 * 2.55f), (byte)num3, 200);
						}
						trafficTex.SetPixel(x, z, color2);
					}
					else if (blockData.a)
					{
						RoadCreator railIfNotNull = roading.GetRailIfNotNull(x, 1, z);
						Color32 color3;
						if (railIfNotNull == null)
						{
							color3 = none;
						}
						else if (railIfNotNull.StopCapacity > 1)
						{
							color3 = Color.cyan;
						}
						else
						{
							int num4 = Mathf.Clamp(railIfNotNull.TrafficPercent * 2, 0, 235);
							color3 = new Color32((byte)(235 - num4), (byte)(235 - num4), byte.MaxValue, (byte)Mathf.Clamp(railIfNotNull.TrafficSlipsal / 10 + 5, 0, 255));
						}
						trafficTex.SetPixel(x, z, color3);
					}
				}
			}
			yield return null;
			valueTex.wrapMode = TextureWrapMode.Clamp;
			valueTex.Apply();
			yield return null;
			trafficTex.wrapMode = TextureWrapMode.Clamp;
			trafficTex.Apply();
			yield return null;
			zoneTex.wrapMode = TextureWrapMode.Clamp;
			zoneTex.Apply();
			yield return null;
			powerTex.wrapMode = TextureWrapMode.Clamp;
			powerTex.Apply();
			yield return null;
			waterTex.wrapMode = TextureWrapMode.Clamp;
			waterTex.Apply();
			yield return null;
			waterTex.wrapMode = TextureWrapMode.Clamp;
			waterTex.Apply();
			yield return null;
			eduTex.wrapMode = TextureWrapMode.Clamp;
			eduTex.Apply();
			yield return null;
			birthTex.wrapMode = TextureWrapMode.Clamp;
			birthTex.Apply();
			yield return null;
			secuTex.wrapMode = TextureWrapMode.Clamp;
			secuTex.Apply();
			yield return null;
			reliTex.wrapMode = TextureWrapMode.Clamp;
			reliTex.Apply();
			yield return null;
			polutTex.wrapMode = TextureWrapMode.Clamp;
			polutTex.Apply();
			yield return null;
			healthTex.wrapMode = TextureWrapMode.Clamp;
			healthTex.Apply();
		}
	}

	public void UpdateAllResource()
	{
		for (int i = 0; i < mapSize; i++)
		{
			for (int j = 0; j < mapSize; j++)
			{
				BlockData bd = world.blockData[i, j];
				UpdateResource(bd);
			}
		}
		SetResourceTexture();
	}

	public void UpdateResource(BlockData bd)
	{
		if (bd.e == 0)
		{
			resourceTex.SetPixel(bd.wP.x, bd.wP.y, new Color32(190, 190, 190, 80));
			return;
		}
		Color32 color;
		if (bd.e > 194 && bd.e < 253)
		{
			color = new Color32(124, 234, byte.MaxValue, (byte)((float)(bd.e - 192) * 3.984375f));
		}
		else if (bd.e > 130 && bd.e < 190)
		{
			color = new Color32(byte.MaxValue, 140, 124, (byte)((float)(bd.e - 128) * 3.984375f));
		}
		else if (bd.e > 66 && bd.e < 126)
		{
			color = new Color32(byte.MaxValue, 123, 230, (byte)((float)(bd.e - 64) * 3.984375f));
		}
		else
		{
			color = ((bd.e <= 2 || bd.e >= 60) ? new Color32(0, 0, 0, 0) : new Color32(149, 123, byte.MaxValue, (byte)((float)(int)bd.e * 3.984375f)));
		}
		resourceTex.SetPixel(bd.wP.x, bd.wP.y, color);
	}

	public void SetResourceTexture()
	{
		resourceTex.wrapMode = TextureWrapMode.Clamp;
		resourceTex.Apply();
	}
}
