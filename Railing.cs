using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Railing : MonoBehaviour
{
	public CostHelper costHelper;

	public GameObject whiteSquare;

	public GameObject railFolder;

	public GameObject undergroundFolder;

	public GameObject wSquare_1;

	public GameObject grid;

	public BlockInteraction bi;

	public World world;

	public SoundManager sm;

	public Material wMat;

	public Roading roading;

	private List<Vector2Int> lateBitwise = new List<Vector2Int>();

	private int autoLimit;

	public static string style = "Ground/";

	private readonly HashSet<Chunk> updates = new HashSet<Chunk>();

	private bool suitable = true;

	private void Start()
	{
		if (costHelper == null)
		{
			costHelper = roading.costHelper;
		}
	}

	private void Update()
	{
		int index = Indexer.Index;
		if (Input.GetMouseButtonDown(0) && index == 17 && bi.tamere == 0)
		{
			if (EventSystem.current.IsPointerOverGameObject())
			{
				return;
			}
			bi.x1 = (int)bi.pos.x;
			bi.z1 = (int)bi.pos.z;
			bi.tamere = 1;
		}
		if (Input.GetMouseButton(0) && index == 17 && bi.tamere == 1)
		{
			RoadUpdate();
			bi.oldx1 = bi.x1;
			bi.oldz1 = bi.z1;
			bi.oldx2 = bi.x2;
			bi.oldz2 = bi.z2;
		}
		if (index == 17)
		{
			if (bi.tamere == 0)
			{
				TooltipSystem.Hide();
			}
			float y = 0.515f;
			if (RTS_Camera.showingUnderground)
			{
				y = -0.02f;
			}
			Vector3 vector = new Vector3(0f, y, 0f);
			BlockData bounded = world.GetBounded((int)bi.pos.x, (int)bi.pos.z);
			if (bounded != null)
			{
				wSquare_1.transform.position = bi.pos + vector;
				if (style == "Ground/" && bounded.a)
				{
					wMat.color = roading.azul;
				}
				else if (style == "Ground/" && !bounded.b)
				{
					wMat.color = roading.lime;
				}
				else if (style == "Underground/")
				{
					if (bounded.u)
					{
						wMat.color = roading.azul;
					}
					else
					{
						wMat.color = roading.lime;
					}
				}
				else
				{
					wMat.color = Color.red;
				}
				grid.transform.position = new Vector3((float)bounded.wP.x - 0.5f, 60f, (float)bounded.wP.y + 0.5f);
			}
		}
		if (Input.GetMouseButtonUp(0) && bi.tamere == 1 && index == 17)
		{
			if (suitable)
			{
				BuildRailLine(bi.x1, bi.z1, bi.x2, bi.z2);
			}
			else
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
		if (Input.GetKeyDown(KeyCode.Escape) && index == 17 && bi.tamere == 1)
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
		int x = bi.x1;
		int z = bi.z1;
		int x2 = bi.x2;
		int z2 = bi.z2;
		int oldx = bi.oldx2;
		int oldz = bi.oldz2;
		float y = bi.pos.y;
		int num = Mathf.Abs(x - x2) + 1;
		int num2 = Mathf.Abs(z - z2) + 1;
		y += 0.5f;
		int num3 = ((!(style == "Ground/")) ? 75 : 10);
		if (x2 == oldx && z2 == oldz)
		{
			return;
		}
		suitable = true;
		StopVisualizeRoad();
		if (x < x2 && num > num2)
		{
			costHelper.SetCost(num * num3);
			for (int i = x; i < x2 + 1; i++)
			{
				VisualizeRoad(new Vector3(i, y, z), goingX: true);
			}
		}
		else if (x2 < x && num > num2)
		{
			costHelper.SetCost(num * num3);
			for (int num4 = x; num4 >= x2; num4--)
			{
				VisualizeRoad(new Vector3(num4, y, z), goingX: true);
			}
		}
		else if (z < z2 && num2 > num)
		{
			costHelper.SetCost(num2 * num3);
			for (int j = z; j <= z2; j++)
			{
				VisualizeRoad(new Vector3(x, y, j), goingX: false);
			}
		}
		else if (z2 < z && num2 > num * 2)
		{
			costHelper.SetCost(num2 * num3);
			for (int num5 = z; num5 > z2 - 1; num5--)
			{
				VisualizeRoad(new Vector3(x, y, num5), goingX: false);
			}
		}
	}

	public void DestroyRail(int x, int z)
	{
		RoadCreator roadCreator = roading.rG[x, 1, z];
		if (roadCreator != null)
		{
			world.Rails.Remove(roadCreator);
			BlockData blockData = world.blockData[x, z];
			blockData.o.lroads.Remove(roadCreator);
			blockData.t = BlockData.Type.GRASS;
			if (roadCreator.gameObject != null)
			{
				Object.Destroy(roadCreator.gameObject);
			}
		}
	}

	public void DestroyUnderground(int x, int z)
	{
		RoadCreator roadCreator = roading.rG[x, 0, z];
		if (roadCreator != null)
		{
			world.Underground.Remove(roadCreator);
			if (roadCreator.gameObject != null)
			{
				Object.Destroy(roadCreator.gameObject);
			}
		}
	}

	private void VisualizeRoad(Vector3 pos, bool goingX)
	{
		BlockData blockData = bi.world.blockData[(int)pos.x, (int)pos.z];
		float num = ((!RTS_Camera.showingUnderground) ? 0.515f : (-0.99f));
		Vector3 position = new Vector3(blockData.wP.x, blockData.h + num, blockData.wP.y);
		bool flag = true;
		if (style == "Underground/")
		{
			TooltipSystem.Hide();
			BlockData ifNotNull = world.GetIfNotNull((int)pos.x, (int)pos.z - 1);
			BlockData ifNotNull2 = world.GetIfNotNull((int)pos.x + 1, (int)pos.z);
			BlockData ifNotNull3 = world.GetIfNotNull((int)pos.x, (int)pos.z + 1);
			BlockData ifNotNull4 = world.GetIfNotNull((int)pos.x - 1, (int)pos.z);
			if ((goingX && Mathf.Abs(ifNotNull4.h - blockData.h) > 0.5f) || (goingX && Mathf.Abs(ifNotNull2.h - blockData.h) > 0.5f))
			{
				TooltipSystem.Show("", "Too Steep");
				suitable = false;
				flag = false;
			}
			else if ((!goingX && Mathf.Abs(ifNotNull3.h - blockData.h) > 0.5f) || (!goingX && Mathf.Abs(ifNotNull.h - blockData.h) > 0.5f))
			{
				TooltipSystem.Show("", "Too Steep");
				suitable = false;
				flag = false;
			}
		}
		else
		{
			RoadCreator ifNotNull5 = roading.GetIfNotNull(blockData.wP.x, 1, blockData.wP.y);
			if (ifNotNull5 != null)
			{
				if (ifNotNull5.tile != "center" && ifNotNull5.tile != "side" && ifNotNull5.tile != "straight")
				{
					suitable = false;
					TooltipSystem.Show("", "straightflat");
					flag = false;
				}
				else if (ifNotNull5.Orientation == 0f || ifNotNull5.Orientation == 180f)
				{
					if (world.GetBounded((int)pos.x, (int)pos.z + 2).h != blockData.h || world.GetBounded((int)pos.x, (int)pos.z - 2).h != blockData.h)
					{
						flag = false;
						TooltipSystem.Show("", "interflat");
					}
				}
				else if (world.GetBounded((int)pos.x + 2, (int)pos.z).h != blockData.h || world.GetBounded((int)pos.x - 2, (int)pos.z).h != blockData.h)
				{
					flag = false;
					TooltipSystem.Show("", "interflat");
				}
			}
		}
		if (flag)
		{
			Object.Instantiate(whiteSquare, position, Quaternion.identity);
			whiteSquare.name = "NewCube";
			whiteSquare.tag = "NewCube";
			sm.Play("RoadPiece");
		}
		else
		{
			suitable = false;
			Object.Instantiate(roading.redSquare, position, Quaternion.identity);
			roading.redSquare.name = "NewCube";
			roading.redSquare.tag = "NewCube";
			sm.Play("Impractical");
		}
	}

	private void StopVisualizeRoad()
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

	private void BuildRailLine(int _x1, int _z1, int _x2, int _z2)
	{
		int num = Mathf.Abs(_x1 - _x2) + 1;
		int num2 = Mathf.Abs(_z1 - _z2) + 1;
		bool flag = style == "Ground/";
		if (_x1 < _x2 && num > num2)
		{
			for (int i = _x1; i < _x2 + 1; i++)
			{
				if (flag)
				{
					world.GetBounded(i, _z1).a = true;
					world.GetBounded(i, _z1).ClearTerrain();
				}
				else
				{
					world.GetBounded(i, _z1).u = true;
				}
			}
			if (flag)
			{
				for (int j = _x1; j < _x2 + 1; j++)
				{
					PathRight(new Vector2Int(j, _z1));
				}
			}
		}
		else if (_x2 < _x1 && num > num2)
		{
			for (int num3 = _x1; num3 >= _x2; num3--)
			{
				if (flag)
				{
					world.GetBounded(num3, _z1).a = true;
					world.GetBounded(num3, _z1).ClearTerrain();
				}
				else
				{
					world.GetBounded(num3, _z1).u = true;
				}
			}
			if (flag)
			{
				for (int num4 = _x1; num4 >= _x2; num4--)
				{
					PathLeft(new Vector2Int(num4, _z1));
				}
			}
		}
		if (_z1 < _z2 && num2 > num)
		{
			for (int k = _z1; k <= _z2; k++)
			{
				if (flag)
				{
					world.GetBounded(_x1, k).a = true;
					world.GetBounded(_x1, k).ClearTerrain();
				}
				else
				{
					world.GetBounded(_x1, k).u = true;
				}
			}
			if (flag)
			{
				for (int l = _z1; l <= _z2; l++)
				{
					PathUp(new Vector2Int(_x1, l));
				}
			}
		}
		else if (_z2 < _z1 && num2 > num)
		{
			for (int num5 = _z1; num5 > _z2 - 1; num5--)
			{
				if (flag)
				{
					world.GetBounded(_x1, num5).a = true;
					world.GetBounded(_x1, num5).ClearTerrain();
				}
				else
				{
					world.GetBounded(_x1, num5).u = true;
				}
			}
			if (flag)
			{
				for (int num6 = _z1; num6 > _z2 - 1; num6--)
				{
					PathDown(new Vector2Int(_x1, num6));
				}
			}
		}
		if (_x1 < _x2 && num > num2)
		{
			for (int m = _x1; m < _x2 + 1; m++)
			{
				RoadBitwise(new Vector2Int(m, _z1));
			}
		}
		else if (_x2 < _x1 && num > num2)
		{
			for (int num7 = _x1; num7 >= _x2; num7--)
			{
				RoadBitwise(new Vector2Int(num7, _z1));
			}
		}
		if (_z1 < _z2 && num2 > num)
		{
			for (int n = _z1; n <= _z2; n++)
			{
				RoadBitwise(new Vector2Int(_x1, n));
			}
		}
		else if (_z2 < _z1 && num2 > num)
		{
			for (int num8 = _z1; num8 > _z2 - 1; num8--)
			{
				RoadBitwise(new Vector2Int(_x1, num8));
			}
		}
		foreach (Vector2Int item in lateBitwise)
		{
			RoadBitwise(item);
		}
		lateBitwise.Clear();
		bi.x1 = 0;
		bi.z1 = 0;
		bi.x2 = 0;
		bi.z2 = 0;
		bi.tamere = 0;
		autoLimit = 0;
		StartCoroutine(UpdateChunks());
		StopVisualizeRoad();
		costHelper.PayForIt();
		sm.Play("RoadBuilt");
	}

	private void PathRight(Vector2Int pos)
	{
		BlockData blockData = world.blockData[pos.x, pos.y];
		BlockData ifNotNull = world.GetIfNotNull(pos.x, pos.y - 1);
		BlockData ifNotNull2 = world.GetIfNotNull(pos.x + 1, pos.y);
		BlockData ifNotNull3 = world.GetIfNotNull(pos.x, pos.y + 1);
		BlockData ifNotNull4 = world.GetIfNotNull(pos.x - 1, pos.y);
		if (ifNotNull == null || ifNotNull2 == null || ifNotNull3 == null || ifNotNull4 == null)
		{
			return;
		}
		int num = 0;
		if (!(style == "Ground/"))
		{
			return;
		}
		if (ifNotNull.a)
		{
			num++;
		}
		if (ifNotNull2.a)
		{
			num += 2;
		}
		if (ifNotNull3.a)
		{
			num += 4;
		}
		if (ifNotNull4.a)
		{
			num += 8;
		}
		if (blockData.h == 30f || blockData.h == 29.75f)
		{
			blockData.FlatHeight(30f);
		}
		else if (blockData.h < 30f)
		{
			return;
		}
		switch (num)
		{
		case 2:
			ifNotNull2.FlatHeight(blockData.h);
			break;
		case 8:
			blockData.FlatHeight(ifNotNull4.h);
			break;
		}
		switch (num)
		{
		case 3:
			blockData.FlatHeight(ifNotNull.h);
			ifNotNull2.FlatHeight(ifNotNull.h);
			lateBitwise.Add(ifNotNull.wP);
			break;
		case 6:
			blockData.FlatHeight(ifNotNull3.h);
			ifNotNull2.FlatHeight(ifNotNull3.h);
			lateBitwise.Add(ifNotNull3.wP);
			break;
		case 9:
			blockData.FlatHeight(ifNotNull4.h);
			ifNotNull.FlatHeight(ifNotNull4.h);
			AutoUpdateLine(ifNotNull, 90);
			break;
		case 12:
			blockData.FlatHeight(ifNotNull4.h);
			ifNotNull3.FlatHeight(ifNotNull4.h);
			AutoUpdateLine(ifNotNull3, 270);
			break;
		case 7:
		case 11:
		case 13:
		case 14:
		case 15:
			FlattenIntersec(blockData.wP, 0, num);
			break;
		}
		if ((blockData.h < ifNotNull4.h && blockData.h < ifNotNull2.h) || (blockData.h > ifNotNull4.h && blockData.h > ifNotNull2.h))
		{
			blockData.FlatHeight(ifNotNull4.h);
		}
		if (num == 10)
		{
			if (ifNotNull2.h > blockData.h + 0.5f)
			{
				ifNotNull2.FlatHeight(blockData.h + 0.5f);
			}
			else if (ifNotNull2.h < blockData.h - 0.5f)
			{
				ifNotNull2.FlatHeight(blockData.h - 0.5f);
			}
			if (ifNotNull2.r)
			{
				blockData.FlatHeight(ifNotNull2.h);
			}
		}
	}

	private void PathLeft(Vector2Int pos)
	{
		BlockData blockData = world.blockData[pos.x, pos.y];
		BlockData ifNotNull = world.GetIfNotNull(pos.x, pos.y - 1);
		BlockData ifNotNull2 = world.GetIfNotNull(pos.x + 1, pos.y);
		BlockData ifNotNull3 = world.GetIfNotNull(pos.x, pos.y + 1);
		BlockData ifNotNull4 = world.GetIfNotNull(pos.x - 1, pos.y);
		if (ifNotNull == null || ifNotNull2 == null || ifNotNull3 == null || ifNotNull4 == null)
		{
			return;
		}
		int num = 0;
		if (!(style == "Ground/"))
		{
			return;
		}
		if (ifNotNull.a)
		{
			num++;
		}
		if (ifNotNull2.a)
		{
			num += 2;
		}
		if (ifNotNull3.a)
		{
			num += 4;
		}
		if (ifNotNull4.a)
		{
			num += 8;
		}
		if (blockData.h == 30f || blockData.h == 29.75f)
		{
			blockData.FlatHeight(30f);
		}
		else if (blockData.h < 30f)
		{
			return;
		}
		switch (num)
		{
		case 2:
			blockData.FlatHeight(ifNotNull2.h);
			break;
		case 8:
			ifNotNull4.FlatHeight(blockData.h);
			break;
		}
		switch (num)
		{
		case 3:
			blockData.FlatHeight(ifNotNull2.h);
			ifNotNull.FlatHeight(ifNotNull2.h);
			AutoUpdateLine(ifNotNull, 90);
			break;
		case 6:
			blockData.FlatHeight(ifNotNull2.h);
			ifNotNull3.FlatHeight(ifNotNull2.h);
			AutoUpdateLine(ifNotNull3, 270);
			break;
		case 9:
			blockData.FlatHeight(ifNotNull.h);
			ifNotNull4.FlatHeight(ifNotNull.h);
			lateBitwise.Add(ifNotNull.wP);
			break;
		case 12:
			blockData.FlatHeight(ifNotNull3.h);
			ifNotNull4.FlatHeight(ifNotNull3.h);
			lateBitwise.Add(ifNotNull3.wP);
			break;
		case 7:
		case 11:
		case 13:
		case 14:
		case 15:
			FlattenIntersec(blockData.wP, 180, num);
			break;
		}
		if ((blockData.h < ifNotNull4.h && blockData.h < ifNotNull2.h) || (blockData.h > ifNotNull4.h && blockData.h > ifNotNull2.h))
		{
			blockData.FlatHeight(ifNotNull2.h);
		}
		if (num == 10)
		{
			if (ifNotNull4.h > blockData.h + 0.5f)
			{
				ifNotNull4.FlatHeight(blockData.h + 0.5f);
			}
			else if (ifNotNull4.h < blockData.h - 0.5f)
			{
				ifNotNull4.FlatHeight(blockData.h - 0.5f);
			}
			if (ifNotNull4.r)
			{
				blockData.FlatHeight(ifNotNull4.h);
			}
		}
	}

	private void PathUp(Vector2Int pos)
	{
		BlockData blockData = world.blockData[pos.x, pos.y];
		BlockData ifNotNull = world.GetIfNotNull(pos.x, pos.y - 1);
		BlockData ifNotNull2 = world.GetIfNotNull(pos.x + 1, pos.y);
		BlockData ifNotNull3 = world.GetIfNotNull(pos.x, pos.y + 1);
		BlockData ifNotNull4 = world.GetIfNotNull(pos.x - 1, pos.y);
		if (ifNotNull == null || ifNotNull2 == null || ifNotNull3 == null || ifNotNull4 == null)
		{
			return;
		}
		int num = 0;
		if (!(style == "Ground/"))
		{
			return;
		}
		if (ifNotNull.a)
		{
			num++;
		}
		if (ifNotNull2.a)
		{
			num += 2;
		}
		if (ifNotNull3.a)
		{
			num += 4;
		}
		if (ifNotNull4.a)
		{
			num += 8;
		}
		if (blockData.h == 30f || blockData.h == 29.75f)
		{
			blockData.FlatHeight(30f);
		}
		else if (blockData.h < 30f)
		{
			return;
		}
		switch (num)
		{
		case 1:
			blockData.FlatHeight(ifNotNull.h);
			break;
		case 4:
			ifNotNull3.FlatHeight(blockData.h);
			break;
		}
		switch (num)
		{
		case 3:
			blockData.FlatHeight(ifNotNull.h);
			ifNotNull2.FlatHeight(ifNotNull.h);
			AutoUpdateLine(ifNotNull2, 0);
			break;
		case 6:
			blockData.FlatHeight(ifNotNull2.h);
			ifNotNull3.FlatHeight(ifNotNull2.h);
			lateBitwise.Add(ifNotNull2.wP);
			break;
		case 9:
			blockData.FlatHeight(ifNotNull.h);
			ifNotNull4.FlatHeight(ifNotNull.h);
			AutoUpdateLine(ifNotNull4, 180);
			break;
		case 12:
			blockData.FlatHeight(ifNotNull4.h);
			ifNotNull3.FlatHeight(ifNotNull4.h);
			lateBitwise.Add(ifNotNull4.wP);
			break;
		case 7:
		case 11:
		case 13:
		case 14:
		case 15:
			FlattenIntersec(blockData.wP, 270, num);
			break;
		}
		if ((blockData.h < ifNotNull.h && blockData.h < ifNotNull3.h) || (blockData.h > ifNotNull.h && blockData.h > ifNotNull3.h))
		{
			blockData.FlatHeight(ifNotNull3.h);
		}
		if (num == 5)
		{
			if (ifNotNull3.h > blockData.h + 0.5f)
			{
				ifNotNull3.FlatHeight(blockData.h + 0.5f);
			}
			else if (ifNotNull3.h < blockData.h - 0.5f)
			{
				ifNotNull3.FlatHeight(blockData.h - 0.5f);
			}
			if (ifNotNull3.r)
			{
				blockData.FlatHeight(ifNotNull3.h);
			}
		}
	}

	private void PathDown(Vector2Int pos)
	{
		BlockData blockData = world.blockData[pos.x, pos.y];
		BlockData ifNotNull = world.GetIfNotNull(pos.x, pos.y - 1);
		BlockData ifNotNull2 = world.GetIfNotNull(pos.x + 1, pos.y);
		BlockData ifNotNull3 = world.GetIfNotNull(pos.x, pos.y + 1);
		BlockData ifNotNull4 = world.GetIfNotNull(pos.x - 1, pos.y);
		if (ifNotNull == null || ifNotNull2 == null || ifNotNull3 == null || ifNotNull4 == null)
		{
			return;
		}
		int num = 0;
		if (!(style == "Ground/"))
		{
			return;
		}
		if (ifNotNull.a)
		{
			num++;
		}
		if (ifNotNull2.a)
		{
			num += 2;
		}
		if (ifNotNull3.a)
		{
			num += 4;
		}
		if (ifNotNull4.a)
		{
			num += 8;
		}
		if (blockData.h == 30f || blockData.h == 29.75f)
		{
			blockData.FlatHeight(30f);
		}
		else if (blockData.h < 30f)
		{
			return;
		}
		switch (num)
		{
		case 1:
			ifNotNull.FlatHeight(blockData.h);
			break;
		case 4:
			blockData.FlatHeight(ifNotNull3.h);
			break;
		}
		switch (num)
		{
		case 3:
			blockData.FlatHeight(ifNotNull2.h);
			ifNotNull.FlatHeight(ifNotNull2.h);
			lateBitwise.Add(ifNotNull2.wP);
			break;
		case 6:
			blockData.FlatHeight(ifNotNull.h);
			ifNotNull2.FlatHeight(ifNotNull.h);
			AutoUpdateLine(ifNotNull2, 0);
			break;
		case 9:
			blockData.FlatHeight(ifNotNull4.h);
			ifNotNull.FlatHeight(ifNotNull4.h);
			lateBitwise.Add(ifNotNull4.wP);
			break;
		case 12:
			blockData.FlatHeight(ifNotNull.h);
			ifNotNull4.FlatHeight(ifNotNull.h);
			AutoUpdateLine(ifNotNull4, 180);
			break;
		case 7:
		case 11:
		case 13:
		case 14:
		case 15:
			FlattenIntersec(blockData.wP, 90, num);
			break;
		}
		if ((blockData.h < ifNotNull.h && blockData.h < ifNotNull3.h) || (blockData.h > ifNotNull.h && blockData.h > ifNotNull3.h))
		{
			blockData.FlatHeight(ifNotNull.h);
		}
		if (num == 5)
		{
			if (ifNotNull.h > blockData.h + 0.5f)
			{
				ifNotNull.FlatHeight(blockData.h + 0.5f);
			}
			else if (ifNotNull.h < blockData.h - 0.5f)
			{
				ifNotNull.FlatHeight(blockData.h - 0.5f);
			}
			if (ifNotNull.r)
			{
				blockData.FlatHeight(ifNotNull.h);
			}
		}
	}

	private void FlattenIntersec(Vector2Int pos, int rot, int bit)
	{
		if (autoLimit > 20)
		{
			Debug.Log("Too many magic autoline calls");
			return;
		}
		BlockData blockData = world.blockData[pos.x, pos.y];
		BlockData blockData2 = world.blockData[pos.x, pos.y - 1];
		BlockData blockData3 = world.blockData[pos.x + 1, pos.y];
		BlockData blockData4 = world.blockData[pos.x, pos.y + 1];
		BlockData blockData5 = world.blockData[pos.x - 1, pos.y];
		switch (rot)
		{
		case 0:
			blockData.FlatHeight(blockData5.h);
			if (bit != 13)
			{
				blockData3.FlatHeight(blockData5.h);
				AutoUpdateLine(blockData3, 0);
			}
			if (bit != 11)
			{
				blockData4.FlatHeight(blockData5.h);
				AutoUpdateLine(blockData4, 270);
			}
			if (bit != 14)
			{
				blockData2.FlatHeight(blockData5.h);
				AutoUpdateLine(blockData2, 90);
			}
			break;
		case 180:
			blockData.FlatHeight(blockData3.h);
			if (bit != 7)
			{
				blockData5.FlatHeight(blockData3.h);
				AutoUpdateLine(blockData5, 180);
			}
			if (bit != 11)
			{
				blockData4.FlatHeight(blockData3.h);
				AutoUpdateLine(blockData4, 270);
			}
			if (bit != 14)
			{
				blockData2.FlatHeight(blockData3.h);
				AutoUpdateLine(blockData2, 90);
			}
			break;
		case 270:
			blockData.FlatHeight(blockData2.h);
			if (bit != 7)
			{
				blockData5.FlatHeight(blockData2.h);
				AutoUpdateLine(blockData5, 180);
			}
			if (bit != 13)
			{
				blockData3.FlatHeight(blockData2.h);
				AutoUpdateLine(blockData3, 0);
			}
			if (bit != 11)
			{
				blockData4.FlatHeight(blockData2.h);
				AutoUpdateLine(blockData4, 270);
			}
			break;
		case 90:
			blockData.FlatHeight(blockData4.h);
			if (bit != 7)
			{
				blockData5.FlatHeight(blockData4.h);
				AutoUpdateLine(blockData5, 180);
			}
			if (bit != 13)
			{
				blockData3.FlatHeight(blockData4.h);
				AutoUpdateLine(blockData3, 0);
			}
			if (bit != 14)
			{
				blockData2.FlatHeight(blockData4.h);
				AutoUpdateLine(blockData2, 90);
			}
			break;
		}
	}

	private void AutoUpdateLine(BlockData bd, int rot)
	{
		bool flag = style == "Ground/";
		autoLimit++;
		if (bd == null || (!bd.a & flag) || (!bd.u && !flag))
		{
			return;
		}
		if (autoLimit > 20)
		{
			Debug.Log("Cancelling autoline method...");
			return;
		}
		lateBitwise.Add(bd.wP);
		switch (rot)
		{
		case 0:
		{
			PathRight(world.blockData[bd.wP.x, bd.wP.y].wP);
			BlockData ifNotNull3 = world.GetIfNotNull(bd.wP.x + 1, bd.wP.y);
			if (((ifNotNull3?.a ?? false) & flag) || (ifNotNull3 != null && ifNotNull3.u && !flag))
			{
				lateBitwise.Add(ifNotNull3.wP);
			}
			BlockData ifNotNull4 = world.GetIfNotNull(bd.wP.x + 2, bd.wP.y);
			if (((ifNotNull4?.a ?? false) & flag) || (ifNotNull4 != null && ifNotNull4.u && !flag))
			{
				lateBitwise.Add(ifNotNull4.wP);
			}
			break;
		}
		case 180:
		{
			PathLeft(world.blockData[bd.wP.x, bd.wP.y].wP);
			BlockData ifNotNull7 = world.GetIfNotNull(bd.wP.x - 1, bd.wP.y);
			if (((ifNotNull7?.a ?? false) & flag) || (ifNotNull7 != null && ifNotNull7.u && !flag))
			{
				lateBitwise.Add(ifNotNull7.wP);
			}
			BlockData ifNotNull8 = world.GetIfNotNull(bd.wP.x - 2, bd.wP.y);
			if (((ifNotNull8?.a ?? false) & flag) || (ifNotNull8 != null && ifNotNull8.u && !flag))
			{
				lateBitwise.Add(ifNotNull8.wP);
			}
			break;
		}
		case 270:
		{
			PathUp(world.blockData[bd.wP.x, bd.wP.y].wP);
			BlockData ifNotNull5 = world.GetIfNotNull(bd.wP.x, bd.wP.y + 1);
			if (((ifNotNull5?.a ?? false) & flag) || (ifNotNull5 != null && ifNotNull5.u && !flag))
			{
				lateBitwise.Add(ifNotNull5.wP);
			}
			BlockData ifNotNull6 = world.GetIfNotNull(bd.wP.x, bd.wP.y + 2);
			if (((ifNotNull6?.a ?? false) & flag) || (ifNotNull6 != null && ifNotNull6.u && !flag))
			{
				lateBitwise.Add(ifNotNull6.wP);
			}
			break;
		}
		case 90:
		{
			BlockData ifNotNull = world.GetIfNotNull(bd.wP.x, bd.wP.y - 1);
			if (((ifNotNull?.a ?? false) & flag) || (ifNotNull != null && ifNotNull.u && !flag))
			{
				lateBitwise.Add(ifNotNull.wP);
			}
			BlockData ifNotNull2 = world.GetIfNotNull(bd.wP.x, bd.wP.y - 2);
			if (((ifNotNull2?.a ?? false) & flag) || (ifNotNull2 != null && ifNotNull2.u && !flag))
			{
				lateBitwise.Add(ifNotNull2.wP);
			}
			break;
		}
		}
	}

	public void RoadBitwise(Vector2Int pos)
	{
		bool flag;
		int num;
		if (style == "Ground/")
		{
			flag = true;
			num = 2;
		}
		else
		{
			flag = false;
			num = 3;
		}
		BlockData blockData = world.blockData[pos.x, pos.y];
		RoadCreator roadCreator = ((!flag) ? roading.rG[pos.x, 0, pos.y] : roading.rG[pos.x, 1, pos.y]);
		if (roadCreator != null && roadCreator.Bitwise == 0)
		{
			blockData.a = false;
			return;
		}
		if (roadCreator != null && roadCreator.style == style)
		{
			if (roadCreator.stopType != RoadCreator.StopType.NONE)
			{
				if (flag)
				{
					blockData.a = false;
				}
				else
				{
					blockData.u = false;
				}
				return;
			}
			if (flag && blockData.a)
			{
				DestroyRail(pos.x, pos.y);
			}
			else if (blockData.u)
			{
				DestroyUnderground(pos.x, pos.y);
			}
		}
		RoadCreator ifNotNull = roading.GetIfNotNull(pos.x, 1, pos.y);
		if (((ifNotNull != null) & flag) && ifNotNull.tile != "center" && ifNotNull.tile != "side" && ifNotNull.tile != "sidetree" && ifNotNull.tile != "straight" && ifNotNull.tile != "straighttree")
		{
			blockData.a = false;
			Debug.Log("IS GROUND! RETURN.");
			return;
		}
		if (flag && blockData.t == BlockData.Type.BUILDING)
		{
			blockData.a = false;
			return;
		}
		CheckChunks(blockData);
		BlockData ifNotNull2 = world.GetIfNotNull(pos.x, pos.y - 1);
		BlockData ifNotNull3 = world.GetIfNotNull(pos.x + 1, pos.y);
		BlockData ifNotNull4 = world.GetIfNotNull(pos.x, pos.y + 1);
		BlockData ifNotNull5 = world.GetIfNotNull(pos.x - 1, pos.y);
		int num2 = 0;
		Vector3 vector = new Vector3(0f, 0f, 0f);
		string text = "Rail_Intersec";
		if (flag)
		{
			if (ifNotNull2 != null && ifNotNull2.a)
			{
				num2++;
			}
			if (ifNotNull3 != null && ifNotNull3.a)
			{
				num2 += 2;
			}
			if (ifNotNull4 != null && ifNotNull4.a)
			{
				num2 += 4;
			}
			if (ifNotNull5 != null && ifNotNull5.a)
			{
				num2 += 8;
			}
		}
		else
		{
			if (ifNotNull2 != null && ifNotNull2.u)
			{
				num2++;
			}
			if (ifNotNull3 != null && ifNotNull3.u)
			{
				num2 += 2;
			}
			if (ifNotNull4 != null && ifNotNull4.u)
			{
				num2 += 4;
			}
			if (ifNotNull5 != null && ifNotNull5.u)
			{
				num2 += 8;
			}
			RoadCreator roadCreator2 = roading.rG[pos.x, 1, pos.y];
			if (roadCreator2 != null && roadCreator2.Bitwise == 16)
			{
				num2 = 16;
				Debug.Log("Attached to Ground Rail");
			}
		}
		switch (num2)
		{
		case 0:
			text = "Rail_Intersec";
			vector = new Vector3(0f, 0f, 0f);
			break;
		case 1:
			text = "Rail_End";
			vector = new Vector3(0f, 0f, 0f);
			break;
		case 2:
			text = "Rail_End";
			vector = new Vector3(0f, 270f, 0f);
			break;
		case 3:
			text = "Rail_Curve";
			vector = new Vector3(0f, 270f, 0f);
			break;
		case 4:
			text = "Rail_End";
			vector = new Vector3(0f, 180f, 0f);
			break;
		case 5:
			text = "Rail_Straight";
			vector = new Vector3(0f, 90f, 0f);
			if ((double)ifNotNull2.h == (double)blockData.h + 0.25)
			{
				text = "Rail_25";
			}
			else if ((double)ifNotNull4.h == (double)blockData.h + 0.25)
			{
				text = "Rail_25";
				vector = new Vector3(0f, 270f, 0f);
			}
			else if ((double)ifNotNull4.h >= (double)blockData.h + 0.5)
			{
				text = "Rail_50";
				vector = new Vector3(0f, 270f, 0f);
			}
			else if ((double)ifNotNull2.h >= (double)blockData.h + 0.5)
			{
				text = "Rail_50";
			}
			break;
		case 6:
			text = "Rail_Curve";
			vector = new Vector3(0f, 180f, 0f);
			break;
		case 7:
			text = "Rail_Semi";
			vector = new Vector3(0f, 270f, 0f);
			break;
		case 8:
			text = "Rail_End";
			vector = new Vector3(0f, 90f, 0f);
			break;
		case 9:
			text = "Rail_Curve";
			vector = new Vector3(0f, 0f, 0f);
			break;
		case 10:
			text = "Rail_Straight";
			vector = new Vector3(0f, 0f, 0f);
			if ((double)ifNotNull5.h == (double)blockData.h + 0.25)
			{
				text = "Rail_25";
				vector = new Vector3(0f, 180f, 0f);
			}
			else if ((double)ifNotNull3.h == (double)blockData.h + 0.25)
			{
				text = "Rail_25";
			}
			else if ((double)ifNotNull3.h >= (double)blockData.h + 0.5)
			{
				text = "Rail_50";
			}
			else if ((double)ifNotNull5.h >= (double)blockData.h + 0.5)
			{
				text = "Rail_50";
				vector = new Vector3(0f, 180f, 0f);
			}
			break;
		case 11:
			text = "Rail_Semi";
			vector = new Vector3(0f, 0f, 0f);
			break;
		case 12:
			text = "Rail_Curve";
			vector = new Vector3(0f, 90f, 0f);
			break;
		case 13:
			text = "Rail_Semi";
			vector = new Vector3(0f, 90f, 0f);
			break;
		case 14:
			text = "Rail_Semi";
			vector = new Vector3(0f, 180f, 0f);
			break;
		case 15:
		case 16:
			text = "Rail_Intersec";
			vector = new Vector3(0f, 0f, 0f);
			break;
		}
		if ((ifNotNull != null && vector.y == ifNotNull.Orientation) & flag)
		{
			blockData.a = false;
			return;
		}
		Vector3 vector2;
		if (flag)
		{
			if (blockData.h < 30f)
			{
				vector2 = new Vector3(pos.x, 30.501f, pos.y);
				text = "Rail_Bridge";
			}
			else
			{
				vector2 = new Vector3(pos.x, blockData.h + 0.501f, pos.y);
			}
		}
		else
		{
			vector2 = new Vector3(pos.x, blockData.h - 1f, pos.y);
		}
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Roads/Rail/" + style + text));
		gameObject.transform.position = vector2;
		gameObject.name = pos.x + "_" + 50 + "_" + pos.y;
		gameObject.transform.Rotate(vector.x, vector.y, vector.z, Space.Self);
		gameObject.isStatic = false;
		gameObject.tag = "Road";
		RoadCreator roadCreator3 = gameObject.AddComponent<RoadCreator>();
		roadCreator3.Capacity = 4000;
		roadCreator3.wPos = vector2;
		roadCreator3.aPos = new Vector3(pos.x, 16f, pos.y);
		roadCreator3.Orientation = vector.y;
		roadCreator3.Bitwise = num2;
		roadCreator3.subType = "Rail/";
		roadCreator3.style = style;
		roadCreator3.tile = text;
		roadCreator3.path = "Roads/Rail/" + style + text;
		roadCreator3.cc = 0;
		roadCreator3.maintenance = num;
		Chunk o = blockData.o;
		if (flag)
		{
			gameObject.transform.parent = railFolder.transform;
			gameObject.layer = 2;
			blockData.t = BlockData.Type.ROAD;
			blockData.b = true;
			roading.rG[pos.x, 1, pos.y] = roadCreator3;
			world.Rails.Add(roadCreator3);
			roadCreator3.rType = RoadCreator.RoadType.RAIL;
			o.lroads.Add(roadCreator3);
		}
		else
		{
			gameObject.transform.parent = undergroundFolder.transform;
			roading.rG[pos.x, 0, pos.y] = roadCreator3;
			gameObject.layer = 14;
			roadCreator3.rType = RoadCreator.RoadType.UNDERGROUND;
			world.Underground.Add(roadCreator3);
		}
	}

	private void CheckChunks(BlockData b)
	{
		updates.Add(b.o);
		if (b.pos[0] == 0)
		{
			BlockData ifNotNull = world.GetIfNotNull(b.wP.x - 1, b.wP.y);
			if (ifNotNull == null)
			{
				return;
			}
			updates.Add(ifNotNull.o);
		}
		else if (b.pos[0] == 15)
		{
			BlockData ifNotNull2 = world.GetIfNotNull(b.wP.x + 1, b.wP.y);
			if (ifNotNull2 == null)
			{
				return;
			}
			updates.Add(ifNotNull2.o);
		}
		if (b.pos[1] == 0)
		{
			BlockData ifNotNull3 = world.GetIfNotNull(b.wP.x, b.wP.y - 1);
			if (ifNotNull3 != null)
			{
				updates.Add(ifNotNull3.o);
			}
		}
		else if (b.pos[1] == 15)
		{
			BlockData ifNotNull4 = world.GetIfNotNull(b.wP.x, b.wP.y + 1);
			if (ifNotNull4 != null)
			{
				updates.Add(ifNotNull4.o);
			}
		}
	}

	public IEnumerator UpdateChunks()
	{
		foreach (Chunk c in updates)
		{
			c.Redraw();
			yield return new WaitForEndOfFrame();
			c.UpdateRoads();
			yield return new WaitForEndOfFrame();
		}
		updates.Clear();
	}
}
