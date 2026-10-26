using UnityEngine;

public class VegetationCreator : MonoBehaviour
{
	private static readonly string[] Grass_Types = new string[18]
	{
		"Trees/Grass_X0", "Trees/Grass_X1", "Trees/Grass_X2", "Trees/Grass_X3", "Trees/Grass_X4", "Trees/Grass_X4b", "Trees/Grass_X5", "Trees/Grass_X6", "Trees/Grass_X7", "Trees/Grass_X8",
		"Trees/Grass_X9", "Trees/Grass_X10", "Trees/Grass_X11", "Trees/Grass_X12", "Trees/Grass_X13", "Trees/Grass_X14", "Trees/Grass_X15", "Trees/Grass_X16"
	};

	private static readonly string[] Rock_Types = new string[4] { "Trees/RockBigA", "Trees/RockSmallA", "Trees/RockSmallC", "Trees/RockSmallD" };

	private static readonly int[] Rotations = new int[4] { 0, 90, 180, 270 };

	private static int GetClimate(BlockData bd, int rX)
	{
		float num = Mathf.Clamp((bd.h - 30f) * 3.33f, 0f, 100f);
		float num2 = Mathf.Clamp((float)(rX * WorldRegion.chunkSize * 8 + bd.wP.y) / 25.6f, 0f, 100f);
		if (num + num2 * 1.25f > 100f)
		{
			return 1;
		}
		if (num + 20f > num2)
		{
			return 2;
		}
		return 0;
	}

