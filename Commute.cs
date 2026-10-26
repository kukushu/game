using System.Collections.Generic;
using UnityEngine;

public class Commute : MonoBehaviour
{
	public Roading r;

	public bool trafficUpdated;

	private List<RoadCreator> neighbors = new List<RoadCreator>();

	private void AddNotNull(int x, int level, int z)
	{
		RoadCreator ifNotNull = r.GetIfNotNull(x, level, z);
		if (ifNotNull != null)
		{
			neighbors.Add(ifNotNull);
		}
	}

	public List<RoadCreator> GetNeighboursRoad(RoadCreator road, bool uTurn)
	{
		neighbors.Clear();
		int level = 1;
		if (road == null)
		{
			return null;
		}
		if (road.rType == RoadCreator.RoadType.HIGHWAY)
		{
			level = 2;
		}
		int num = (int)road.wPos.x;
		int num2 = (int)road.wPos.z;
		if (uTurn)
		{
			AddNotNull(num + 1, level, num2);
			AddNotNull(num - 1, level, num2);
			AddNotNull(num, level, num2 + 1);
			AddNotNull(num, level, num2 - 1);
			return neighbors;
		}
		switch (road.cc)
		{
		case 1:
			AddNotNull(num + 1, level, num2);
			break;
		case 2:
			AddNotNull(num - 1, level, num2);
			break;
		case 3:
			AddNotNull(num, level, num2 + 1);
			break;
		case 4:
			AddNotNull(num, level, num2 - 1);
			break;
		case 5:
			AddNotNull(num - 1, level, num2);
			AddNotNull(num, level, num2 + 1);
			break;
		case 6:
			AddNotNull(num + 1, level, num2);
			AddNotNull(num, level, num2 + 1);
			break;
		case 7:
			AddNotNull(num, level, num2 - 1);
			AddNotNull(num + 1, level, num2);
			break;
		case 8:
			AddNotNull(num - 1, level, num2);
			AddNotNull(num, level, num2 - 1);
			break;
		case 9:
			AddNotNull(num - 1, level, num2);
			AddNotNull(num + 1, level, num2);
			AddNotNull(num, level, num2 + 1);
			break;
		case 10:
			AddNotNull(num, level, num2 - 1);
			AddNotNull(num, level, num2 + 1);
			AddNotNull(num + 1, level, num2);
			break;
		case 11:
			AddNotNull(num - 1, level, num2);
			AddNotNull(num + 1, level, num2);
			AddNotNull(num, level, num2 - 1);
			break;
		case 12:
			AddNotNull(num, level, num2 - 1);
			AddNotNull(num, level, num2 + 1);
			AddNotNull(num - 1, level, num2);
			break;
		case 14:
			AddNotNull(num + 1, level, num2);
			AddNotNull(num - 1, level, num2);
			AddNotNull(num, level, num2 + 1);
			AddNotNull(num, level, num2 - 1);
			break;
		case 15:
			AddNotNull(num + 1, 2, num2);
			AddNotNull(num - 1, 2, num2);
			AddNotNull(num + 1, 1, num2);
			AddNotNull(num - 1, 1, num2);
			AddNotNull(num, level, num2 + 1);
			AddNotNull(num, level, num2 - 1);
			break;
		case 16:
			AddNotNull(num, 2, num2 + 1);
			AddNotNull(num, 2, num2 - 1);
			AddNotNull(num, 1, num2 + 1);
			AddNotNull(num, 1, num2 - 1);
			AddNotNull(num - 1, level, num2);
			AddNotNull(num + 1, level, num2);
			break;
		}
		return neighbors;
	}

