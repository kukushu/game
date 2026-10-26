using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AccessRoading : MonoBehaviour
{
	public CostHelper costHelper;

	public GameObject whiteSquare;

	public GameObject tRoad;

	public GameObject wSquare_1;

	public BlockInteraction bi;

	public World world;

	public Indexer indexer;

	public SoundManager sm;

	public Material wMat;

	public Material placerMaterial;

	public Roading roading;

	public Avenueing avenueing;

	public static int Type;

	private readonly HashSet<Chunk> updates = new HashSet<Chunk>();

	private List<Vector3Int> lateBitwise = new List<Vector3Int>();

	private int autoLimit;

	private int Orientation;

	private bool avenueType;

	private bool busy;

	private string cas = "";

	private void Update()
	{
		int index = Indexer.Index;
		if (index == 120)
		{
			RoadCreator ifNotNull = roading.GetIfNotNull((int)bi.pos.x, 1, (int)bi.pos.z);
			RoadCreator ifNotNull2 = roading.GetIfNotNull((int)bi.pos.x, 2, (int)bi.pos.z);
			Vector3 vector = new Vector3(0f, 1.015f, 0f);
			BlockData ifNotNull3 = world.GetIfNotNull((int)bi.pos.x, (int)bi.pos.z);
			if (ifNotNull3 == null)
			{
				return;
			}
			cas = "";
			wMat.color = Color.red;
			if (ifNotNull2 == null || ifNotNull == null)
			{
				TooltipSystem.Show("Ramps Must");
			}
			else if ((ifNotNull2.cc == 17 && ifNotNull.cc == 8 && ifNotNull.rType != RoadCreator.RoadType.AVENUE) || (ifNotNull2.cc == 17 && ifNotNull.cc == 2 && ifNotNull.rType != RoadCreator.RoadType.AVENUE) || (ifNotNull2.cc == 17 && ifNotNull.cc == 18 && ifNotNull.rType == RoadCreator.RoadType.AVENUE))
			{
				Orientation = 0;
				BlockData blockData = world.Get(ifNotNull3.wP.x, ifNotNull3.wP.y + 6);
				BlockData blockData2 = world.Get(ifNotNull3.wP.x, ifNotNull3.wP.y - 6);
				BlockData blockData3 = world.Get(ifNotNull3.wP.x + 3, ifNotNull3.wP.y);
				BlockData blockData4 = world.Get(ifNotNull3.wP.x - 3, ifNotNull3.wP.y);
				if (ifNotNull == null || ifNotNull.cc == 8 || ifNotNull.cc == 18 || ifNotNull.cc == 2)
				{
					if (Mathf.Abs(blockData.h - blockData2.h) < 1f && Mathf.Abs(blockData3.h - blockData4.h) < 1f)
					{
						cas = "Z";
						wMat.color = roading.azul;
						TooltipSystem.Hide();
					}
					else
					{
						cas = "";
						TooltipSystem.Show("", "Too Steep");
					}
				}
				else
				{
					cas = "";
					TooltipSystem.Show("", "Area Occupied");
				}
			}
			else if ((ifNotNull2.cc == 18 && ifNotNull.cc == 7 && ifNotNull.rType != RoadCreator.RoadType.AVENUE) || (ifNotNull2.cc == 18 && ifNotNull.cc == 4 && ifNotNull.rType != RoadCreator.RoadType.AVENUE) || (ifNotNull2.cc == 18 && ifNotNull.cc == 17 && ifNotNull.rType == RoadCreator.RoadType.AVENUE))
			{
				Orientation = 270;
				BlockData blockData5 = world.Get(ifNotNull3.wP.x, ifNotNull3.wP.y + 3);
				BlockData blockData6 = world.Get(ifNotNull3.wP.x, ifNotNull3.wP.y - 3);
				BlockData blockData7 = world.Get(ifNotNull3.wP.x + 6, ifNotNull3.wP.y);
				BlockData blockData8 = world.Get(ifNotNull3.wP.x - 6, ifNotNull3.wP.y);
				if (ifNotNull == null || ifNotNull.cc == 4 || ifNotNull.cc == 17 || ifNotNull.cc == 7)
				{
					if (Mathf.Abs(blockData5.h - blockData6.h) < 1f && Mathf.Abs(blockData7.h - blockData8.h) < 1f)
					{
						cas = "X";
						wMat.color = roading.azul;
						TooltipSystem.Hide();
					}
					else
					{
						cas = "";
						TooltipSystem.Show("", "Too Steep");
					}
				}
				else
				{
					cas = "";
					TooltipSystem.Show("", "Area Occupied");
				}
			}
			if (cas != "")
			{
				if (ifNotNull.rType == RoadCreator.RoadType.ROAD)
				{
					indexer.ramps.transform.position = bi.pos + vector;
					indexer.ramps.transform.eulerAngles = new Vector3(0f, Orientation, 0f);
					placerMaterial.SetColor("_Color", Color.green);
					indexer.aveRamps.transform.position = new Vector3(1000f, -1000f, 1000f);
					avenueType = false;
				}
				else if (ifNotNull.rType == RoadCreator.RoadType.AVENUE)
				{
					indexer.aveRamps.transform.position = bi.pos + vector;
					indexer.aveRamps.transform.eulerAngles = new Vector3(0f, Orientation, 0f);
					placerMaterial.SetColor("_Color", Color.green);
					indexer.ramps.transform.position = new Vector3(1000f, -1000f, 1000f);
					avenueType = true;
				}
			}
			else
			{
				placerMaterial.SetColor("_Color", Color.red);
				indexer.ramps.transform.position = bi.pos + vector;
				indexer.aveRamps.transform.position = new Vector3(1000f, -1000f, 1000f);
				avenueType = false;
			}
			costHelper.SetCost(500);
			wSquare_1.transform.position = bi.pos + vector;
		}
		if (Input.GetMouseButton(0) && index == 120 && cas != "")
		{
			if (busy)
			{
				return;
			}
			busy = true;
			if (avenueType)
			{
				StartCoroutine(BuildAveBretelle(bi.pos));
			}
			else
			{
				StartCoroutine(BuildRoBretelle(bi.pos));
			}
			StartCoroutine(UpdateChunks());
		}
		if (Input.GetMouseButtonDown(0) && index == 12 && bi.tamere == 0)
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
		if (Input.GetMouseButton(0) && index == 12 && bi.tamere == 1)
		{
			RoadUpdate();
			bi.oldx1 = bi.x1;
			bi.oldz1 = bi.z1;
			bi.oldx2 = bi.x2;
			bi.oldz2 = bi.z2;
		}
		if (index == 12)
		{
			Vector3 vector2 = new Vector3(0f, 0.515f, 0f);
			TooltipSystem.Hide();
			BlockData ifNotNull4 = world.GetIfNotNull((int)bi.pos.x, (int)bi.pos.z);
			if (ifNotNull4 == null)
			{
				return;
			}
			wMat.color = roading.lime;
			RoadCreator ifNotNull5 = roading.GetIfNotNull((int)bi.pos.x, 1, (int)bi.pos.z);
			if (ifNotNull5 != null)
			{
				if (!ifNotNull4.p && ifNotNull5.tile != "side" && ifNotNull5.tile != "straight" && ifNotNull5.tile != "straighttree" && ifNotNull5.tile != "sidetree")
				{
					TooltipSystem.Show("", "Unfeasible Road Layout");
					wMat.color = Color.red;
				}
				else if (ifNotNull5.tile == "side" || ifNotNull5.tile == "straight" || ifNotNull5.tile == "sidetree" || ifNotNull5.tile == "straighttree" || ifNotNull4.p)
				{
					wMat.color = roading.azul;
				}
			}
			wSquare_1.transform.position = bi.pos + vector2;
		}
		if (Input.GetMouseButtonUp(0) && bi.tamere == 1 && index == 12)
		{
			Vector3 pos = bi.pos;
			BuildAccessLine(pos, bi.x1, bi.z1, bi.x2, bi.z2);
		}
	}

	private IEnumerator BuildRoBretelle(Vector3 pos)
	{
		BlockData blockData = world.Get((int)pos.x, (int)pos.z);
		if (cas == "Z")
		{
			float height = blockData.h;
			for (int i = (int)pos.x - 3; i <= (int)pos.x + 3; i++)
			{
				for (int j = (int)pos.z - 6; j <= (int)pos.z + 5; j++)
				{
					BlockData blockData2 = world.Get(i, j);
					blockData2?.FlatHeight(height);
					roading.DestroyRoad(i, j);
					CheckChunks(blockData2);
				}
			}
			yield return new WaitForEndOfFrame();
			InstantiatePiece(new Vector3(pos.x - 2f, height + 1.25f, pos.z + 4f), 0, "BretelleOUT", 4);
			InstantiatePiece(new Vector3(pos.x - 2f, height + 1f, pos.z + 3f), 270, "Access_25", 4);
			InstantiatePiece(new Vector3(pos.x - 2f, height + 0.75f, pos.z + 2f), 270, "Access_25", 4);
			InstantiatePiece(new Vector3(pos.x - 2f, height + 0.5f, pos.z + 1f), 270, "Access_25", 16);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 1.25f, pos.z + 4f), 0, "BretelleIN", 2);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 1f, pos.z + 3f), 270, "Access_25", 3);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 0.75f, pos.z + 2f), 270, "Access_25", 3);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 0.5f, pos.z + 1f), 270, "Access_25", 3);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 1.25f, pos.z - 5f), 180, "BretelleOUT", 3);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 1f, pos.z - 4f), 90, "Access_25", 3);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 0.75f, pos.z - 3f), 90, "Access_25", 3);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 0.5f, pos.z - 2f), 90, "Access_25", 16);
			InstantiatePiece(new Vector3(pos.x - 2f, height + 1.25f, pos.z - 5f), 180, "BretelleIN", 1);
			InstantiatePiece(new Vector3(pos.x - 2f, height + 1f, pos.z - 4f), 90, "Access_25", 4);
			InstantiatePiece(new Vector3(pos.x - 2f, height + 0.75f, pos.z - 3f), 90, "Access_25", 4);
			InstantiatePiece(new Vector3(pos.x - 2f, height + 0.5f, pos.z - 2f), 90, "Access_25", 4);
			costHelper.PayForIt();
			sm.Play("RoadPiece");
			yield return new WaitForEndOfFrame();
			for (int k = (int)pos.z - 7; k <= (int)pos.z + 6; k++)
			{
				Avenueing.subType = "Hway/";
				Avenueing.level = 2;
				avenueing.FlatAndBuildLineZ(new Vector3((int)pos.x, (int)pos.y, k));
			}
			yield return new WaitForEndOfFrame();
			for (int l = (int)pos.x - 4; l <= (int)pos.x + 4; l++)
			{
				Roading.subType = "Road/";
				roading.FlatNewLineX(new Vector2Int(l, (int)pos.z));
				roading.BuildNewLineX(new Vector2Int(l, (int)pos.z));
			}
			RoadCreator roadCreator = roading.rD[(int)pos.x - 2, 1, (int)pos.z];
			RoadCreator roadCreator2 = roading.rD[(int)pos.x + 2, 1, (int)pos.z];
			RoadCreator roadCreator3 = roading.rD[(int)pos.x + 2, 1, (int)pos.z - 1];
			RoadCreator roadCreator4 = roading.rD[(int)pos.x - 2, 1, (int)pos.z - 1];
			RoadCreator roadCreator5 = roading.rD[(int)pos.x - 1, 2, (int)pos.z + 4];
			RoadCreator roadCreator6 = roading.rD[(int)pos.x + 1, 2, (int)pos.z + 4];
			RoadCreator roadCreator7 = roading.rD[(int)pos.x + 1, 2, (int)pos.z - 5];
			RoadCreator roadCreator8 = roading.rD[(int)pos.x - 1, 2, (int)pos.z - 5];
			roadCreator.tile = "access";
			roadCreator.cc = 8;
			roadCreator.Orientation = 0f;
			roadCreator2.tile = "access";
			roadCreator2.cc = 16;
			roadCreator3.tile = "access";
			roadCreator3.cc = 6;
			roadCreator3.Orientation = 180f;
			roadCreator4.tile = "access";
			roadCreator4.cc = 16;
			roadCreator5.tile = "sideaccess";
			roadCreator5.cc = 8;
			roadCreator6.tile = "sideaccess";
			roadCreator6.cc = 3;
			roadCreator7.tile = "sideaccess";
			roadCreator7.cc = 6;
			roadCreator8.tile = "sideaccess";
			roadCreator8.cc = 4;
			roading.rD[(int)pos.x - 1, 2, (int)pos.z].tile = "sideroad";
			roading.rD[(int)pos.x + 1, 2, (int)pos.z].tile = "sideroad";
			roading.rD[(int)pos.x - 1, 2, (int)pos.z - 1].tile = "sideroad";
			roading.rD[(int)pos.x + 1, 2, (int)pos.z - 1].tile = "sideroad";
			yield return new WaitForEndOfFrame();
			sm.Play("PlopBuild");
		}
		else if (cas == "X")
		{
			float height = blockData.h;
			for (int m = (int)pos.x - 5; m <= (int)pos.x + 6; m++)
			{
				for (int n = (int)pos.z - 3; n <= (int)pos.z + 3; n++)
				{
					BlockData blockData3 = world.Get(m, n);
					blockData3?.FlatHeight(height);
					roading.DestroyRoad(m, n);
					CheckChunks(blockData3);
				}
			}
			yield return new WaitForEndOfFrame();
			InstantiatePiece(new Vector3(pos.x - 4f, height + 1.25f, pos.z + 2f), 270, "BretelleIN", 4);
			InstantiatePiece(new Vector3(pos.x - 3f, height + 1f, pos.z + 2f), 180, "Access_25", 2);
			InstantiatePiece(new Vector3(pos.x - 2f, height + 0.75f, pos.z + 2f), 180, "Access_25", 2);
			InstantiatePiece(new Vector3(pos.x - 1f, height + 0.5f, pos.z + 2f), 180, "Access_25", 2);
			InstantiatePiece(new Vector3(pos.x + 5f, height + 1.25f, pos.z + 2f), 90, "BretelleOUT", 2);
			InstantiatePiece(new Vector3(pos.x + 4f, height + 1f, pos.z + 2f), 0, "Access_25", 2);
			InstantiatePiece(new Vector3(pos.x + 3f, height + 0.75f, pos.z + 2f), 0, "Access_25", 2);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 0.5f, pos.z + 2f), 0, "Access_25", 15);
			InstantiatePiece(new Vector3(pos.x + 5f, height + 1.25f, pos.z - 2f), 90, "BretelleIN", 3);
			InstantiatePiece(new Vector3(pos.x + 4f, height + 1f, pos.z - 2f), 0, "Access_25", 1);
			InstantiatePiece(new Vector3(pos.x + 3f, height + 0.75f, pos.z - 2f), 0, "Access_25", 1);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 0.5f, pos.z - 2f), 0, "Access_25", 1);
			InstantiatePiece(new Vector3(pos.x - 4f, height + 1.25f, pos.z - 2f), 270, "BretelleOUT", 1);
			InstantiatePiece(new Vector3(pos.x - 3f, height + 1f, pos.z - 2f), 180, "Access_25", 1);
			InstantiatePiece(new Vector3(pos.x - 2f, height + 0.75f, pos.z - 2f), 180, "Access_25", 1);
			InstantiatePiece(new Vector3(pos.x - 1f, height + 0.5f, pos.z - 2f), 180, "Access_25", 15);
			costHelper.PayForIt();
			sm.Play("RoadPiece");
			yield return new WaitForEndOfFrame();
			for (int num = (int)pos.x - 6; num <= (int)pos.x + 7; num++)
			{
				Avenueing.subType = "Hway/";
				Avenueing.level = 2;
				avenueing.FlatAndBuildLineX(new Vector3(num, (int)pos.y, (int)pos.z));
			}
			yield return new WaitForEndOfFrame();
			for (int num2 = (int)pos.z - 4; num2 <= (int)pos.z + 4; num2++)
			{
				Roading.subType = "Road/";
				roading.FlatNewLineZ(new Vector2Int((int)pos.x, num2));
				roading.BuildNewLineZ(new Vector2Int((int)pos.x, num2));
			}
			RoadCreator roadCreator9 = roading.rD[(int)pos.x, 1, (int)pos.z + 2];
			RoadCreator roadCreator10 = roading.rD[(int)pos.x + 1, 1, (int)pos.z + 2];
			RoadCreator roadCreator11 = roading.rD[(int)pos.x + 1, 1, (int)pos.z - 2];
			RoadCreator roadCreator12 = roading.rD[(int)pos.x, 1, (int)pos.z - 2];
			RoadCreator roadCreator13 = roading.rD[(int)pos.x - 4, 2, (int)pos.z + 1];
			RoadCreator roadCreator14 = roading.rD[(int)pos.x + 5, 2, (int)pos.z + 1];
			RoadCreator roadCreator15 = roading.rD[(int)pos.x + 5, 2, (int)pos.z - 1];
			RoadCreator roadCreator16 = roading.rD[(int)pos.x - 4, 2, (int)pos.z - 1];
			roadCreator9.tile = "access";
			roadCreator9.cc = 15;
			roadCreator10.tile = "access";
			roadCreator10.cc = 5;
			roadCreator10.Orientation = 90f;
			roadCreator11.tile = "access";
			roadCreator11.cc = 15;
			roadCreator12.tile = "access";
			roadCreator12.cc = 7;
			roadCreator12.Orientation = 270f;
			roadCreator13.tile = "sideaccess";
			roadCreator13.cc = 2;
			roadCreator14.tile = "sideaccess";
			roadCreator14.cc = 5;
			roadCreator15.tile = "sideaccess";
			roadCreator15.cc = 1;
			roadCreator16.tile = "sideaccess";
			roadCreator16.cc = 7;
			roading.rD[(int)pos.x + 1, 2, (int)pos.z + 1].tile = "sideroad";
			roading.rD[(int)pos.x, 2, (int)pos.z + 1].tile = "sideroad";
			roading.rD[(int)pos.x + 1, 2, (int)pos.z - 1].tile = "sideroad";
			roading.rD[(int)pos.x, 2, (int)pos.z - 1].tile = "sideroad";
			yield return new WaitForEndOfFrame();
			sm.Play("PlopBuild");
		}
		yield return new WaitForSecondsRealtime(0.2f);
		Avenueing.subType = "Avenue/";
		Avenueing.level = 1;
		busy = false;
	}

	private IEnumerator BuildAveBretelle(Vector3 pos)
	{
		BlockData blockData = world.Get((int)pos.x, (int)pos.z);
		if (cas == "Z")
		{
			float height = blockData.h;
			for (int i = (int)pos.x - 3; i <= (int)pos.x + 3; i++)
			{
				for (int j = (int)pos.z - 6; j <= (int)pos.z + 6; j++)
				{
					BlockData blockData2 = world.Get(i, j);
					blockData2?.FlatHeight(height);
					roading.DestroyRoad(i, j);
					CheckChunks(blockData2);
				}
			}
			yield return new WaitForEndOfFrame();
			InstantiatePiece(new Vector3(pos.x - 2f, height + 1.25f, pos.z + 5f), 0, "BretelleOUT", 4);
			InstantiatePiece(new Vector3(pos.x - 2f, height + 1f, pos.z + 4f), 270, "Access_25", 4);
			InstantiatePiece(new Vector3(pos.x - 2f, height + 0.75f, pos.z + 3f), 270, "Access_25", 4);
			InstantiatePiece(new Vector3(pos.x - 2f, height + 0.5f, pos.z + 2f), 270, "Access_25", 16);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 1.25f, pos.z + 5f), 0, "BretelleIN", 2);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 1f, pos.z + 4f), 270, "Access_25", 3);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 0.75f, pos.z + 3f), 270, "Access_25", 3);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 0.5f, pos.z + 2f), 270, "Access_25", 3);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 1.25f, pos.z - 5f), 180, "BretelleOUT", 3);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 1f, pos.z - 4f), 90, "Access_25", 3);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 0.75f, pos.z - 3f), 90, "Access_25", 3);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 0.5f, pos.z - 2f), 90, "Access_25", 16);
			InstantiatePiece(new Vector3(pos.x - 2f, height + 1.25f, pos.z - 5f), 180, "BretelleIN", 1);
			InstantiatePiece(new Vector3(pos.x - 2f, height + 1f, pos.z - 4f), 90, "Access_25", 4);
			InstantiatePiece(new Vector3(pos.x - 2f, height + 0.75f, pos.z - 3f), 90, "Access_25", 4);
			InstantiatePiece(new Vector3(pos.x - 2f, height + 0.5f, pos.z - 2f), 90, "Access_25", 4);
			costHelper.PayForIt();
			sm.Play("RoadPiece");
			yield return new WaitForEndOfFrame();
			for (int k = (int)pos.z - 7; k <= (int)pos.z + 7; k++)
			{
				Avenueing.subType = "Hway/";
				Avenueing.level = 2;
				avenueing.FlatAndBuildLineZ(new Vector3((int)pos.x, (int)pos.y, k));
			}
			yield return new WaitForEndOfFrame();
			for (int l = (int)pos.x - 4; l <= (int)pos.x + 4; l++)
			{
				Avenueing.subType = "Avenue/";
				Avenueing.level = 1;
				avenueing.FlatAndBuildLineX(new Vector3(l, 0f, (int)pos.z));
			}
			RoadCreator roadCreator = roading.rD[(int)pos.x - 2, 1, (int)pos.z + 1];
			RoadCreator roadCreator2 = roading.rD[(int)pos.x + 2, 1, (int)pos.z + 1];
			RoadCreator roadCreator3 = roading.rD[(int)pos.x + 2, 1, (int)pos.z - 1];
			RoadCreator roadCreator4 = roading.rD[(int)pos.x - 2, 1, (int)pos.z - 1];
			RoadCreator roadCreator5 = roading.rD[(int)pos.x - 1, 2, (int)pos.z + 5];
			RoadCreator roadCreator6 = roading.rD[(int)pos.x + 1, 2, (int)pos.z + 5];
			RoadCreator roadCreator7 = roading.rD[(int)pos.x + 1, 2, (int)pos.z - 5];
			RoadCreator roadCreator8 = roading.rD[(int)pos.x - 1, 2, (int)pos.z - 5];
			roadCreator.tile = "access";
			roadCreator.cc = 8;
			roadCreator2.tile = "access";
			roadCreator2.cc = 16;
			roadCreator3.tile = "access";
			roadCreator3.cc = 6;
			roadCreator4.tile = "access";
			roadCreator4.cc = 16;
			roadCreator5.tile = "sideaccess";
			roadCreator5.cc = 8;
			roadCreator6.tile = "sideaccess";
			roadCreator6.cc = 3;
			roadCreator7.tile = "sideaccess";
			roadCreator7.cc = 6;
			roadCreator8.tile = "sideaccess";
			roadCreator8.cc = 4;
			roading.rD[(int)pos.x - 1, 2, (int)pos.z + 1].tile = "sideroad";
			roading.rD[(int)pos.x + 1, 2, (int)pos.z + 1].tile = "sideroad";
			roading.rD[(int)pos.x - 1, 2, (int)pos.z].tile = "sideroad";
			roading.rD[(int)pos.x + 1, 2, (int)pos.z].tile = "sideroad";
			roading.rD[(int)pos.x - 1, 2, (int)pos.z - 1].tile = "sideroad";
			roading.rD[(int)pos.x + 1, 2, (int)pos.z - 1].tile = "sideroad";
			yield return new WaitForEndOfFrame();
			sm.Play("PlopBuild");
		}
		else if (cas == "X")
		{
			float height = blockData.h;
			for (int m = (int)pos.x - 6; m <= (int)pos.x + 6; m++)
			{
				for (int n = (int)pos.z - 3; n <= (int)pos.z + 3; n++)
				{
					BlockData blockData3 = world.Get(m, n);
					blockData3?.FlatHeight(height);
					roading.DestroyRoad(m, n);
					CheckChunks(blockData3);
				}
			}
			yield return new WaitForEndOfFrame();
			InstantiatePiece(new Vector3(pos.x - 5f, height + 1.25f, pos.z + 2f), 270, "BretelleIN", 4);
			InstantiatePiece(new Vector3(pos.x - 4f, height + 1f, pos.z + 2f), 180, "Access_25", 2);
			InstantiatePiece(new Vector3(pos.x - 3f, height + 0.75f, pos.z + 2f), 180, "Access_25", 2);
			InstantiatePiece(new Vector3(pos.x - 2f, height + 0.5f, pos.z + 2f), 180, "Access_25", 2);
			InstantiatePiece(new Vector3(pos.x + 5f, height + 1.25f, pos.z + 2f), 90, "BretelleOUT", 2);
			InstantiatePiece(new Vector3(pos.x + 4f, height + 1f, pos.z + 2f), 0, "Access_25", 2);
			InstantiatePiece(new Vector3(pos.x + 3f, height + 0.75f, pos.z + 2f), 0, "Access_25", 2);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 0.5f, pos.z + 2f), 0, "Access_25", 15);
			InstantiatePiece(new Vector3(pos.x + 5f, height + 1.25f, pos.z - 2f), 90, "BretelleIN", 3);
			InstantiatePiece(new Vector3(pos.x + 4f, height + 1f, pos.z - 2f), 0, "Access_25", 1);
			InstantiatePiece(new Vector3(pos.x + 3f, height + 0.75f, pos.z - 2f), 0, "Access_25", 1);
			InstantiatePiece(new Vector3(pos.x + 2f, height + 0.5f, pos.z - 2f), 0, "Access_25", 1);
			InstantiatePiece(new Vector3(pos.x - 5f, height + 1.25f, pos.z - 2f), 270, "BretelleOUT", 1);
			InstantiatePiece(new Vector3(pos.x - 4f, height + 1f, pos.z - 2f), 180, "Access_25", 1);
			InstantiatePiece(new Vector3(pos.x - 3f, height + 0.75f, pos.z - 2f), 180, "Access_25", 1);
			InstantiatePiece(new Vector3(pos.x - 2f, height + 0.5f, pos.z - 2f), 180, "Access_25", 15);
			costHelper.PayForIt();
			sm.Play("RoadPiece");
			yield return new WaitForEndOfFrame();
			for (int num = (int)pos.x - 7; num <= (int)pos.x + 7; num++)
			{
				Avenueing.subType = "Hway/";
				Avenueing.level = 2;
				avenueing.FlatAndBuildLineX(new Vector3(num, (int)pos.y, (int)pos.z));
			}
			yield return new WaitForEndOfFrame();
			for (int num2 = (int)pos.z - 4; num2 <= (int)pos.z + 4; num2++)
			{
				Avenueing.subType = "Avenue/";
				Avenueing.level = 1;
				avenueing.FlatAndBuildLineZ(new Vector3((int)pos.x, 0f, num2));
			}
			RoadCreator roadCreator9 = roading.rD[(int)pos.x - 1, 1, (int)pos.z + 2];
			RoadCreator roadCreator10 = roading.rD[(int)pos.x + 1, 1, (int)pos.z + 2];
			RoadCreator roadCreator11 = roading.rD[(int)pos.x + 1, 1, (int)pos.z - 2];
			RoadCreator roadCreator12 = roading.rD[(int)pos.x - 1, 1, (int)pos.z - 2];
			RoadCreator roadCreator13 = roading.rD[(int)pos.x - 5, 2, (int)pos.z + 1];
			RoadCreator roadCreator14 = roading.rD[(int)pos.x + 5, 2, (int)pos.z + 1];
			RoadCreator roadCreator15 = roading.rD[(int)pos.x + 5, 2, (int)pos.z - 1];
			RoadCreator roadCreator16 = roading.rD[(int)pos.x - 5, 2, (int)pos.z - 1];
			roadCreator9.tile = "access";
			roadCreator9.cc = 15;
			roadCreator10.tile = "access";
			roadCreator10.cc = 5;
			roadCreator11.tile = "access";
			roadCreator11.cc = 15;
			roadCreator12.tile = "access";
			roadCreator12.cc = 7;
			roadCreator13.tile = "sideaccess";
			roadCreator13.cc = 2;
			roadCreator14.tile = "sideaccess";
			roadCreator14.cc = 5;
			roadCreator15.tile = "sideaccess";
			roadCreator15.cc = 1;
			roadCreator16.tile = "sideaccess";
			roadCreator16.cc = 7;
			roading.rD[(int)pos.x + 1, 2, (int)pos.z + 1].tile = "sideroad";
			roading.rD[(int)pos.x, 2, (int)pos.z + 1].tile = "sideroad";
			roading.rD[(int)pos.x + 1, 2, (int)pos.z - 1].tile = "sideroad";
			roading.rD[(int)pos.x, 2, (int)pos.z - 1].tile = "sideroad";
			roading.rD[(int)pos.x - 1, 2, (int)pos.z - 1].tile = "sideroad";
			roading.rD[(int)pos.x - 1, 2, (int)pos.z + 1].tile = "sideroad";
			yield return new WaitForEndOfFrame();
			sm.Play("PlopBuild");
		}
		yield return new WaitForSecondsRealtime(0.2f);
		Avenueing.subType = "Avenue/";
		Avenueing.level = 1;
		busy = false;
	}

	private void InstantiatePiece(Vector3 pos, int rotation, string tile, int code)
	{
		BlockData blockData = world.Get((int)pos.x, (int)pos.z);
		if (blockData != null)
		{
			blockData.t = BlockData.Type.BUILDING;
			blockData.g = false;
			blockData.b = true;
			blockData.r = true;
			GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Roads/Hway/Ground/" + tile));
			gameObject.transform.position = pos;
			gameObject.name = pos.x + "_" + 50 + "_" + pos.z;
			gameObject.transform.Rotate(0f, rotation, 0f, Space.Self);
			gameObject.isStatic = false;
			gameObject.transform.parent = tRoad.transform;
			gameObject.tag = "Road";
			gameObject.AddComponent<RoadCreator>();
			RoadCreator roadCreator = (RoadCreator)gameObject.GetComponent(typeof(RoadCreator));
			roadCreator.Capacity = 30000;
			roadCreator.wPos = pos;
			roadCreator.aPos = new Vector3(pos.x, 16f, pos.z);
			roadCreator.Orientation = rotation;
			roadCreator.subType = "Hway/";
			roadCreator.style = "Ground/";
			roadCreator.tile = tile;
			roadCreator.path = "Roads/Hway/Ground/" + tile;
			roadCreator.rType = RoadCreator.RoadType.HIGHWAY;
			roadCreator.cc = code;
			roadCreator.isAccess = true;
			blockData.b = true;
			roading.rD[(int)pos.x, 2, (int)pos.z] = roadCreator;
			world.Roads.Add(roadCreator);
			blockData.o.lavenues.Add(roadCreator);
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
		StopVisualizeRoad();
		if (x < x2 && num > num2)
		{
			costHelper.SetCost(num * 10);
			for (int i = x; i < x2 + 1; i++)
			{
				VisualizeRoad(new Vector3(i, y, z));
			}
		}
		else if (x2 < x && num > num2)
		{
			costHelper.SetCost(num * 10);
			for (int num3 = x; num3 >= x2; num3--)
			{
				VisualizeRoad(new Vector3(num3, y, z));
			}
		}
		else if (z < z2 && num2 > num)
		{
			costHelper.SetCost(num2 * 10);
			for (int j = z; j <= z2; j++)
			{
				VisualizeRoad(new Vector3(x, y, j));
			}
		}
		else if (z2 < z && num2 > num * 2)
		{
			costHelper.SetCost(num2 * 10);
			for (int num4 = z; num4 > z2 - 1; num4--)
			{
				VisualizeRoad(new Vector3(x, y, num4));
			}
		}
	}

	private void VisualizeRoad(Vector3 pos)
	{
		BlockData blockData = bi.world.blockData[(int)pos.x, (int)pos.z];
		Vector3 position = new Vector3(blockData.wP.x, blockData.h + 0.515f, blockData.wP.y);
		RoadCreator ifNotNull = roading.GetIfNotNull((int)pos.x, 1, (int)pos.z);
		if (ifNotNull != null && !blockData.p && ifNotNull.tile != "side" && ifNotNull.tile != "straight" && ifNotNull.tile != "straighttree" && ifNotNull.tile != "sidetree")
		{
			wMat.color = Color.red;
			sm.Play("Impractical");
			Object.Instantiate(roading.redSquare, position, Quaternion.identity);
			roading.redSquare.name = "NewCube";
			roading.redSquare.tag = "NewCube";
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
			if (gameObject.name == "NewCube(Clone)")
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

	private void IntersectionAccess(BlockData bd)
	{
		StopVisualizeRoad();
		bi.tamere = 0;
		RoadCreator roadCreator = roading.rD[bd.wP.x, 1, bd.wP.y];
		if (roadCreator == null)
		{
			Debug.Log("rc is null");
		}
		else if (!(roadCreator.tile != "side") || !(roadCreator.tile != "straight") || !(roadCreator.tile != "straighttree") || !(roadCreator.tile != "sidetree"))
		{
			if (roadCreator.rType == RoadCreator.RoadType.HIGHWAY)
			{
				roadCreator.tile = "sideaccessout";
				Debug.Log("Highway access created");
			}
			else
			{
				roadCreator.tile = "access";
			}
			roadCreator.cc = 14;
		}
	}

	private void BuildAccessLine(Vector3 t_pos, int _x1, int _z1, int _x2, int _z2)
	{
		int num = Mathf.Abs(_x1 - _x2) + 1;
		int num2 = Mathf.Abs(_z1 - _z2) + 1;
		if (_x1 < _x2 && num > num2)
		{
			for (int i = _x1; i < _x2 + 1; i++)
			{
				BlockData blockData = world.Get(i, _z1);
				if (blockData.b && !blockData.p)
				{
					IntersectionAccess(blockData);
					continue;
				}
				blockData.p = true;
				blockData.ClearTerrain();
			}
			for (int j = _x1; j < _x2 + 1; j++)
			{
				PathRight(new Vector2Int(j, _z1));
			}
		}
		else if (_x2 < _x1 && num > num2)
		{
			for (int num3 = _x1; num3 >= _x2; num3--)
			{
				BlockData blockData2 = world.Get(num3, _z1);
				if (blockData2.b && !blockData2.p)
				{
					IntersectionAccess(blockData2);
				}
				else
				{
					blockData2.p = true;
					blockData2.ClearTerrain();
				}
			}
			for (int num4 = _x1; num4 >= _x2; num4--)
			{
				PathLeft(new Vector2Int(num4, _z1));
			}
		}
		if (_z1 < _z2 && num2 > num)
		{
			for (int k = _z1; k <= _z2; k++)
			{
				BlockData blockData3 = world.Get(_x1, k);
				if (blockData3.b && !blockData3.p)
				{
					IntersectionAccess(blockData3);
					continue;
				}
				blockData3.p = true;
				blockData3.ClearTerrain();
			}
			for (int l = _z1; l <= _z2; l++)
			{
				PathUp(new Vector2Int(_x1, l));
			}
		}
		else if (_z2 < _z1 && num2 > num)
		{
			for (int num5 = _z1; num5 > _z2 - 1; num5--)
			{
				BlockData blockData4 = world.Get(_x1, num5);
				if (blockData4.b && !blockData4.p)
				{
					IntersectionAccess(blockData4);
				}
				else
				{
					blockData4.p = true;
					blockData4.ClearTerrain();
				}
			}
			for (int num6 = _z1; num6 > _z2 - 1; num6--)
			{
				PathDown(new Vector2Int(_x1, num6));
			}
		}
		if (_x1 < _x2 && num > num2)
		{
			for (int m = _x1; m < _x2 + 1; m++)
			{
				RoadBitwise(new Vector2Int(m, _z1), 0);
			}
		}
		else if (_x2 < _x1 && num > num2)
		{
			for (int num7 = _x1; num7 >= _x2; num7--)
			{
				RoadBitwise(new Vector2Int(num7, _z1), 180);
			}
		}
		if (_z1 < _z2 && num2 > num)
		{
			for (int n = _z1; n <= _z2; n++)
			{
				RoadBitwise(new Vector2Int(_x1, n), 270);
			}
		}
		else if (_z2 < _z1 && num2 > num)
		{
			for (int num8 = _z1; num8 > _z2 - 1; num8--)
			{
				RoadBitwise(new Vector2Int(_x1, num8), 90);
			}
		}
		foreach (Vector3Int item in lateBitwise)
		{
			RoadBitwise(new Vector2Int(item.x, item.z), item.y);
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
		if (!blockData.p)
		{
			return;
		}
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
		switch (num)
		{
		case 8:
			if (!ifNotNull2.r)
			{
				blockData.FlatHeight(ifNotNull4.h);
			}
			else if (ifNotNull2.h < blockData.h)
			{
				blockData.FlatHeight(ifNotNull2.h);
			}
			else if (ifNotNull2.h > blockData.h + 0.5f)
			{
				blockData.FlatHeight(blockData.h + 0.5f);
			}
			break;
		case 2:
			if (!ifNotNull4.r)
			{
				blockData.FlatHeight(ifNotNull2.h);
			}
			else if (ifNotNull4.h < blockData.h)
			{
				blockData.FlatHeight(ifNotNull4.h);
			}
			else if (ifNotNull4.h > blockData.h + 0.5f)
			{
				blockData.FlatHeight(blockData.h + 0.5f);
			}
			break;
		}
		switch (num)
		{
		case 3:
			blockData.FlatHeight(ifNotNull.h);
			ifNotNull2.FlatHeight(ifNotNull.h);
			lateBitwise.Add(new Vector3Int(ifNotNull.wP.x, 270, ifNotNull.wP.y));
			break;
		case 6:
			blockData.FlatHeight(ifNotNull3.h);
			ifNotNull2.FlatHeight(ifNotNull3.h);
			lateBitwise.Add(new Vector3Int(ifNotNull3.wP.x, 90, ifNotNull3.wP.y));
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
		if (!blockData.p)
		{
			return;
		}
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
		switch (num)
		{
		case 2:
			blockData.FlatHeight(ifNotNull2.h);
			break;
		case 8:
			if (!ifNotNull4.b)
			{
				ifNotNull4.FlatHeight(blockData.h);
			}
			break;
		}
		switch (num)
		{
		case 2:
			if (!ifNotNull4.r)
			{
				blockData.FlatHeight(ifNotNull2.h);
			}
			else if (ifNotNull4.h < blockData.h)
			{
				blockData.FlatHeight(ifNotNull4.h);
			}
			else if (ifNotNull4.h > blockData.h + 0.5f)
			{
				blockData.FlatHeight(blockData.h + 0.5f);
			}
			break;
		case 8:
			if (!ifNotNull2.r)
			{
				blockData.FlatHeight(ifNotNull4.h);
			}
			else if (ifNotNull2.h < blockData.h)
			{
				blockData.FlatHeight(ifNotNull2.h);
			}
			else if (ifNotNull2.h > blockData.h + 0.5f)
			{
				blockData.FlatHeight(blockData.h + 0.5f);
			}
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
			lateBitwise.Add(new Vector3Int(ifNotNull.wP.x, 270, ifNotNull.wP.y));
			break;
		case 12:
			blockData.FlatHeight(ifNotNull3.h);
			ifNotNull4.FlatHeight(ifNotNull3.h);
			lateBitwise.Add(new Vector3Int(ifNotNull3.wP.x, 90, ifNotNull3.wP.y));
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
		if (!blockData.p)
		{
			return;
		}
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
		switch (num)
		{
		case 1:
			if (!ifNotNull3.r)
			{
				blockData.FlatHeight(ifNotNull.h);
			}
			else if (ifNotNull3.h < blockData.h)
			{
				blockData.FlatHeight(ifNotNull3.h);
			}
			else if (ifNotNull3.h > blockData.h + 0.5f)
			{
				blockData.FlatHeight(blockData.h + 0.5f);
			}
			break;
		case 4:
			if (!ifNotNull.r)
			{
				blockData.FlatHeight(ifNotNull3.h);
			}
			else if (ifNotNull.h < blockData.h)
			{
				blockData.FlatHeight(ifNotNull.h);
			}
			else if (ifNotNull.h > blockData.h + 0.5f)
			{
				blockData.FlatHeight(blockData.h + 0.5f);
			}
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
			lateBitwise.Add(new Vector3Int(ifNotNull2.wP.x, 180, ifNotNull2.wP.y));
			break;
		case 9:
			blockData.FlatHeight(ifNotNull.h);
			ifNotNull4.FlatHeight(ifNotNull.h);
			AutoUpdateLine(ifNotNull4, 180);
			break;
		case 12:
			blockData.FlatHeight(ifNotNull4.h);
			ifNotNull3.FlatHeight(ifNotNull4.h);
			lateBitwise.Add(new Vector3Int(ifNotNull4.wP.x, 0, ifNotNull4.wP.y));
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
		if (!blockData.p)
		{
			return;
		}
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
		switch (num)
		{
		case 4:
			if (!ifNotNull.r)
			{
				blockData.FlatHeight(ifNotNull3.h);
			}
			else if (ifNotNull.h < blockData.h)
			{
				blockData.FlatHeight(ifNotNull.h);
			}
			else if (ifNotNull.h > blockData.h + 0.5f)
			{
				blockData.FlatHeight(blockData.h + 0.5f);
			}
			break;
		case 1:
			if (!ifNotNull.b)
			{
				if (!ifNotNull3.r)
				{
					blockData.FlatHeight(ifNotNull.h);
				}
				else if (ifNotNull3.h < blockData.h)
				{
					blockData.FlatHeight(ifNotNull3.h);
				}
				else if (ifNotNull3.h > blockData.h + 0.5f)
				{
					blockData.FlatHeight(blockData.h + 0.5f);
				}
			}
			break;
		}
		switch (num)
		{
		case 3:
			blockData.FlatHeight(ifNotNull2.h);
			ifNotNull.FlatHeight(ifNotNull2.h);
			lateBitwise.Add(new Vector3Int(ifNotNull2.wP.x, 180, ifNotNull2.wP.y));
			break;
		case 6:
			blockData.FlatHeight(ifNotNull.h);
			ifNotNull2.FlatHeight(ifNotNull.h);
			AutoUpdateLine(ifNotNull2, 0);
			break;
		case 9:
			blockData.FlatHeight(ifNotNull4.h);
			ifNotNull.FlatHeight(ifNotNull4.h);
			lateBitwise.Add(new Vector3Int(ifNotNull4.wP.x, 0, ifNotNull4.wP.y));
			break;
		case 12:
			blockData.FlatHeight(ifNotNull.h);
			ifNotNull4.FlatHeight(ifNotNull.h);
			AutoUpdateLine(ifNotNull4, 180);
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

	private void AutoUpdateLine(BlockData bd, int rot)
	{
		autoLimit++;
		if (bd == null || !bd.p)
		{
			return;
		}
		if (autoLimit > 20)
		{
			Debug.Log("Cancelling autoline method...");
			return;
		}
		lateBitwise.Add(new Vector3Int(bd.wP.x, rot, bd.wP.y));
		switch (rot)
		{
		case 0:
		{
			PathRight(world.blockData[bd.wP.x, bd.wP.y].wP);
			BlockData ifNotNull3 = world.GetIfNotNull(bd.wP.x + 1, bd.wP.y);
			if (ifNotNull3 != null && ifNotNull3.p)
			{
				lateBitwise.Add(new Vector3Int(ifNotNull3.wP.x, rot, ifNotNull3.wP.y));
			}
			BlockData ifNotNull4 = world.GetIfNotNull(bd.wP.x + 2, bd.wP.y);
			if (ifNotNull4 != null && ifNotNull4.p)
			{
				lateBitwise.Add(new Vector3Int(ifNotNull4.wP.x, rot, ifNotNull4.wP.y));
			}
			break;
		}
		case 180:
		{
			PathLeft(world.blockData[bd.wP.x, bd.wP.y].wP);
			BlockData ifNotNull7 = world.GetIfNotNull(bd.wP.x - 1, bd.wP.y);
			if (ifNotNull7 != null && ifNotNull7.p)
			{
				lateBitwise.Add(new Vector3Int(ifNotNull7.wP.x, rot, ifNotNull7.wP.y));
			}
			BlockData ifNotNull8 = world.GetIfNotNull(bd.wP.x - 2, bd.wP.y);
			if (ifNotNull8 != null && ifNotNull8.p)
			{
				lateBitwise.Add(new Vector3Int(ifNotNull8.wP.x, rot, ifNotNull8.wP.y));
			}
			break;
		}
		case 270:
		{
			PathUp(world.blockData[bd.wP.x, bd.wP.y].wP);
			BlockData ifNotNull5 = world.GetIfNotNull(bd.wP.x, bd.wP.y + 1);
			if (ifNotNull5 != null && ifNotNull5.p)
			{
				lateBitwise.Add(new Vector3Int(ifNotNull5.wP.x, rot, ifNotNull5.wP.y));
			}
			BlockData ifNotNull6 = world.GetIfNotNull(bd.wP.x, bd.wP.y + 2);
			if (ifNotNull6 != null && ifNotNull6.p)
			{
				lateBitwise.Add(new Vector3Int(ifNotNull6.wP.x, rot, ifNotNull6.wP.y));
			}
			break;
		}
		case 90:
		{
			BlockData ifNotNull = world.GetIfNotNull(bd.wP.x, bd.wP.y - 1);
			if (ifNotNull != null && ifNotNull.p)
			{
				lateBitwise.Add(new Vector3Int(ifNotNull.wP.x, rot, ifNotNull.wP.y));
			}
			BlockData ifNotNull2 = world.GetIfNotNull(bd.wP.x, bd.wP.y - 2);
			if (ifNotNull2 != null && ifNotNull2.p)
			{
				lateBitwise.Add(new Vector3Int(ifNotNull2.wP.x, rot, ifNotNull2.wP.y));
			}
			break;
		}
		}
	}

	public void RoadBitwise(Vector2Int pos, int orientation)
	{
		BlockData blockData = world.blockData[pos.x, pos.y];
		RoadCreator roadCreator = roading.rD[pos.x, 1, pos.y];
		if (blockData.p && roadCreator != null)
		{
			if (roadCreator.rType != RoadCreator.RoadType.HIGHWAY)
			{
				return;
			}
			roading.DestroyRoad(pos.x, pos.y);
		}
		if (blockData.r || (blockData.b && !blockData.p))
		{
			return;
		}
		blockData.b = true;
		CheckChunks(blockData);
		BlockData ifNotNull = world.GetIfNotNull(pos.x, pos.y - 1);
		BlockData ifNotNull2 = world.GetIfNotNull(pos.x + 1, pos.y);
		BlockData ifNotNull3 = world.GetIfNotNull(pos.x, pos.y + 1);
		BlockData ifNotNull4 = world.GetIfNotNull(pos.x - 1, pos.y);
		int num = 0;
		int cc = 4;
		switch (orientation)
		{
		case 0:
			cc = 1;
			break;
		case 180:
			cc = 2;
			break;
		case 270:
			cc = 3;
			break;
		}
		Vector3 vector = new Vector3(0f, 0f, 0f);
		string text = "Access_Straight";
		if (ifNotNull != null && ifNotNull.p)
		{
			num++;
		}
		if (ifNotNull2 != null && ifNotNull2.p)
		{
			num += 2;
		}
		if (ifNotNull3 != null && ifNotNull3.p)
		{
			num += 4;
		}
		if (ifNotNull4 != null && ifNotNull4.p)
		{
			num += 8;
		}
		switch (num)
		{
		case 0:
			text = "Access_Straight";
			vector = new Vector3(0f, orientation, 0f);
			break;
		case 3:
			text = "Access_Curve";
			vector = new Vector3(0f, 270f, 0f);
			cc = 14;
			break;
		case 1:
		case 4:
		case 5:
			text = "Access_Straight";
			vector = new Vector3(0f, orientation, 0f);
			if ((double)ifNotNull.h == (double)blockData.h + 0.25)
			{
				text = "Access_25";
				vector = new Vector3(0f, 90f, 0f);
			}
			else if ((double)ifNotNull3.h == (double)blockData.h + 0.25)
			{
				text = "Access_25";
				vector = new Vector3(0f, 270f, 0f);
			}
			else if ((double)ifNotNull3.h == (double)blockData.h + 0.5)
			{
				text = "Access_50";
				vector = new Vector3(0f, 270f, 0f);
			}
			else if ((double)ifNotNull.h == (double)blockData.h + 0.5)
			{
				text = "Access_50";
				vector = new Vector3(0f, 90f, 0f);
			}
			break;
		case 6:
			text = "Access_Curve";
			vector = new Vector3(0f, 180f, 0f);
			cc = 14;
			break;
		case 7:
			text = "Access_Straight";
			vector = new Vector3(0f, orientation, 0f);
			break;
		case 9:
			text = "Access_Curve";
			vector = new Vector3(0f, 0f, 0f);
			cc = 14;
			break;
		case 2:
		case 8:
		case 10:
			text = "Access_Straight";
			vector = new Vector3(0f, orientation, 0f);
			if ((double)ifNotNull4.h == (double)blockData.h + 0.25)
			{
				text = "Access_25";
				vector = new Vector3(0f, 180f, 0f);
			}
			else if ((double)ifNotNull2.h == (double)blockData.h + 0.25)
			{
				text = "Access_25";
				vector = new Vector3(0f, 0f, 0f);
			}
			else if ((double)ifNotNull2.h == (double)blockData.h + 0.5)
			{
				text = "Access_50";
				vector = new Vector3(0f, 0f, 0f);
			}
			else if ((double)ifNotNull4.h == (double)blockData.h + 0.5)
			{
				text = "Access_50";
				vector = new Vector3(0f, 180f, 0f);
			}
			break;
		case 11:
			text = "Access_Straight";
			vector = new Vector3(0f, orientation, 0f);
			break;
		case 12:
			text = "Access_Curve";
			vector = new Vector3(0f, 90f, 0f);
			cc = 14;
			break;
		case 13:
			text = "Access_Straight";
			vector = new Vector3(0f, orientation, 0f);
			break;
		case 14:
			text = "Access_Straight";
			vector = new Vector3(0f, orientation, 0f);
			break;
		case 15:
			text = "Access_Straight";
			vector = new Vector3(0f, orientation, 0f);
			break;
		}
		if (!(roadCreator != null) || roadCreator.rType != RoadCreator.RoadType.HIGHWAY || blockData.p)
		{
			Vector3 vector2 = new Vector3(pos.x, blockData.h + 0.502f, pos.y);
			GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Roads/Hway/Ground/" + text));
			gameObject.transform.position = vector2;
			gameObject.name = pos.x + "_" + 50 + "_" + pos.y;
			gameObject.transform.Rotate(vector.x, vector.y, vector.z, Space.Self);
			gameObject.isStatic = false;
			gameObject.transform.parent = tRoad.transform;
			gameObject.tag = "Road";
			gameObject.AddComponent<RoadCreator>();
			RoadCreator roadCreator2 = (RoadCreator)gameObject.GetComponent(typeof(RoadCreator));
			roadCreator2.Capacity = 500;
			roadCreator2.wPos = vector2;
			roadCreator2.aPos = new Vector3(pos.x, 16f, pos.y);
			roadCreator2.Orientation = vector.y;
			roadCreator2.Bitwise = num;
			roadCreator2.subType = "Hway/";
			roadCreator2.style = "Ground/";
			roadCreator2.tile = text;
			roadCreator2.path = "Roads/Hway/Ground/" + text;
			roadCreator2.rType = RoadCreator.RoadType.HIGHWAY;
			roadCreator2.cc = cc;
			roadCreator2.isAccess = true;
			blockData.b = true;
			roading.rD[pos.x, 1, pos.y] = roadCreator2;
			world.Roads.Add(roadCreator2);
			blockData.o.lavenues.Add(roadCreator2);
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

	private IEnumerator UpdateChunks()
	{
		yield return new WaitForSecondsRealtime(0.2f);
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