	public static void CreateTree(Vector3 pos, GameObject parent, BlockData bd, bool isTrunk, int rZ)
	{
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(GetClimate(bd, rZ) switch
		{
			0 => (!isTrunk) ? Dico.Leaves_Types[Dico.TreeType((float)(int)bd.bio / 100f, (int)bd.s1, bd.h)] : Dico.Trunk_Types[Dico.TreeType((float)(int)bd.bio / 100f, (int)bd.s1, bd.h)], 
			1 => (!isTrunk) ? Dico.Leaves_Temp[Dico.TreeType((float)(int)bd.bio / 100f, (int)bd.s1, bd.h)] : Dico.Trunk_Temp[Dico.TreeType((float)(int)bd.bio / 100f, (int)bd.s1, bd.h)], 
			_ => (!isTrunk) ? Dico.Leaves_Arid[Dico.TreeType((float)(int)bd.bio / 100f, (int)bd.s1, bd.h)] : Dico.Trunk_Arid[Dico.TreeType((float)(int)bd.bio / 100f, (int)bd.s1, bd.h)], 
		}));
		gameObject.transform.localScale = new Vector3(0.2f + (float)(int)bd.s1 * 0.0008f, 0.15f + (float)(int)bd.s1 * 0.0015f, 0.2f + (float)(int)bd.s1 * 0.0008f);
		gameObject.transform.rotation = Quaternion.Euler(0f, (float)(int)bd.s2 * 3.6f, 0f);
		Vector2 vector = new Vector2((50f - (float)(int)bd.s2) / 120f, (50f - (float)(int)bd.s1) / 120f);
		gameObject.transform.position = new Vector3(pos.x + vector.x, pos.y + 0.49f, pos.z + vector.y);
		gameObject.transform.SetParent(parent.transform);
	}

	public static void CreateFarTree(Vector3 pos, GameObject parent, BlockData bd, bool isTrunk)
	{
		string path = ((!isTrunk) ? "Trees/FarLeaves Ab" : "Trees/FarTrunk Ab");
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(path));
		gameObject.transform.localScale = new Vector3(0.2f + (float)(int)bd.s1 * 0.0008f, 0.15f + (float)(int)bd.s1 * 0.0015f, 0.2f + (float)(int)bd.s1 * 0.0008f);
		gameObject.transform.rotation = Quaternion.Euler(0f, (float)(int)bd.s2 * 3.6f, 0f);
		Vector2 vector = new Vector2((50f - (float)(int)bd.s2) / 120f, (50f - (float)(int)bd.s1) / 120f);
		gameObject.transform.position = new Vector3(pos.x + vector.x, pos.y + 0.49f, pos.z + vector.y);
		gameObject.transform.SetParent(parent.transform);
	}

	public static void CreateClosePropTree(Vector3 pos, GameObject closeParent, bool isTrunk, ChunkMB cMB, int type, int rZ)
	{
		float num = 0.19f;
		float num2 = 0.19f;
		BlockData ifNotNull = cMB.owner.world.GetIfNotNull((int)pos.x, (int)pos.z);
		if (ifNotNull != null)
		{
			int num3;
			switch (type)
			{
			case 0:
			case 5:
				type = Dico.TreeType((float)(int)ifNotNull.bio / 100f, (int)ifNotNull.s1, ifNotNull.h);
				num3 = 3;
				break;
			case 2:
				type = Dico.TreeType((float)(int)ifNotNull.bio / 100f, (int)ifNotNull.s1, ifNotNull.h);
				num3 = 4;
				break;
			case 10:
				num = 0.14f;
				num2 = 0.15f;
				type = Dico.TreeType((float)(int)ifNotNull.bio / 100f, (int)ifNotNull.s1, ifNotNull.h);
				num3 = 5;
				break;
			default:
				type = Dico.TreeType((float)(int)ifNotNull.bio / 100f, (int)ifNotNull.s1, ifNotNull.h);
				num3 = GetClimate(ifNotNull, rZ);
				break;
			}
			GameObject gameObject = Object.Instantiate(num3 switch
			{
				0 => (!isTrunk) ? AssetLibrary.assetLib.tropical_leaves[type] : AssetLibrary.assetLib.tropical_trunk[type], 
				1 => (!isTrunk) ? AssetLibrary.assetLib.temperate_leaves[type] : AssetLibrary.assetLib.temperate_trunk[type], 
				2 => (!isTrunk) ? AssetLibrary.assetLib.arid_leaves[type] : AssetLibrary.assetLib.arid_trunk[type], 
				3 => (!isTrunk) ? AssetLibrary.assetLib.LC_leaves[type] : AssetLibrary.assetLib.LC_trunk[type], 
				4 => (!isTrunk) ? AssetLibrary.assetLib.UC_leaves[type] : AssetLibrary.assetLib.UC_trunk[type], 
				_ => (!isTrunk) ? AssetLibrary.assetLib.roads_leaves[type] : AssetLibrary.assetLib.roads_trunk[type], 
			});
			pos.x -= cMB.transform.position.x;
			pos.z -= cMB.transform.position.z;
			gameObject.transform.localScale = new Vector3(num2 + (float)(int)ifNotNull.s1 * 0.0004f, num + (float)(int)ifNotNull.s1 * 0.0008f, num2 + (float)(int)ifNotNull.s1 * 0.0004f);
			gameObject.transform.rotation = Quaternion.Euler(0f, (float)(int)ifNotNull.s2 * 3.6f, 0f);
			gameObject.transform.position = new Vector3(pos.x, pos.y, pos.z);
			gameObject.transform.SetParent(closeParent.transform);
		}
	}

	public static void CreateFarPropTree(Vector3 pos, GameObject farParent, bool isTrunk, ChunkMB cMB)
	{
		BlockData blockData = cMB.owner.world.blockData[(int)pos.x, (int)pos.y];
		GameObject original = ((!isTrunk) ? AssetLibrary.assetLib.farLeaves : AssetLibrary.assetLib.farTrunk);
		GameObject gameObject = Object.Instantiate(original);
		pos.x -= cMB.transform.position.x;
		pos.z -= cMB.transform.position.z;
		gameObject.transform.localScale = new Vector3(0.2f + (float)(int)blockData.s1 * 0.0008f, 0.2f + (float)(int)blockData.s1 * 0.0015f, 0.2f + (float)(int)blockData.s1 * 0.0008f);
		gameObject.transform.rotation = Quaternion.Euler(0f, (float)(int)blockData.s2 * 3.6f, 0f);
		Vector2 vector = new Vector2((50f - (float)(int)blockData.s2) / 120f, (50f - (float)(int)blockData.s1) / 120f);
		gameObject.transform.position = new Vector3(pos.x + vector.x, pos.y, pos.z + vector.y);
		gameObject.transform.SetParent(farParent.transform);
	}

	public static void CreateGrass(Vector3 pos, GameObject grassParent, BlockData bd)
	{
		int num;
		if (bd.gT != 0)
		{
			if (bd.gT >= 0)
			{
				num = bd.gT;
			}
			else if (bd.gT == -1)
			{
				num = 18;
			}
			else if (bd.gT == -2)
			{
				num = 19;
			}
			else
			{
				num = ((bd.gT != -3) ? 21 : 20);
			}
		}
		else
		{
			if (bd.h < 30.25f && bd.h >= 29.25f)
			{
				num = Random.Range(8, 13);
			}
			else if (bd.bio > 69)
			{
				num = Random.Range(0, 4);
			}
			else if (bd.bio <= 69 && bd.bio > 64)
			{
				num = Random.Range(4, 6);
			}
			else
			{
				if (bd.bio > 64 || bd.bio <= 58)
				{
					return;
				}
				num = Random.Range(6, 8);
			}
			bd.gT = (sbyte)num;
		}
		GameObject gameObject = Object.Instantiate(AssetLibrary.assetLib.grass[num]);
		gameObject.transform.rotation = Quaternion.Euler(0f, Rotations[Mathf.FloorToInt((float)(int)bd.s2 / 25.1f)], 0f);
		gameObject.transform.position = new Vector3(pos.x, pos.y + 0.5f, pos.z);
		gameObject.transform.SetParent(grassParent.transform);
	}

	public static void CreateGrassProp(Vector3 pos, GameObject grassParent, LotCreator.LotClass lotClass, BlockData bd, ChunkMB cMB)
	{
		int num;
		switch (lotClass)
		{
		case LotCreator.LotClass.UPPER:
		case LotCreator.LotClass.OFFICE:
		case LotCreator.LotClass.HIGHTECH:
			num = Mathf.RoundToInt(7f + (float)(int)bd.s1 * 0.05f);
			break;
		case LotCreator.LotClass.LOW:
		case LotCreator.LotClass.MANUFACTURE:
			num = Mathf.RoundToInt(3f + (float)(int)bd.s1 * 0.04f);
			break;
		default:
			num = Mathf.RoundToInt(12f + (float)(int)bd.s1 * 0.05f);
			break;
		}
		pos.x -= cMB.transform.position.x;
		pos.z -= cMB.transform.position.z;
		GameObject gameObject = Object.Instantiate(AssetLibrary.assetLib.grass[num]);
		gameObject.transform.rotation = Quaternion.Euler(0f, Rotations[Mathf.FloorToInt((float)(int)bd.s2 / 25.1f)], 0f);
		gameObject.transform.position = new Vector3(pos.x, pos.y + 0.5f, pos.z);
		gameObject.transform.SetParent(grassParent.transform);
	}

	public static void CreateProp(Vector3 pos, GameObject propChunk, BlockData bd)
	{
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(bd.pT));
		gameObject.transform.rotation = Quaternion.Euler(0f, bd.pR, 0f);
		gameObject.transform.position = new Vector3(pos.x, pos.y, pos.z);
		gameObject.transform.SetParent(propChunk.transform);
	}

	public static void CreateVehicleProp(Vector3 pos, GameObject vehicleParent, LotCreator lc, BlockData bd)
	{
		if (bd.s1 > 70)
		{
			return;
		}
		string path;
		if (lc.lClass != LotCreator.LotClass.LOW)
		{
			if (lc.lClass == LotCreator.LotClass.HIGHTECH)
			{
				path = Dico.P_Cars_HT[Mathf.FloorToInt((float)(int)bd.s2 / 11.12f)];
			}
			else if (lc.lClass == LotCreator.LotClass.UPPER || lc.lClass == LotCreator.LotClass.MIXOFFICE)
			{
				path = Dico.P_Cars_UC[Mathf.FloorToInt((float)(int)bd.s2 / 11.12f)];
			}
			else
			{
				path = ((lc.lClass != LotCreator.LotClass.MANUFACTURE) ? Dico.P_Cars_MC[Mathf.FloorToInt((float)(int)bd.s2 / 11.12f)] : Dico.P_Cars_M[Mathf.FloorToInt((float)(int)bd.s2 / 11.12f)]);
			}
		}
		else
		{
			path = Dico.P_Cars_LC[Mathf.FloorToInt((float)(int)bd.s2 / 11.12f)];
		}
		int num;
		if (lc.Occupants > 2)
		{
			num = 90;
			if (bd.s1 < 50)
			{
				num = 270;
			}
		}
		else
		{
			num = 180;
			if (bd.s1 < 50)
			{
				num = 0;
			}
		}
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(path));
		gameObject.transform.SetPositionAndRotation(new Vector3(pos.x, pos.y, pos.z), Quaternion.Euler(0f, lc.Orientation + num, 0f));
		gameObject.transform.SetParent(vehicleParent.transform);
	}

	public static void CreateVehicleService(Vector3 pos, GameObject vehicleParent, ServiceCreator sc, BlockData bd)
	{
		string path = Dico.P_Cars_LC[Mathf.FloorToInt((float)(int)bd.s2 / 11.12f)];
		float y = sc.worldPos.y;
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(path));
		gameObject.transform.SetPositionAndRotation(new Vector3(pos.x, y, pos.z), Quaternion.Euler(0f, (float)sc.Orientation + pos.y, 0f));
		gameObject.transform.SetParent(vehicleParent.transform);
	}

	public static void CreatePark(Vector3 pos, GameObject propChunk, BlockData bd)
	{
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(bd.pT));
		gameObject.transform.rotation = Quaternion.Euler(0f, bd.pR, 0f);
		gameObject.transform.position = new Vector3(pos.x, pos.y, pos.z);
		gameObject.transform.SetParent(propChunk.transform);
	}

	public static void CreateRock(Vector3 pos, GameObject parent, BlockData bd)
	{
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(Rock_Types[Mathf.FloorToInt((float)(int)bd.s2 * 0.038f)]));
		gameObject.transform.localScale = new Vector3(0.15f + (float)(int)bd.s2 * 0.015f, 0.15f + (float)(int)bd.s2 * 0.015f, 0.15f + (float)(int)bd.s2 * 0.015f);
		gameObject.transform.rotation = Quaternion.Euler(0f, (float)(int)bd.s2 * 3.6f, 0f);
		gameObject.transform.position = new Vector3(pos.x, pos.y + 0.49f, pos.z);
		gameObject.transform.SetParent(parent.transform);
	}

	public static void CreateCliff(GameObject parent, BlockData bd, int rotation, bool colorize = false)
	{
		GameObject gameObject;
		if (!colorize)
		{
			gameObject = ((!bd.r) ? Object.Instantiate(World.staticWorld.stoneCliffs[Mathf.FloorToInt((float)(int)bd.s2 * 0.079f)]) : Object.Instantiate(World.staticWorld.stoneCliffBuilt));
		}
		else
		{
			gameObject = Object.Instantiate(World.staticWorld.grassCliff);
			Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
			bd.o.meshTerrain.Add(mesh);
			mesh.colors32 = TerrainColor.GrassColor(bd.h + (float)bd.v[1] / 80f, mesh, 0, (float)(int)bd.bio / 100f);
		}
		gameObject.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
		gameObject.transform.position = new Vector3((int)bd.pos[0], bd.h + 0.5f, (int)bd.pos[1]);
		gameObject.transform.SetParent(parent.transform);
	}
}