	public List<RoadCreator> GetNeighboursRail(RoadCreator rail)
	{
		int num = ((rail.rType == RoadCreator.RoadType.RAIL) ? 1 : 0);
		List<RoadCreator> list = new List<RoadCreator>();
		int num2 = (int)rail.wPos.x;
		int num3 = (int)rail.wPos.z;
		switch (rail.Bitwise)
		{
		case 0:
			list.Add(r.GetRailBounded(num2 + 1, num, num3, rail));
			list.Add(r.GetRailBounded(num2 - 1, num, num3, rail));
			list.Add(r.GetRailBounded(num2, num, num3 + 1, rail));
			list.Add(r.GetRailBounded(num2, num, num3 - 1, rail));
			break;
		case 1:
			list.Add(r.GetRailBounded(num2, num, num3 - 1, rail));
			break;
		case 2:
			list.Add(r.GetRailBounded(num2 + 1, num, num3, rail));
			break;
		case 3:
			list.Add(r.GetRailBounded(num2 + 1, num, num3, rail));
			list.Add(r.GetRailBounded(num2, num, num3 - 1, rail));
			break;
		case 4:
			list.Add(r.GetRailBounded(num2, num, num3 + 1, rail));
			break;
		case 5:
			list.Add(r.GetRailBounded(num2, num, num3 - 1, rail));
			list.Add(r.GetRailBounded(num2, num, num3 + 1, rail));
			break;
		case 6:
			list.Add(r.GetRailBounded(num2 + 1, num, num3, rail));
			list.Add(r.GetRailBounded(num2, num, num3 + 1, rail));
			break;
		case 7:
			list.Add(r.GetRailBounded(num2, num, num3 - 1, rail));
			list.Add(r.GetRailBounded(num2, num, num3 + 1, rail));
			list.Add(r.GetRailBounded(num2 + 1, num, num3, rail));
			break;
		case 8:
			list.Add(r.GetRailBounded(num2 - 1, num, num3, rail));
			break;
		case 9:
			list.Add(r.GetRailBounded(num2 - 1, num, num3, rail));
			list.Add(r.GetRailBounded(num2, num, num3 - 1, rail));
			break;
		case 10:
			list.Add(r.GetRailBounded(num2 + 1, num, num3, rail));
			list.Add(r.GetRailBounded(num2 - 1, num, num3, rail));
			break;
		case 11:
			list.Add(r.GetRailBounded(num2 + 1, num, num3, rail));
			list.Add(r.GetRailBounded(num2 - 1, num, num3, rail));
			list.Add(r.GetRailBounded(num2, num, num3 - 1, rail));
			break;
		case 12:
			list.Add(r.GetRailBounded(num2, num, num3 + 1, rail));
			list.Add(r.GetRailBounded(num2 - 1, num, num3, rail));
			break;
		case 13:
			list.Add(r.GetRailBounded(num2 - 1, num, num3, rail));
			list.Add(r.GetRailBounded(num2, num, num3 + 1, rail));
			list.Add(r.GetRailBounded(num2, num, num3 - 1, rail));
			break;
		case 14:
			list.Add(r.GetRailBounded(num2 + 1, num, num3, rail));
			list.Add(r.GetRailBounded(num2 - 1, num, num3, rail));
			list.Add(r.GetRailBounded(num2, num, num3 + 1, rail));
			break;
		case 15:
			list.Add(r.GetRailBounded(num2 + 1, num, num3, rail));
			list.Add(r.GetRailBounded(num2 - 1, num, num3, rail));
			list.Add(r.GetRailBounded(num2, num, num3 + 1, rail));
			list.Add(r.GetRailBounded(num2, num, num3 - 1, rail));
			break;
		case 16:
			if (num == 1)
			{
				list.Add(r.GetRailBounded(num2, 0, num3, rail));
			}
			else
			{
				list.Add(r.GetRailBounded(num2, 1, num3, rail));
			}
			list.Add(r.GetRailBounded(num2 + 1, 0, num3, rail));
			list.Add(r.GetRailBounded(num2 - 1, 0, num3, rail));
			list.Add(r.GetRailBounded(num2, 0, num3 + 1, rail));
			list.Add(r.GetRailBounded(num2, 0, num3 - 1, rail));
			list.Add(r.GetRailBounded(num2 + 1, 1, num3, rail));
			list.Add(r.GetRailBounded(num2 - 1, 1, num3, rail));
			list.Add(r.GetRailBounded(num2, 1, num3 + 1, rail));
			list.Add(r.GetRailBounded(num2, 1, num3 - 1, rail));
			break;
		}
		return list;
	}

