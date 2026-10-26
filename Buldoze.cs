using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Buldoze : MonoBehaviour
{
	public CostHelper costHelper;

	public GameObject cube1x;

	private Renderer rend;

	public Roading roading;

	public Pathing pathing;

	public Railing railing;

	public BlockInteraction bi;

	public World w;

	public SoundManager sm;

	public Indexer indexer;

	public GameObject buldozeBig;

	public GameObject buldozeSmall;

	private List<Vector3> smallDestructions = new List<Vector3>();

	private List<LotCreator> lotDestructions = new List<LotCreator>();

	private int tempCost;

	private void Start()
	{
		rend = cube1x.GetComponent<Renderer>();
	}

	private void Update()
	{
		if (Input.GetMouseButton(0) && bi.tamere == 3 && (Indexer.Index == 9 || Indexer.Index == 4 || Indexer.Index == 3 || Indexer.Index == 55 || Indexer.Index == 56 || Indexer.Index == 26))
		{
			DestructionUpdate();
		}
		if (Input.GetKeyDown(KeyCode.Escape) && Indexer.Index == 9 && bi.tamere == 3)
		{
			bi.x1 = 0;
			bi.z1 = 0;
			bi.x2 = 0;
			bi.z2 = 0;
			bi.tamere = 0;
			StopVisualizeDestruction();
			sm.Play("Impractical");
		}
	}

	private void DestructionUpdate()
	{
		int x = bi.x1;
		int z = bi.z1;
		int x2 = bi.x2;
		int oldx = bi.oldx2;
		int z2 = bi.z2;
		int oldz = bi.oldz2;
		tempCost = 0;
		if (x2 != oldx || z2 != oldz)
		{
			StopVisualizeDestruction();
			if (x <= x2 && z <= z2)
			{
				for (int i = x; i <= x2; i++)
				{
					for (int j = z; j <= z2; j++)
					{
						VisualizeDestruction(new Vector3(i, 16f, j));
					}
				}
			}
			if (x >= x2 && z <= z2)
			{
				for (int k = x2; k <= x; k++)
				{
					for (int l = z; l <= z2; l++)
					{
						VisualizeDestruction(new Vector3(k, 16f, l));
					}
				}
			}
			if (x >= x2 && z >= z2)
			{
				for (int m = x2; m <= x; m++)
				{
					for (int n = z2; n <= z; n++)
					{
						VisualizeDestruction(new Vector3(m, 16f, n));
					}
				}
			}
			if (x <= x2 && z >= z2)
			{
				for (int num = x; num <= x2; num++)
				{
					for (int num2 = z2; num2 <= z; num2++)
					{
						VisualizeDestruction(new Vector3(num, 16f, num2));
					}
				}
			}
		}
		costHelper.SetCost(tempCost);
		if ((float)tempCost > costHelper.budget.Balance)
		{
			sm.Play("NoMoney");
		}
	}

	private void VisualizeDestruction(Vector3 pos)
	{
		BlockData bounded = w.GetBounded((int)pos.x, (int)pos.z);
		if (Indexer.Index == 9)
		{
			rend.material.color = Color.red;
			if (bounded.b)
			{
				tempCost += 2;
			}
		}
		else if (Indexer.Index == 26)
		{
			rend.material.color = Color.cyan;
			tempCost += 15;
		}
		else if (!bounded.b)
		{
			rend.material.color = Color.white;
			if (Indexer.Index == 3)
			{
				tempCost += 5;
			}
			else if (Indexer.Index == 4)
			{
				tempCost++;
			}
			else if (Indexer.Index == 55)
			{
				tempCost += 3;
			}
			else if (Indexer.Index == 56)
			{
				tempCost++;
			}
		}
		else
		{
			rend.material.color = Color.red;
		}
		float num = 1f;
		if (RTS_Camera.showingUnderground)
		{
			num = -0.98f;
		}
		Vector3 position = new Vector3(bounded.wP.x, bounded.h + num, bounded.wP.y);
		if (position.y < 30f && !RTS_Camera.showingUnderground)
		{
			position.y = 30f;
		}
		Object.Instantiate(cube1x, position, Quaternion.identity);
		cube1x.name = "DestroyCube";
		cube1x.tag = "NewCube";
	}

	public void StopVisualizeDestruction()
	{
		GameObject[] array = GameObject.FindGameObjectsWithTag("NewCube");
		foreach (GameObject gameObject in array)
		{
			if (gameObject.name == "DestroyCube(Clone)" || gameObject.name == "NewCube(Clone)" || gameObject.name == "SquareRed1x(Clone)")
			{
				Object.Destroy(gameObject);
			}
			if (gameObject.name == "FriendlyCube(Clone)")
			{
				Object.Destroy(gameObject);
			}
		}
		costHelper.gameObject.SetActive(value: false);
		bi.One_one.transform.position = new Vector3(-1000f, 1000f, -1000f);
	}

	public void FlattenZone(int _x1, int _z1, int _x2, int _z2)
	{
		List<BlockData> list = new List<BlockData>();
		float h = w.GetBounded(bi.x1, bi.z1).h;
		float h2 = w.GetBounded(bi.x1, bi.z1).h;
		if (_x1 <= _x2 && _z1 <= _z2)
		{
			for (int i = _x1; i <= _x2; i++)
			{
				for (int j = _z1; j <= _z2; j++)
				{
					BlockData bounded = w.GetBounded(i, j);
					list.Add(bounded);
					if (bounded.h > h)
					{
						h = bounded.h;
					}
					if (bounded.h < h2)
					{
						h2 = bounded.h;
					}
				}
			}
		}
		if (_x2 <= _x1 && _z1 <= _z2)
		{
			for (int num = _x1; num >= _x2; num--)
			{
				for (int k = _z1; k <= _z2; k++)
				{
					BlockData bounded2 = w.GetBounded(num, k);
					list.Add(bounded2);
					if (bounded2.h > h)
					{
						h = bounded2.h;
					}
					if (bounded2.h < h2)
					{
						h2 = bounded2.h;
					}
				}
			}
		}
		if (_x1 <= _x2 && _z2 <= _z1)
		{
			for (int l = _x1; l <= _x2; l++)
			{
				for (int num2 = _z1; num2 >= _z2; num2--)
				{
					BlockData bounded3 = w.GetBounded(l, num2);
					list.Add(bounded3);
					if (bounded3.h > h)
					{
						h = bounded3.h;
					}
					if (bounded3.h < h2)
					{
						h2 = bounded3.h;
					}
				}
			}
		}
		if (_x2 <= _x1 && _z2 <= _z1)
		{
			for (int num3 = _x1; num3 >= _x2; num3--)
			{
				for (int num4 = _z1; num4 >= _z2; num4--)
				{
					BlockData bounded4 = w.GetBounded(num3, num4);
					list.Add(bounded4);
					if (bounded4.h > h)
					{
						h = bounded4.h;
					}
					if (bounded4.h < h2)
					{
						h2 = bounded4.h;
					}
				}
			}
		}
		int num5 = 0;
		StopVisualizeDestruction();
		if (Indexer.Index == 3)
		{
			if (h - h2 >= 8f)
			{
				sm.Play("Impractical");
				return;
			}
			foreach (BlockData item in list)
			{
				if (item.b || item.r || item.p || item.a)
				{
					continue;
				}
				num5 += 5;
				if ((float)num5 > costHelper.budget.Balance)
				{
					num5 -= 5;
					continue;
				}
				item.h = w.GetBounded(bi.x1, bi.z1).h;
				if (item.h < 30f)
				{
					item.t = BlockData.Type.SAND;
					item.g = false;
				}
				else if (item.t == BlockData.Type.SAND)
				{
					item.t = BlockData.Type.GRASS;
					item.g = true;
				}
				bi.CheckChunks(item);
			}
		}
		else if (Indexer.Index == 4)
		{
			foreach (BlockData item2 in list)
			{
				if (item2.b || item2.r || item2.p || item2.a)
				{
					continue;
				}
				num5++;
				if ((float)num5 > costHelper.budget.Balance)
				{
					num5--;
					continue;
				}
				BlockData bounded5 = w.GetBounded(item2.wP.x + 1, item2.wP.y);
				BlockData bounded6 = w.GetBounded(item2.wP.x - 1, item2.wP.y);
				BlockData bounded7 = w.GetBounded(item2.wP.x, item2.wP.y + 1);
				BlockData bounded8 = w.GetBounded(item2.wP.x, item2.wP.y - 1);
				float num6 = (bounded5.h + bounded6.h + bounded7.h + bounded8.h) / 4f;
				num6 *= 4f;
				num6 = Mathf.Round(num6);
				num6 /= 4f;
				item2.h = num6;
				if (item2.h < 30f)
				{
					item2.t = BlockData.Type.SAND;
					item2.g = false;
				}
				else if (item2.t == BlockData.Type.SAND)
				{
					item2.t = BlockData.Type.GRASS;
					item2.g = true;
				}
				bi.CheckChunks(item2);
			}
		}
		else if (Indexer.Index == 55)
		{
			foreach (BlockData item3 in list)
			{
				if (!item3.b && !item3.r && !item3.p && !item3.a && !(item3.h < 30f))
				{
					num5 += 3;
					if ((float)num5 > costHelper.budget.Balance)
					{
						num5 -= 3;
						continue;
					}
					item3.t = BlockData.Type.GRASSTREE;
					item3.g = false;
					bi.CheckChunks(item3);
				}
			}
		}
		else if (Indexer.Index == 56)
		{
			foreach (BlockData item4 in list)
			{
				if (!item4.b && !item4.r && !item4.p && !item4.a && !(item4.h < 30f))
				{
					num5++;
					if ((float)num5 > costHelper.budget.Balance)
					{
						num5--;
						continue;
					}
					item4.t = BlockData.Type.GRASS;
					item4.g = true;
					item4.bio = 68;
					bi.CheckChunks(item4);
				}
			}
		}
		else if (Indexer.Index == 26)
		{
			foreach (BlockData item5 in list)
			{
				num5 += 15;
				if ((float)num5 > costHelper.budget.Balance)
				{
					num5 -= 15;
					continue;
				}
				ProspectBlock(item5);
				indexer.minimap.UpdateResource(item5);
			}
			indexer.minimap.SetResourceTexture();
		}
		bi.x1 = 0;
		bi.z1 = 0;
		bi.x2 = 0;
		bi.z2 = 0;
		bi.tamere = 0;
		costHelper.PayAmount(num5);
		StartCoroutine(bi.UpdateChunks());
		StopVisualizeDestruction();
	}

	private void ProspectBlock(BlockData b)
	{
		float num = Utils.GenerateResource(b.wP.x, b.wP.y);
		num = Mathf.Clamp(num * 2.2f, 1f, 255f);
		float num2;
		if (num >= 192f)
		{
			if (b.h < 38f)
			{
				num2 = 1f;
			}
			else
			{
				float num3 = Mathf.Clamp(26f - Mathf.Abs(num - 224f) * 4f, 0f, 63f);
				num2 = 192f + num3;
			}
		}
		else if (num >= 128f)
		{
			if (b.h < 32f)
			{
				num2 = 1f;
			}
			else
			{
				float num4 = Mathf.Clamp(16f - Mathf.Abs(num - 160f) * 4f, 0f, 63f);
				num2 = 128f + num4;
			}
		}
		else if (num >= 64f)
		{
			if (b.h >= 38f || b.h < 29.75f)
			{
				num2 = 1f;
			}
			else
			{
				float num5 = Mathf.Clamp(16f - Mathf.Abs(num - 96f) * 4f, 0f, 63f);
				num2 = 64f + num5;
			}
		}
		else if (num >= 10f)
		{
			if (b.h >= 32f)
			{
				num2 = 1f;
			}
			else
			{
				float num6 = ((!(b.h < 29.5f)) ? Mathf.Clamp(24f - Mathf.Abs(num - 32f) * 3f, 1f, 63f) : Mathf.Clamp(26f - Mathf.Abs(num - 32f) * 4f, 1f, 63f));
				num2 = num6;
			}
		}
		else
		{
			num2 = 1f;
		}
		b.e = (byte)num2;
	}

	public void BuldozeZone(int _x1, int _z1, int _x2, int _z2)
	{
		List<BlockData> list = new List<BlockData>();
		HashSet<BlockData> hashSet = new HashSet<BlockData>();
		if (_x1 <= _x2 && _z1 <= _z2)
		{
			for (int i = _x1 - 1; i <= _x2 + 1; i++)
			{
				for (int j = _z1 - 1; j <= _z2 + 1; j++)
				{
					BlockData bounded = w.GetBounded(i, j);
					if (i == _x1 - 1 || i == _x2 + 1 || j == _z1 - 1 || j == _z2 + 1)
					{
						hashSet.Add(bounded);
					}
					else
					{
						list.Add(bounded);
					}
				}
			}
		}
		if (_x2 <= _x1 && _z1 <= _z2)
		{
			for (int num = _x1 + 1; num >= _x2 - 1; num--)
			{
				for (int k = _z1 - 1; k <= _z2 + 1; k++)
				{
					BlockData bounded2 = w.GetBounded(num, k);
					if (num == _x1 + 1 || num == _x2 - 1 || k == _z1 - 1 || k == _z2 + 1)
					{
						hashSet.Add(bounded2);
					}
					else
					{
						list.Add(bounded2);
					}
				}
			}
		}
		if (_x1 <= _x2 && _z2 <= _z1)
		{
			for (int l = _x1 - 1; l <= _x2 + 1; l++)
			{
				for (int num2 = _z1 + 1; num2 >= _z2 - 1; num2--)
				{
					BlockData bounded3 = w.GetBounded(l, num2);
					if (l == _x1 - 1 || l == _x2 + 1 || num2 == _z1 + 1 || num2 == _z2 - 1)
					{
						hashSet.Add(bounded3);
					}
					else
					{
						list.Add(bounded3);
					}
				}
			}
		}
		if (_x2 <= _x1 && _z2 <= _z1)
		{
			for (int num3 = _x1 + 1; num3 >= _x2 - 1; num3--)
			{
				for (int num4 = _z1 + 1; num4 >= _z2 - 1; num4--)
				{
					BlockData bounded4 = w.GetBounded(num3, num4);
					if (num3 == _x1 + 1 || num3 == _x2 - 1 || num4 == _z1 + 1 || num4 == _z2 - 1)
					{
						hashSet.Add(bounded4);
					}
					else
					{
						list.Add(bounded4);
					}
				}
			}
		}
		bi.x1 = 0;
		bi.z1 = 0;
		bi.x2 = 0;
		bi.z2 = 0;
		bi.tamere = 0;
		int num5 = 0;
		StopVisualizeDestruction();
		if (Indexer.undergroundDestroy)
		{
			foreach (BlockData item in list)
			{
				if (item.u)
				{
					item.u = false;
					item.b = false;
					RoadCreator roadCreator = roading.rG[item.wP.x, 0, item.wP.y];
					if (roadCreator != null && roadCreator.stopType == RoadCreator.StopType.TRAIN)
					{
						roading.trainStops.Remove(roadCreator);
					}
					railing.DestroyUnderground(item.wP.x, item.wP.y);
					bi.CheckChunks(item);
				}
			}
		}
		else
		{
			foreach (BlockData item2 in list)
			{
				if (item2.r)
				{
					item2.r = false;
					item2.b = false;
					if (item2.h < 30f)
					{
						item2.t = BlockData.Type.SAND;
					}
					else
					{
						item2.t = BlockData.Type.GRASS;
					}
					smallDestructions.Add(new Vector3(item2.wP.x, item2.h, item2.wP.y));
					roading.DestroyRoad(item2.wP.x, item2.wP.y);
				}
				else if (item2.p)
				{
					item2.p = false;
					item2.b = false;
					if (item2.h < 30f)
					{
						item2.t = BlockData.Type.SAND;
					}
					else
					{
						item2.t = BlockData.Type.GRASS;
					}
					smallDestructions.Add(new Vector3(item2.wP.x, item2.h, item2.wP.y));
					pathing.DestroyRoad(item2.wP.x, item2.wP.y);
				}
				else if (item2.a)
				{
					item2.a = false;
					item2.b = false;
					if (item2.h < 30f)
					{
						item2.t = BlockData.Type.SAND;
					}
					else
					{
						item2.t = BlockData.Type.GRASS;
					}
					RoadCreator roadCreator2 = roading.rG[item2.wP.x, 0, item2.wP.y];
					if (roadCreator2 != null && roadCreator2.stopType == RoadCreator.StopType.TRAIN)
					{
						roading.trainStops.Remove(roadCreator2);
					}
					smallDestructions.Add(new Vector3(item2.wP.x, item2.h, item2.wP.y));
					railing.DestroyRail(item2.wP.x, item2.wP.y);
				}
				if (item2.b)
				{
					num5 += 2;
					if (item2.lO != null)
					{
						foreach (Vector2Int blockPo in item2.lO.blockPos)
						{
							BlockData blockData = w.blockData[blockPo.x, blockPo.y];
							blockData.b = false;
							blockData.pT = "";
							blockData.st = 0;
							if (blockData.h > 30f)
							{
								blockData.t = BlockData.Type.GRASS;
								blockData.g = true;
							}
							else
							{
								blockData.t = BlockData.Type.SAND;
								blockData.g = false;
							}
							bi.CheckChunks(item2);
						}
						if (item2.lO.lType == LotCreator.LotType.INDUSTRIAL)
						{
							w.VacantIndusLots.Remove(item2.lO);
							w.IndustrialBuildings.Remove(item2.lO);
						}
						else if (item2.lO.lType == LotCreator.LotType.RESIDENTIAL)
						{
							w.VacantResLots.Remove(item2.lO);
							w.ResidentialBuildings.Remove(item2.lO);
						}
						else if (item2.lO.lType == LotCreator.LotType.COMMERCIAL)
						{
							w.VacantCommLots.Remove(item2.lO);
							w.CommercialBuildings.Remove(item2.lO);
						}
						else if (item2.lO.lType == LotCreator.LotType.MIXED)
						{
							w.VacantMixedLots.Remove(item2.lO);
							w.MixedBuildings.Remove(item2.lO);
						}
						if (item2.lO.Stage >= 0)
						{
							lotDestructions.Add(item2.lO);
						}
					}
					else if (item2.sO != null)
					{
						ServiceCreator sO = item2.sO;
						if (sO.type == ServiceCreator.Type.Government && sO.typeName != "Hsc")
						{
							sm.Play("Impractical");
							return;
						}
						item2.o.services.Remove(sO);
						if (sO.type == ServiceCreator.Type.Park && sO.typeName != "L" && sO.typeName != "CC")
						{
							w.Services.Remove(sO);
							item2.b = false;
							item2.sO = null;
							if (item2.h > 30f)
							{
								item2.t = BlockData.Type.GRASS;
								item2.g = true;
							}
							else
							{
								item2.t = BlockData.Type.SAND;
								item2.g = false;
							}
							item2.pT = "";
							Object.Destroy(sO.gameObject);
							continue;
						}
						if (sO.type == ServiceCreator.Type.Arcology)
						{
							w.Arcologies.Remove(sO);
							foreach (Vector2Int blockPo2 in item2.sO.blockPos)
							{
								BlockData blockData2 = w.blockData[blockPo2.x, blockPo2.y];
								blockData2.b = false;
								blockData2.sO = null;
								if (blockData2.h > 30f)
								{
									blockData2.t = BlockData.Type.GRASS;
									blockData2.g = true;
								}
								else
								{
									blockData2.t = BlockData.Type.SAND;
									blockData2.g = false;
								}
								bi.CheckChunks(item2);
							}
							w.eco.MC_Demand += sO.MinEmployees / 2;
							w.eco.R_Demand -= (float)sO.MinEmployees * 0.5f * (w.eco.MC_X * w.eco.Retail);
							Object.Destroy(sO.gameObject);
							continue;
						}
						if (sO.type == ServiceCreator.Type.TrainStation)
						{
							foreach (RoadCreator item3 in roading.trainStops.ToList())
							{
								if (item3 != null && item3.name == sO.name)
								{
									item3.stopType = RoadCreator.StopType.NONE;
									item3.Capacity = 0;
									item3.StopUsers = 0;
									item3.commuteUsers.Clear();
									roading.trainStops.Remove(item3);
								}
							}
							w.Services.Remove(sO);
							foreach (Vector2Int blockPo3 in item2.sO.blockPos)
							{
								BlockData blockData3 = w.blockData[blockPo3.x, blockPo3.y];
								blockData3.b = false;
								blockData3.sO = null;
								if (blockData3.h > 30f)
								{
									blockData3.t = BlockData.Type.GRASS;
									blockData3.g = true;
								}
								else
								{
									blockData3.t = BlockData.Type.SAND;
									blockData3.g = false;
								}
								bi.CheckChunks(item2);
							}
							Object.Destroy(sO.gameObject);
							continue;
						}
						w.Services.Remove(sO);
						if (sO.typeName == "Hsc")
						{
							Indexer.padPos = Vector3.zero;
							indexer.copterModel.transform.position = new Vector3(1000f, 1000f, 1000f);
						}
						else if (sO.typeName == "CC")
						{
							ResetCarbonArea((int)sO.Anchor.x, (int)sO.Anchor.z);
						}
						foreach (Vector2Int blockPo4 in item2.sO.blockPos)
						{
							BlockData blockData4 = w.blockData[blockPo4.x, blockPo4.y];
							blockData4.b = false;
							blockData4.sO = null;
							if (blockData4.h > 30f)
							{
								blockData4.t = BlockData.Type.GRASS;
								blockData4.g = true;
							}
							else
							{
								blockData4.t = BlockData.Type.SAND;
								blockData4.g = false;
							}
							bi.CheckChunks(item2);
						}
						Object.Destroy(sO.gameObject);
					}
					else if (item2.uO != null)
					{
						foreach (Vector2Int bd in item2.uO.bdList)
						{
							BlockData blockData5 = w.blockData[bd.x, bd.y];
							blockData5.b = false;
							if (blockData5.h > 30f)
							{
								blockData5.t = BlockData.Type.GRASS;
								blockData5.g = true;
							}
							else
							{
								blockData5.t = BlockData.Type.SAND;
								blockData5.g = false;
							}
							bi.CheckChunks(item2);
						}
						w.eco.Stability -= item2.uO.Stage;
						w.eco.LC_Demand += item2.uO.Occupants / 2;
						w.SlumBuildings.Remove(item2.uO);
						smallDestructions.Add(item2.uO.gameObject.transform.position);
						Object.Destroy(item2.uO.gameObject);
					}
					else
					{
						item2.b = false;
						item2.gT = 0;
						bi.CheckChunks(item2);
					}
				}
				else
				{
					if (item2.h > 30f)
					{
						item2.t = BlockData.Type.GRASS;
						item2.pT = "";
					}
					else
					{
						item2.t = BlockData.Type.SAND;
						item2.pT = "";
						item2.g = false;
					}
					bi.CheckChunks(item2);
				}
			}
		}
		foreach (BlockData item4 in hashSet)
		{
			RoadCreator ifNotNull = roading.GetIfNotNull(item4.wP.x, 1, item4.wP.y);
			if (ifNotNull != null && ifNotNull.rType == RoadCreator.RoadType.ROAD && item4.wP.x >= 1 && item4.wP.y >= 1 && item4.wP.x < World.chunkSize * World.worldSize - 1 && item4.wP.y < World.chunkSize * World.worldSize - 1)
			{
				Roading.subType = ifNotNull.subType;
				Roading.style = ifNotNull.style;
				if (ifNotNull.isIntersec)
				{
					roading.DestroyRoad(item4.wP.x, item4.wP.y);
					roading.CreateIntersection(item4.wP);
				}
				else
				{
					roading.DestroyRoad(item4.wP.x, item4.wP.y);
					roading.CreateStraight(item4.wP);
				}
			}
		}
		costHelper.PayAmount(num5);
		StartCoroutine(bi.UpdateChunks());
		StartCoroutine(DestructionAnimation());
	}

	private void ResetCarbonArea(int x, int z)
	{
		for (int i = x - 14; i <= x + 14; i++)
		{
			for (int j = z - 14; j <= z + 14; j++)
			{
				w.GetBounded(i, j).v[1] = 1;
			}
		}
	}

	private IEnumerator DestructionAnimation()
	{
		yield return null;
		foreach (LotCreator item in lotDestructions.ToList())
		{
			if (item == null)
			{
				Debug.Log("Lot was null. Cancel Destroy");
				continue;
			}
			Object.Instantiate(buldozeBig).transform.position = item.Anchor + new Vector3(0f, 1f, 0f);
			GiveBackDemand(item, item.Occupants);
			StartCoroutine(item.ShitsGoingDown());
			yield return null;
		}
		lotDestructions.Clear();
		smallDestructions.Clear();
	}

	public void GiveBackDemand(LotCreator lc, float occupants)
	{
		if (lc.lClass == LotCreator.LotClass.LOW)
		{
			w.eco.LC_Demand += occupants * 0.3f;
			w.eco.Slum_Demand += occupants * 0.4f;
		}
		else if (lc.lClass == LotCreator.LotClass.MIDDLE)
		{
			w.eco.LC_Demand += occupants * 0.6f;
			w.eco.MC_Demand += occupants * 0.1f;
		}
		else if (lc.lClass == LotCreator.LotClass.UPPER)
		{
			w.eco.MC_Demand += occupants * 0.6f;
			w.eco.UC_Demand += occupants * 0.1f;
		}
		else if (lc.lClass == LotCreator.LotClass.MIXRETAIL)
		{
			w.eco.MC_Demand += occupants * 0.5f;
			w.eco.R_Demand += (float)lc.Visitors * 0.4f;
		}
		else if (lc.lClass == LotCreator.LotClass.MIXOFFICE)
		{
			w.eco.UC_Demand += occupants * 0.5f;
			w.eco.O_Demand += (float)lc.Visitors * 0.4f;
		}
		else if (lc.lClass == LotCreator.LotClass.RETAIL)
		{
			w.eco.R_Demand += occupants * 0.4f;
		}
		else if (lc.lClass == LotCreator.LotClass.OFFICE)
		{
			w.eco.O_Demand += occupants * 0.4f;
		}
		else if (lc.lClass == LotCreator.LotClass.HIGHTECH)
		{
			w.eco.HT_Demand += occupants * 0.4f;
		}
		else if (lc.lClass == LotCreator.LotClass.MANUFACTURE)
		{
			w.eco.M_Demand += occupants * 0.4f;
		}
	}
}
