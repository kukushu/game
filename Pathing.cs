using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Pathing : MonoBehaviour
{
	public CostHelper costHelper;

	public GameObject whiteSquare;

	public GameObject tRoad;

	public GameObject wSquare_1;

	public World world;

	public BlockInteraction bi;

	public Roading roading;

	public SoundManager sm;

	public Material wMat;

	private List<Vector2Int> lateBitwise = new List<Vector2Int>();

	private int worldSize = 20;

	private int autoLimit;

	private bool suitable = true;

	public static int Type;

	public static int Case;

	private readonly HashSet<Chunk> updates = new HashSet<Chunk>();

	private void Start()
	{
		worldSize = World.worldSize * World.chunkSize;
		if (costHelper == null)
		{
			costHelper = roading.costHelper;
		}
	}

	private void Update()
	{
		int index = Indexer.Index;
		if (Input.GetMouseButtonDown(0) && index == 50 && bi.tamere == 0)
		{
			if (EventSystem.current.IsPointerOverGameObject())
			{
				return;
			}
			if (wMat.color == Color.red)
			{
				sm.Play("Impractical");
			}
			bi.x1 = (int)bi.pos.x;
			bi.z1 = (int)bi.pos.z;
			bi.tamere = 1;
		}
		if (Input.GetMouseButton(0) && index == 50 && bi.tamere == 1)
		{
			RoadUpdate();
			bi.oldx1 = bi.x1;
			bi.oldz1 = bi.z1;
			bi.oldx2 = bi.x2;
			bi.oldz2 = bi.z2;
		}
		if (index == 50)
		{
			Vector3 vector = new Vector3(0f, 0.515f, 0f);
			TooltipSystem.Hide();
			wMat.color = roading.lime;
			if (bi.pos.z > -1f && bi.pos.x > -1f && bi.pos.z < (float)(worldSize * 16) && bi.pos.x < (float)(worldSize * 16))
			{
				wSquare_1.transform.position = bi.pos + vector;
			}
			BlockData ifNotNull = world.GetIfNotNull((int)bi.pos.x, (int)bi.pos.z);
			if (ifNotNull == null)
			{
				return;
			}
			if (ifNotNull.b && !ifNotNull.p)
			{
				TooltipSystem.Show("", "Area Occupied");
				wMat.color = Color.red;
			}
			else if (ifNotNull.p)
			{
				wMat.color = roading.azul;
			}
			else
			{
				wMat.color = roading.lime;
			}
		}
		if (Input.GetMouseButtonUp(0) && bi.tamere == 1 && index == 50)
		{
			if (wMat.color == Color.red || !suitable)
			{
				bi.x1 = 0;
				bi.z1 = 0;
				bi.x2 = 0;
				bi.z2 = 0;
				bi.tamere = 0;
				sm.Play("Impractical");
				StopVisualizeRoad();
			}
			else
			{
				BuildPathLine(bi.x1, bi.z1, bi.x2, bi.z2);
			}
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
		if (x2 == oldx && z2 == oldz)
		{
			return;
		}
		suitable = true;
		StopVisualizeRoad();
		if (x < x2 && num > num2)
		{
			costHelper.SetCost(num * 10);
			Case = 1;
			for (int i = x; i < x2 + 1; i++)
			{
				VisualizeRoad(new Vector3(i, y, z));
			}
		}
		else if (x2 < x && num > num2)
		{
			costHelper.SetCost(num * 10);
			Case = 2;
			for (int num3 = x; num3 >= x2; num3--)
			{
				VisualizeRoad(new Vector3(num3, y, z));
			}
		}
		else if (z < z2 && num2 > num)
		{
			costHelper.SetCost(num2 * 10);
			Case = 3;
			for (int j = z; j <= z2; j++)
			{
				VisualizeRoad(new Vector3(x, y, j));
			}
		}
		else if (z2 < z && num2 > num * 2)
		{
			costHelper.SetCost(num2 * 10);
			Case = 4;
			for (int num4 = z; num4 > z2 - 1; num4--)
			{
				VisualizeRoad(new Vector3(x, y, num4));
			}
		}
	}

	private void FlattenIntersec(Vector2Int pos, int rot, int bit)
	{
		if (autoLimit > 20)
		{
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
		autoLimit++;
		if (bd == null || !bd.p || autoLimit > 20)
		{
			return;
		}
		lateBitwise.Add(bd.wP);
		switch (rot)
		{
		case 0:
		{
			PathRight(world.blockData[bd.wP.x, bd.wP.y].wP);
			BlockData ifNotNull3 = world.GetIfNotNull(bd.wP.x + 1, bd.wP.y);
			if (ifNotNull3 != null && ifNotNull3.p)
			{
				lateBitwise.Add(ifNotNull3.wP);
			}
			BlockData ifNotNull4 = world.GetIfNotNull(bd.wP.x + 2, bd.wP.y);
			if (ifNotNull4 != null && ifNotNull4.p)
			{
				lateBitwise.Add(ifNotNull4.wP);
			}
			break;
		}
		case 180:
		{
			PathLeft(world.blockData[bd.wP.x, bd.wP.y].wP);
			BlockData ifNotNull7 = world.GetIfNotNull(bd.wP.x - 1, bd.wP.y);
			if (ifNotNull7 != null && ifNotNull7.p)
			{
				lateBitwise.Add(ifNotNull7.wP);
			}
			BlockData ifNotNull8 = world.GetIfNotNull(bd.wP.x - 2, bd.wP.y);
			if (ifNotNull8 != null && ifNotNull8.p)
			{
				lateBitwise.Add(ifNotNull8.wP);
			}
			break;
		}
		case 270:
		{
			PathUp(world.blockData[bd.wP.x, bd.wP.y].wP);
			BlockData ifNotNull5 = world.GetIfNotNull(bd.wP.x, bd.wP.y + 1);
			if (ifNotNull5 != null && ifNotNull5.p)
			{
				lateBitwise.Add(ifNotNull5.wP);
			}
			BlockData ifNotNull6 = world.GetIfNotNull(bd.wP.x, bd.wP.y + 2);
			if (ifNotNull6 != null && ifNotNull6.p)
			{
				lateBitwise.Add(ifNotNull6.wP);
			}
			break;
		}
		case 90:
		{
			BlockData ifNotNull = world.GetIfNotNull(bd.wP.x, bd.wP.y - 1);
			if (ifNotNull != null && ifNotNull.p)
			{
				lateBitwise.Add(ifNotNull.wP);
			}
			BlockData ifNotNull2 = world.GetIfNotNull(bd.wP.x, bd.wP.y - 2);
			if (ifNotNull2 != null && ifNotNull2.p)
			{
				lateBitwise.Add(ifNotNull2.wP);
			}
			break;
		}
		}
	}

	public void DestroyRoad(int x, int z)
	{
		if (x < 0 || z < 0 || x >= worldSize || z >= worldSize)
		{
			return;
		}
		RoadCreator roadCreator = roading.rD[x, 1, z];
		if (roadCreator != null && roadCreator.rType == RoadCreator.RoadType.PATH)
		{
			world.Roads.Remove(roadCreator);
			BlockData blockData = bi.world.blockData[x, z];
			blockData.o.lroads.Remove(roadCreator);
			blockData.t = BlockData.Type.GRASS;
			if (roadCreator.gameObject != null)
			{
				Object.Destroy(roadCreator.gameObject);
			}
			else
			{
				Debug.Log("Path GO is null!");
			}
		}
	}

	private void VisualizeRoad(Vector3 pos)
	{
		BlockData blockData = bi.world.blockData[(int)pos.x, (int)pos.z];
		Vector3 position = new Vector3(blockData.wP.x, blockData.h + 0.515f, blockData.wP.y);
		if (blockData.b && !blockData.p)
		{
			wMat.color = Color.red;
			Object.Instantiate(roading.redSquare, position, Quaternion.identity);
			roading.redSquare.name = "NewCube";
			roading.redSquare.tag = "NewCube";
			sm.Play("Impractical");
			suitable = false;
		}
		else
		{
			Object.Instantiate(whiteSquare, position, Quaternion.identity);
			whiteSquare.name = "NewCube";
			whiteSquare.tag = "NewCube";
			sm.Play("RoadPiece");
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

	private void BuildPathLine(int _x1, int _z1, int _x2, int _z2)
	{
		int num = Mathf.Abs(_x1 - _x2) + 1;
		int num2 = Mathf.Abs(_z1 - _z2) + 1;
		if (_x1 < _x2 && num > num2)
		{
			for (int i = _x1; i < _x2 + 1; i++)
			{
				world.blockData[i, _z1].p = true;
				world.blockData[i, _z1].ClearTerrain();
			}
		}
		else if (_x2 < _x1 && num > num2)
		{
			for (int num3 = _x1; num3 >= _x2; num3--)
			{
				world.blockData[num3, _z1].p = true;
				world.blockData[num3, _z1].ClearTerrain();
			}
		}
		if (_z1 < _z2 && num2 > num)
		{
			for (int j = _z1; j <= _z2; j++)
			{
				world.blockData[_x1, j].p = true;
				world.blockData[_x1, j].ClearTerrain();
			}
		}
		else if (_z2 < _z1 && num2 > num)
		{
			for (int num4 = _z1; num4 > _z2 - 1; num4--)
			{
				world.blockData[_x1, num4].p = true;
				world.blockData[_x1, num4].ClearTerrain();
			}
		}
		if (_x1 < _x2 && num > num2)
		{
			for (int k = _x1; k < _x2 + 1; k++)
			{
				PathRight(new Vector2Int(k, _z1));
			}
		}
		else if (_x2 < _x1 && num > num2)
		{
			for (int num5 = _x1; num5 >= _x2; num5--)
			{
				PathLeft(new Vector2Int(num5, _z1));
			}
		}
		if (_z1 < _z2 && num2 > num)
		{
			for (int l = _z1; l <= _z2; l++)
			{
				PathUp(new Vector2Int(_x1, l));
			}
		}
		else if (_z2 < _z1 && num2 > num)
		{
			for (int num6 = _z1; num6 > _z2 - 1; num6--)
			{
				PathDown(new Vector2Int(_x1, num6));
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
		if (ifNotNull.p)
		{
			num++;
		}
		if (ifNotNull2.p)
		{
			num += 2;
		}
		if (ifNotNull3.p)
		{
			num += 4;
		}
		if (ifNotNull4.p)
		{
			num += 8;
		}
		if (blockData.h < 29.75f)
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
		if (ifNotNull.p)
		{
			num++;
		}
		if (ifNotNull2.p)
		{
			num += 2;
		}
		if (ifNotNull3.p)
		{
			num += 4;
		}
		if (ifNotNull4.p)
		{
			num += 8;
		}
		if (blockData.h < 29.75f)
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
		if (ifNotNull.p)
		{
			num++;
		}
		if (ifNotNull2.p)
		{
			num += 2;
		}
		if (ifNotNull3.p)
		{
			num += 4;
		}
		if (ifNotNull4.p)
		{
			num += 8;
		}
		if (blockData.h < 29.75f)
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
		if (ifNotNull.p)
		{
			num++;
		}
		if (ifNotNull2.p)
		{
			num += 2;
		}
		if (ifNotNull3.p)
		{
			num += 4;
		}
		if (ifNotNull4.p)
		{
			num += 8;
		}
		if (blockData.h < 29.75f)
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
		}
	}

	public void RoadBitwise(Vector2Int pos)
	{
		BlockData ifNotNull = world.GetIfNotNull(pos.x, pos.y);
		RoadCreator roadCreator = null;
		if (ifNotNull == null)
		{
			ifNotNull.p = false;
			return;
		}
		RoadCreator roadCreator2 = roading.rD[pos.x, 1, pos.y];
		if (roadCreator2 != null && roadCreator2.rType == RoadCreator.RoadType.PATH)
		{
			roadCreator = roadCreator2;
		}
		if (ifNotNull.p && roadCreator != null && roadCreator.rType == RoadCreator.RoadType.PATH)
		{
			if (roadCreator.rType != RoadCreator.RoadType.PATH)
			{
				Debug.Log("Existing road creator. Weird.");
				ifNotNull.p = false;
				return;
			}
			DestroyRoad(pos.x, pos.y);
		}
		if (ifNotNull.r || (ifNotNull.b && !ifNotNull.p))
		{
			ifNotNull.p = false;
			return;
		}
		CheckChunks(ifNotNull);
		BlockData ifNotNull2 = world.GetIfNotNull(pos.x, pos.y - 1);
		BlockData ifNotNull3 = world.GetIfNotNull(pos.x + 1, pos.y);
		BlockData ifNotNull4 = world.GetIfNotNull(pos.x, pos.y + 1);
		BlockData ifNotNull5 = world.GetIfNotNull(pos.x - 1, pos.y);
		int num = 0;
		Vector3 vector = new Vector3(0f, 0f, 0f);
		string text = "Path_Intersec";
		if (ifNotNull2 != null && ifNotNull2.p)
		{
			num++;
		}
		if (ifNotNull3 != null && ifNotNull3.p)
		{
			num += 2;
		}
		if (ifNotNull4 != null && ifNotNull4.p)
		{
			num += 4;
		}
		if (ifNotNull5 != null && ifNotNull5.p)
		{
			num += 8;
		}
		switch (num)
		{
		case 0:
			text = "Path_Intersec";
			vector = new Vector3(0f, 0f, 0f);
			break;
		case 1:
			text = "Path_End";
			vector = new Vector3(0f, 0f, 0f);
			break;
		case 2:
			text = "Path_End";
			vector = new Vector3(0f, 270f, 0f);
			break;
		case 3:
			text = "Path_Curve";
			vector = new Vector3(0f, 270f, 0f);
			break;
		case 4:
			text = "Path_End";
			vector = new Vector3(0f, 180f, 0f);
			break;
		case 5:
			text = "Path_Straight";
			vector = new Vector3(0f, 90f, 0f);
			if ((double)ifNotNull2.h == (double)ifNotNull.h + 0.25)
			{
				text = "Path_25";
			}
			else if ((double)ifNotNull4.h == (double)ifNotNull.h + 0.25)
			{
				text = "Path_25";
				vector = new Vector3(0f, 270f, 0f);
			}
			else if ((double)ifNotNull4.h == (double)ifNotNull.h + 0.5)
			{
				text = "Path_50";
				vector = new Vector3(0f, 270f, 0f);
			}
			else if ((double)ifNotNull2.h == (double)ifNotNull.h + 0.5)
			{
				text = "Path_50";
			}
			break;
		case 6:
			text = "Path_Curve";
			vector = new Vector3(0f, 180f, 0f);
			break;
		case 7:
			text = "Path_Semi";
			vector = new Vector3(0f, 270f, 0f);
			break;
		case 8:
			text = "Path_End";
			vector = new Vector3(0f, 90f, 0f);
			break;
		case 9:
			text = "Path_Curve";
			vector = new Vector3(0f, 0f, 0f);
			break;
		case 10:
			text = "Path_Straight";
			vector = new Vector3(0f, 0f, 0f);
			if ((double)ifNotNull5.h == (double)ifNotNull.h + 0.25)
			{
				text = "Path_25";
				vector = new Vector3(0f, 180f, 0f);
			}
			else if ((double)ifNotNull3.h == (double)ifNotNull.h + 0.25)
			{
				text = "Path_25";
			}
			else if ((double)ifNotNull3.h == (double)ifNotNull.h + 0.5)
			{
				text = "Path_50";
			}
			else if ((double)ifNotNull5.h == (double)ifNotNull.h + 0.5)
			{
				text = "Path_50";
				vector = new Vector3(0f, 180f, 0f);
			}
			break;
		case 11:
			text = "Path_Semi";
			vector = new Vector3(0f, 0f, 0f);
			break;
		case 12:
			text = "Path_Curve";
			vector = new Vector3(0f, 90f, 0f);
			break;
		case 13:
			text = "Path_Semi";
			vector = new Vector3(0f, 90f, 0f);
			break;
		case 14:
			text = "Path_Semi";
			vector = new Vector3(0f, 180f, 0f);
			break;
		case 15:
			text = "Path_Intersec";
			vector = new Vector3(0f, 0f, 0f);
			break;
		}
		Vector3 vector2;
		if (ifNotNull.h < 30f)
		{
			vector2 = new Vector3(pos.x, 30.502f, pos.y);
			if (text == "Path_End")
			{
				vector.y += 90f;
			}
			text = "Path_Bridge";
		}
		else
		{
			vector2 = new Vector3(pos.x, ifNotNull.h + 0.502f, pos.y);
		}
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Roads/Path/Ground/" + text));
		gameObject.transform.position = vector2;
		gameObject.name = pos.x + "_" + 50 + "_" + pos.y;
		gameObject.transform.Rotate(vector.x, vector.y, vector.z, Space.Self);
		gameObject.isStatic = false;
		gameObject.transform.parent = tRoad.transform;
		gameObject.tag = "Road";
		gameObject.AddComponent<RoadCreator>();
		RoadCreator roadCreator3 = (RoadCreator)gameObject.GetComponent(typeof(RoadCreator));
		roadCreator3.Capacity = 50;
		roadCreator3.wPos = vector2;
		roadCreator3.aPos = new Vector3(pos.x, 16f, pos.y);
		roadCreator3.Orientation = vector.y;
		roadCreator3.Bitwise = num;
		roadCreator3.subType = "Path/";
		roadCreator3.style = "Ground/";
		roadCreator3.tile = text;
		roadCreator3.path = "Roads/Path/Ground/" + text;
		roadCreator3.rType = RoadCreator.RoadType.PATH;
		roadCreator3.cc = 14;
		roading.rD[pos.x, 1, pos.y] = roadCreator3;
		world.Roads.Add(roadCreator3);
		ifNotNull.b = true;
		ifNotNull.t = BlockData.Type.ROAD;
		ifNotNull.o.lroads.Add(roadCreator3);
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

	private IEnumerator UpdateChunks()
	{
		yield return new WaitForEndOfFrame();
		foreach (Chunk update in updates)
		{
			update.Redraw();
			yield return new WaitForEndOfFrame();
		}
		updates.Clear();
	}
}
