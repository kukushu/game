using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Zoning : MonoBehaviour
{
	public CostHelper costHelper;

	public GameObject One_one;

	public GameObject ResidentialCube;

	public GameObject FriendlyPlane;

	public GameObject Z_2x4;

	public GameObject Z_2x5;

	public GameObject Z_2x6;

	public GameObject Z_3x4;

	public GameObject Z_3x5;

	public GameObject Z_3x6;

	public GameObject Z_4x4;

	public GameObject Z_4x5;

	public GameObject Z_4x6;

	public GameObject Z_6x4;

	public GameObject Z_6x5;

	public GameObject Z_6x6;

	public Material ZoningAtlas;

	private Color32 FriendlyColor;

	public Material decayedConcrete;

	public static Material dConcrete;

	public Material decayedBricks;

	public static Material dBricks;

	public Material decayedMetal;

	public static Material dMetal;

	public Material decayedWood;

	public static Material dWood;

	public Material decayedWindow;

	public static Material dWindow;

	public Color32 LowRes;

	public Color32 MedRes;

	public Color32 HiRes;

	public Color32 LowCom;

	public Color32 MedCom;

	public Color32 HiCom;

	public Color32 LowInd;

	public Color32 MedInd;

	public Color32 HiInd;

	public Color32 LowMix;

	public Color32 MedMix;

	public Color32 HiMix;

	public GameObject tZone;

	public BlockInteraction bi;

	public LotManager lm;

	public GameObject lotUnit;

	public static int Type;

	public static int Case;

	private HashSet<Chunk> updates = new HashSet<Chunk>();

	private List<GameObject> tempLots = new List<GameObject>();

	private HashSet<LotCreator> changeDensity = new HashSet<LotCreator>();

	private Color32 green = new Color32(0, 150, 0, 200);

	private Color32 red = new Color32(150, 0, 0, 200);

	private int loc_ind;

	private int sub_ind;

	private int locX;

	private int locY;

	public int series = 1;

	private bool newSeries;

	public bool _xConnected;

	public bool x_Connected;

	public bool _zConnected;

	public bool z_Connected;

	public SoundManager sm;

	private int lotsCreated;

	private int _cost;

	private void Start()
	{
		dConcrete = decayedConcrete;
		dBricks = decayedBricks;
		dMetal = decayedMetal;
		dWindow = decayedWindow;
		dWood = decayedWood;
	}

	private void Update()
	{
		loc_ind = Indexer.Index;
		sub_ind = Indexer.Sub_Index;
		if (Input.GetMouseButton(0) && bi.tamere == 2)
		{
			if (bi.x2 != bi.oldx2 || bi.z2 != bi.oldz2 || newSeries)
			{
				StopVisualize();
				_cost = 0;
				PreCreateZone(permanent: false, inverted: false);
				PreCreateZone(permanent: false, inverted: true);
				newSeries = false;
			}
			bi.oldx1 = bi.x1;
			bi.oldz1 = bi.z1;
			bi.oldx2 = bi.x2;
			bi.oldz2 = bi.z2;
		}
		if (loc_ind == 20 || loc_ind == 21 || loc_ind == 22 || loc_ind == 24)
		{
			Vector3 vector = new Vector3(0f, 0.515f, 0f);
			One_one.transform.position = bi.pos + vector;
			ZoningFriendly();
		}
		if (Input.GetMouseButtonUp(0) && bi.tamere == 2)
		{
			if (Type == 5)
			{
				Densify();
			}
			else
			{
				PreCreateZone(permanent: true, inverted: false);
				PreCreateZone(permanent: true, inverted: true);
				StartCoroutine(UpdateChunks());
			}
			bi.tamere = 0;
			Type = 0;
		}
		if (Input.GetKeyDown(KeyCode.Escape) && loc_ind >= 20 && loc_ind <= 24 && bi.tamere == 2)
		{
			StopVisualize();
			bi.tamere = 0;
			Type = 0;
			sm.Play("Impractical");
		}
		if (Input.GetKeyDown(KeyCode.LeftShift) && loc_ind >= 20 && loc_ind <= 24)
		{
			if (series != 0)
			{
				series = 0;
				sm.Play("ClickButtonB");
				newSeries = true;
			}
			else if (series == 0)
			{
				series = 1;
				sm.Play("ClickButtonB");
				newSeries = true;
			}
		}
		if (Input.GetKeyDown(KeyCode.LeftControl) && loc_ind >= 20 && loc_ind <= 24)
		{
			if (series != 2)
			{
				series = 2;
				sm.Play("ClickButtonB");
				newSeries = true;
			}
			else if (series == 2)
			{
				series = 1;
				sm.Play("ClickButtonB");
				newSeries = true;
			}
		}
		if (Input.GetKeyDown(KeyCode.LeftAlt) && loc_ind >= 20 && loc_ind <= 24)
		{
			if (series != 3)
			{
				series = 3;
				sm.Play("ClickButtonB");
				newSeries = true;
			}
			else if (series == 3)
			{
				series = 1;
				sm.Play("ClickButtonB");
				newSeries = true;
			}
		}
	}

	public void ZoningFriendly()
	{
		if ((locX == bi.x2 || Input.GetMouseButton(0)) && (locY == bi.z2 || Input.GetMouseButton(0)))
		{
			return;
		}
		int num = (int)bi.pos.x;
		int num2 = (int)bi.pos.z;
		locX = num;
		locY = num2;
		Renderer component = One_one.GetComponent<Renderer>();
		component.material.color = red;
		Type = 0;
		ClearFriendly();
		BlockData ifNotNull = bi.world.GetIfNotNull(num, num2);
		BlockData ifNotNull2 = bi.world.GetIfNotNull(num - 1, num2);
		BlockData ifNotNull3 = bi.world.GetIfNotNull(num + 1, num2);
		BlockData ifNotNull4 = bi.world.GetIfNotNull(num, num2 - 1);
		BlockData ifNotNull5 = bi.world.GetIfNotNull(num, num2 + 1);
		if (ifNotNull == null || ifNotNull2 == null || ifNotNull3 == null || ifNotNull4 == null || ifNotNull5 == null)
		{
			return;
		}
		if (ifNotNull2.r)
		{
			Type = 1;
		}
		if (ifNotNull3.r)
		{
			Type = 2;
		}
		if (ifNotNull4.r)
		{
			Type = 3;
		}
		if (ifNotNull5.r)
		{
			Type = 4;
		}
		if (ifNotNull.lO != null)
		{
			Type = 5;
		}
		if (Type == 1)
		{
			for (int i = num; i <= num + 5; i++)
			{
				BlockData ifNotNull6 = bi.world.GetIfNotNull(i, num2);
				if (ifNotNull6 == null)
				{
					break;
				}
				Vector3 position = new Vector3(ifNotNull6.wP.x, ifNotNull6.h + 0.51f, ifNotNull6.wP.y);
				if (ifNotNull.b)
				{
					component.material.color = red;
					continue;
				}
				Object.Instantiate(FriendlyPlane, position, Quaternion.identity);
				FriendlyPlane.name = "FriendlyPlane";
				FriendlyPlane.tag = "NewCube";
				component.material.color = green;
			}
		}
		else if (Type == 2)
		{
			for (int j = num - 5; j <= num; j++)
			{
				BlockData ifNotNull7 = bi.world.GetIfNotNull(j, num2);
				if (ifNotNull7 == null)
				{
					break;
				}
				Vector3 position2 = new Vector3(ifNotNull7.wP.x, ifNotNull7.h + 0.51f, ifNotNull7.wP.y);
				if (ifNotNull.b)
				{
					component.material.color = red;
					continue;
				}
				Object.Instantiate(FriendlyPlane, position2, Quaternion.identity);
				FriendlyPlane.name = "FriendlyPlane";
				FriendlyPlane.tag = "NewCube";
				component.material.color = green;
			}
		}
		else if (Type == 3)
		{
			for (int k = num2; k <= num2 + 5; k++)
			{
				BlockData ifNotNull8 = bi.world.GetIfNotNull(num, k);
				if (ifNotNull8 == null)
				{
					break;
				}
				Vector3 position3 = new Vector3(ifNotNull8.wP.x, ifNotNull8.h + 0.51f, ifNotNull8.wP.y);
				if (ifNotNull.b)
				{
					component.material.color = red;
					continue;
				}
				Object.Instantiate(FriendlyPlane, position3, Quaternion.identity);
				FriendlyPlane.name = "FriendlyPlane";
				FriendlyPlane.tag = "NewCube";
				component.material.color = green;
			}
		}
		else if (Type == 4)
		{
			for (int l = num2 - 5; l <= num2; l++)
			{
				BlockData ifNotNull9 = bi.world.GetIfNotNull(num, l);
				if (ifNotNull9 == null)
				{
					break;
				}
				Vector3 position4 = new Vector3(ifNotNull9.wP.x, ifNotNull9.h + 0.51f, ifNotNull9.wP.y);
				if (ifNotNull.b)
				{
					component.material.color = red;
					continue;
				}
				Object.Instantiate(FriendlyPlane, position4, Quaternion.identity);
				FriendlyPlane.name = "FriendlyPlane";
				FriendlyPlane.tag = "NewCube";
				component.material.color = green;
			}
		}
		else if (Type == 5)
		{
			component.material.color = Color.cyan;
		}
	}

	public void ClearFriendly()
	{
		GameObject[] array = GameObject.FindGameObjectsWithTag("NewCube");
		foreach (GameObject gameObject in array)
		{
			if (gameObject.name == "FriendlyPlane(Clone)")
			{
				Object.Destroy(gameObject);
			}
		}
	}

	private void PreCreateZone(bool permanent, bool inverted)
	{
		float num = (float)(sub_ind + 1) / 2f;
		lotsCreated = 0;
		if (sub_ind == 4)
		{
			if (Indexer.Index == 20)
			{
				num = Mathf.CeilToInt(Indexer.resStage / 2);
			}
			else if (Indexer.Index == 21)
			{
				num = Mathf.CeilToInt(Indexer.comStage / 2);
			}
			else if (Indexer.Index == 22)
			{
				num = Mathf.CeilToInt(Indexer.indStage / 2);
			}
			else if (Indexer.Index == 24)
			{
				num = Mathf.CeilToInt(Indexer.mixStage / 2);
			}
		}
		int num2;
		int num3;
		int num4;
		int num5;
		if (!inverted)
		{
			num2 = bi.x1;
			num3 = bi.x2;
			num4 = bi.z1;
			num5 = bi.z2;
		}
		else
		{
			num2 = bi.x2;
			num3 = bi.x1;
			num4 = bi.z2;
			num5 = bi.z1;
		}
		SetLotStyle();
		int num6 = ((num2 < num3) ? num2 : num3);
		int num7 = ((num4 < num5) ? num4 : num5);
		int num8 = Mathf.Abs(num2 - num3) + 1;
		int num9 = Mathf.Abs(num4 - num5) + 1;
		int num10 = 0;
		int num11 = 0;
		int width = 0;
		if ((num8 == 2 && num9 <= 6 && num9 >= 4) || (num8 == 3 && num9 <= 6 && num9 >= 4) || (num8 == 4 && num9 <= 6 && num9 >= 4))
		{
			switch (num9)
			{
			case 4:
				width = num8;
				num11 = 4;
				num10 = 2;
				break;
			case 5:
				width = num8;
				num11 = 5;
				num10 = 2;
				break;
			case 6:
				width = num8;
				num11 = 6;
				num10 = 2;
				break;
			}
		}
		else if ((num9 == 2 && num8 <= 6 && num8 >= 4) || (num9 == 3 && num8 <= 6 && num8 >= 4) || (num9 == 4 && num8 <= 6 && num8 >= 4))
		{
			switch (num8)
			{
			case 4:
				width = num9;
				num11 = 4;
				num10 = 1;
				break;
			case 5:
				width = num9;
				num11 = 5;
				num10 = 1;
				break;
			case 6:
				width = num9;
				num11 = 6;
				num10 = 1;
				break;
			}
		}
		else if (num8 >= num9)
		{
			width = num8;
			num11 = num9;
			num10 = 2;
		}
		else
		{
			width = num9;
			num11 = num8;
			num10 = 1;
		}
		if ((num11 < 8) & inverted)
		{
			return;
		}
		int num12 = FindLotDepth(num11, inverted);
		List<string> list = FindLotWidths(width, series);
		int num13 = num7;
		int num14 = num6;
		_xConnected = false;
		x_Connected = false;
		_zConnected = false;
		z_Connected = false;
		foreach (string item in list)
		{
			_ = "Z_" + item + "x" + num12;
			switch (num10)
			{
			case 2:
			{
				int num16 = int.Parse(item);
				if (bi.roading.GetAccess(num2, num4 - 1) != null)
				{
					_zConnected = true;
				}
				else if (bi.roading.GetAccess(num2, num4 + 1) != null)
				{
					z_Connected = true;
				}
				PreCreateLot(num14, num4, num16, num12, permanent, num);
				num14 += num16;
				break;
			}
			case 1:
			{
				int num15 = int.Parse(item);
				if (bi.roading.GetAccess(num2 - 1, num4) != null)
				{
					_xConnected = true;
				}
				else if (bi.roading.GetAccess(num2 + 1, num4) != null)
				{
					x_Connected = true;
				}
				PreCreateLot(num2, num13, num15, num12, permanent, num);
				num13 += num15;
				break;
			}
			}
		}
		costHelper.SetCost(_cost);
		if (Type == 5)
		{
			for (int i = num6; i < num6 + num8; i++)
			{
				for (int j = num7; j < num7 + num9; j++)
				{
					BlockData ifNotNull = bi.world.GetIfNotNull(i, j);
					if (ifNotNull.lO != null)
					{
						changeDensity.Add(ifNotNull.lO);
					}
				}
			}
			costHelper.SetCost(Mathf.RoundToInt((float)(changeDensity.Count * 4) * (num / 2f)));
			foreach (LotCreator item2 in changeDensity)
			{
				PreDensify(item2);
			}
		}
		if (permanent && lotsCreated > 0)
		{
			if (lotsCreated > 0)
			{
				sm.Play("LotBuilt");
				costHelper.PayForIt();
			}
			else
			{
				sm.Play("Impractical");
			}
			StopVisualize();
		}
	}

	public void PreCreateLot(int xi, int zi, int lotWidth, int lotDepth, bool permanent, float unitCost, int farmSize = 0)
	{
		BlockData ifNotNull = bi.world.GetIfNotNull(xi, zi);
		if (ifNotNull == null || ifNotNull.h < 29.5f || ifNotNull.b)
		{
			return;
		}
		bool flag = true;
		float h = ifNotNull.h;
		if (!_zConnected && !z_Connected && !_xConnected && !x_Connected)
		{
			return;
		}
		List<BlockData> list = new List<BlockData>();
		int num;
		float num2;
		BlockData ifNotNull2;
		float num3;
		if (_zConnected)
		{
			num = 270;
			switch (lotWidth)
			{
			case 6:
				num2 = 2.5f;
				ifNotNull2 = bi.world.GetIfNotNull(xi + 2, zi - 1);
				break;
			case 4:
				num2 = 1.5f;
				ifNotNull2 = bi.world.GetIfNotNull(xi + 1, zi - 1);
				break;
			case 3:
				num2 = 1f;
				ifNotNull2 = bi.world.GetIfNotNull(xi + 1, zi - 1);
				break;
			default:
				num2 = 0.5f;
				ifNotNull2 = bi.world.GetIfNotNull(xi, zi - 1);
				break;
			}
			num3 = lotDepth switch
			{
				4 => 1.5f, 
				5 => 2f, 
				_ => 2.5f, 
			};
			for (int i = xi; i < xi + lotWidth; i++)
			{
				for (int j = zi; j < zi + lotDepth; j++)
				{
					BlockData ifNotNull3 = bi.world.GetIfNotNull(i, j);
					if (ifNotNull3 == null)
					{
						return;
					}
					if (ifNotNull3.h > h)
					{
						h = ifNotNull3.h;
					}
					if (ifNotNull3.b)
					{
						flag = false;
					}
					list.Add(ifNotNull3);
				}
			}
		}
		else if (z_Connected)
		{
			num = 90;
			switch (lotWidth)
			{
			case 6:
				num2 = 2.5f;
				ifNotNull2 = bi.world.GetIfNotNull(xi + 2, zi + 1);
				break;
			case 4:
				num2 = 1.5f;
				ifNotNull2 = bi.world.GetIfNotNull(xi + 1, zi + 1);
				break;
			case 3:
				num2 = 1f;
				ifNotNull2 = bi.world.GetIfNotNull(xi + 1, zi + 1);
				break;
			default:
				num2 = 0.5f;
				ifNotNull2 = bi.world.GetIfNotNull(xi, zi + 1);
				break;
			}
			num3 = lotDepth switch
			{
				4 => -1.5f, 
				5 => -2f, 
				_ => -2.5f, 
			};
			for (int k = xi; k < xi + lotWidth; k++)
			{
				for (int l = zi - (lotDepth - 1); l <= zi; l++)
				{
					BlockData ifNotNull4 = bi.world.GetIfNotNull(k, l);
					if (ifNotNull4 == null)
					{
						return;
					}
					if (ifNotNull4.h > h)
					{
						h = ifNotNull4.h;
					}
					if (ifNotNull4.b)
					{
						flag = false;
					}
					list.Add(ifNotNull4);
				}
			}
		}
		else if (_xConnected)
		{
			num = 0;
			switch (lotWidth)
			{
			case 6:
				num3 = 2.5f;
				ifNotNull2 = bi.world.GetIfNotNull(xi - 1, zi + 2);
				break;
			case 4:
				num3 = 1.5f;
				ifNotNull2 = bi.world.GetIfNotNull(xi - 1, zi + 1);
				break;
			case 3:
				num3 = 1f;
				ifNotNull2 = bi.world.GetIfNotNull(xi - 1, zi + 1);
				break;
			default:
				num3 = 0.5f;
				ifNotNull2 = bi.world.GetIfNotNull(xi - 1, zi);
				break;
			}
			num2 = lotDepth switch
			{
				4 => 1.5f, 
				5 => 2f, 
				_ => 2.5f, 
			};
			for (int m = xi; m < xi + lotDepth; m++)
			{
				for (int n = zi; n < zi + lotWidth; n++)
				{
					BlockData ifNotNull5 = bi.world.GetIfNotNull(m, n);
					if (ifNotNull5 == null)
					{
						return;
					}
					if (ifNotNull5.h > h)
					{
						h = ifNotNull5.h;
					}
					if (ifNotNull5.b)
					{
						flag = false;
					}
					list.Add(ifNotNull5);
				}
			}
		}
		else
		{
			if (!x_Connected)
			{
				return;
			}
			num = 180;
			switch (lotWidth)
			{
			case 6:
				num3 = 2.5f;
				ifNotNull2 = bi.world.GetIfNotNull(xi + 1, zi + 2);
				break;
			case 4:
				num3 = 1.5f;
				ifNotNull2 = bi.world.GetIfNotNull(xi + 1, zi + 1);
				break;
			case 3:
				num3 = 1f;
				ifNotNull2 = bi.world.GetIfNotNull(xi + 1, zi + 1);
				break;
			default:
				num3 = 0.5f;
				ifNotNull2 = bi.world.GetIfNotNull(xi + 1, zi);
				break;
			}
			num2 = lotDepth switch
			{
				4 => -1.5f, 
				5 => -2f, 
				_ => -2.5f, 
			};
			for (int num4 = xi - (lotDepth - 1); num4 <= xi; num4++)
			{
				for (int num5 = zi; num5 < zi + lotWidth; num5++)
				{
					BlockData ifNotNull6 = bi.world.GetIfNotNull(num4, num5);
					if (ifNotNull6 == null)
					{
						return;
					}
					if (ifNotNull6.h > h)
					{
						h = ifNotNull6.h;
					}
					if (ifNotNull6.b)
					{
						flag = false;
					}
					list.Add(ifNotNull6);
				}
			}
		}
		_cost += Mathf.RoundToInt((float)(lotWidth * lotDepth) * unitCost);
		if (!flag)
		{
			return;
		}
		GameObject gameObject;
		string text;
		LotCreator.LotSize lSize;
		switch (lotWidth)
		{
		case 4:
			switch (lotDepth)
			{
			case 4:
				gameObject = Object.Instantiate(Z_4x4);
				text = "4x4";
				lSize = LotCreator.LotSize._4x4;
				break;
			case 5:
				gameObject = Object.Instantiate(Z_4x5);
				text = "4x5";
				lSize = LotCreator.LotSize._4x5;
				break;
			default:
				gameObject = Object.Instantiate(Z_4x6);
				text = "4x6";
				lSize = LotCreator.LotSize._4x6;
				break;
			}
			break;
		case 6:
			switch (lotDepth)
			{
			case 4:
				gameObject = Object.Instantiate(Z_6x4);
				text = "6x4";
				lSize = LotCreator.LotSize._6x4;
				break;
			case 5:
				gameObject = Object.Instantiate(Z_6x5);
				text = "6x5";
				lSize = LotCreator.LotSize._6x5;
				break;
			default:
				gameObject = Object.Instantiate(Z_6x6);
				text = "6x6";
				lSize = LotCreator.LotSize._6x6;
				break;
			}
			break;
		case 3:
			switch (lotDepth)
			{
			case 4:
				gameObject = Object.Instantiate(Z_3x4);
				text = "3x4";
				lSize = LotCreator.LotSize._3x4;
				break;
			case 5:
				gameObject = Object.Instantiate(Z_3x5);
				text = "3x5";
				lSize = LotCreator.LotSize._3x5;
				break;
			default:
				gameObject = Object.Instantiate(Z_3x6);
				text = "3x6";
				lSize = LotCreator.LotSize._3x6;
				break;
			}
			break;
		default:
			switch (lotDepth)
			{
			case 4:
				gameObject = Object.Instantiate(Z_2x4);
				text = "2x4";
				lSize = LotCreator.LotSize._2x4;
				break;
			case 5:
				gameObject = Object.Instantiate(Z_2x5);
				text = "2x5";
				lSize = LotCreator.LotSize._2x5;
				break;
			default:
				gameObject = Object.Instantiate(Z_2x6);
				text = "2x6";
				lSize = LotCreator.LotSize._2x6;
				break;
			}
			break;
		}
		gameObject.transform.position = new Vector3((float)xi + num2, h + 0.51f, (float)zi + num3);
		gameObject.transform.Rotate(0f, num, 0f, Space.Self);
		gameObject.transform.name = text;
		tempLots.Add(gameObject);
		if (!permanent)
		{
			return;
		}
		if ((float)_cost > bi.eco.budget.Balance)
		{
			sm.Play("Impractical");
		}
		string text2 = "I";
		GameObject gameObject2 = Object.Instantiate(lotUnit);
		LotCreator component = gameObject2.GetComponent<LotCreator>();
		component.r = bi.roading;
		component.w = bi.world;
		component.name = xi + "_lot_" + zi;
		if (loc_ind == 20)
		{
			component.lType = LotCreator.LotType.RESIDENTIAL;
			bi.world.VacantResLots.Add(component);
			text2 = "R";
			component.nextDemand = 5;
		}
		else if (loc_ind == 21)
		{
			component.lType = LotCreator.LotType.COMMERCIAL;
			bi.world.VacantCommLots.Add(component);
			text2 = "C";
			component.nextDemand = 18;
		}
		else if (loc_ind == 22)
		{
			component.lType = LotCreator.LotType.INDUSTRIAL;
			bi.world.VacantIndusLots.Add(component);
			component.nextDemand = 20;
		}
		else if (loc_ind == 24)
		{
			component.lType = LotCreator.LotType.MIXED;
			bi.world.VacantMixedLots.Add(component);
			text2 = "M";
			component.nextDemand = 100;
			component.nextValue = 768;
		}
		else if (loc_ind == 23)
		{
			component.lType = LotCreator.LotType.INDUSTRIAL;
			component.lClass = LotCreator.LotClass.PRIMARY;
			bi.world.VacantIndusLots.Add(component);
			component.nextDemand = 20;
		}
		foreach (BlockData item in list)
		{
			item.FlatHeight(ifNotNull2.h);
			item.b = true;
			item.lO = component;
			item.t = BlockData.Type.BUILDING;
			component.blockPos.Add(new Vector2Int(item.wP.x, item.wP.y));
			CheckChunks(item);
		}
		if (sub_ind == 1)
		{
			component.MaxStage = 1;
		}
		else if (sub_ind == 2)
		{
			if (component.lType == LotCreator.LotType.RESIDENTIAL)
			{
				component.MaxStage = 4;
			}
			else if (component.lType == LotCreator.LotType.MIXED)
			{
				component.MaxStage = 2;
			}
			else
			{
				component.MaxStage = 3;
			}
		}
		else if (sub_ind == 3)
		{
			if (component.lType == LotCreator.LotType.RESIDENTIAL)
			{
				component.MaxStage = 7;
			}
			else if (component.lType == LotCreator.LotType.COMMERCIAL)
			{
				component.MaxStage = 6;
			}
			else if (component.lType == LotCreator.LotType.INDUSTRIAL)
			{
				component.MaxStage = 4;
			}
			else if (component.lType == LotCreator.LotType.MIXED)
			{
				component.MaxStage = 4;
			}
		}
		else if (sub_ind == 4)
		{
			if (component.lType == LotCreator.LotType.RESIDENTIAL)
			{
				component.MaxStage = (short)Indexer.resStage;
			}
			else if (component.lType == LotCreator.LotType.COMMERCIAL)
			{
				component.MaxStage = (short)Indexer.comStage;
			}
			else if (component.lType == LotCreator.LotType.INDUSTRIAL)
			{
				component.MaxStage = (short)Indexer.indStage;
			}
			else if (component.lType == LotCreator.LotType.MIXED)
			{
				component.MaxStage = (short)Indexer.mixStage;
			}
		}
		lotsCreated++;
		GameObject gameObject3 = (GameObject)Object.Instantiate(Resources.Load("Lots/V/V_" + text2 + "_" + text));
		gameObject3.transform.position = new Vector3((float)xi + num2, ifNotNull.h + 0.501f, (float)zi + num3);
		gameObject3.name = xi + "_vacant_" + zi;
		gameObject3.transform.Rotate(0f, num, 0f, Space.Self);
		gameObject3.layer = 15;
		component.wP = new Vector3(xi, ifNotNull.h, zi);
		component.Orientation = (short)num;
		component.Anchor = new Vector3((float)xi + num2, ifNotNull.h, (float)zi + num3);
		component.Entry = new Vector2Int(ifNotNull2.wP.x, ifNotNull2.wP.y);
		component.tempVacant = gameObject3;
		component.lSize = lSize;
		if (farmSize > 0)
		{
			component.nextDemand = (short)farmSize;
		}
		ifNotNull2.o.lots.Add(component);
		gameObject3.isStatic = true;
		gameObject3.transform.parent = gameObject2.transform;
		gameObject2.transform.parent = tZone.transform;
		lm.ValueUpdate(component);
		component.SetMaxStage();
		if (Messages.step == 3)
		{
			if (component.lType == LotCreator.LotType.RESIDENTIAL)
			{
				Messages.counter++;
			}
		}
		else if (Messages.step == 5)
		{
			if (component.lType == LotCreator.LotType.COMMERCIAL)
			{
				Messages.counter++;
			}
		}
		else if (Messages.step == 7 && component.lType == LotCreator.LotType.INDUSTRIAL)
		{
			Messages.counter++;
		}
		StopVisualize();
	}

	private void PreDensify(LotCreator lc)
	{
		GameObject gameObject = null;
		switch (lc.lSize)
		{
		case LotCreator.LotSize._4x4:
			gameObject = Object.Instantiate(Z_4x4);
			break;
		case LotCreator.LotSize._4x5:
			gameObject = Object.Instantiate(Z_4x5);
			break;
		case LotCreator.LotSize._4x6:
			gameObject = Object.Instantiate(Z_4x6);
			break;
		case LotCreator.LotSize._3x4:
			gameObject = Object.Instantiate(Z_3x4);
			break;
		case LotCreator.LotSize._3x5:
			gameObject = Object.Instantiate(Z_3x5);
			break;
		case LotCreator.LotSize._3x6:
			gameObject = Object.Instantiate(Z_3x6);
			break;
		case LotCreator.LotSize._6x4:
			gameObject = Object.Instantiate(Z_6x4);
			break;
		case LotCreator.LotSize._6x5:
			gameObject = Object.Instantiate(Z_6x5);
			break;
		case LotCreator.LotSize._6x6:
			gameObject = Object.Instantiate(Z_6x6);
			break;
		case LotCreator.LotSize._2x4:
			gameObject = Object.Instantiate(Z_2x4);
			break;
		case LotCreator.LotSize._2x5:
			gameObject = Object.Instantiate(Z_2x5);
			break;
		case LotCreator.LotSize._2x6:
			gameObject = Object.Instantiate(Z_2x6);
			break;
		}
		if (!(gameObject == null))
		{
			gameObject.transform.position = lc.Anchor + Vector3.up;
			gameObject.transform.Rotate(0f, lc.Orientation, 0f, Space.Self);
			tempLots.Add(gameObject);
		}
	}

	public void StopVisualize()
	{
		foreach (GameObject tempLot in tempLots)
		{
			Object.Destroy(tempLot);
		}
		costHelper.gameObject.SetActive(value: false);
		tempLots.Clear();
		changeDensity.Clear();
	}

	private void SetLotStyle()
	{
		if (loc_ind == 20)
		{
			if (sub_ind == 1)
			{
				FriendlyColor = LowRes;
			}
			else if (sub_ind == 2)
			{
				FriendlyColor = MedRes;
			}
			else if (sub_ind >= 3)
			{
				FriendlyColor = HiRes;
			}
		}
		else if (loc_ind == 21)
		{
			if (sub_ind == 1)
			{
				FriendlyColor = LowCom;
			}
			else if (sub_ind == 2)
			{
				FriendlyColor = MedCom;
			}
			else if (sub_ind >= 3)
			{
				FriendlyColor = HiCom;
			}
		}
		else if (loc_ind == 22)
		{
			if (sub_ind == 1)
			{
				FriendlyColor = LowInd;
			}
			else if (sub_ind == 2)
			{
				FriendlyColor = MedInd;
			}
			else if (sub_ind >= 3)
			{
				FriendlyColor = HiInd;
			}
		}
		else if (loc_ind == 24)
		{
			if (sub_ind == 1)
			{
				FriendlyColor = LowMix;
			}
			else if (sub_ind == 2)
			{
				FriendlyColor = MedMix;
			}
			else if (sub_ind >= 3)
			{
				FriendlyColor = HiMix;
			}
		}
		ZoningAtlas.color = FriendlyColor;
	}

	private void Densify()
	{
		foreach (LotCreator item in changeDensity)
		{
			if (loc_ind == 20)
			{
				ChangeLot(item, LotCreator.LotType.RESIDENTIAL);
			}
			else if (loc_ind == 21)
			{
				ChangeLot(item, LotCreator.LotType.COMMERCIAL);
			}
			else if (loc_ind == 22)
			{
				ChangeLot(item, LotCreator.LotType.INDUSTRIAL);
			}
			else if (loc_ind == 24)
			{
				ChangeLot(item, LotCreator.LotType.MIXED);
			}
			item.SetMaxStage();
		}
		StopVisualize();
		sm.Play("LotBuilt");
		costHelper.PayForIt();
		StartCoroutine(UpdateChunks());
	}

	private void ChangeLot(LotCreator oldLc, LotCreator.LotType newlotType)
	{
		if (sub_ind == 1)
		{
			oldLc.MaxStage = 1;
		}
		else if (sub_ind == 2)
		{
			switch (newlotType)
			{
			case LotCreator.LotType.RESIDENTIAL:
				oldLc.MaxStage = 4;
				break;
			case LotCreator.LotType.MIXED:
				oldLc.MaxStage = 2;
				break;
			default:
				oldLc.MaxStage = 3;
				break;
			}
		}
		else if (sub_ind == 3)
		{
			switch (newlotType)
			{
			case LotCreator.LotType.RESIDENTIAL:
				oldLc.MaxStage = 7;
				break;
			case LotCreator.LotType.COMMERCIAL:
				oldLc.MaxStage = 6;
				break;
			case LotCreator.LotType.MIXED:
				oldLc.MaxStage = 4;
				break;
			default:
				oldLc.MaxStage = 4;
				break;
			}
		}
		else if (sub_ind == 4)
		{
			switch (newlotType)
			{
			case LotCreator.LotType.RESIDENTIAL:
				oldLc.MaxStage = (short)Indexer.resStage;
				break;
			case LotCreator.LotType.COMMERCIAL:
				oldLc.MaxStage = (short)Indexer.comStage;
				break;
			case LotCreator.LotType.MIXED:
				oldLc.MaxStage = (short)Indexer.mixStage;
				break;
			default:
				oldLc.MaxStage = (short)Indexer.indStage;
				break;
			}
		}
		if (!oldLc.underConstruction && (oldLc.lType != newlotType || oldLc.Stage > oldLc.MaxStage))
		{
			bi.buldoze.GiveBackDemand(oldLc, oldLc.Occupants);
			if (oldLc.lType == LotCreator.LotType.INDUSTRIAL)
			{
				bi.world.IndustrialBuildings.Remove(oldLc);
				bi.world.VacantIndusLots.Remove(oldLc);
			}
			else if (oldLc.lType == LotCreator.LotType.RESIDENTIAL)
			{
				bi.world.ResidentialBuildings.Remove(oldLc);
				bi.world.VacantResLots.Remove(oldLc);
			}
			else if (oldLc.lType == LotCreator.LotType.COMMERCIAL)
			{
				bi.world.CommercialBuildings.Remove(oldLc);
				bi.world.VacantCommLots.Remove(oldLc);
			}
			else if (oldLc.lType == LotCreator.LotType.MIXED)
			{
				bi.world.MixedBuildings.Remove(oldLc);
				bi.world.VacantMixedLots.Remove(oldLc);
			}
			switch (newlotType)
			{
			case LotCreator.LotType.INDUSTRIAL:
				bi.world.VacantIndusLots.Add(oldLc);
				break;
			case LotCreator.LotType.RESIDENTIAL:
				bi.world.VacantResLots.Add(oldLc);
				break;
			case LotCreator.LotType.COMMERCIAL:
				bi.world.VacantCommLots.Add(oldLc);
				break;
			case LotCreator.LotType.MIXED:
				bi.world.VacantMixedLots.Add(oldLc);
				break;
			}
			oldLc.Stage = 0;
			oldLc.Occupants = 0;
			oldLc.TaxIncome = 0f;
			oldLc.lType = newlotType;
			oldLc.lClass = LotCreator.LotClass.LOW;
			oldLc.ChangeVacantLot();
		}
	}

	private void CheckChunks(BlockData b)
	{
		updates.Add(b.o);
		if (b.pos[0] == 0)
		{
			BlockData ifNotNull = bi.world.GetIfNotNull(b.wP.x - 1, b.wP.y);
			if (ifNotNull == null)
			{
				return;
			}
			updates.Add(ifNotNull.o);
		}
		else if (b.pos[0] == 15)
		{
			BlockData ifNotNull2 = bi.world.GetIfNotNull(b.wP.x + 1, b.wP.y);
			if (ifNotNull2 == null)
			{
				return;
			}
			updates.Add(ifNotNull2.o);
		}
		if (b.pos[1] == 0)
		{
			BlockData ifNotNull3 = bi.world.GetIfNotNull(b.wP.x, b.wP.y - 1);
			if (ifNotNull3 != null)
			{
				updates.Add(ifNotNull3.o);
			}
		}
		else if (b.pos[1] == 15)
		{
			BlockData ifNotNull4 = bi.world.GetIfNotNull(b.wP.x, b.wP.y + 1);
			if (ifNotNull4 != null)
			{
				updates.Add(ifNotNull4.o);
			}
		}
	}

	private IEnumerator UpdateChunks()
	{
		foreach (Chunk item in updates.ToList())
		{
			item.Redraw();
			yield return new WaitForEndOfFrame();
		}
		updates.Clear();
	}

	public int FindLotDepth(int lotZ, bool inverted)
	{
		if (lotZ <= 4)
		{
			return 4;
		}
		if (lotZ == 5)
		{
			return 5;
		}
		if (lotZ == 6)
		{
			return 6;
		}
		if (lotZ == 7)
		{
			return 6;
		}
		if (lotZ == 8)
		{
			return 4;
		}
		if (lotZ == 9)
		{
			if (!inverted)
			{
				return 5;
			}
			return 4;
		}
		if (lotZ == 10)
		{
			return 5;
		}
		if (lotZ == 11)
		{
			if (!inverted)
			{
				return 6;
			}
			return 5;
		}
		if (lotZ >= 12)
		{
			return 6;
		}
		return 0;
	}

	public List<string> FindLotWidths(int width, int type)
	{
		List<string> list = new List<string>();
		int num = width;
		while (num > 1)
		{
			int num2 = num;
			if (num2 > 13)
			{
				num2 = 12;
			}
			switch (type)
			{
			case 0:
				switch (num2)
				{
				case 2:
					list.Add("2");
					num -= 2;
					break;
				case 3:
					list.Add("3");
					num -= 3;
					break;
				case 4:
					list.Add("2");
					list.Add("2");
					num -= 4;
					break;
				case 5:
					list.Add("3");
					list.Add("2");
					num -= 5;
					break;
				case 6:
					list.Add("2");
					list.Add("2");
					list.Add("2");
					num -= 6;
					break;
				case 7:
					list.Add("3");
					list.Add("2");
					list.Add("2");
					num -= 7;
					break;
				case 8:
					list.Add("2");
					list.Add("2");
					list.Add("2");
					list.Add("2");
					num -= 8;
					break;
				case 9:
					list.Add("3");
					list.Add("2");
					list.Add("2");
					list.Add("2");
					num -= 9;
					break;
				case 10:
					list.Add("3");
					list.Add("2");
					list.Add("3");
					list.Add("2");
					num -= 10;
					break;
				case 11:
					list.Add("3");
					list.Add("3");
					list.Add("3");
					list.Add("2");
					num -= 11;
					break;
				case 12:
					list.Add("2");
					list.Add("2");
					list.Add("2");
					list.Add("2");
					list.Add("2");
					list.Add("2");
					num -= 12;
					break;
				case 13:
					if (num == 13)
					{
						list.Add("3");
						list.Add("2");
						list.Add("3");
						list.Add("3");
						list.Add("2");
						num -= 13;
					}
					break;
				}
				continue;
			case 1:
				switch (num2)
				{
				case 2:
					list.Add("2");
					num -= 2;
					break;
				case 3:
					list.Add("3");
					num -= 3;
					break;
				case 4:
					list.Add("4");
					num -= 4;
					break;
				case 5:
					list.Add("3");
					list.Add("2");
					num -= 5;
					break;
				case 6:
					list.Add("6");
					num -= 6;
					break;
				case 7:
					list.Add("3");
					list.Add("4");
					num -= 7;
					break;
				case 8:
					list.Add("4");
					list.Add("4");
					num -= 8;
					break;
				case 9:
					list.Add("6");
					list.Add("3");
					num -= 9;
					break;
				case 10:
					list.Add("4");
					list.Add("3");
					list.Add("3");
					num -= 10;
					break;
				case 11:
					list.Add("4");
					list.Add("4");
					list.Add("3");
					num -= 11;
					break;
				case 12:
					list.Add("4");
					list.Add("4");
					list.Add("4");
					num -= 12;
					break;
				case 13:
					if (num == 13)
					{
						list.Add("4");
						list.Add("3");
						list.Add("3");
						list.Add("3");
						num -= 13;
					}
					break;
				}
				continue;
			case 2:
				switch (num2)
				{
				case 2:
					num -= 2;
					break;
				case 3:
					num -= 3;
					break;
				case 4:
					list.Add("4");
					num -= 4;
					break;
				case 5:
					list.Add("4");
					num -= 5;
					break;
				case 6:
					list.Add("6");
					num -= 6;
					break;
				case 7:
					list.Add("6");
					num -= 7;
					break;
				case 8:
					list.Add("4");
					list.Add("4");
					num -= 8;
					break;
				case 9:
					list.Add("3");
					list.Add("6");
					num -= 9;
					break;
				case 10:
					list.Add("4");
					list.Add("6");
					num -= 10;
					break;
				case 11:
					list.Add("4");
					list.Add("4");
					list.Add("3");
					num -= 11;
					break;
				case 12:
					list.Add("6");
					list.Add("6");
					num -= 12;
					break;
				case 13:
					if (num == 13)
					{
						list.Add("6");
						list.Add("4");
						list.Add("3");
						num -= 13;
					}
					break;
				}
				continue;
			}
			switch (num2)
			{
			case 2:
				num -= 2;
				break;
			case 3:
				list.Add("3");
				num -= 4;
				break;
			case 4:
				list.Add("3");
				num -= 4;
				break;
			case 5:
				list.Add("3");
				num -= 4;
				break;
			case 6:
				list.Add("3");
				list.Add("3");
				num -= 6;
				break;
			case 7:
				list.Add("3");
				list.Add("3");
				num -= 7;
				break;
			case 8:
				list.Add("3");
				list.Add("3");
				num -= 8;
				break;
			case 9:
				list.Add("3");
				list.Add("3");
				list.Add("3");
				num -= 9;
				break;
			case 10:
				list.Add("3");
				list.Add("3");
				list.Add("3");
				num -= 10;
				break;
			case 11:
				list.Add("3");
				list.Add("3");
				list.Add("3");
				num -= 11;
				break;
			case 12:
				list.Add("3");
				list.Add("3");
				list.Add("3");
				list.Add("3");
				num -= 12;
				break;
			case 13:
				if (num == 13)
				{
					list.Add("3");
					list.Add("3");
					list.Add("3");
					list.Add("3");
					num -= 13;
				}
				break;
			}
		}
		return list;
	}
}
