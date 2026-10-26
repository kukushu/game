using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class Roading : MonoBehaviour
{
	public Camera cam;

	public CostHelper costHelper;

	public GameObject whiteSquare;

	public GameObject redSquare;

	public GameObject tRoad;

	public GameObject wSquare_1;

	public GameObject wSquare_2;

	public GameObject wSquare_3;

	public GameObject wSquare_4;

	public Material wMat;

	public GameObject grid;

	public GameObject gridAve;

	public BlockInteraction bi;

	public World w;

	public Avenueing ave;

	public SoundManager sm;

	public LotAudio la;

	public RoadCreator[,,] rD;

	public RoadCreator[,,] rG;

	public List<RoadCreator> busStops;

	public List<RoadCreator> trainStops;

	private HashSet<Vector2Int> aIntersecX = new HashSet<Vector2Int>();

	private HashSet<Vector2Int> aIntersecZ = new HashSet<Vector2Int>();

	private List<RoadCreator> rcBridge = new List<RoadCreator>();

	private int worldSize = World.chunkSize * World.worldSize;

	private Vector2Int pos = Vector2Int.zero;

	public static string subType = "Road/";

	public static string sideType = "sidetree";

	public static string style = "Light/";

	private int price = 20;

	public Color32 azul;

	public Color32 lime;

	private bool suitable = true;

	private int xo1;

	private int zo1;

	private int xo2;

	private int zo2;

	private Vector2Int o;

	public Material trafficLightMat;

	private readonly HashSet<Chunk> updates = new HashSet<Chunk>();

	private HashSet<Vector2Int> tileChanged = new HashSet<Vector2Int>();

	private void Start()
	{
		if (rD == null)
		{
			rD = new RoadCreator[World.worldSize * 16, 3, World.worldSize * 16];
		}
		if (rG == null)
		{
			rG = new RoadCreator[World.worldSize * 16, 3, World.worldSize * 16];
		}
	}

	private void Update()
	{
		if (Indexer.Index == 10)
		{
			o = Vector2Int.zero;
			if (bi.tamere == 0)
			{
				pos = new Vector2Int((int)bi.pos.x, (int)bi.pos.z);
			}
			else
			{
				int num = Mathf.Abs(bi.x1 - bi.x2) + 1;
				int num2 = Mathf.Abs(bi.z1 - bi.z2) + 1;
				if (num >= num2)
				{
					pos = new Vector2Int((int)bi.pos.x, bi.z1);
				}
				else
				{
					pos = new Vector2Int(bi.x1, (int)bi.pos.z);
				}
			}
			if (pos.x >= worldSize - 1)
			{
				pos.x = worldSize - 2;
			}
			if (pos.y < 1)
			{
				pos.y = 1;
			}
			BlockData ifNotNull = w.GetIfNotNull(pos.x, pos.y);
			BlockData ifNotNull2 = w.GetIfNotNull(pos.x + 1, pos.y);
			BlockData bounded = w.GetBounded(pos.x - 1, pos.y);
			BlockData ifNotNull3 = w.GetIfNotNull(pos.x, pos.y - 1);
			BlockData bounded2 = w.GetBounded(pos.x, pos.y + 1);
			BlockData blockData = w.GetBounded(pos.x + 1, pos.y - 1);
			BlockData bounded3 = w.GetBounded(pos.x - 1, pos.y - 1);
			BlockData bounded4 = w.GetBounded(pos.x + 1, pos.y + 1);
			TooltipSystem.Hide();
			if (ifNotNull == null || ifNotNull2 == null || ifNotNull3 == null || bounded == null || bounded2 == null || bounded3 == null || bounded4 == null || blockData == null || bi.pos == Vector3.zero)
			{
				return;
			}
			if (bi.tamere == 0)
			{
				if (ifNotNull.r && ifNotNull2.r && ifNotNull3.r && blockData.r)
				{
					wMat.color = azul;
				}
				else if (!ifNotNull.r && !ifNotNull2.r && !ifNotNull3.r && !blockData.r)
				{
					wMat.color = lime;
				}
				else if (ifNotNull.r && ifNotNull2.r && !ifNotNull3.r)
				{
					o.y = 1;
					wMat.color = azul;
				}
				else if (!ifNotNull.r && ifNotNull3.r && !ifNotNull2.r)
				{
					o.y = -1;
					wMat.color = azul;
				}
				else if (!ifNotNull.r && ifNotNull2.r && blockData.r)
				{
					o.x = 1;
					wMat.color = azul;
				}
				else if (ifNotNull.r && !ifNotNull2.r && bounded3.r)
				{
					o.x = -1;
					wMat.color = azul;
				}
				else if (!ifNotNull.r && !ifNotNull3.r && bounded.r && bounded3.r)
				{
					o.x = -2;
					wMat.color = azul;
				}
				else if (!ifNotNull.r && bounded2.r && !ifNotNull2.r && bounded4.r)
				{
					o.y = 2;
					wMat.color = azul;
				}
				else
				{
					wMat.color = Color.red;
					TooltipSystem.Show("", "Unfeasible Road Layout");
				}
				if (wMat.color != Color.red)
				{
					ifNotNull = w.GetIfNotNull(pos.x + o.x, pos.y + o.y);
					ifNotNull2 = w.GetIfNotNull(pos.x + 1 + o.x, pos.y + o.y);
					ifNotNull3 = w.GetIfNotNull(pos.x + o.x, pos.y - 1 + o.y);
					blockData = w.GetIfNotNull(pos.x + 1 + o.x, pos.y - 1 + o.y);
				}
			}
			else
			{
				wMat.color = lime;
			}
			if (ifNotNull.h < 29.75f || ifNotNull2.h < 29.75f || ifNotNull3.h < 29.75f || blockData.h < 29.75f)
			{
				wMat.color = Color.red;
				TooltipSystem.Show("", "Unfeasible Road Layout");
			}
			wSquare_1.transform.position = new Vector3(ifNotNull.wP.x, ifNotNull.h + 1f, ifNotNull.wP.y);
			wSquare_2.transform.position = new Vector3(ifNotNull2.wP.x, ifNotNull2.h + 1f, ifNotNull2.wP.y);
			wSquare_3.transform.position = new Vector3(blockData.wP.x, blockData.h + 1f, blockData.wP.y);
			wSquare_4.transform.position = new Vector3(ifNotNull3.wP.x, ifNotNull3.h + 1f, ifNotNull3.wP.y);
			xo1 = ifNotNull.wP.x;
			zo1 = ifNotNull.wP.y;
			xo2 = ifNotNull.wP.x;
			zo2 = ifNotNull.wP.y;
			grid.transform.position = new Vector3((float)ifNotNull.wP.x + 0.5f, 60f, (float)ifNotNull.wP.y - 0.5f);
		}
		if (Input.GetKeyDown(KeyCode.Escape) && Indexer.Index == 10 && bi.tamere == 1)
		{
			bi.x1 = 0;
			bi.z1 = 0;
			bi.x2 = 0;
			bi.z2 = 0;
			bi.tamere = 0;
			StopVisualizeRoad();
			sm.Play("Impractical");
		}
		if (Input.GetMouseButtonDown(0) && Indexer.Index == 10 && bi.tamere == 0)
		{
			if (EventSystem.current.IsPointerOverGameObject())
			{
				return;
			}
			if (wMat.color == Color.red || bi.pos.y < 29.75f)
			{
				sm.Play("Impractical");
			}
			else
			{
				bi.x1 = xo1;
				bi.z1 = zo1;
				bi.tamere = 1;
			}
		}
		if (Input.GetMouseButton(0) && Indexer.Index == 10 && bi.tamere == 1)
		{
			if (EventSystem.current.IsPointerOverGameObject())
			{
				return;
			}
			if (xo2 != bi.oldx2 || zo2 != bi.oldz2)
			{
				RoadUpdate(bi.x1, bi.z1);
			}
		}
		if (Input.GetMouseButtonUp(0) && Indexer.Index == 10 && bi.tamere == 1)
		{
			if (wMat.color != Color.red && suitable)
			{
				BuildRoadLines(bi.x1, bi.z1);
				return;
			}
			bi.x1 = 0;
			bi.z1 = 0;
			bi.x2 = 0;
			bi.z2 = 0;
			bi.tamere = 0;
			StopVisualizeRoad();
			sm.Play("Impractical");
		}
	}

	private void RoadUpdate(int _x1, int _z1)
	{
		bi.oldx2 = xo2;
		bi.oldz2 = zo2;
		int x = bi.x1;
		int z = bi.z1;
		int num = xo2;
		int num2 = zo2;
		float y = bi.pos.y + 0.5f;
		int num3;
		int num4;
		if (x >= num)
		{
			num3 = num;
			num4 = x;
		}
		else
		{
			num3 = x;
			num4 = num;
		}
		int num5;
		int num6;
		if (z >= num2)
		{
			num5 = num2;
			num6 = z;
		}
		else
		{
			num5 = z;
			num6 = num2;
		}
		int num7 = Mathf.Abs(x - num) + 1;
		int num8 = Mathf.Abs(z - num2) + 1;
		StopVisualizeRoad();
		suitable = true;
		sm.Play("RoadPiece");
		if (w.GetIfNotNull((int)bi.pos.x, (int)bi.pos.z).h < 29.75f)
		{
			suitable = false;
		}
		if (Indexer.roadStyle == "Dirt/")
		{
			price = 5;
		}
		else
		{
			price = 20;
		}
		if (num7 >= num8)
		{
			costHelper.SetCost(num7 * price);
			if (w.GetIfNotNull(_x1, _z1 + 1) == null || w.GetIfNotNull(_x1, _z1 - 2) == null)
			{
				suitable = false;
			}
			for (int i = num3; i < num4 + 2; i++)
			{
				VisualizeRoad(new Vector3(i, y, _z1));
				VisualizeRoad(new Vector3(i, y, _z1 - 1));
			}
		}
		else
		{
			if (w.GetIfNotNull(_x1 - 1, _z1) == null || w.GetIfNotNull(_x1 + 2, _z1) == null)
			{
				suitable = false;
			}
			costHelper.SetCost(num8 * price);
			for (int j = num5 - 1; j < num6 + 1; j++)
			{
				VisualizeRoad(new Vector3(_x1, y, j));
				VisualizeRoad(new Vector3(_x1 + 1, y, j));
			}
		}
	}

	private void VisualizeRoad(Vector3 pos)
	{
		BlockData blockData = w.blockData[(int)pos.x, (int)pos.z];
		Vector3 position = new Vector3(blockData.wP.x, blockData.h + 0.515f, blockData.wP.y);
		if ((!blockData.r && blockData.b && !blockData.a) || !suitable)
		{
			if (suitable)
			{
				TooltipSystem.Show("", "Area Occupied");
			}
			suitable = false;
			sm.Play("Impractical");
			Object.Instantiate(redSquare, position, Quaternion.identity);
			redSquare.name = "NewCube";
			redSquare.tag = "NewCube";
		}
		else
		{
			Object.Instantiate(whiteSquare, position, Quaternion.identity);
			whiteSquare.name = "NewCube";
			whiteSquare.tag = "NewCube";
		}
	}

	private void BuildRoadLines(int _x1, int _z1)
	{
		subType = Indexer.roadSubType;
		style = Indexer.roadStyle;
		suitable = true;
		int x = bi.x1;
		int z = bi.z1;
		int num = xo2;
		int num2 = zo2;
		int num3;
		int num4;
		if (x >= num)
		{
			num3 = num;
			num4 = x;
		}
		else
		{
			num3 = x;
			num4 = num;
		}
		int num5;
		int num6;
		if (z >= num2)
		{
			num5 = num2;
			num6 = z;
		}
		else
		{
			num5 = z;
			num6 = num2;
		}
		int num7 = Mathf.Abs(x - num) + 1;
		int num8 = Mathf.Abs(z - num2) + 1;
		if (num7 >= num8)
		{
			for (int i = num3; i < num4 + 2; i++)
			{
				w.GetBounded(i, _z1).r = true;
				w.GetBounded(i, _z1 - 1).r = true;
			}
			for (int j = num3; j < num4 + 2; j++)
			{
				FlatNewLineX(new Vector2Int(j, _z1));
			}
			for (int k = num3; k < num4 + 2; k++)
			{
				BuildNewLineX(new Vector2Int(k, _z1));
			}
		}
		else
		{
			for (int l = num5 - 1; l < num6 + 1; l++)
			{
				w.GetBounded(_x1, l).r = true;
				w.GetBounded(_x1 + 1, l).r = true;
			}
			for (int m = num5 - 1; m < num6 + 1; m++)
			{
				FlatNewLineZ(new Vector2Int(_x1, m));
			}
			for (int n = num5 - 1; n < num6 + 1; n++)
			{
				BuildNewLineZ(new Vector2Int(_x1, n));
			}
		}
		string text = style;
		foreach (Vector2Int item in tileChanged.ToList())
		{
			RoadCreator ifNotNull = GetIfNotNull(item.x, 1, item.y);
			if (GetIfNotNull(item.x, 2, item.y) != null)
			{
				Debug.Log("continue");
			}
			else if (!(ifNotNull == null) && ifNotNull.rType == RoadCreator.RoadType.ROAD && !ifNotNull.tile.StartsWith("ac"))
			{
				string existingType = ifNotNull.subType;
				if (ifNotNull.style == "Dirt/")
				{
					style = "Dirt/";
				}
				else
				{
					style = text;
				}
				if (ifNotNull.isIntersec)
				{
					DestroyRoad(item.x, item.y);
					CreateIntersection(item, 0, existingType);
				}
				else
				{
					DestroyRoad(item.x, item.y);
					CreateStraight(item, existingType);
				}
			}
		}
		style = text;
		tileChanged.Clear();
		foreach (Vector2Int item2 in aIntersecX.ToList())
		{
			Vector3 vector = new Vector3(item2.x, 16f, item2.y);
			ave.IntersectionRoadMasterLineX(vector);
		}
		foreach (Vector2Int item3 in aIntersecZ.ToList())
		{
			Vector3 vector2 = new Vector3(item3.x, 16f, item3.y);
			ave.IntersectionRoadMasterLineZ(vector2);
		}
		aIntersecX.Clear();
		aIntersecZ.Clear();
		StartCoroutine(UpdateChunks());
		bi.x1 = 0;
		bi.z1 = 0;
		bi.x2 = 0;
		bi.z2 = 0;
		bi.tamere = 0;
		StopVisualizeRoad();
		sm.Play("RoadBuilt");
		costHelper.PayForIt();
	}

	public void FlatNewLineX(Vector2Int pos)
	{
		RoadCreator ifNotNull = GetIfNotNull(pos.x, 1, pos.y);
		RoadCreator ifNotNull2 = GetIfNotNull(pos.x, 1, pos.y - 1);
		if (ifNotNull != null)
		{
			if (ifNotNull.isBridge || ifNotNull.rType == RoadCreator.RoadType.PATH)
			{
				return;
			}
			if (ifNotNull.rType != RoadCreator.RoadType.ROAD && ifNotNull.rType == RoadCreator.RoadType.AVENUE)
			{
				if (ifNotNull.cc == 17)
				{
					aIntersecZ.Add(new Vector2Int(pos.x, pos.y - 1));
				}
				if (ifNotNull.cc == 12)
				{
					aIntersecZ.Add(new Vector2Int(pos.x, pos.y - 1));
				}
			}
			DestroyRoad(pos.x, pos.y);
		}
		if (ifNotNull2 != null)
		{
			if (ifNotNull2.isBridge || ifNotNull2.rType == RoadCreator.RoadType.PATH)
			{
				return;
			}
			DestroyRoad(pos.x, pos.y - 1);
		}
		BlockData blockData = w.blockData[pos.x, pos.y];
		BlockData bounded = w.GetBounded(pos.x, pos.y - 1);
		BlockData bounded2 = w.GetBounded(pos.x, pos.y - 2);
		BlockData bounded3 = w.GetBounded(pos.x, pos.y + 1);
		BlockData bounded4 = w.GetBounded(pos.x + 1, pos.y - 2);
		BlockData bounded5 = w.GetBounded(pos.x + 1, pos.y);
		BlockData bounded6 = w.GetBounded(pos.x - 1, pos.y);
		BlockData bounded7 = w.GetBounded(pos.x + 2, pos.y);
		BlockData bounded8 = w.GetBounded(pos.x + 1, pos.y + 1);
		BlockData bounded9 = w.GetBounded(pos.x + 3, pos.y);
		BlockData bounded10 = w.GetBounded(pos.x, pos.y + 2);
		BlockData bounded11 = w.GetBounded(pos.x + 5, pos.y);
		BlockData bounded12 = w.GetBounded(pos.x - 1, pos.y - 1);
		BlockData bounded13 = w.GetBounded(pos.x, pos.y - 3);
		BlockData bounded14 = w.GetBounded(pos.x - 2, pos.y);
		BlockData bounded15 = w.GetBounded(pos.x - 2, pos.y - 1);
		if (GetIfNotNull(pos.x + 1, 1, pos.y) == null)
		{
			if (blockData.h == w.GetBounded(pos.x + 5, pos.y).h)
			{
				bounded5.FlatHeight(blockData.h);
			}
			if (blockData.h == w.GetBounded(pos.x + 4, pos.y).h)
			{
				bounded5.FlatHeight(blockData.h);
			}
			if (blockData.h == bounded9.h)
			{
				bounded5.FlatHeight(blockData.h);
			}
			if (blockData.h == bounded7.h)
			{
				bounded5.FlatHeight(blockData.h);
			}
			if (bounded11.h == blockData.h + 0.5f && bounded11.r)
			{
				bounded5.FlatHeight(blockData.h + 0.25f);
				bounded7.FlatHeight(blockData.h + 0.5f);
			}
			else if (bounded11.h == blockData.h - 0.5f && bounded11.r)
			{
				bounded5.FlatHeight(blockData.h - 0.25f);
				bounded7.FlatHeight(blockData.h - 0.5f);
			}
		}
		if (bounded5.h > blockData.h + 0.5f && blockData.h > 30f)
		{
			bounded5.FlatHeight(blockData.h + 0.5f);
		}
		else if (bounded5.h < blockData.h - 0.5f && blockData.h > 30f)
		{
			bounded5.FlatHeight(blockData.h - 0.5f);
		}
		if (!w.GetBounded(pos.x + 1, pos.y - 3).r && !w.GetBounded(pos.x + 1, pos.y + 2).r)
		{
			if (bounded4.r && bounded4.h != bounded5.h)
			{
				bounded5.FlatHeight(bounded4.h);
			}
			if (bounded8.r && bounded8.h != bounded5.h)
			{
				bounded5.FlatHeight(bounded8.h);
			}
		}
		if ((bounded3.r && blockData.h > 30f) || (bounded2.r && blockData.h > 30f))
		{
			if (bounded3.r && !bounded2.r)
			{
				blockData.FlatHeight(bounded3.h);
			}
			else if (bounded2.r && !bounded3.r)
			{
				blockData.FlatHeight(bounded2.h);
			}
			else if (bounded3.r && bounded2.r)
			{
				float num = (bounded3.h + bounded2.h) / 2f;
				num *= 4f;
				num = Mathf.RoundToInt(num);
				num /= 4f;
				blockData.FlatHeight(num);
			}
			if (bounded3.r)
			{
				if (bounded3.h > blockData.h)
				{
					bounded3.FlatHeight(blockData.h);
				}
				else if (bounded3.h < blockData.h - 0.5f)
				{
					bounded3.FlatHeight(blockData.h - 0.5f);
				}
			}
			if (bounded10.r)
			{
				if (bounded10.h > bounded3.h + 0.5f)
				{
					bounded10.FlatHeight(bounded3.h + 0.5f);
				}
				else if (bounded10.h < bounded3.h - 0.5f)
				{
					bounded10.FlatHeight(bounded3.h - 0.5f);
				}
			}
			if (bounded2.r)
			{
				if (bounded2.h > blockData.h)
				{
					bounded2.FlatHeight(blockData.h);
				}
				else if (bounded2.h < blockData.h - 0.5f)
				{
					bounded2.FlatHeight(blockData.h - 0.5f);
				}
			}
			if (bounded13.r)
			{
				if (bounded13.h > bounded2.h + 0.5f)
				{
					bounded13.FlatHeight(bounded2.h + 0.5f);
				}
				else if (bounded13.h < bounded2.h - 0.5f)
				{
					bounded13.FlatHeight(bounded2.h - 0.5f);
				}
			}
			if (bounded5.r)
			{
				if (bounded5.h > blockData.h)
				{
					bounded5.FlatHeight(blockData.h);
					w.GetBounded(pos.x + 1, pos.y - 1).FlatHeight(blockData.h);
				}
				else if (bounded5.h < blockData.h - 0.5f)
				{
					bounded5.FlatHeight(blockData.h - 0.5f);
					w.GetBounded(pos.x + 1, pos.y - 1).FlatHeight(blockData.h - 0.5f);
				}
			}
			if (bounded7.r)
			{
				if (bounded7.h > bounded5.h + 0.5f)
				{
					bounded7.FlatHeight(bounded5.h + 0.5f);
					w.GetBounded(pos.x + 2, pos.y - 1).FlatHeight(bounded5.h + 0.5f);
				}
				else if (bounded7.h < bounded5.h - 0.5f)
				{
					bounded7.FlatHeight(bounded5.h - 0.5f);
					w.GetBounded(pos.x + 2, pos.y - 1).FlatHeight(bounded5.h - 0.5f);
				}
			}
			if (bounded6.r)
			{
				if (bounded6.h > blockData.h)
				{
					bounded6.FlatHeight(blockData.h);
					bounded12.FlatHeight(blockData.h);
				}
				else if (bounded6.h < blockData.h - 0.5f)
				{
					bounded6.FlatHeight(blockData.h - 0.5f);
					bounded12.FlatHeight(blockData.h - 0.5f);
				}
			}
			if (bounded14.r)
			{
				if (bounded14.h > bounded6.h + 0.5f)
				{
					bounded14.FlatHeight(bounded6.h + 0.5f);
					bounded15.FlatHeight(bounded6.h + 0.5f);
				}
				else if (bounded14.h < bounded6.h - 0.5f)
				{
					bounded14.FlatHeight(bounded6.h - 0.5f);
					bounded15.FlatHeight(bounded6.h - 0.5f);
				}
			}
			for (int i = -2; i <= 3; i++)
			{
				for (int j = -3; j <= 2; j++)
				{
					RoadCreator ifNotNull3 = GetIfNotNull(pos.x + i, 1, pos.y + j);
					if (ifNotNull3 != null)
					{
						if (ifNotNull3.rType == RoadCreator.RoadType.ROAD)
						{
							tileChanged.Add(new Vector2Int(pos.x + i, pos.y + j));
							continue;
						}
						_ = ifNotNull3.rType;
						_ = 1;
					}
				}
			}
		}
		else if (!bounded6.r)
		{
			blockData.FlatHeight(bounded5.h);
		}
		else if (!bounded5.r)
		{
			blockData.FlatHeight(bounded6.h);
		}
		if (blockData.h < 30.25f && blockData.h >= 29.75f)
		{
			blockData.FlatHeight(30.25f);
		}
		if (bounded.h != blockData.h)
		{
			bounded.FlatHeight(blockData.h);
		}
		CheckChunks(blockData);
		CheckChunks(bounded);
	}

	public void BuildNewLineX(Vector2Int pos)
	{
		RoadCreator ifNotNull = GetIfNotNull(pos.x, 1, pos.y);
		RoadCreator ifNotNull2 = GetIfNotNull(pos.x, 1, pos.y - 1);
		if ((ifNotNull != null && (ifNotNull.isBridge || ifNotNull.rType == RoadCreator.RoadType.PATH)) || (ifNotNull2 != null && (ifNotNull2.isBridge || ifNotNull2.rType == RoadCreator.RoadType.PATH)))
		{
			return;
		}
		if ((pos.x == 0 && ifNotNull == null) || (pos.x == worldSize - 1 && ifNotNull == null))
		{
			BuildConnectionRoadLineX(pos);
			return;
		}
		BlockData bounded = w.GetBounded(pos.x, pos.y - 2);
		if (w.GetBounded(pos.x, pos.y + 1).r || bounded.r)
		{
			if (GetIfNotNull(pos.x, 2, pos.y) != null)
			{
				CreateIntersection(pos, 14);
				CreateIntersection(new Vector2Int(pos.x, pos.y - 1), 11);
			}
			else
			{
				CreateIntersection(pos);
				CreateIntersection(new Vector2Int(pos.x, pos.y - 1));
			}
		}
		else
		{
			CreateStraight(pos);
			CreateStraight(new Vector2Int(pos.x, pos.y - 1));
		}
		RoadCreator ifNotNull3 = GetIfNotNull(pos.x, 2, pos.y);
		RoadCreator ifNotNull4 = GetIfNotNull(pos.x, 2, pos.y - 1);
		if (ifNotNull3 != null && ifNotNull3.tile == "side")
		{
			ifNotNull3.tile = "sideroad";
		}
		if (ifNotNull4 != null && ifNotNull4.tile == "side")
		{
			ifNotNull4.tile = "sideroad";
		}
	}

	public void FlatNewLineZ(Vector2Int pos)
	{
		RoadCreator ifNotNull = GetIfNotNull(pos.x, 1, pos.y);
		RoadCreator ifNotNull2 = GetIfNotNull(pos.x + 1, 1, pos.y);
		if (ifNotNull != null)
		{
			if (ifNotNull.isBridge || ifNotNull.rType == RoadCreator.RoadType.PATH)
			{
				return;
			}
			if (ifNotNull.rType == RoadCreator.RoadType.AVENUE && ifNotNull.cc == 18)
			{
				aIntersecX.Add(new Vector2Int(pos.x, pos.y));
			}
			DestroyRoad(pos.x, pos.y);
		}
		if (ifNotNull2 != null)
		{
			if (ifNotNull2.isBridge || ifNotNull2.rType == RoadCreator.RoadType.PATH)
			{
				return;
			}
			if (ifNotNull2.rType == RoadCreator.RoadType.AVENUE && ifNotNull2.cc == 9)
			{
				aIntersecX.Add(new Vector2Int(pos.x, pos.y));
			}
			DestroyRoad(pos.x + 1, pos.y);
		}
		BlockData blockData = w.blockData[pos.x, pos.y];
		BlockData bounded = w.GetBounded(pos.x - 1, pos.y);
		BlockData bounded2 = w.GetBounded(pos.x + 3, pos.y);
		BlockData bounded3 = w.GetBounded(pos.x + 1, pos.y);
		BlockData bounded4 = w.GetBounded(pos.x + 2, pos.y);
		BlockData bounded5 = w.GetBounded(pos.x, pos.y + 1);
		BlockData bounded6 = w.GetBounded(pos.x, pos.y - 1);
		BlockData bounded7 = w.GetBounded(pos.x, pos.y + 2);
		BlockData bounded8 = w.GetBounded(pos.x - 1, pos.y + 1);
		BlockData bounded9 = w.GetBounded(pos.x, pos.y + 3);
		BlockData bounded10 = w.GetBounded(pos.x + 2, pos.y + 1);
		BlockData bounded11 = w.GetBounded(pos.x, pos.y + 5);
		BlockData bounded12 = w.GetBounded(pos.x + 1, pos.y - 1);
		BlockData bounded13 = w.GetBounded(pos.x - 2, pos.y);
		BlockData bounded14 = w.GetBounded(pos.x, pos.y - 2);
		BlockData bounded15 = w.GetBounded(pos.x + 1, pos.y - 2);
		if (GetIfNotNull(pos.x, 1, pos.y + 1) == null)
		{
			if (blockData.h == w.GetBounded(pos.x, pos.y + 5).h)
			{
				bounded5.FlatHeight(blockData.h);
			}
			if (blockData.h == w.GetBounded(pos.x, pos.y + 4).h)
			{
				bounded5.FlatHeight(blockData.h);
			}
			if (blockData.h == bounded9.h)
			{
				bounded5.FlatHeight(blockData.h);
			}
			if (blockData.h == bounded7.h)
			{
				bounded5.FlatHeight(blockData.h);
			}
			if (bounded11.h == blockData.h + 0.5f && bounded11.r)
			{
				bounded3.FlatHeight(blockData.h + 0.25f);
				bounded7.FlatHeight(blockData.h + 0.5f);
			}
			else if (bounded11.h == blockData.h - 0.5f && bounded11.r)
			{
				bounded5.FlatHeight(blockData.h - 0.25f);
				bounded7.FlatHeight(blockData.h - 0.5f);
			}
		}
		if (bounded5.h > blockData.h + 0.5f && blockData.h > 30f)
		{
			bounded5.FlatHeight(blockData.h + 0.5f);
		}
		else if (bounded5.h < blockData.h - 0.5f && blockData.h > 30f)
		{
			bounded5.FlatHeight(blockData.h - 0.5f);
		}
		if (!w.GetBounded(pos.x - 2, pos.y + 1).r && !w.GetBounded(pos.x + 3, pos.y + 1).r)
		{
			if (bounded8.r && bounded8.h != bounded5.h)
			{
				bounded5.FlatHeight(bounded8.h);
			}
			if (bounded10.r && bounded10.h != bounded5.h)
			{
				bounded5.FlatHeight(bounded10.h);
			}
		}
		if ((bounded4.r && blockData.h > 30f) || (bounded.r && blockData.h > 30f))
		{
			if (bounded4.r && !bounded.r)
			{
				blockData.FlatHeight(bounded4.h);
				tileChanged.Add(bounded4.wP);
			}
			else if (bounded.r && !bounded4.r)
			{
				blockData.FlatHeight(bounded.h);
				tileChanged.Add(bounded.wP);
			}
			else if (bounded4.r && bounded.r)
			{
				float num = (bounded4.h + bounded.h) / 2f;
				num *= 4f;
				num = Mathf.RoundToInt(num);
				num /= 4f;
				blockData.FlatHeight(num);
			}
			if (bounded4.r)
			{
				if (bounded4.h > blockData.h)
				{
					bounded4.FlatHeight(blockData.h);
				}
				else if (bounded4.h < blockData.h - 0.5f)
				{
					bounded4.FlatHeight(blockData.h - 0.5f);
				}
			}
			if (bounded2.r)
			{
				if (bounded2.h > bounded4.h + 0.5f)
				{
					bounded2.FlatHeight(bounded4.h + 0.5f);
				}
				else if (bounded2.h < bounded4.h - 0.5f)
				{
					bounded2.FlatHeight(bounded4.h - 0.5f);
				}
			}
			if (bounded.r)
			{
				if (bounded.h > blockData.h)
				{
					bounded.FlatHeight(blockData.h);
				}
				else if (bounded.h < blockData.h - 0.5f)
				{
					bounded.FlatHeight(blockData.h - 0.5f);
				}
			}
			if (bounded13.r)
			{
				if (bounded13.h > bounded.h + 0.5f)
				{
					bounded13.FlatHeight(bounded.h + 0.5f);
				}
				else if (bounded13.h < bounded.h - 0.5f)
				{
					bounded13.FlatHeight(bounded.h - 0.5f);
				}
			}
			if (bounded5.r)
			{
				if (bounded5.h > blockData.h)
				{
					bounded5.FlatHeight(blockData.h);
					w.GetBounded(pos.x + 1, pos.y + 1).FlatHeight(blockData.h);
				}
				else if (bounded5.h < blockData.h - 0.5f)
				{
					bounded5.FlatHeight(blockData.h - 0.5f);
					w.GetBounded(pos.x + 1, pos.y + 1).FlatHeight(blockData.h - 0.5f);
				}
			}
			if (bounded7.r)
			{
				if (bounded7.h > bounded5.h + 0.5f)
				{
					bounded7.FlatHeight(bounded5.h + 0.5f);
					w.GetBounded(pos.x + 1, pos.y + 2).FlatHeight(bounded5.h + 0.5f);
				}
				else if (bounded7.h < bounded5.h - 0.5f)
				{
					bounded7.FlatHeight(bounded5.h - 0.5f);
					w.GetBounded(pos.x + 1, pos.y + 2).FlatHeight(bounded5.h - 0.5f);
				}
			}
			if (bounded6.r)
			{
				if (bounded6.h > blockData.h)
				{
					bounded6.FlatHeight(blockData.h);
					bounded12.FlatHeight(blockData.h);
				}
				else if (bounded6.h < blockData.h - 0.5f)
				{
					bounded6.FlatHeight(blockData.h - 0.5f);
					bounded12.FlatHeight(blockData.h - 0.5f);
				}
			}
			if (bounded14.r)
			{
				if (bounded14.h > bounded6.h + 0.5f)
				{
					bounded14.FlatHeight(bounded6.h + 0.5f);
					bounded15.FlatHeight(bounded6.h + 0.5f);
				}
				else if (bounded14.h < bounded6.h - 0.5f)
				{
					bounded14.FlatHeight(bounded6.h - 0.5f);
					bounded15.FlatHeight(bounded6.h - 0.5f);
				}
			}
			for (int i = -2; i <= 3; i++)
			{
				for (int j = -2; j <= 3; j++)
				{
					RoadCreator ifNotNull3 = GetIfNotNull(pos.x + i, 1, pos.y + j);
					if (ifNotNull3 != null)
					{
						if (ifNotNull3.rType == RoadCreator.RoadType.ROAD)
						{
							tileChanged.Add(new Vector2Int(pos.x + i, pos.y + j));
							continue;
						}
						_ = ifNotNull3.rType;
						_ = 1;
					}
				}
			}
		}
		else if (!bounded6.r)
		{
			blockData.FlatHeight(bounded5.h);
		}
		else if (!bounded5.r)
		{
			blockData.FlatHeight(bounded6.h);
		}
		if (blockData.h < 30.25f && blockData.h >= 29.75f)
		{
			blockData.FlatHeight(30.25f);
		}
		if (bounded3.h != blockData.h)
		{
			bounded3.FlatHeight(blockData.h);
		}
		CheckChunks(blockData);
		CheckChunks(bounded3);
	}

	public void BuildNewLineZ(Vector2Int pos)
	{
		RoadCreator ifNotNull = GetIfNotNull(pos.x, 1, pos.y);
		RoadCreator ifNotNull2 = GetIfNotNull(pos.x + 1, 1, pos.y);
		if ((ifNotNull != null && (ifNotNull.isBridge || ifNotNull.rType == RoadCreator.RoadType.PATH)) || (ifNotNull2 != null && (ifNotNull2.isBridge || ifNotNull2.rType == RoadCreator.RoadType.PATH)))
		{
			return;
		}
		if ((pos.y == 0 && ifNotNull == null) || (pos.y == worldSize - 1 && ifNotNull == null))
		{
			BuildConnectionRoadLineZ(pos);
			return;
		}
		BlockData bounded = w.GetBounded(pos.x + 2, pos.y);
		BlockData bounded2 = w.GetBounded(pos.x - 1, pos.y);
		if (bounded.r || bounded2.r)
		{
			if (GetIfNotNull(pos.x, 2, pos.y) != null)
			{
				CreateIntersection(pos, 7);
				CreateIntersection(new Vector2Int(pos.x + 1, pos.y), 13);
			}
			else
			{
				CreateIntersection(pos);
				CreateIntersection(new Vector2Int(pos.x + 1, pos.y));
			}
		}
		else
		{
			CreateStraight(pos);
			CreateStraight(new Vector2Int(pos.x + 1, pos.y));
		}
		RoadCreator ifNotNull3 = GetIfNotNull(pos.x, 2, pos.y);
		RoadCreator ifNotNull4 = GetIfNotNull(pos.x + 1, 2, pos.y);
		if (ifNotNull3 != null && ifNotNull3.tile == "side")
		{
			ifNotNull3.tile = "sideroad";
		}
		if (ifNotNull4 != null && ifNotNull4.tile == "side")
		{
			ifNotNull4.tile = "sideroad";
		}
	}

	private int GetBitwise(Vector2Int v)
	{
		int num = 0;
		if (IsRoadRoad(v.x, v.y + 1))
		{
			num++;
		}
		if (IsRoadRoad(v.x + 1, v.y))
		{
			num += 2;
		}
		if (IsRoadRoad(v.x, v.y - 1))
		{
			num += 4;
		}
		if (IsRoadRoad(v.x - 1, v.y))
		{
			num += 8;
		}
		if (num != 15)
		{
			return num;
		}
		num = 100;
		if (IsRoadRoad(v.x - 1, v.y + 1))
		{
			num++;
		}
		if (IsRoadRoad(v.x + 1, v.y + 1))
		{
			num += 2;
		}
		if (IsRoadRoad(v.x + 1, v.y - 1))
		{
			num += 4;
		}
		if (IsRoadRoad(v.x - 1, v.y - 1))
		{
			num += 8;
		}
		return num;
	}

	private bool IsRoadRoad(int x, int y)
	{
		if (w.GetBounded(x, y).r)
		{
			return true;
		}
		return false;
	}

	public void CreateStraight(Vector2Int pos, string existingType = "")
	{
		string text = subType;
		if (existingType != "")
		{
			if (existingType == "Road/")
			{
				subType = "Road/";
			}
			else if (existingType == "Street/")
			{
				subType = "Street/";
			}
		}
		int bitwise = GetBitwise(pos);
		BlockData bounded = w.GetBounded(pos.x, pos.y);
		BlockData bounded2 = w.GetBounded(pos.x, pos.y + 1);
		BlockData bounded3 = w.GetBounded(pos.x, pos.y - 1);
		BlockData bounded4 = w.GetBounded(pos.x + 1, pos.y);
		BlockData bounded5 = w.GetBounded(pos.x - 1, pos.y);
		switch (bitwise)
		{
		case 114:
			CreatePiece(pos, 270, "curve_in_no", 8, inters: true);
			break;
		case 113:
			CreatePiece(pos, 0, "curve_in_no", 5, inters: true);
			break;
		case 111:
			CreatePiece(pos, 90, "curve_in_no", 6, inters: true);
			break;
		case 107:
			CreatePiece(pos, 180, "curve_in_no", 7, inters: true);
			break;
		case 14:
			if (bounded4.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 0, "straight25", 2, inters: false);
			}
			else if (bounded5.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 180, "straight25in", 2, inters: false);
			}
			else if (bounded4.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 0, "straight50", 2, inters: false);
			}
			else if (bounded5.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 180, "straight50in", 2, inters: false);
			}
			else
			{
				CreatePiece(pos, 0, "straight", 2, inters: false);
			}
			break;
		case 11:
			if (bounded4.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 0, "straight25in", 1, inters: false);
			}
			else if (bounded5.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 180, "straight25", 1, inters: false);
			}
			else if (bounded4.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 0, "straight50in", 1, inters: false);
			}
			else if (bounded5.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 180, "straight50", 1, inters: false);
			}
			else
			{
				CreatePiece(pos, 180, "straight", 1, inters: false);
			}
			break;
		case 7:
			if (bounded2.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 270, "straight25", 4, inters: false);
			}
			else if (bounded3.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 90, "straight25in", 4, inters: false);
			}
			else if (bounded2.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 270, "straight50", 4, inters: false);
			}
			else if (bounded3.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 90, "straight50in", 4, inters: false);
			}
			else
			{
				CreatePiece(pos, 270, "straight", 4, inters: false);
			}
			break;
		case 13:
			if (bounded2.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 270, "straight25in", 3, inters: false);
			}
			else if (bounded3.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 90, "straight25", 3, inters: false);
			}
			else if (bounded2.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 270, "straight50in", 3, inters: false);
			}
			else if (bounded3.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 90, "straight50", 3, inters: false);
			}
			else
			{
				CreatePiece(pos, 90, "straight", 3, inters: false);
			}
			break;
		case 6:
			CreatePiece(pos, 90, "curveout", 4, inters: true);
			break;
		case 12:
			CreatePiece(pos, 180, "curveout", 2, inters: true);
			break;
		case 9:
			CreatePiece(pos, 270, "curveout", 3, inters: true);
			break;
		case 3:
			CreatePiece(pos, 0, "curveout", 1, inters: true);
			break;
		case 115:
			if (bounded4.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 0, "full25", 14, inters: false);
			}
			else if (bounded5.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 180, "full25", 14, inters: false);
			}
			else if (bounded4.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 0, "full50", 14, inters: false);
			}
			else if (bounded5.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 180, "full50", 14, inters: false);
			}
			else if (bounded2.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 270, "full25", 14, inters: false);
			}
			else if (bounded3.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 90, "full25", 14, inters: false);
			}
			else if (bounded2.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 270, "full50", 14, inters: false);
			}
			else if (bounded3.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 90, "full50", 14, inters: false);
			}
			else
			{
				CreatePiece(pos, 0, "full", 14, inters: true);
			}
			break;
		default:
			CreatePiece(pos, 0, "full", 0, inters: true);
			break;
		}
		if (existingType != "")
		{
			subType = text;
		}
	}

	public void CreateIntersection(Vector2Int pos, int forceBitwise = 0, string existingType = "")
	{
		string text = subType;
		if (existingType != "")
		{
			if (existingType == "Road/")
			{
				subType = "Road/";
			}
			else if (existingType == "Street/")
			{
				subType = "Street/";
			}
		}
		string roadType = "curve_in_no";
		if (subType == "Road/" && w.GetBounded(pos.x, pos.y - 2).r && w.GetBounded(pos.x, pos.y + 2).r && w.GetBounded(pos.x - 2, pos.y).r && w.GetBounded(pos.x + 2, pos.y).r)
		{
			roadType = ((!(GetIfNotNull(pos.x, 2, pos.y - 2) != null) && !(GetIfNotNull(pos.x, 2, pos.y + 2) != null) && !(GetIfNotNull(pos.x - 2, 2, pos.y) != null) && !(GetIfNotNull(pos.x + 2, 2, pos.y) != null)) ? "curve_in_light" : "curve_in_no");
		}
		int num = ((forceBitwise <= 0) ? GetBitwise(pos) : forceBitwise);
		BlockData bounded = w.GetBounded(pos.x, pos.y);
		BlockData bounded2 = w.GetBounded(pos.x, pos.y + 1);
		BlockData bounded3 = w.GetBounded(pos.x, pos.y - 1);
		BlockData bounded4 = w.GetBounded(pos.x + 1, pos.y);
		BlockData bounded5 = w.GetBounded(pos.x - 1, pos.y);
		switch (num)
		{
		case 114:
			CreatePiece(pos, 270, roadType, 8, inters: true);
			break;
		case 113:
			CreatePiece(pos, 0, roadType, 5, inters: true);
			break;
		case 111:
			CreatePiece(pos, 90, roadType, 6, inters: true);
			break;
		case 107:
			CreatePiece(pos, 180, roadType, 7, inters: true);
			break;
		case 14:
			if (bounded4.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 0, "straight25", 8, inters: true);
			}
			else if (bounded5.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 180, "straight25in", 8, inters: true);
			}
			else if (bounded4.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 0, "straight50", 8, inters: true);
			}
			else if (bounded5.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 180, "straight50in", 8, inters: true);
			}
			else
			{
				CreatePiece(pos, 0, "precurve", 8, inters: true);
			}
			break;
		case 11:
			if (bounded4.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 0, "straight25in", 6, inters: true);
			}
			else if (bounded5.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 180, "straight25", 6, inters: true);
			}
			else if (bounded4.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 0, "straight50in", 6, inters: true);
			}
			else if (bounded5.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 180, "straight50", 6, inters: true);
			}
			else
			{
				CreatePiece(pos, 180, "precurve", 6, inters: true);
			}
			break;
		case 7:
			if (bounded2.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 270, "straight25", 7, inters: true);
			}
			else if (bounded3.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 90, "straight25in", 7, inters: true);
			}
			else if (bounded2.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 270, "straight50", 7, inters: true);
			}
			else if (bounded3.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 90, "straight50in", 7, inters: true);
			}
			else
			{
				CreatePiece(pos, 270, "precurve", 7, inters: true);
			}
			break;
		case 13:
			if (bounded2.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 270, "straight25in", 5, inters: true);
			}
			else if (bounded3.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 90, "straight25", 5, inters: true);
			}
			else if (bounded2.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 270, "straight50in", 5, inters: true);
			}
			else if (bounded3.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 90, "straight50", 5, inters: true);
			}
			else
			{
				CreatePiece(pos, 90, "precurve", 5, inters: true);
			}
			break;
		case 6:
			CreatePiece(pos, 90, "curveout", 4, inters: true);
			break;
		case 12:
			CreatePiece(pos, 180, "curveout", 2, inters: true);
			break;
		case 9:
			CreatePiece(pos, 270, "curveout", 3, inters: true);
			break;
		case 3:
			CreatePiece(pos, 0, "curveout", 1, inters: true);
			break;
		case 115:
			if (bounded4.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 0, "full25", 14, inters: false);
			}
			else if (bounded5.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 180, "full25", 14, inters: false);
			}
			else if (bounded4.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 0, "full50", 14, inters: false);
			}
			else if (bounded5.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 180, "full50", 14, inters: false);
			}
			else if (bounded2.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 270, "full25", 14, inters: false);
			}
			else if (bounded3.h == bounded.h + 0.25f)
			{
				CreatePiece(pos, 90, "full25", 14, inters: false);
			}
			else if (bounded2.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 270, "full50", 14, inters: false);
			}
			else if (bounded3.h >= bounded.h + 0.5f)
			{
				CreatePiece(pos, 90, "full50", 14, inters: false);
			}
			else
			{
				CreatePiece(pos, 0, "full", 14, inters: true);
			}
			break;
		default:
			CreatePiece(pos, 0, "full", 0, inters: true);
			break;
		}
		if (existingType != "")
		{
			subType = text;
		}
	}

	public void BuildConnectionRoadLineX(Vector2Int pos)
	{
		CreatePiece(new Vector2Int(pos.x, pos.y - 1), 180, "precurve", 14, inters: false);
		CreatePiece(new Vector2Int(pos.x, pos.y), 0, "precurve", 14, inters: false);
		CreateConnection(new Vector2Int(pos.x, pos.y - 1), isExit: true, 1);
		CreateConnection(new Vector2Int(pos.x, pos.y), isExit: false, 1);
	}

	public void BuildConnectionRoadLineZ(Vector2Int pos)
	{
		CreatePiece(new Vector2Int(pos.x, pos.y), 270, "precurve", 14, inters: false);
		CreatePiece(new Vector2Int(pos.x + 1, pos.y), 90, "precurve", 14, inters: false);
		CreateConnection(new Vector2Int(pos.x, pos.y), isExit: true, 1);
		CreateConnection(new Vector2Int(pos.x + 1, pos.y), isExit: false, 1);
	}

	private void CreatePiece(Vector2Int pos, int rotation, string roadType, int code, bool inters)
	{
		BlockData ifNotNull = w.GetIfNotNull(pos.x, pos.y);
		if (ifNotNull == null)
		{
			return;
		}
		if (roadType == "straight" && subType == "Street/" && sideType == "straighttree")
		{
			roadType = sideType;
		}
		GameObject gameObject = Resources.Load<GameObject>("Roads/" + subType + style + roadType);
		if (gameObject == null)
		{
			Debug.Log("This object could not be found: Roads/" + subType + style + roadType);
			return;
		}
		ifNotNull.r = true;
		ifNotNull.g = false;
		ifNotNull.t = BlockData.Type.ROAD;
		ifNotNull.b = true;
		Vector3 vector = new Vector3(pos.x, ifNotNull.h + 0.502f, pos.y);
		GameObject gameObject2 = Object.Instantiate(gameObject);
		gameObject2.transform.position = vector;
		gameObject2.name = pos.x + "_" + 50 + "_" + pos.y;
		gameObject2.transform.Rotate(0f, rotation, 0f, Space.Self);
		gameObject2.transform.parent = tRoad.transform;
		gameObject2.tag = "Road";
		RoadCreator roadCreator = gameObject2.AddComponent<RoadCreator>();
		if (subType == "Road/")
		{
			roadCreator.Capacity = 8000;
		}
		else if (style == "Dirt/")
		{
			roadCreator.Capacity = 1500;
		}
		else
		{
			roadCreator.Capacity = 4000;
		}
		roadCreator.wPos = vector;
		roadCreator.aPos = new Vector3(pos.x, 16f, pos.y);
		roadCreator.Orientation = rotation;
		roadCreator.subType = subType;
		roadCreator.style = style;
		roadCreator.path = "Roads/" + subType + style + roadType;
		roadCreator.tile = roadType;
		roadCreator.cc = code;
		roadCreator.rType = RoadCreator.RoadType.ROAD;
		roadCreator.isIntersec = inters;
		ifNotNull.o.lroads.Add(roadCreator);
		rD[pos.x, 1, pos.y] = roadCreator;
		w.Roads.Add(roadCreator);
		if (ifNotNull.h < 29.75f)
		{
			rcBridge.Add(roadCreator);
		}
		if (Messages.step == 1)
		{
			Messages.counter++;
		}
	}

	public RoadCreator GetIfNotNull(int x, int level, int z)
	{
		if (x < 0 || z < 0 || x >= worldSize || z >= worldSize)
		{
			return null;
		}
		if (rD[x, level, z] != null && rD[x, level, z].rType == RoadCreator.RoadType.PATH)
		{
			return null;
		}
		return rD[x, level, z];
	}

	public RoadCreator GetAccess(int x, int z)
	{
		if (x < 0 || z < 0 || x >= worldSize || z >= worldSize)
		{
			return null;
		}
		RoadCreator roadCreator = rD[x, 1, z];
		if (roadCreator != null)
		{
			if (roadCreator.rType == RoadCreator.RoadType.ROAD || roadCreator.rType == RoadCreator.RoadType.AVENUE)
			{
				return roadCreator;
			}
			return null;
		}
		return null;
	}

	public RoadCreator GetBounded(int x, int level, int z, RoadCreator previous)
	{
		if (x < 0)
		{
			x = 0;
		}
		else if (x >= worldSize)
		{
			x = worldSize - 1;
		}
		if (z < 0)
		{
			z = 0;
		}
		else if (z >= worldSize)
		{
			z = worldSize - 1;
		}
		if (rD[x, level, z] == null)
		{
			return previous;
		}
		if (rD[x, level, z].rType == RoadCreator.RoadType.PATH)
		{
			return previous;
		}
		return rD[x, level, z];
	}

	public RoadCreator GetRailBounded(int x, int level, int z, RoadCreator previous)
	{
		if (x < 0)
		{
			x = 0;
		}
		else if (x >= worldSize)
		{
			x = worldSize - 1;
		}
		if (z < 0)
		{
			z = 0;
		}
		else if (z >= worldSize)
		{
			z = worldSize - 1;
		}
		if (rG[x, level, z] == null)
		{
			return previous;
		}
		return rG[x, level, z];
	}

	public RoadCreator GetRailIfNotNull(int x, int level, int z)
	{
		if (x < 0)
		{
			return null;
		}
		if (x >= worldSize)
		{
			return null;
		}
		if (z < 0)
		{
			return null;
		}
		if (z >= worldSize)
		{
			return null;
		}
		return rG[x, level, z];
	}

	public void DestroyRoad(int x, int z)
	{
		RoadCreator ifNotNull = GetIfNotNull(x, 1, z);
		if (GetIfNotNull(x, 2, z) != null)
		{
			DestroyHighway(x, z);
		}
		if (ifNotNull != null)
		{
			BlockData blockData = w.blockData[x, z];
			blockData.t = BlockData.Type.GRASS;
			if (ifNotNull.stopType == RoadCreator.StopType.BUS)
			{
				busStops.Remove(ifNotNull);
			}
			else if (ifNotNull.stopType == RoadCreator.StopType.TRAIN)
			{
				trainStops.Remove(ifNotNull);
			}
			w.Roads.Remove(ifNotNull);
			if (ifNotNull.rType == RoadCreator.RoadType.ROAD)
			{
				blockData.o.lroads.Remove(ifNotNull);
			}
			else if (ifNotNull.rType == RoadCreator.RoadType.AVENUE || ifNotNull.rType == RoadCreator.RoadType.HIGHWAY)
			{
				blockData.o.lavenues.Remove(ifNotNull);
			}
			if (ifNotNull.isConnection)
			{
				DestroyConnection(ifNotNull);
			}
			if (ifNotNull.gameObject != null)
			{
				Object.Destroy(ifNotNull.gameObject);
			}
		}
	}

	public void DestroyHighway(int x, int z)
	{
		RoadCreator ifNotNull = GetIfNotNull(x, 2, z);
		if (!(ifNotNull == null) && ifNotNull != null)
		{
			BlockData blockData = w.blockData[x, z];
			blockData.t = BlockData.Type.GRASS;
			w.Roads.Remove(ifNotNull);
			blockData.o.lavenues.Remove(ifNotNull);
			if (ifNotNull.gameObject != null)
			{
				Object.Destroy(ifNotNull.gameObject);
			}
		}
	}

	public void StopVisualizeRoad()
	{
		GameObject[] array = GameObject.FindGameObjectsWithTag("NewCube");
		foreach (GameObject gameObject in array)
		{
			if (gameObject.name == "NewCube(Clone)" || gameObject.name == "DestroyCube(Clone)" || gameObject.name == "SquareRed1x(Clone)")
			{
				Object.Destroy(gameObject);
			}
		}
		array = GameObject.FindGameObjectsWithTag("Player");
		foreach (GameObject gameObject2 in array)
		{
			if (gameObject2.name == "SquareWhite1x(Clone)")
			{
				Object.Destroy(gameObject2);
			}
		}
		costHelper.gameObject.SetActive(value: false);
	}

	private void BuildBridge()
	{
		float num = 0f;
		foreach (RoadCreator item in rcBridge)
		{
			if (!(item == null))
			{
				num++;
				int num2;
				switch (item.cc)
				{
				case 4:
					num2 = -90;
					break;
				case 3:
					num2 = 90;
					break;
				case 2:
					num2 = 0;
					break;
				case 1:
					num2 = 180;
					break;
				default:
					Debug.Log("Bridge Error: Not straight?");
					return;
				}
				Vector3 wPos = item.wPos;
				Vector3 position = item.gameObject.transform.position;
				position.y = 30.75f;
				GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Roads/Road/Bridge/straight"));
				gameObject.transform.position = position;
				gameObject.name = wPos.x + "_" + 50 + "_" + wPos.z;
				gameObject.transform.Rotate(0f, num2, 0f, Space.Self);
				gameObject.transform.parent = tRoad.transform;
				gameObject.isStatic = false;
				gameObject.tag = "Road";
				RoadCreator roadCreator = gameObject.AddComponent<RoadCreator>();
				roadCreator.Capacity = 5000;
				roadCreator.wPos = position;
				roadCreator.aPos = new Vector3(wPos.x, 16f, wPos.z);
				roadCreator.Orientation = num2;
				roadCreator.rType = RoadCreator.RoadType.ROAD;
				roadCreator.cc = item.cc;
				roadCreator.isBridge = true;
				roadCreator.subType = "Road/";
				roadCreator.style = "Bridge/";
				roadCreator.tile = "straight";
				DestroyRoad((int)wPos.x, (int)wPos.z);
				rD[(int)wPos.x, 1, (int)wPos.z] = roadCreator;
				w.Roads.Add(roadCreator);
				BlockData blockData = w.blockData[(int)wPos.x, (int)wPos.z];
				blockData.t = BlockData.Type.ROAD;
				blockData.o.lroads.Add(roadCreator);
				if ((num == 12f || num == 36f || num == 60f || num == 84f || num == 108f || num == 132f) && num <= (float)(rcBridge.Count + 22))
				{
					BuildBridgeArch(roadCreator, num2);
				}
			}
		}
	}

	private void BuildBridgeArch(RoadCreator rc, int Orientation)
	{
		Vector3 wPos = rc.wPos;
		Vector3 vector = Orientation switch
		{
			90 => new Vector3(-0.5f, 0f, 0f), 
			-90 => new Vector3(0.5f, 0f, 0f), 
			180 => new Vector3(0f, 0f, 0.5f), 
			_ => new Vector3(0f, 0f, -0.5f), 
		};
		Vector3 position = rc.gameObject.transform.position;
		position.y = 30.75f;
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Roads/Bridge_Road_A"));
		gameObject.transform.position = position + vector;
		gameObject.name = wPos.x + "_bridge_" + wPos.z;
		gameObject.transform.Rotate(0f, Orientation, 0f, Space.Self);
		gameObject.isStatic = false;
		gameObject.transform.parent = rc.gameObject.transform;
	}

	private void BuildArrow(RoadCreator rc)
	{
		Vector3 wPos = rc.wPos;
		Vector3 position = rc.gameObject.transform.position;
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Roads/Arrow"));
		gameObject.transform.position = position;
		gameObject.name = wPos.x + "_arrow_" + wPos.z;
		gameObject.transform.Rotate(0f, rc.Orientation, 0f, Space.Self);
		gameObject.isStatic = false;
		gameObject.transform.parent = rc.gameObject.transform;
	}

	public void CreateConnection(Vector2Int pos, bool isExit, int level)
	{
		int rX = DataRegion.selectedCity.rX;
		int rZ = DataRegion.selectedCity.rZ;
		RoadCreator ifNotNull = GetIfNotNull(pos.x, level, pos.y);
		if (ifNotNull == null)
		{
			Debug.Log("Rc is null");
			return;
		}
		int roadType = 0;
		if (ifNotNull.rType == RoadCreator.RoadType.AVENUE)
		{
			roadType = 1;
		}
		else if (ifNotNull.rType == RoadCreator.RoadType.HIGHWAY)
		{
			roadType = 2;
		}
		else if (ifNotNull.rType == RoadCreator.RoadType.RAIL)
		{
			roadType = 3;
		}
		DataCity city = DataRegion.GetCity(rX, rZ);
		DataCity city2 = DataRegion.GetCity(rX - 1, rZ);
		DataCity city3 = DataRegion.GetCity(rX + 1, rZ);
		DataCity city4 = DataRegion.GetCity(rX, rZ - 1);
		DataCity city5 = DataRegion.GetCity(rX, rZ + 1);
		Connection connection = new Connection();
		if ((pos.x == 0 && city2 != null) || (pos.x == worldSize - 1 && city3 != null) || (pos.y == 0 && city4 != null) || (pos.y == worldSize - 1 && city5 != null))
		{
			city.connections.Add(connection);
			if (isExit)
			{
				connection.exitPos = pos;
				ifNotNull.isExit = true;
				World.exitRoads.Add(ifNotNull);
			}
			else
			{
				connection.entrancePos = pos;
				ifNotNull.isEntrance = true;
				World.entranceRoads.Add(ifNotNull);
			}
			connection.Capacity = 10000;
			connection.roadType = roadType;
			ifNotNull.isConnection = true;
			BuildArrow(ifNotNull);
			sm.Play("PlopBuild");
		}
	}

	public void DestroyConnection(RoadCreator rc)
	{
		int rX = DataRegion.selectedCity.rX;
		int rZ = DataRegion.selectedCity.rZ;
		DataCity city = DataRegion.GetCity(rX, rZ);
		foreach (Connection item in city.connections.ToList())
		{
			if (item.entrancePos == new Vector2Int((int)rc.aPos.x, (int)rc.aPos.z))
			{
				city.connections.Remove(item);
			}
			else if (item.exitPos == new Vector2Int((int)rc.aPos.x, (int)rc.aPos.z))
			{
				city.connections.Remove(item);
			}
		}
	}

	private void CheckChunks(BlockData b)
	{
		updates.Add(b.o);
		if (b.pos[0] == 0)
		{
			BlockData ifNotNull = w.GetIfNotNull(b.wP.x - 1, b.wP.y);
			if (ifNotNull == null)
			{
				return;
			}
			updates.Add(ifNotNull.o);
		}
		else if (b.pos[0] == 15)
		{
			BlockData ifNotNull2 = w.GetIfNotNull(b.wP.x + 1, b.wP.y);
			if (ifNotNull2 == null)
			{
				return;
			}
			updates.Add(ifNotNull2.o);
		}
		if (b.pos[1] == 0)
		{
			BlockData ifNotNull3 = w.GetIfNotNull(b.wP.x, b.wP.y - 1);
			if (ifNotNull3 != null)
			{
				updates.Add(ifNotNull3.o);
			}
		}
		else if (b.pos[1] == 15)
		{
			BlockData ifNotNull4 = w.GetIfNotNull(b.wP.x, b.wP.y + 1);
			if (ifNotNull4 != null)
			{
				updates.Add(ifNotNull4.o);
			}
		}
	}

	public IEnumerator UpdateChunks()
	{
		yield return new WaitForSecondsRealtime(0.05f);
		if (rcBridge.Count > 0)
		{
			BuildBridge();
			rcBridge.Clear();
		}
		yield return new WaitForEndOfFrame();
		foreach (Chunk c in updates.ToList())
		{
			if (c != null)
			{
				c.Redraw();
				yield return new WaitForEndOfFrame();
				c.UpdateRoads();
				yield return new WaitForEndOfFrame();
			}
		}
		updates.Clear();
	}
}