	public void FindPath(RoadCreator startBlock, RoadCreator targetBlock, LotCreator lc, bool byCar)
	{
		List<RoadCreator> list = new List<RoadCreator>();
		HashSet<RoadCreator> hashSet = new HashSet<RoadCreator>();
		list.Add(startBlock);
		lc.pathTried++;
		int num = 0;
		while (list.Count > 0)
		{
			RoadCreator roadCreator = list[0];
			for (int i = 1; i < list.Count; i++)
			{
				double hCost = list[i].hCost;
				if (hCost < roadCreator.hCost)
				{
					roadCreator = list[i];
				}
				else if (hCost == roadCreator.hCost && list[i].Capacity > roadCreator.Capacity)
				{
					roadCreator = list[i];
				}
				else if (hCost == roadCreator.hCost && list[i].TrafficPercent < roadCreator.TrafficPercent)
				{
					roadCreator = list[i];
				}
			}
			list.Remove(roadCreator);
			hashSet.Add(roadCreator);
			if (hashSet.Count > 2048)
			{
				lc.validCommute = false;
				break;
			}
			if (roadCreator == targetBlock)
			{
				if (startBlock == null || targetBlock == null || lc == null)
				{
					lc.validCommute = false;
					break;
				}
				lc.validCommute = true;
				lc.pathTried = 0;
				RetracePath(startBlock, targetBlock, lc);
				break;
			}
			List<RoadCreator> list2;
			if (byCar)
			{
				list2 = ((!StartBlockAccross(roadCreator, targetBlock) && !(roadCreator == startBlock)) ? GetNeighboursRoad(roadCreator, uTurn: false) : GetNeighboursRoad(roadCreator, uTurn: true));
			}
			else
			{
				list2 = GetNeighboursRail(roadCreator);
			}
			if (list2 == null || list2.Count < 1)
			{
				list2 = GetNeighboursRoad(roadCreator, uTurn: true);
				if (list2 == null || list2.Count < 1)
				{
					break;
				}
			}
			foreach (RoadCreator item in list2)
			{
				if (item == null || item.cc >= 17 || item.rType == RoadCreator.RoadType.PATH)
				{
					continue;
				}
				if (hashSet.Contains(item))
				{
					num++;
					if (num > 34 + hashSet.Count)
					{
						lc.validCommute = false;
						return;
					}
				}
				else
				{
					item.hCost = GetDistance(item, targetBlock);
					item.parentRoad = roadCreator;
					list.Add(item);
				}
			}
		}
	}

	private bool StartBlockAccross(RoadCreator currentBlock, RoadCreator startBlock)
	{
		if (currentBlock == null)
		{
			return false;
		}
		if (currentBlock.cc == 1)
		{
			if (r.GetBounded((int)currentBlock.wPos.x, 1, (int)currentBlock.wPos.z + 1, currentBlock) == startBlock)
			{
				return true;
			}
			return false;
		}
		if (currentBlock.cc == 2)
		{
			if (r.GetBounded((int)currentBlock.wPos.x, 1, (int)currentBlock.wPos.z - 1, currentBlock) == startBlock)
			{
				return true;
			}
			return false;
		}
		if (currentBlock.cc == 3)
		{
			if (r.GetBounded((int)currentBlock.wPos.x - 1, 1, (int)currentBlock.wPos.z, currentBlock) == startBlock)
			{
				return true;
			}
			return false;
		}
		if (currentBlock.cc == 4)
		{
			if (r.GetBounded((int)currentBlock.wPos.x + 1, 1, (int)currentBlock.wPos.z, currentBlock) == startBlock)
			{
				return true;
			}
			return false;
		}
		return false;
	}

	private void RetracePath(RoadCreator startBlock, RoadCreator endBlock, LotCreator lc)
	{
		List<RoadCreator> list = new List<RoadCreator>();
		RoadCreator roadCreator = endBlock;
		while (roadCreator != startBlock)
		{
			list.Add(roadCreator);
			roadCreator.hCost = 1.0;
			roadCreator = roadCreator.parentRoad;
		}
		list.Add(startBlock);
		lc.roadPath = list;
	}

	private double GetDistance(RoadCreator blockA, RoadCreator blockB)
	{
		if (blockA == null || blockB == null)
		{
			return 10000.0;
		}
		int num = Mathf.Abs((int)blockA.wPos.x - (int)blockB.wPos.x) + Mathf.Abs((int)blockA.wPos.z - (int)blockB.wPos.z);
		if (blockA.rType == RoadCreator.RoadType.HIGHWAY)
		{
			return (float)num * 0.75f;
		}
		return num;
	}
}
