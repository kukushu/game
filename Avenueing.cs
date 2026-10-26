using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class Avenueing : MonoBehaviour
{
	public CostHelper costHelper;

	public GameObject whiteSquare;

	public GameObject redSquare;

	public GameObject tRoad;

	public GameObject wSquare;

	public List<GameObject> cells = new List<GameObject>();

	public Material wMat;

	private List<RoadCreator> rcBridge = new List<RoadCreator>();

	private int worldSize = World.chunkSize * World.worldSize;

	public BlockInteraction bi;

	public SoundManager sm;

	public Roading r;

	public World world;

	private bool suitable = true;

	private int xo1;

	private int zo1;

	private int xo2;

	private int zo2;

	public static string subType = "Avenue/";

	public static int level = 1;

	public static int Type = 0;

	public static int Case = 0;

	public static string sideType = "side";

	private int price = 60;

	private readonly HashSet<Vector3> avenueIntersections = new HashSet<Vector3>();

	private readonly HashSet<Vector3> roadIntersectionsX = new HashSet<Vector3>();

	private readonly HashSet<Vector3> roadIntersectionsZ = new HashSet<Vector3>();

	private readonly HashSet<Vector3> hwayIntersections = new HashSet<Vector3>();

	private HashSet<Vector2Int> bitCheck = new HashSet<Vector2Int>();

	private HashSet<Vector2Int> bridgeX = new HashSet<Vector2Int>();

	private HashSet<Vector2Int> bridgeZ = new HashSet<Vector2Int>();

	private readonly HashSet<Chunk> updates = new HashSet<Chunk>();

	private Vector2Int vPos = Vector2Int.zero;

	private void Start()
	{
		for (int i = 0; i <= 9; i++)
		{
			GameObject gameObject = Object.Instantiate(wSquare);
			gameObject.transform.parent = bi.cursorManager.transform;
			cells.Add(gameObject);
		}
	}

	private void Update()
	{
		if (Indexer.Index == 11)
		{
			bi.cursorManager.SetActive(value: true);
			TooltipSystem.Hide();
			if (bi.tamere == 0)
			{
				vPos = new Vector2Int((int)bi.pos.x, (int)bi.pos.z);
			}
			else
			{
				int num = Mathf.Abs(bi.x1 - bi.x2) + 1;
				int num2 = Mathf.Abs(bi.z1 - bi.z2) + 1;
				if (num >= num2)
				{
					vPos = new Vector2Int((int)bi.pos.x, bi.z1);
				}
				else
				{
					vPos = new Vector2Int(bi.x1, (int)bi.pos.z);
				}
			}
			if (vPos.x >= worldSize - 1)
			{
				vPos.x = worldSize - 2;
			}
			else if (vPos.x < 1)
			{
				vPos.x = 1;
			}
			if (vPos.y < 1)
			{
				vPos.y = 1;
			}
			else if (vPos.y >= worldSize - 1)
			{
				vPos.y = worldSize - 2;
			}
			BlockData ifNotNull = world.GetIfNotNull(vPos.x, vPos.y);
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 0;
			for (int i = -1; i < 2; i++)
			{
				for (int j = -1; j < 2; j++)
				{
					BlockData ifNotNull2 = world.GetIfNotNull(vPos.x + i, vPos.y + j);
					if (ifNotNull2 == null)
					{
						return;
					}
					if (ifNotNull2.b && !ifNotNull2.r)
					{
						wMat.color = Color.red;
						TooltipSystem.Show("", "Area Occupied");
						return;
					}
					if (ifNotNull2.h < 29.75f)
					{
						wMat.color = Color.red;
						TooltipSystem.Show("", "Unfeasible Road Layout");
						return;
					}
					if (ifNotNull2.r)
					{
						RoadCreator ifNotNull3 = r.GetIfNotNull(vPos.x + i, level, vPos.y + j);
						if (ifNotNull3 != null && ifNotNull3.rType == RoadCreator.RoadType.AVENUE)
						{
							num3++;
						}
						else if (ifNotNull3 != null && ifNotNull3.rType == RoadCreator.RoadType.ROAD)
						{
							num4++;
						}
						if (ifNotNull3 != null && ifNotNull3.isIntersec)
						{
							num5++;
						}
					}
					cells[num6].transform.position = new Vector3(vPos.x + i, ifNotNull2.h + 0.515f, vPos.y + j);
					num6++;
				}
			}
			if ((num5 == 6 && num4 == 6) || (num5 == 9 && num4 == 0) || (num5 == 0 && num3 == 9) || (num5 == 0 && num4 == 6) || (num5 == 9 && num3 == 9) || (num5 == 4 && num4 == 6))
			{
				wMat.color = r.azul;
			}
			else if (num5 == 0 && num3 == 0 && num4 == 0)
			{
				wMat.color = r.lime;
			}
			else
			{
				wMat.color = Color.red;
				TooltipSystem.Show("", "Unaligned With");
			}
			xo1 = ifNotNull.wP.x;
			zo1 = ifNotNull.wP.y;
			xo2 = ifNotNull.wP.x;
			zo2 = ifNotNull.wP.y;
			r.gridAve.transform.position = new Vector3(ifNotNull.wP.x, 60f, ifNotNull.wP.y);
		}
		if (Input.GetMouseButtonDown(0) && Indexer.Index == 11 && bi.tamere == 0)
		{
			if (EventSystem.current.IsPointerOverGameObject())
			{
				return;
			}
			if (wMat.color == Color.red)
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
		if (Input.GetMouseButton(0) && Indexer.Index == 11 && bi.tamere == 1)
		{
			if (EventSystem.current.IsPointerOverGameObject())
			{
				return;
			}
			if (xo2 != bi.oldx2 || zo2 != bi.oldz2)
			{
				RoadUpdate();
			}
		}
		if (Input.GetMouseButtonUp(0) && Indexer.Index == 11 && bi.tamere == 1)
		{
			if (wMat.color == Color.red || !suitable)
			{
				bi.x1 = 0;
				bi.z1 = 0;
				bi.x2 = 0;
				bi.z2 = 0;
				bi.tamere = 0;
				StopVisualizeRoad();
				sm.Play("Impractical");
				return;
			}
			BuildAvenueLine(bi.pos, bi.x1, bi.z1);
		}
		if (Input.GetKeyDown(KeyCode.Escape) && Indexer.Index == 11 && bi.tamere == 1)
		{
			bi.x1 = 0;
			bi.z1 = 0;
			bi.x2 = 0;
			bi.z2 = 0;
			bi.tamere = 0;
			StopVisualizeRoad();
			sm.Play("Impractical");
		}
	}

	private void RoadUpdate()
	{
		bi.oldx2 = xo2;
		bi.oldz2 = zo2;
		int x = bi.x1;
		int z = bi.z1;
		int num = xo2;
		int num2 = zo2;
		float y = bi.pos.y;
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
		y += 0.5f;
		StopVisualizeRoad();
		suitable = true;
		sm.Play("RoadPiece");
		if (world.GetIfNotNull((int)bi.pos.x, (int)bi.pos.z).h < 29.75f)
		{
			suitable = false;
		}
		if (subType == "Avenue/")
		{
			level = 1;
			price = 60;
		}
		else
		{
			level = 2;
			price = 120;
		}
		if (num7 >= num8)
		{
			if (world.GetIfNotNull(x, z + 2) == null || world.GetIfNotNull(x, z - 2) == null)
			{
				suitable = false;
			}
			costHelper.SetCost(num7 * price);
			for (int i = num3 - 1; i < num4 + 2; i++)
			{
				BlockData blockData = world.blockData[i, z];
				BlockData blockData2 = world.blockData[i, z - 1];
				BlockData blockData3 = world.blockData[i, z + 1];
				if ((blockData.r || blockData2.r || blockData3.r) && (!blockData.r || !blockData2.r || !blockData3.r))
				{
					suitable = false;
				}
				if (blockData.r && blockData2.r && blockData3.r)
				{
					RoadCreator ifNotNull = r.GetIfNotNull(i, level, z);
					RoadCreator ifNotNull2 = r.GetIfNotNull(i, level, z - 1);
					RoadCreator ifNotNull3 = r.GetIfNotNull(i, level, z + 1);
					if (ifNotNull == null || ifNotNull2 == null || ifNotNull3 == null)
					{
						continue;
					}
					if ((!ifNotNull.isIntersec || !ifNotNull2.isIntersec || !ifNotNull3.isIntersec) && ifNotNull.rType != RoadCreator.RoadType.ROAD && (ifNotNull.isIntersec || !(!ifNotNull2.isIntersec & !ifNotNull3.isIntersec)))
					{
						suitable = false;
					}
				}
				VisualizeRoad(new Vector3(i, y, z));
				VisualizeRoad(new Vector3(i, y, z - 1));
				VisualizeRoad(new Vector3(i, y, z + 1));
			}
			return;
		}
		if (world.GetIfNotNull(x + 2, z) == null || world.GetIfNotNull(x - 2, z) == null)
		{
			suitable = false;
		}
		costHelper.SetCost(num8 * price);
		for (int j = num5 - 1; j < num6 + 2; j++)
		{
			BlockData blockData4 = world.blockData[x, j];
			BlockData blockData5 = world.blockData[x - 1, j];
			BlockData blockData6 = world.blockData[x + 1, j];
			if ((blockData4.r || blockData5.r || blockData6.r) && (!blockData4.r || !blockData5.r || !blockData6.r))
			{
				suitable = false;
			}
			if (blockData4.r && blockData5.r && blockData6.r)
			{
				RoadCreator ifNotNull4 = r.GetIfNotNull(x, level, j);
				RoadCreator ifNotNull5 = r.GetIfNotNull(x - 1, level, j);
				RoadCreator ifNotNull6 = r.GetIfNotNull(x + 1, level, j);
				if (ifNotNull4 == null || ifNotNull5 == null || ifNotNull6 == null)
				{
					continue;
				}
				if ((!ifNotNull4.isIntersec || !ifNotNull5.isIntersec || !ifNotNull6.isIntersec) && ifNotNull4.rType != RoadCreator.RoadType.ROAD && (ifNotNull4.isIntersec || !(!ifNotNull5.isIntersec & !ifNotNull6.isIntersec)))
				{
					suitable = false;
				}
			}
			if (j > num5 + 1)
			{
				BlockData ifNotNull7 = world.GetIfNotNull(x, j - 3);
				if (blockData4.r)
				{
					if (ifNotNull7.h > blockData4.h + 1f || ifNotNull7.h < blockData4.h - 1f)
					{
						suitable = false;
						TooltipSystem.Show("", "Too Steep");
					}
				}
				else if (ifNotNull7.h > blockData4.h + 1.5f || ifNotNull7.h < blockData4.h - 1.5f)
				{
					suitable = false;
					TooltipSystem.Show("", "Too Steep");
				}
			}
			VisualizeRoad(new Vector3(x, y, j));
			VisualizeRoad(new Vector3(x - 1, y, j));
			VisualizeRoad(new Vector3(x + 1, y, j));
		}
	}

	private void VisualizeRoad(Vector3 pos)
	{
		BlockData bounded = world.GetBounded((int)pos.x, (int)pos.z);
		Vector3 position = new Vector3(bounded.wP.x, bounded.h + 0.515f, bounded.wP.y);
		if ((!bounded.r && bounded.b && !bounded.a) || !suitable || (r.rG[bounded.wP.x, level, bounded.wP.y] != null && r.rG[bounded.wP.x, 1, bounded.wP.y].tile != "Rail_Straight"))
		{
			suitable = false;
			wMat.color = Color.red;
			TooltipSystem.Show("", "Area Occupied");
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

	private void BuildAvenueLine(Vector3 pos, int _x1, int _z1)
	{
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
			for (int i = num3 - 1; i < num4 + 2; i++)
			{
				world.GetBounded(i, _z1).r = true;
			}
		}
		else
		{
			for (int j = num5 - 1; j < num6 + 2; j++)
			{
				world.GetBounded(_x1, j).r = true;
			}
		}
		if (num7 >= num8)
		{
			for (int k = num3 - 1; k < num4 + 2; k++)
			{
				FlatAndBuildLineX(new Vector3(k, pos.y, _z1));
			}
		}
		else
		{
			for (int l = num5 - 1; l < num6 + 2; l++)
			{
				FlatAndBuildLineZ(new Vector3(_x1, pos.y, l));
			}
		}
		foreach (Vector3 item in avenueIntersections.ToList())
		{
			IntersectionMaster(item);
		}
		foreach (Vector3 item2 in roadIntersectionsX.ToList())
		{
			IntersectionRoadMasterLineX(item2);
		}
		foreach (Vector3 item3 in roadIntersectionsZ.ToList())
		{
			IntersectionRoadMasterLineZ(item3);
		}
		foreach (Vector2Int item4 in bitCheck.ToList())
		{
			PerTileIntersection(item4);
		}
		avenueIntersections.Clear();
		roadIntersectionsX.Clear();
		roadIntersectionsZ.Clear();
		hwayIntersections.Clear();
		bitCheck.Clear();
		bridgeX.Clear();
		bridgeZ.Clear();
		StartCoroutine(UpdateChunksAndBridge());
		bi.x1 = 0;
		bi.z1 = 0;
		bi.x2 = 0;
		bi.z2 = 0;
		bi.tamere = 0;
		StopVisualizeRoad();
		sm.Play("RoadBuilt");
		costHelper.PayForIt();
	}

	public void FlatAndBuildLineX(Vector3 pos)
	{
		BlockData blockData = world.Get((int)pos.x, (int)pos.z);
		BlockData blockData2 = world.Get((int)pos.x + 1, (int)pos.z);
		BlockData blockData3 = world.Get((int)pos.x, (int)pos.z + 1);
		BlockData blockData4 = world.Get((int)pos.x, (int)pos.z - 1);
		BlockData blockData5 = world.Get((int)pos.x - 1, (int)pos.z);
		BlockData blockData6 = world.Get((int)pos.x + 4, (int)pos.z);
		BlockData blockData7 = world.Get((int)pos.x + 2, (int)pos.z);
		BlockData blockData8 = world.Get((int)pos.x + 3, (int)pos.z);
		BlockData blockData9 = world.Get((int)pos.x, (int)pos.z + 2);
		BlockData blockData10 = world.Get((int)pos.x, (int)pos.z - 2);
		RoadCreator ifNotNull = r.GetIfNotNull((int)pos.x, level, (int)pos.z);
		RoadCreator ifNotNull2 = r.GetIfNotNull((int)pos.x + 2, level, (int)pos.z);
		RoadCreator ifNotNull3 = r.GetIfNotNull((int)pos.x - 2, level, (int)pos.z);
		RoadCreator ifNotNull4 = r.GetIfNotNull((int)pos.x, level, (int)pos.z + 2);
		RoadCreator ifNotNull5 = r.GetIfNotNull((int)pos.x, level, (int)pos.z - 2);
		RoadCreator ifNotNull6 = r.GetIfNotNull((int)pos.x + 1, level, (int)pos.z);
		if (ifNotNull != null && ifNotNull.isBridge)
		{
			return;
		}
		if (blockData.h == blockData6.h && ifNotNull2 == null)
		{
			blockData2.FlatHeight(blockData.h);
			blockData7.FlatHeight(blockData.h);
			blockData8.FlatHeight(blockData.h);
		}
		else if (blockData.h == blockData8.h && ifNotNull2 == null)
		{
			blockData2.FlatHeight(blockData.h);
			blockData7.FlatHeight(blockData.h);
		}
		else if (blockData.h == blockData7.h && ifNotNull2 == null)
		{
			blockData2.FlatHeight(blockData.h);
		}
		if (blockData2.h > blockData.h + 0.5f && blockData2.h > 30.25f)
		{
			blockData2.FlatHeight(blockData.h + 0.5f);
		}
		else if (blockData2.h < blockData.h - 0.5f && blockData2.h > 30.25f)
		{
			blockData2.FlatHeight(blockData.h - 0.5f);
		}
		if (!blockData5.r)
		{
			blockData.FlatHeight(blockData2.h);
		}
		else if (!blockData7.r && blockData2.r)
		{
			blockData2.FlatHeight(blockData.h);
		}
		if (blockData.h < 30.25f && blockData.h >= 29.75f)
		{
			blockData.FlatHeight(30.25f);
		}
		if (ifNotNull6 != null && ifNotNull6.rType == RoadCreator.RoadType.ROAD)
		{
			blockData2.FlatHeight(blockData.h);
		}
		if (blockData3.h != blockData.h)
		{
			blockData3.FlatHeight(blockData.h);
		}
		if (blockData4.h != blockData.h)
		{
			blockData4.FlatHeight(blockData.h);
		}
		if (ifNotNull5 != null && ifNotNull5.rType == RoadCreator.RoadType.ROAD && ifNotNull5.cc == 4)
		{
			blockData10.FlatHeight(blockData.h);
			Roading.subType = ifNotNull5.subType;
			if (r.GetIfNotNull((int)pos.x, 1, (int)pos.z - 2) != null && !r.GetIfNotNull((int)pos.x, 1, (int)pos.z - 2).isIntersec)
			{
				r.FlatNewLineZ(new Vector2Int((int)pos.x, (int)pos.z - 2));
				r.BuildNewLineZ(new Vector2Int((int)pos.x, (int)pos.z - 2));
			}
			if (r.GetIfNotNull((int)pos.x, 1, (int)pos.z - 3) != null && !r.GetIfNotNull((int)pos.x, 1, (int)pos.z - 3).isIntersec)
			{
				r.FlatNewLineZ(new Vector2Int((int)pos.x, (int)pos.z - 3));
				r.BuildNewLineZ(new Vector2Int((int)pos.x, (int)pos.z - 3));
			}
		}
		if (ifNotNull4 != null && ifNotNull4.rType == RoadCreator.RoadType.ROAD && ifNotNull4.cc == 4)
		{
			blockData9.FlatHeight(blockData.h);
			Roading.subType = ifNotNull4.subType;
			if (r.GetIfNotNull((int)pos.x, 1, (int)pos.z + 2) != null && !r.GetIfNotNull((int)pos.x, 1, (int)pos.z + 2).isIntersec)
			{
				r.FlatNewLineZ(new Vector2Int((int)pos.x, (int)pos.z + 2));
				r.BuildNewLineZ(new Vector2Int((int)pos.x, (int)pos.z + 2));
			}
			if (r.GetIfNotNull((int)pos.x, 1, (int)pos.z + 3) != null && !r.GetIfNotNull((int)pos.x, 1, (int)pos.z + 3).isIntersec)
			{
				r.FlatNewLineZ(new Vector2Int((int)pos.x, (int)pos.z + 3));
				r.BuildNewLineZ(new Vector2Int((int)pos.x, (int)pos.z + 3));
			}
		}
		CheckChunks(blockData);
		CheckChunks(blockData3);
		CheckChunks(blockData4);
		if ((pos.x == 0f && ifNotNull == null) || (pos.x == (float)(worldSize - 1) && ifNotNull == null))
		{
			BuildConnectionRoadLineX(pos);
		}
		else if (!blockData5.r)
		{
			if (ifNotNull != null)
			{
				r.DestroyRoad((int)pos.x, (int)pos.z);
				r.DestroyRoad((int)pos.x, (int)pos.z - 1);
				r.DestroyRoad((int)pos.x, (int)pos.z + 1);
			}
			BuildEndOfRoad(pos, 180);
		}
		else if (!blockData2.r)
		{
			if (ifNotNull != null)
			{
				r.DestroyRoad((int)pos.x, (int)pos.z);
				r.DestroyRoad((int)pos.x, (int)pos.z - 1);
				r.DestroyRoad((int)pos.x, (int)pos.z + 1);
			}
			BuildEndOfRoad(pos, 0);
		}
		else
		{
			if (ifNotNull != null)
			{
				r.DestroyRoad((int)pos.x, (int)pos.z);
				r.DestroyRoad((int)pos.x, (int)pos.z - 1);
				r.DestroyRoad((int)pos.x, (int)pos.z + 1);
			}
			if (blockData.h < 29.75f)
			{
				BuildStraightRoadLineX(pos);
			}
			else if (blockData2.h == blockData.h + 0.25f)
			{
				BuildStraight25LineX(pos, 180);
			}
			else if (blockData5.h == blockData.h + 0.25f)
			{
				BuildStraight25LineX(pos, 0);
			}
			else if (blockData2.h == blockData.h + 0.5f)
			{
				BuildStraight50LineX(pos, 180);
			}
			else if (blockData5.h == blockData.h + 0.5f)
			{
				BuildStraight50LineX(pos, 0);
			}
			else
			{
				BuildStraightRoadLineX(pos);
			}
		}
		if ((ifNotNull != null && ifNotNull.cc == 17) || (ifNotNull != null && ifNotNull.isIntersec && ifNotNull.tile == "curvecenter"))
		{
			avenueIntersections.Add(pos);
		}
		else if ((ifNotNull2 != null && ifNotNull2.cc == 17) || (ifNotNull2 != null && ifNotNull2.cc == 0))
		{
			avenueIntersections.Add(new Vector3(pos.x + 2f, pos.y, pos.z));
		}
		else if ((ifNotNull3 != null && ifNotNull3.cc == 17) || (ifNotNull3 != null && ifNotNull3.cc == 0))
		{
			avenueIntersections.Add(new Vector3(pos.x - 2f, pos.y, pos.z));
		}
		else if (ifNotNull != null && ifNotNull.rType == RoadCreator.RoadType.ROAD && subType == "Avenue/")
		{
			bitCheck.Add(new Vector2Int((int)pos.x, (int)pos.z - 1));
			bitCheck.Add(new Vector2Int((int)pos.x, (int)pos.z));
			bitCheck.Add(new Vector2Int((int)pos.x, (int)pos.z + 1));
		}
	}

	public void FlatAndBuildLineZ(Vector3 pos)
	{
		BlockData blockData = world.Get((int)pos.x, (int)pos.z);
		BlockData blockData2 = world.Get((int)pos.x - 1, (int)pos.z);
		BlockData blockData3 = world.Get((int)pos.x, (int)pos.z + 1);
		BlockData blockData4 = world.Get((int)pos.x, (int)pos.z + 2);
		BlockData blockData5 = world.Get((int)pos.x, (int)pos.z - 1);
		BlockData blockData6 = world.Get((int)pos.x, (int)pos.z + 3);
		BlockData blockData7 = world.Get((int)pos.x + 1, (int)pos.z);
		BlockData blockData8 = world.Get((int)pos.x, (int)pos.z + 4);
		BlockData blockData9 = world.Get((int)pos.x + 2, (int)pos.z);
		BlockData blockData10 = world.Get((int)pos.x - 2, (int)pos.z);
		RoadCreator ifNotNull = r.GetIfNotNull((int)pos.x, level, (int)pos.z);
		RoadCreator ifNotNull2 = r.GetIfNotNull((int)pos.x, level, (int)pos.z + 1);
		RoadCreator ifNotNull3 = r.GetIfNotNull((int)pos.x, level, (int)pos.z + 2);
		RoadCreator ifNotNull4 = r.GetIfNotNull((int)pos.x, level, (int)pos.z - 2);
		RoadCreator ifNotNull5 = r.GetIfNotNull((int)pos.x - 2, level, (int)pos.z);
		RoadCreator ifNotNull6 = r.GetIfNotNull((int)pos.x + 2, level, (int)pos.z);
		if (ifNotNull != null && ifNotNull.isBridge)
		{
			return;
		}
		if (blockData.h == blockData8.h && ifNotNull3 == null)
		{
			blockData3.FlatHeight(blockData.h);
			blockData4.FlatHeight(blockData.h);
			blockData6.FlatHeight(blockData.h);
		}
		else if (blockData.h == blockData6.h && ifNotNull3 == null)
		{
			blockData3.FlatHeight(blockData.h);
			blockData4.FlatHeight(blockData.h);
		}
		else if (blockData.h == blockData4.h && ifNotNull3 == null)
		{
			blockData3.FlatHeight(blockData.h);
		}
		if (blockData3.h > blockData.h + 0.5f && blockData3.h > 30.25f)
		{
			blockData3.FlatHeight(blockData.h + 0.5f);
		}
		else if (blockData3.h < blockData.h - 0.5f && blockData3.h > 30.25f)
		{
			blockData3.FlatHeight(blockData.h - 0.5f);
		}
		if (!blockData5.r)
		{
			blockData.FlatHeight(blockData3.h);
		}
		else if (!blockData4.r && blockData3.r)
		{
			blockData3.FlatHeight(blockData.h);
		}
		if (blockData.h < 30.25f && blockData.h >= 29.75f)
		{
			blockData.FlatHeight(30.25f);
		}
		if (ifNotNull2 != null && ifNotNull2.rType == RoadCreator.RoadType.ROAD)
		{
			blockData3.FlatHeight(blockData.h);
		}
		if (blockData7.h != blockData.h)
		{
			blockData7.FlatHeight(blockData.h);
		}
		if (blockData2.h != blockData.h)
		{
			blockData2.FlatHeight(blockData.h);
		}
		if (ifNotNull6 != null && ifNotNull6.rType == RoadCreator.RoadType.ROAD && ifNotNull6.cc == 1)
		{
			blockData9.FlatHeight(blockData.h);
			Indexer.roadSubType = ifNotNull6.subType;
			if (r.GetIfNotNull((int)pos.x + 2, 1, (int)pos.z + 1) != null && !r.GetIfNotNull((int)pos.x + 2, 1, (int)pos.z + 1).isIntersec)
			{
				r.FlatNewLineX(new Vector2Int((int)pos.x + 2, (int)pos.z + 1));
				r.BuildNewLineX(new Vector2Int((int)pos.x + 2, (int)pos.z + 1));
			}
			if (r.GetIfNotNull((int)pos.x + 3, 1, (int)pos.z + 1) != null && !r.GetIfNotNull((int)pos.x + 3, 1, (int)pos.z + 1).isIntersec)
			{
				r.FlatNewLineX(new Vector2Int((int)pos.x + 3, (int)pos.z + 1));
				r.BuildNewLineX(new Vector2Int((int)pos.x + 3, (int)pos.z + 1));
			}
		}
		if (ifNotNull5 != null && ifNotNull5.rType == RoadCreator.RoadType.ROAD && ifNotNull5.cc == 1)
		{
			blockData10.FlatHeight(blockData.h);
			Indexer.roadSubType = ifNotNull5.subType;
			if (r.GetIfNotNull((int)pos.x - 2, 1, (int)pos.z + 1) != null && !r.GetIfNotNull((int)pos.x - 2, 1, (int)pos.z + 1).isIntersec)
			{
				r.FlatNewLineX(new Vector2Int((int)pos.x - 2, (int)pos.z + 1));
				r.BuildNewLineX(new Vector2Int((int)pos.x - 2, (int)pos.z + 1));
			}
			if (r.GetIfNotNull((int)pos.x - 3, 1, (int)pos.z + 1) != null && !r.GetIfNotNull((int)pos.x - 3, 1, (int)pos.z + 1).isIntersec)
			{
				r.FlatNewLineX(new Vector2Int((int)pos.x - 3, (int)pos.z + 1));
				r.BuildNewLineX(new Vector2Int((int)pos.x - 3, (int)pos.z + 1));
			}
		}
		CheckChunks(blockData);
		CheckChunks(blockData7);
		CheckChunks(blockData2);
		if ((pos.z == 0f && ifNotNull == null) || (pos.z == (float)(worldSize - 1) && ifNotNull == null))
		{
			BuildConnectionRoadLineZ(pos);
		}
		else if (!blockData5.r)
		{
			if (ifNotNull != null)
			{
				r.DestroyRoad((int)pos.x, (int)pos.z);
				r.DestroyRoad((int)pos.x + 1, (int)pos.z);
				r.DestroyRoad((int)pos.x - 1, (int)pos.z);
			}
			BuildEndOfRoad(pos, 90);
		}
		else if (!blockData3.r)
		{
			if (ifNotNull != null)
			{
				r.DestroyRoad((int)pos.x, (int)pos.z);
				r.DestroyRoad((int)pos.x + 1, (int)pos.z);
				r.DestroyRoad((int)pos.x - 1, (int)pos.z);
			}
			BuildEndOfRoad(pos, 270);
		}
		else
		{
			if (ifNotNull != null)
			{
				r.DestroyRoad((int)pos.x, (int)pos.z);
				r.DestroyRoad((int)pos.x + 1, (int)pos.z);
				r.DestroyRoad((int)pos.x - 1, (int)pos.z);
			}
			if (blockData.h < 29.75f)
			{
				BuildStraightRoadLineZ(pos);
			}
			else if (blockData3.h == blockData.h + 0.25f)
			{
				BuildStraight25LineZ(pos, 0);
			}
			else if (blockData5.h == blockData.h + 0.25f)
			{
				BuildStraight25LineZ(pos, 180);
			}
			else if (blockData3.h == blockData.h + 0.5f)
			{
				BuildStraight50LineZ(pos, 0);
			}
			else if (blockData5.h == blockData.h + 0.5f)
			{
				BuildStraight50LineZ(pos, 180);
			}
			else
			{
				BuildStraightRoadLineZ(pos);
			}
		}
		if ((ifNotNull != null && ifNotNull.cc == 18) || (ifNotNull != null && ifNotNull.isIntersec && ifNotNull.tile == "curvecenter"))
		{
			avenueIntersections.Add(pos);
		}
		else if ((ifNotNull3 != null && ifNotNull3.cc == 18) || (ifNotNull3 != null && ifNotNull3.cc == 0))
		{
			avenueIntersections.Add(new Vector3(pos.x, pos.y, pos.z + 2f));
		}
		else if ((ifNotNull4 != null && ifNotNull4.cc == 18) || (ifNotNull4 != null && ifNotNull4.cc == 0))
		{
			avenueIntersections.Add(new Vector3(pos.x, pos.y, pos.z - 2f));
		}
		else if (ifNotNull != null && ifNotNull.rType == RoadCreator.RoadType.ROAD && subType == "Avenue/")
		{
			bitCheck.Add(new Vector2Int((int)pos.x - 1, (int)pos.z));
			bitCheck.Add(new Vector2Int((int)pos.x, (int)pos.z));
			bitCheck.Add(new Vector2Int((int)pos.x + 1, (int)pos.z));
		}
	}

	private void PerTileIntersection(Vector2Int pos)
	{
		r.DestroyRoad(pos.x, pos.y);
		BlockData ifNotNull = world.GetIfNotNull(pos.x, pos.y - 1);
		BlockData ifNotNull2 = world.GetIfNotNull(pos.x + 1, pos.y);
		BlockData ifNotNull3 = world.GetIfNotNull(pos.x, pos.y + 1);
		BlockData ifNotNull4 = world.GetIfNotNull(pos.x - 1, pos.y);
		int num = 0;
		if (ifNotNull != null && ifNotNull.r)
		{
			num++;
		}
		if (ifNotNull2 != null && ifNotNull2.r)
		{
			num += 2;
		}
		if (ifNotNull3 != null && ifNotNull3.r)
		{
			num += 4;
		}
		if (ifNotNull4 != null && ifNotNull4.r)
		{
			num += 8;
		}
		string tile;
		int rotation;
		int code;
		if (num == 15)
		{
			BlockData ifNotNull5 = world.GetIfNotNull(pos.x + 1, pos.y + 1);
			BlockData ifNotNull6 = world.GetIfNotNull(pos.x + 1, pos.y - 1);
			BlockData ifNotNull7 = world.GetIfNotNull(pos.x - 1, pos.y - 1);
			BlockData ifNotNull8 = world.GetIfNotNull(pos.x - 1, pos.y + 1);
			tile = "curvein_spec";
			if (ifNotNull5 != null && !ifNotNull5.r)
			{
				rotation = 0;
				code = 5;
			}
			else if (ifNotNull6 != null && !ifNotNull6.r)
			{
				rotation = 90;
				code = 6;
			}
			else if (ifNotNull7 != null && !ifNotNull7.r)
			{
				rotation = 180;
				code = 7;
			}
			else if (ifNotNull8 != null && !ifNotNull8.r)
			{
				rotation = 270;
				code = 8;
			}
			else
			{
				tile = "raw";
				rotation = 0;
				code = 14;
			}
		}
		else
		{
			code = 14;
			switch (num)
			{
			case 3:
				tile = "curveout";
				rotation = 270;
				break;
			case 6:
				tile = "curveout";
				rotation = 180;
				break;
			case 7:
				tile = "precurve";
				rotation = 270;
				break;
			case 9:
				tile = "curveout";
				rotation = 0;
				break;
			case 11:
				tile = "precurve";
				rotation = 0;
				break;
			case 12:
				tile = "curveout";
				rotation = 90;
				break;
			case 13:
				tile = "precurve";
				rotation = 90;
				break;
			case 14:
				tile = "precurve";
				rotation = 180;
				break;
			default:
				tile = "curvecenter";
				rotation = 0;
				break;
			}
		}
		CreatePiece(new Vector3(pos.x, 16f, pos.y), rotation, tile, code, intersec: true);
	}

	private void IntersectionMaster(Vector3 pos)
	{
		Vector2Int vector2Int = new Vector2Int((int)pos.x, (int)pos.z);
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		if (vector2Int.x < 1 || vector2Int.x > worldSize || vector2Int.y < 1 || vector2Int.y > worldSize)
		{
			return;
		}
		for (int i = (int)pos.x - 1; i <= (int)pos.x + 1; i++)
		{
			for (int j = (int)pos.z - 1; j <= (int)pos.z + 1; j++)
			{
				r.DestroyRoad(i, j);
			}
		}
		int num = 0;
		RoadCreator ifNotNull = r.GetIfNotNull(vector2Int.x - 3, level, vector2Int.y);
		RoadCreator ifNotNull2 = r.GetIfNotNull(vector2Int.x + 3, level, vector2Int.y);
		RoadCreator ifNotNull3 = r.GetIfNotNull(vector2Int.x, level, vector2Int.y - 3);
		RoadCreator ifNotNull4 = r.GetIfNotNull(vector2Int.x, level, vector2Int.y + 3);
		if (ifNotNull != null)
		{
			num++;
		}
		if (ifNotNull2 != null)
		{
			num++;
		}
		if (ifNotNull3 != null)
		{
			num++;
		}
		if (ifNotNull4 != null)
		{
			num++;
		}
		string tile = "curvein_no";
		if (num == 4 && level == 1)
		{
			tile = "curvein";
		}
		else if (num == 2 && level == 2)
		{
			int rotation = 0;
			if (ifNotNull4 != null && ifNotNull != null)
			{
				rotation = 0;
			}
			else if (ifNotNull4 != null && ifNotNull2 != null)
			{
				rotation = 2;
			}
			else if (ifNotNull3 != null && ifNotNull2 != null)
			{
				rotation = 3;
			}
			else if (ifNotNull3 != null && ifNotNull != null)
			{
				rotation = 1;
			}
			BuildHighwayCurve(pos, rotation);
			return;
		}
		CreatePiece(new Vector3(pos.x, pos.y, pos.z), 0, "curvecenter", 14, intersec: true);
		if (r.GetIfNotNull(vector2Int.x - 1, level, vector2Int.y + 2) != null && r.GetIfNotNull(vector2Int.x - 2, level, vector2Int.y + 1) != null)
		{
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z + 1f), 270, tile, 8, intersec: true);
			flag2 = true;
			flag3 = true;
		}
		else if (r.GetIfNotNull(vector2Int.x - 1, level, vector2Int.y + 2) != null && r.GetIfNotNull(vector2Int.x - 2, level, vector2Int.y + 1) == null)
		{
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z + 1f), 270, "precurve", 4, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x - 1, level, vector2Int.y + 2) == null && r.GetIfNotNull(vector2Int.x - 2, level, vector2Int.y + 1) != null)
		{
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z + 1f), 0, "precurve", 2, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x - 1, level, vector2Int.y + 2) == null && r.GetIfNotNull(vector2Int.x - 2, level, vector2Int.y + 1) == null)
		{
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z + 1f), 90, "curveout", 4, intersec: true);
		}
		if (r.GetIfNotNull(vector2Int.x, level, vector2Int.y + 2) == null)
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 0, "precurve", 2, intersec: true);
		}
		else
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 270, "curveincenter", 12, intersec: true);
		}
		if (r.GetIfNotNull(vector2Int.x + 1, level, vector2Int.y + 2) != null && r.GetIfNotNull(vector2Int.x + 2, level, vector2Int.y + 1) != null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z + 1f), 0, tile, 5, intersec: true);
			flag = true;
			flag3 = true;
		}
		else if (r.GetIfNotNull(vector2Int.x + 1, level, vector2Int.y + 2) != null && r.GetIfNotNull(vector2Int.x + 2, level, vector2Int.y + 1) == null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z + 1f), 90, "precurve", 3, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x + 1, level, vector2Int.y + 2) == null && r.GetIfNotNull(vector2Int.x + 2, level, vector2Int.y + 1) != null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z + 1f), 0, "precurve", 2, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x + 1, level, vector2Int.y + 2) == null && r.GetIfNotNull(vector2Int.x + 2, level, vector2Int.y + 1) == null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z + 1f), 180, "curveout", 2, intersec: true);
		}
		if (r.GetIfNotNull(vector2Int.x + 2, level, vector2Int.y) == null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 90, "precurve", 14, intersec: true);
		}
		else
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 0, "curveincenter", 9, intersec: true);
		}
		if (r.GetIfNotNull(vector2Int.x + 1, level, vector2Int.y - 2) != null && r.GetIfNotNull(vector2Int.x + 2, level, vector2Int.y - 1) != null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z - 1f), 90, tile, 6, intersec: true);
			flag = true;
			flag4 = true;
		}
		else if (r.GetIfNotNull(vector2Int.x + 1, level, vector2Int.y - 2) != null && r.GetIfNotNull(vector2Int.x + 2, level, vector2Int.y - 1) == null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z - 1f), 90, "precurve", 3, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x + 1, level, vector2Int.y - 2) == null && r.GetIfNotNull(vector2Int.x + 2, level, vector2Int.y - 1) != null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z - 1f), 180, "precurve", 1, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x + 1, level, vector2Int.y - 2) == null && r.GetIfNotNull(vector2Int.x + 2, level, vector2Int.y - 1) == null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z - 1f), 270, "curveout", 3, intersec: true);
		}
		if (r.GetIfNotNull(vector2Int.x, level, vector2Int.y - 2) == null)
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z - 1f), 180, "precurve", 1, intersec: true);
		}
		else
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z - 1f), 90, "curveincenter", 10, intersec: true);
		}
		if (r.GetIfNotNull(vector2Int.x - 1, level, vector2Int.y - 2) != null && r.GetIfNotNull(vector2Int.x - 2, level, vector2Int.y - 1) != null)
		{
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z - 1f), 180, tile, 7, intersec: true);
			flag2 = true;
			flag4 = true;
		}
		else if (r.GetIfNotNull(vector2Int.x - 1, level, vector2Int.y - 2) != null && r.GetIfNotNull(vector2Int.x - 2, level, vector2Int.y - 1) == null)
		{
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z - 1f), 270, "precurve", 4, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x - 1, level, vector2Int.y - 2) == null && r.GetIfNotNull(vector2Int.x - 2, level, vector2Int.y - 1) != null)
		{
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z - 1f), 180, "precurve", 1, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x - 1, level, vector2Int.y - 2) == null && r.GetIfNotNull(vector2Int.x - 2, level, vector2Int.y - 1) == null)
		{
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z - 1f), 0, "curveout", 1, intersec: true);
		}
		if (r.GetIfNotNull(vector2Int.x - 2, level, vector2Int.y) == null)
		{
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z), 270, "precurve", 4, intersec: true);
		}
		else
		{
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z), 180, "curveincenter", 11, intersec: true);
		}
		float h = world.GetBounded((int)pos.x, (int)pos.z).h;
		if (flag)
		{
			if (world.GetBounded((int)pos.x + 2, (int)pos.z).h > h)
			{
				world.GetBounded((int)pos.x + 2, (int)pos.z - 1).FlatHeight(h);
				world.GetBounded((int)pos.x + 2, (int)pos.z).FlatHeight(h);
				world.GetBounded((int)pos.x + 2, (int)pos.z + 1).FlatHeight(h);
			}
			FlatAndBuildLineX(new Vector3(pos.x + 2f, pos.y, pos.z));
			if (r.GetIfNotNull(vector2Int.x + 3, level, vector2Int.y) != null)
			{
				FlatAndBuildLineX(new Vector3(pos.x + 3f, pos.y, pos.z));
			}
		}
		if (flag2)
		{
			if (world.GetBounded((int)pos.x - 2, (int)pos.z).h > h)
			{
				world.GetBounded((int)pos.x - 2, (int)pos.z - 1).FlatHeight(h);
				world.GetBounded((int)pos.x - 2, (int)pos.z).FlatHeight(h);
				world.GetBounded((int)pos.x - 2, (int)pos.z + 1).FlatHeight(h);
			}
			if (r.GetIfNotNull(vector2Int.x - 3, level, vector2Int.y) != null)
			{
				FlatAndBuildLineX(new Vector3(pos.x - 3f, pos.y, pos.z));
			}
			FlatAndBuildLineX(new Vector3(pos.x - 2f, pos.y, pos.z));
		}
		if (flag3)
		{
			if (world.GetBounded((int)pos.x, (int)pos.z + 2).h > h)
			{
				world.GetBounded((int)pos.x - 1, (int)pos.z + 2).FlatHeight(h);
				world.GetBounded((int)pos.x, (int)pos.z + 2).FlatHeight(h);
				world.GetBounded((int)pos.x + 1, (int)pos.z + 2).FlatHeight(h);
			}
			FlatAndBuildLineZ(new Vector3(pos.x, pos.y, pos.z + 2f));
			if (r.GetIfNotNull(vector2Int.x, level, vector2Int.y + 3) != null)
			{
				FlatAndBuildLineZ(new Vector3(pos.x, pos.y, pos.z + 3f));
			}
		}
		if (flag4)
		{
			if (world.GetBounded((int)pos.x, (int)pos.z - 2).h > h)
			{
				world.GetBounded((int)pos.x - 1, (int)pos.z - 2).FlatHeight(h);
				world.GetBounded((int)pos.x, (int)pos.z - 2).FlatHeight(h);
				world.GetBounded((int)pos.x + 1, (int)pos.z - 2).FlatHeight(h);
			}
			if (r.GetIfNotNull(vector2Int.x, level, vector2Int.y - 3) != null)
			{
				FlatAndBuildLineZ(new Vector3(pos.x, pos.y, pos.z - 3f));
			}
			FlatAndBuildLineZ(new Vector3(pos.x, pos.y, pos.z - 2f));
		}
	}

	public void IntersectionRoadMasterLineX(Vector3 pos)
	{
		Vector2Int vector2Int = new Vector2Int((int)pos.x, (int)pos.z);
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		float h = world.GetBounded((int)pos.x, (int)pos.z).h;
		for (int i = (int)pos.x; i <= (int)pos.x + 1; i++)
		{
			for (int j = (int)pos.z - 1; j <= (int)pos.z + 1; j++)
			{
				r.DestroyRoad(i, j);
				world.GetBounded(i, j).FlatHeight(h);
			}
		}
		if (r.GetIfNotNull(vector2Int.x, 1, vector2Int.y + 2) != null && r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y + 1) != null)
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 270, "curvein_no_road", 8, intersec: true);
			flag2 = true;
			flag3 = true;
		}
		else if (r.GetIfNotNull(vector2Int.x, 1, vector2Int.y + 2) != null && r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y + 1) == null)
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 270, "precurve", 4, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x, 1, vector2Int.y + 2) == null && r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y + 1) != null)
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 0, "precurve", 2, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x, 1, vector2Int.y + 2) == null && r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y + 1) == null)
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 90, "curveout", 4, intersec: true);
		}
		if (r.GetIfNotNull(vector2Int.x + 1, 1, vector2Int.y + 2) != null && r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y + 1) != null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z + 1f), 0, "curvein_no_road", 5, intersec: true);
			flag = true;
			flag3 = true;
		}
		else if (r.GetIfNotNull(vector2Int.x + 1, 1, vector2Int.y + 2) != null && r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y + 1) == null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z + 1f), 90, "precurve", 3, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x + 1, 1, vector2Int.y + 2) == null && r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y + 1) != null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z + 1f), 0, "precurve", 2, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x + 1, 1, vector2Int.y + 2) == null && r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y + 1) == null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z + 1f), 180, "curveout", 2, intersec: true);
		}
		if (r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y) == null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 90, "precurve", 14, intersec: true);
		}
		else
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 0, "curveincenter", 9, intersec: true);
		}
		if (r.GetIfNotNull(vector2Int.x + 1, 1, vector2Int.y - 2) != null && r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y - 1) != null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z - 1f), 90, "curvein_no_road", 6, intersec: true);
			flag = true;
			flag4 = true;
		}
		else if (r.GetIfNotNull(vector2Int.x + 1, 1, vector2Int.y - 2) != null && r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y - 1) == null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z - 1f), 90, "precurve", 3, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x + 1, 1, vector2Int.y - 2) == null && r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y - 1) != null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z - 1f), 180, "precurve", 1, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x + 1, 1, vector2Int.y - 2) == null && r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y - 1) == null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z - 1f), 270, "curveout", 3, intersec: true);
		}
		if (r.GetIfNotNull(vector2Int.x, 1, vector2Int.y - 2) != null && r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y - 1) != null)
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z - 1f), 180, "curvein_no_road", 7, intersec: true);
			flag2 = true;
			flag4 = true;
		}
		else if (r.GetIfNotNull(vector2Int.x, 1, vector2Int.y - 2) != null && r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y - 1) == null)
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z - 1f), 270, "precurve", 4, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x, 1, vector2Int.y - 2) == null && r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y - 1) != null)
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z - 1f), 180, "precurve", 1, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x, 1, vector2Int.y - 2) == null && r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y - 1) == null)
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z - 1f), 0, "curveout", 1, intersec: true);
		}
		if (r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y) == null)
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 270, "precurve", 4, intersec: true);
		}
		else
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 180, "curveincenter", 11, intersec: true);
		}
		if (flag)
		{
			if (world.GetBounded((int)pos.x + 2, (int)pos.z).h > h)
			{
				world.GetBounded((int)pos.x + 2, (int)pos.z - 1).FlatHeight(h);
				world.GetBounded((int)pos.x + 2, (int)pos.z).FlatHeight(h);
				world.GetBounded((int)pos.x + 2, (int)pos.z + 1).FlatHeight(h);
			}
			if (r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y) != null && !r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y).isIntersec)
			{
				FlatAndBuildLineX(new Vector3(pos.x + 2f, pos.y, pos.z));
			}
			if (r.GetIfNotNull(vector2Int.x + 3, 1, vector2Int.y) != null && !r.GetIfNotNull(vector2Int.x + 3, 1, vector2Int.y).isIntersec)
			{
				FlatAndBuildLineX(new Vector3(pos.x + 3f, pos.y, pos.z));
			}
		}
		if (flag2)
		{
			if (world.GetBounded((int)pos.x - 1, (int)pos.z).h > h)
			{
				world.GetBounded((int)pos.x - 1, (int)pos.z - 1).FlatHeight(h);
				world.GetBounded((int)pos.x - 1, (int)pos.z).FlatHeight(h);
				world.GetBounded((int)pos.x - 1, (int)pos.z + 1).FlatHeight(h);
			}
			if (r.GetIfNotNull(vector2Int.x - 2, 1, vector2Int.y) != null && !r.GetIfNotNull(vector2Int.x - 2, 1, vector2Int.y).isIntersec)
			{
				FlatAndBuildLineX(new Vector3(pos.x - 2f, pos.y, pos.z));
			}
			if (r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y) != null && !r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y).isIntersec)
			{
				FlatAndBuildLineX(new Vector3(pos.x - 1f, pos.y, pos.z));
			}
		}
		if (flag3)
		{
			if (world.GetBounded((int)pos.x, (int)pos.z + 2).h > h)
			{
				world.GetBounded((int)pos.x, (int)pos.z + 2).FlatHeight(h);
				world.GetBounded((int)pos.x + 1, (int)pos.z + 2).FlatHeight(h);
			}
			Roading.subType = r.rD[vector2Int.x, 1, vector2Int.y + 2].subType;
			if (r.GetIfNotNull(vector2Int.x, 1, vector2Int.y + 2) != null && !r.GetIfNotNull(vector2Int.x, 1, vector2Int.y + 2).isIntersec)
			{
				r.FlatNewLineZ(new Vector2Int((int)pos.x, (int)pos.z + 2));
				r.BuildNewLineZ(new Vector2Int((int)pos.x, (int)pos.z + 2));
			}
			if (r.GetIfNotNull(vector2Int.x, 1, vector2Int.y + 3) != null && !r.GetIfNotNull(vector2Int.x, 1, vector2Int.y + 3).isIntersec)
			{
				r.FlatNewLineZ(new Vector2Int((int)pos.x, (int)pos.z + 3));
				r.BuildNewLineZ(new Vector2Int((int)pos.x, (int)pos.z + 3));
			}
		}
		if (flag4)
		{
			if (world.GetBounded((int)pos.x, (int)pos.z - 2).h > h)
			{
				world.GetBounded((int)pos.x, (int)pos.z - 2).FlatHeight(h);
				world.GetBounded((int)pos.x + 1, (int)pos.z - 2).FlatHeight(h);
			}
			Roading.subType = r.rD[vector2Int.x, 1, vector2Int.y - 2].subType;
			if (r.GetIfNotNull(vector2Int.x, 1, vector2Int.y - 3) != null && !r.GetIfNotNull(vector2Int.x, 1, vector2Int.y - 3).isIntersec)
			{
				r.FlatNewLineZ(new Vector2Int((int)pos.x, (int)pos.z - 3));
				r.BuildNewLineZ(new Vector2Int((int)pos.x, (int)pos.z - 3));
			}
			if (r.GetIfNotNull(vector2Int.x, 1, vector2Int.y - 2) != null && !r.GetIfNotNull(vector2Int.x, 1, vector2Int.y - 2).isIntersec)
			{
				r.FlatNewLineZ(new Vector2Int((int)pos.x, (int)pos.z - 2));
				r.BuildNewLineZ(new Vector2Int((int)pos.x, (int)pos.z - 2));
			}
		}
	}

	public void IntersectionRoadMasterLineZ(Vector3 pos)
	{
		Vector2Int vector2Int = new Vector2Int((int)pos.x, (int)pos.z);
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		float h = world.GetBounded((int)pos.x, (int)pos.z).h;
		for (int i = (int)pos.x - 1; i <= (int)pos.x + 1; i++)
		{
			for (int j = (int)pos.z; j <= (int)pos.z + 1; j++)
			{
				r.DestroyRoad(i, j);
				world.GetBounded(i, j).FlatHeight(h);
			}
		}
		string tile = "curvein_no_road";
		if (r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y + 2) != null && r.GetIfNotNull(vector2Int.x - 2, 1, vector2Int.y + 1) != null)
		{
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z + 1f), 270, tile, 8, intersec: true);
			flag2 = true;
			flag3 = true;
		}
		else if (r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y + 2) != null && r.GetIfNotNull(vector2Int.x - 2, 1, vector2Int.y + 1) == null)
		{
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z + 1f), 270, "precurve", 4, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y + 2) == null && r.GetIfNotNull(vector2Int.x - 2, 1, vector2Int.y + 1) != null)
		{
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z + 1f), 0, "precurve", 2, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y + 2) == null && r.GetIfNotNull(vector2Int.x - 2, 1, vector2Int.y + 1) == null)
		{
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z + 1f), 90, "curveout", 4, intersec: true);
		}
		if (r.GetIfNotNull(vector2Int.x, 1, vector2Int.y + 2) == null)
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 0, "precurve", 2, intersec: true);
		}
		else
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 270, "curveincenter", 12, intersec: true);
		}
		if (r.GetIfNotNull(vector2Int.x + 1, 1, vector2Int.y + 2) != null && r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y + 1) != null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z + 1f), 0, tile, 5, intersec: true);
			flag = true;
			flag3 = true;
		}
		else if (r.GetIfNotNull(vector2Int.x + 1, 1, vector2Int.y + 2) != null && r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y + 1) == null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z + 1f), 90, "precurve", 3, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x + 1, 1, vector2Int.y + 2) == null && r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y + 1) != null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z + 1f), 0, "precurve", 2, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x + 1, 1, vector2Int.y + 2) == null && r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y + 1) == null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z + 1f), 180, "curveout", 2, intersec: true);
		}
		if (r.GetIfNotNull(vector2Int.x + 1, 1, vector2Int.y - 1) != null && r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y) != null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 90, tile, 6, intersec: true);
			flag = true;
			flag4 = true;
		}
		else if (r.GetIfNotNull(vector2Int.x + 1, 1, vector2Int.y - 1) != null && r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y) == null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 90, "precurve", 3, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x + 1, 1, vector2Int.y - 1) == null && r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y) != null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 180, "precurve", 1, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x + 1, 1, vector2Int.y - 1) == null && r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y) == null)
		{
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 270, "curveout", 3, intersec: true);
		}
		if (r.GetIfNotNull(vector2Int.x, 1, vector2Int.y - 1) == null)
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 180, "precurve", 1, intersec: true);
		}
		else
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 90, "curveincenter", 10, intersec: true);
		}
		if (r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y - 1) != null && r.GetIfNotNull(vector2Int.x - 2, 1, vector2Int.y) != null)
		{
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z), 180, tile, 7, intersec: true);
			flag2 = true;
			flag4 = true;
		}
		else if (r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y - 1) != null && r.GetIfNotNull(vector2Int.x - 2, 1, vector2Int.y) == null)
		{
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z), 270, "precurve", 4, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y - 1) == null && r.GetIfNotNull(vector2Int.x - 2, 1, vector2Int.y) != null)
		{
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z), 180, "precurve", 1, intersec: true);
		}
		else if (r.GetIfNotNull(vector2Int.x - 1, 1, vector2Int.y - 1) == null && r.GetIfNotNull(vector2Int.x - 2, 1, vector2Int.y) == null)
		{
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z), 0, "curveout", 1, intersec: true);
		}
		if (flag3)
		{
			if (world.GetBounded((int)pos.x, (int)pos.z + 2).h > h)
			{
				world.GetBounded((int)pos.x - 1, (int)pos.z + 2).FlatHeight(h);
				world.GetBounded((int)pos.x, (int)pos.z + 2).FlatHeight(h);
				world.GetBounded((int)pos.x + 1, (int)pos.z + 2).FlatHeight(h);
			}
			if (r.GetIfNotNull(vector2Int.x, 1, vector2Int.y + 2) != null && !r.GetIfNotNull(vector2Int.x, 1, vector2Int.y + 2).isIntersec)
			{
				FlatAndBuildLineZ(new Vector3(pos.x, pos.y, pos.z + 2f));
			}
			if (r.GetIfNotNull(vector2Int.x, 1, vector2Int.y + 3) != null && !r.GetIfNotNull(vector2Int.x, 1, vector2Int.y + 3).isIntersec)
			{
				FlatAndBuildLineZ(new Vector3((int)pos.x, (int)pos.y, (int)pos.z + 3));
			}
		}
		if (flag4)
		{
			if (world.GetBounded((int)pos.x, (int)pos.z - 1).h > h)
			{
				world.GetBounded((int)pos.x - 1, (int)pos.z - 1).FlatHeight(h);
				world.GetBounded((int)pos.x, (int)pos.z - 1).FlatHeight(h);
				world.GetBounded((int)pos.x + 1, (int)pos.z - 1).FlatHeight(h);
			}
			if (r.GetIfNotNull(vector2Int.x, 1, vector2Int.y - 2) != null && !r.GetIfNotNull(vector2Int.x, 1, vector2Int.y - 2).isIntersec)
			{
				FlatAndBuildLineZ(new Vector3((int)pos.x, (int)pos.y, (int)pos.z - 2));
			}
			if (r.GetIfNotNull(vector2Int.x, 1, vector2Int.y - 1) != null && !r.GetIfNotNull(vector2Int.x, 1, vector2Int.y - 1).isIntersec)
			{
				FlatAndBuildLineZ(new Vector3(pos.x, pos.y, pos.z - 1f));
			}
		}
		if (flag)
		{
			if (world.GetBounded((int)pos.x + 2, (int)pos.z).h > h)
			{
				world.GetBounded((int)pos.x + 2, (int)pos.z).FlatHeight(h);
				world.GetBounded((int)pos.x + 2, (int)pos.z + 1).FlatHeight(h);
			}
			Indexer.roadSubType = r.rD[vector2Int.x + 2, 1, vector2Int.y].subType;
			if (r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y + 1) != null && !r.GetIfNotNull(vector2Int.x + 2, 1, vector2Int.y + 1).isIntersec)
			{
				r.FlatNewLineX(new Vector2Int((int)pos.x + 2, (int)pos.z + 1));
				r.BuildNewLineX(new Vector2Int((int)pos.x + 2, (int)pos.z + 1));
			}
			if (r.GetIfNotNull(vector2Int.x + 3, 1, vector2Int.y + 1) != null && !r.GetIfNotNull(vector2Int.x + 3, 1, vector2Int.y + 1).isIntersec)
			{
				r.FlatNewLineX(new Vector2Int((int)pos.x + 3, (int)pos.z + 1));
				r.BuildNewLineX(new Vector2Int((int)pos.x + 3, (int)pos.z + 1));
			}
		}
		if (flag2)
		{
			if (world.GetBounded((int)pos.x - 2, (int)pos.z).h > h)
			{
				world.GetBounded((int)pos.x - 2, (int)pos.z).FlatHeight(h);
				world.GetBounded((int)pos.x - 2, (int)pos.z + 1).FlatHeight(h);
			}
			Indexer.roadSubType = r.rD[vector2Int.x - 2, 1, vector2Int.y].subType;
			if (r.GetIfNotNull(vector2Int.x - 3, 1, vector2Int.y + 1) != null && !r.GetIfNotNull(vector2Int.x - 3, 1, vector2Int.y + 1).isIntersec)
			{
				r.FlatNewLineX(new Vector2Int((int)pos.x - 3, (int)pos.z + 1));
				r.BuildNewLineX(new Vector2Int((int)pos.x - 3, (int)pos.z + 1));
			}
			if (r.GetIfNotNull(vector2Int.x - 2, 1, vector2Int.y + 1) != null && !r.GetIfNotNull(vector2Int.x - 2, 1, vector2Int.y + 1).isIntersec)
			{
				r.FlatNewLineX(new Vector2Int((int)pos.x - 2, (int)pos.z + 1));
				r.BuildNewLineX(new Vector2Int((int)pos.x - 2, (int)pos.z + 1));
			}
		}
	}

	private void BuildStraightRoadLineX(Vector3 pos)
	{
		CreatePiece(new Vector3(pos.x, pos.y, pos.z), 180, "center", 18);
		CreatePiece(new Vector3(pos.x, pos.y, pos.z - 1f), 180, sideType, 1);
		CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 0, sideType, 2);
	}

	private void BuildConnectionRoadLineX(Vector3 pos)
	{
		sm.Play("PlopBuild");
		CreatePiece(new Vector3(pos.x, pos.y, pos.z), 180, "raw", 14);
		CreatePiece(new Vector3(pos.x, pos.y, pos.z - 1f), 180, "precurve", 14);
		CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 0, "precurve", 14);
		int num = 1;
		if (subType == "Hway/")
		{
			num = 2;
		}
		r.CreateConnection(new Vector2Int((int)pos.x, (int)pos.z - 1), isExit: true, num);
		Debug.Log("Try to create connection at x=" + (int)pos.x + " z=" + (int)pos.z + " level=" + num);
		r.CreateConnection(new Vector2Int((int)pos.x, (int)pos.z + 1), isExit: false, num);
	}

	private void BuildStraightRoadLineZ(Vector3 pos)
	{
		CreatePiece(new Vector3(pos.x, pos.y, pos.z), 270, "center", 17);
		CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z), 270, sideType, 4);
		CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 90, sideType, 3);
	}

	private void BuildConnectionRoadLineZ(Vector3 pos)
	{
		sm.Play("PlopBuild");
		CreatePiece(new Vector3(pos.x, pos.y, pos.z), 270, "raw", 14);
		CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z), 270, "precurve", 14);
		CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 90, "precurve", 14);
		int num = 1;
		if (subType == "Hway/")
		{
			num = 2;
		}
		r.CreateConnection(new Vector2Int((int)pos.x - 1, (int)pos.z), isExit: true, num);
		Debug.Log("Try to create connection at x=" + (int)pos.x + " z=" + (int)pos.z + " level=" + num);
		r.CreateConnection(new Vector2Int((int)pos.x + 1, (int)pos.z), isExit: false, num);
	}

	private void BuildStraight25LineX(Vector3 pos, int rotation)
	{
		if (rotation == 0)
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 180, "center25", 18);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z - 1f), 180, "side25", 1);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 0, "side25in", 2);
		}
		else
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 0, "center25", 18);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z - 1f), 180, "side25in", 1);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 0, "side25", 2);
		}
	}

	private void BuildStraight50LineX(Vector3 pos, int rotation)
	{
		if (rotation == 0)
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 0, "center50", 18);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z - 1f), 180, "side50", 1);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 0, "side50in", 2);
		}
		else
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 180, "center50", 18);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z - 1f), 180, "side50in", 1);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 0, "side50", 2);
		}
	}

	private void BuildStraight25LineZ(Vector3 pos, int rotation)
	{
		if (rotation == 0)
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 270, "center25", 17);
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z), 270, "side25", 4);
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 90, "side25in", 3);
		}
		else
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 90, "center25", 17);
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z), 270, "side25in", 4);
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 90, "side25", 3);
		}
	}

	private void BuildStraight50LineZ(Vector3 pos, int rotation)
	{
		if (rotation == 0)
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 90, "center50", 17);
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z), 270, "side50", 4);
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 90, "side50in", 3);
		}
		else
		{
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 270, "center50", 17);
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z), 270, "side50in", 4);
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 90, "side50", 3);
		}
	}

	private void BuildEndOfRoad(Vector3 pos, int rotation)
	{
		switch (rotation)
		{
		case 0:
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 90, "precurve", 3);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z - 1f), 270, "curveout", 3);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 180, "curveout", 2);
			break;
		case 90:
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 180, "precurve", 1);
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z), 0, "curveout", 1);
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 270, "curveout", 3);
			break;
		case 180:
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 270, "precurve", 4);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z - 1f), 0, "curveout", 1);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 90, "curveout", 4);
			break;
		case 270:
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 0, "precurve", 2);
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z), 90, "curveout", 4);
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 180, "curveout", 2);
			break;
		}
	}

	private void CreatePiece(Vector3 pos, int rotation, string tile, int code, bool intersec = false)
	{
		BlockData ifNotNull = world.GetIfNotNull((int)pos.x, (int)pos.z);
		if (ifNotNull == null)
		{
			return;
		}
		int num = 1;
		Vector3 vector = new Vector3(pos.x, ifNotNull.h + 0.502f, pos.z);
		if (subType != "Avenue/")
		{
			vector.y += 0.75f;
			num = 2;
			if (tile == sideType && r.GetIfNotNull((int)pos.x, 1, (int)pos.z) != null)
			{
				tile = "sideroad";
			}
		}
		GameObject gameObject = Resources.Load<GameObject>("Roads/" + subType + "Ground/" + tile);
		if (gameObject == null)
		{
			Debug.LogError("This object could not be found: Roads/" + subType + "Ground/" + tile);
			return;
		}
		ifNotNull.r = true;
		ifNotNull.g = false;
		ifNotNull.b = true;
		ifNotNull.t = BlockData.Type.ROAD;
		GameObject gameObject2 = Object.Instantiate(gameObject);
		gameObject2.transform.position = vector;
		gameObject2.name = pos.x + "_" + 50 + "_" + pos.z;
		gameObject2.transform.Rotate(0f, rotation, 0f, Space.Self);
		gameObject2.transform.parent = tRoad.transform;
		gameObject2.tag = "Road";
		RoadCreator roadCreator = gameObject2.AddComponent<RoadCreator>();
		roadCreator.wPos = vector;
		roadCreator.aPos = new Vector3(pos.x, 16f, pos.z);
		roadCreator.Orientation = rotation;
		roadCreator.tile = tile;
		roadCreator.subType = subType;
		roadCreator.style = "Ground/";
		roadCreator.path = "Roads/" + subType + "Ground/" + tile;
		roadCreator.cc = code;
		roadCreator.isIntersec = intersec;
		if (subType == "Avenue/")
		{
			roadCreator.rType = RoadCreator.RoadType.AVENUE;
			roadCreator.Capacity = 12000;
		}
		else
		{
			roadCreator.rType = RoadCreator.RoadType.HIGHWAY;
			roadCreator.Capacity = 24000;
		}
		ifNotNull.o.lavenues.Add(roadCreator);
		r.rD[(int)pos.x, num, (int)pos.z] = roadCreator;
		world.Roads.Add(roadCreator);
		if (ifNotNull.h < 29.75f)
		{
			rcBridge.Add(roadCreator);
		}
	}

	private void BuildHighwayCurve(Vector3 pos, int rotation)
	{
		Vector2Int vector2Int = new Vector2Int((int)pos.x, (int)pos.z);
		pos.y = world.GetBounded((int)pos.x, (int)pos.z).wP.y;
		for (int i = (int)pos.x - 1; i <= (int)pos.x + 1; i++)
		{
			for (int j = (int)pos.z - 1; j <= (int)pos.z + 1; j++)
			{
				r.DestroyRoad(i, j);
			}
		}
		switch (rotation)
		{
		case 0:
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z - 1f), 180, sideType, 1, intersec: true);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z - 1f), 180, sideType, 1, intersec: true);
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 90, sideType, 3, intersec: true);
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z + 1f), 90, sideType, 3, intersec: true);
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z - 1f), 270, "curveout", 3, intersec: true);
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z + 1f), 90, "curvein", 2, intersec: true);
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z), 180, "center", 18, intersec: true);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 90, "center", 17, intersec: true);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 90, "curvein_no_road", 18, intersec: true);
			break;
		case 1:
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z + 1f), 0, sideType, 2, intersec: true);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 0, sideType, 2, intersec: true);
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 90, sideType, 3, intersec: true);
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z - 1f), 90, sideType, 3, intersec: true);
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z + 1f), 180, "curveout", 2, intersec: true);
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z - 1f), 0, "curvein", 4, intersec: true);
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z), 0, "center", 18, intersec: true);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z - 1f), 90, "center", 17, intersec: true);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 0, "curvein_no_road", 18, intersec: true);
			break;
		case 2:
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z + 1f), 270, sideType, 4, intersec: true);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 270, "center", 17, intersec: true);
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z + 1f), 180, "curvein", 3, intersec: true);
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z), 270, sideType, 4, intersec: true);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 180, "curvein_no_road", 18, intersec: true);
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 180, "center", 18, intersec: true);
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z - 1f), 0, "curveout", 1, intersec: true);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z - 1f), 180, sideType, 1, intersec: true);
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z - 1f), 180, sideType, 1, intersec: true);
			break;
		case 3:
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z + 1f), 90, "curveout", 4, intersec: true);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z + 1f), 0, sideType, 2, intersec: true);
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z + 1f), 0, sideType, 2, intersec: true);
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z), 270, sideType, 4, intersec: true);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z), 270, "curvein_no_road", 18, intersec: true);
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z), 0, "center", 18, intersec: true);
			CreatePiece(new Vector3(pos.x - 1f, pos.y, pos.z - 1f), 270, sideType, 4, intersec: true);
			CreatePiece(new Vector3(pos.x, pos.y, pos.z - 1f), 270, "center", 17, intersec: true);
			CreatePiece(new Vector3(pos.x + 1f, pos.y, pos.z - 1f), 270, "curvein", 1, intersec: true);
			break;
		}
		float h = world.GetBounded((int)pos.x, (int)pos.z).h;
		if (rotation == 2 || rotation == 3)
		{
			if (world.GetBounded((int)pos.x + 2, (int)pos.z).h > h)
			{
				world.GetBounded((int)pos.x + 2, (int)pos.z - 1).FlatHeight(h);
				world.GetBounded((int)pos.x + 2, (int)pos.z).FlatHeight(h);
				world.GetBounded((int)pos.x + 2, (int)pos.z + 1).FlatHeight(h);
			}
			FlatAndBuildLineX(new Vector3(pos.x + 2f, pos.y, pos.z));
			if (r.GetIfNotNull(vector2Int.x + 3, 1, vector2Int.y) != null)
			{
				FlatAndBuildLineX(new Vector3(pos.x + 3f, pos.y, pos.z));
			}
		}
		if (rotation == 0 || rotation == 1)
		{
			if (world.GetBounded((int)pos.x - 2, (int)pos.z).h > h)
			{
				world.GetBounded((int)pos.x - 2, (int)pos.z - 1).FlatHeight(h);
				world.GetBounded((int)pos.x - 2, (int)pos.z).FlatHeight(h);
				world.GetBounded((int)pos.x - 2, (int)pos.z + 1).FlatHeight(h);
			}
			if (r.GetIfNotNull(vector2Int.x - 3, 2, vector2Int.y) != null)
			{
				FlatAndBuildLineX(new Vector3(pos.x - 3f, pos.y, pos.z));
			}
			FlatAndBuildLineX(new Vector3(pos.x - 2f, pos.y, pos.z));
		}
		if (rotation == 0 || rotation == 2)
		{
			if (world.GetBounded((int)pos.x, (int)pos.z + 2).h > h)
			{
				world.GetBounded((int)pos.x - 1, (int)pos.z + 2).FlatHeight(h);
				world.GetBounded((int)pos.x, (int)pos.z + 2).FlatHeight(h);
				world.GetBounded((int)pos.x + 1, (int)pos.z + 2).FlatHeight(h);
			}
			FlatAndBuildLineZ(new Vector3(pos.x, pos.y, pos.z + 2f));
			if (r.GetIfNotNull(vector2Int.x, 2, vector2Int.y + 3) != null)
			{
				FlatAndBuildLineZ(new Vector3(pos.x, pos.y, pos.z + 3f));
			}
		}
		if (rotation == 1 || rotation == 3)
		{
			if (world.GetBounded((int)pos.x, (int)pos.z - 2).h > h)
			{
				world.GetBounded((int)pos.x - 1, (int)pos.z - 2).FlatHeight(h);
				world.GetBounded((int)pos.x, (int)pos.z - 2).FlatHeight(h);
				world.GetBounded((int)pos.x + 1, (int)pos.z - 2).FlatHeight(h);
			}
			if (r.GetIfNotNull(vector2Int.x, 2, vector2Int.y - 3) != null)
			{
				FlatAndBuildLineZ(new Vector3(pos.x, pos.y, pos.z - 3f));
			}
			FlatAndBuildLineZ(new Vector3(pos.x, pos.y, pos.z - 2f));
		}
	}

	private void BuildBridge()
	{
		float num = 0f;
		foreach (RoadCreator item in rcBridge)
		{
			if (!(item == null))
			{
				string text = "side";
				int cc = item.cc;
				int num2;
				switch (cc)
				{
				case 4:
					num2 = -90;
					num++;
					break;
				case 3:
					num2 = 90;
					break;
				case 2:
					num2 = 0;
					num++;
					break;
				case 1:
					num2 = 180;
					break;
				case 17:
				case 18:
					num2 = (int)item.Orientation;
					text = "center";
					break;
				default:
					Debug.Log("Bridge Error: Not straight?");
					return;
				}
				Vector3 wPos = item.wPos;
				Vector3 position = item.gameObject.transform.position;
				position.y = 30.75f;
				level = 1;
				if (subType != "Avenue/")
				{
					position.y += 0.75f;
					level = 2;
				}
				GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Roads/" + subType + "Bridge/" + text));
				gameObject.transform.position = position;
				gameObject.name = wPos.x + "_" + 50 + "_" + wPos.z;
				gameObject.transform.Rotate(0f, num2, 0f, Space.Self);
				gameObject.transform.parent = tRoad.transform;
				gameObject.isStatic = false;
				gameObject.tag = "Road";
				RoadCreator roadCreator = gameObject.AddComponent<RoadCreator>();
				roadCreator.wPos = position;
				roadCreator.aPos = new Vector3(wPos.x, 16f, wPos.z);
				roadCreator.Orientation = num2;
				roadCreator.rType = item.rType;
				roadCreator.cc = item.cc;
				roadCreator.isBridge = true;
				roadCreator.subType = subType;
				roadCreator.style = "Bridge/";
				roadCreator.tile = text;
				r.DestroyRoad((int)wPos.x, (int)wPos.z);
				r.rD[(int)wPos.x, level, (int)wPos.z] = roadCreator;
				world.Roads.Add(roadCreator);
				BlockData bounded = world.GetBounded((int)wPos.x, (int)wPos.z);
				bounded.t = BlockData.Type.ROAD;
				if (subType == "Avenue/")
				{
					roadCreator.rType = RoadCreator.RoadType.AVENUE;
					roadCreator.Capacity = 12000;
				}
				else
				{
					roadCreator.rType = RoadCreator.RoadType.HIGHWAY;
					roadCreator.Capacity = 30000;
				}
				bounded.o.lavenues.Add(roadCreator);
				if ((cc == 4 || cc == 2) && (num == 11f || num == 33f || num == 55f || num == 77f || num == 99f || num == 121f || num == 143f) && num * 3f + 33f <= (float)rcBridge.Count)
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
			90 => new Vector3(-1f, 0f, 0f), 
			-90 => new Vector3(1f, 0f, 0f), 
			180 => new Vector3(0f, 0f, 1f), 
			_ => new Vector3(0f, 0f, -1f), 
		};
		Vector3 position = rc.gameObject.transform.position;
		position.y = 30.75f;
		if (subType != "Avenue/")
		{
			position.y += 0.75f;
		}
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Roads/Bridge_Avenue_A"));
		gameObject.transform.position = position + vector;
		gameObject.name = wPos.x + "_bridge_" + wPos.z;
		gameObject.transform.Rotate(0f, Orientation, 0f, Space.Self);
		gameObject.isStatic = false;
		gameObject.transform.parent = rc.gameObject.transform;
	}

	private void CheckChunks(BlockData b)
	{
		updates.Add(b.o);
		if (b.pos[0] == 0)
		{
			BlockData bounded = world.GetBounded(b.wP.x - 1, b.wP.y);
			updates.Add(bounded.o);
		}
		else if (b.pos[0] == 15)
		{
			BlockData bounded2 = world.GetBounded(b.wP.x + 1, b.wP.y);
			updates.Add(bounded2.o);
		}
		if (b.pos[1] == 0)
		{
			BlockData bounded3 = world.GetBounded(b.wP.x, b.wP.y - 1);
			updates.Add(bounded3.o);
		}
		else if (b.pos[1] == 15)
		{
			BlockData bounded4 = world.GetBounded(b.wP.x, b.wP.y + 1);
			updates.Add(bounded4.o);
		}
	}

	private IEnumerator UpdateChunksAndBridge()
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
