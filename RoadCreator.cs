using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoadCreator : MonoBehaviour
{
	public enum Direction
	{
		upX,
		downX,
		upZ,
		downZ
	}

	public enum RoadType
	{
		ROAD,
		AVENUE,
		HIGHWAY,
		PATH,
		RAIL,
		UNDERGROUND
	}

	public enum StopType
	{
		NONE,
		BUS,
		TRAIN
	}

	public int Capacity = 500;

	public int TrafficSlipsal;

	public int TrafficPercent;

	public int Bitwise;

	public int cc;

	public string subType = "Road/";

	public string style = "Light/";

	public string path = "none";

	public string tile = "none";

	public float Orientation;

	public bool isBridge;

	public bool isIntersec;

	public bool isConnection;

	public bool isAccess;

	public bool isExit;

	public bool isEntrance;

	public int culDeSac;

	public float maintenance;

	public bool isRed;

	public bool treeLine;

	public Direction dir;

	public int StopCapacity;

	public int StopUsers;

	public Vector3 wPos = Vector3.zero;

	public Vector3 aPos = Vector3.zero;

	public HashSet<LotCreator> commuteUsers = new HashSet<LotCreator>();

	public double hCost;

	public RoadCreator parentRoad;

	public RoadType rType;

	public StopType stopType;

	public bool reload;

	private World w;

	private Roading r;

	public Waypoint waypoint;

	public Vector3 treePos = Vector3.zero;

	private bool cleanedUp;

	private void Start()
	{
		StartCoroutine(DelayStart());
	}

	private IEnumerator DelayStart()
	{
		yield return new WaitForEndOfFrame();
		if (reload)
		{
			Reload();
		}
		waypoint = gameObject.GetComponent<Waypoint>();
		if (waypoint == null)
		{
			waypoint = gameObject.AddComponent<Waypoint>();
			waypoint.next = gameObject.transform;
			Debug.Log("Waypoint was null. Readed for rc " + tile);
		}
		if (waypoint.treePos != null)
		{
			treePos = waypoint.treePos.transform.position;
		}
	}

	public void TrafficRecount(World w)
	{
		TrafficPercent = Mathf.RoundToInt(TrafficSlipsal * 100 / (Capacity + 1));
		TrafficSlipsal = 0;
		if (stopType != StopType.NONE)
		{
			StopUsers = 0;
			foreach (LotCreator item in commuteUsers.ToList())
			{
				if (item == null)
				{
					commuteUsers.Remove(item);
				}
				else if (item.commuteState == "tc" || item.commuteState == "bc")
				{
					StopUsers += item.Occupants;
				}
				else
				{
					commuteUsers.Remove(item);
				}
			}
		}
		if (rType == RoadType.ROAD)
		{
			bool flag = false;
			if (isBridge)
			{
				maintenance = 4f;
				return;
			}
			if (style.StartsWith("Di"))
			{
				maintenance = 0f;
				return;
			}
			if (style.StartsWith("T"))
			{
				maintenance = 0.5f;
				return;
			}
			if (cc == 3)
			{
				BlockData bounded = w.GetBounded((int)aPos.x + 3, (int)aPos.z);
				if (bounded.st > 2 && style != "Dense/")
				{
					style = "Dense/";
					flag = true;
				}
				else if (bounded.st < 3 && style != "Light/")
				{
					style = "Light/";
					flag = true;
				}
			}
			else if (cc == 4)
			{
				BlockData bounded2 = w.GetBounded((int)aPos.x - 3, (int)aPos.z);
				if (bounded2.st > 2 && style != "Dense/")
				{
					style = "Dense/";
					flag = true;
				}
				else if (bounded2.st < 3 && style != "Light/")
				{
					style = "Light/";
					flag = true;
				}
			}
			else if (cc == 2)
			{
				BlockData bounded3 = w.GetBounded((int)aPos.x, (int)aPos.z + 3);
				if (bounded3.st > 2 && style != "Dense/")
				{
					style = "Dense/";
					flag = true;
				}
				else if (bounded3.st < 3 && style != "Light/")
				{
					style = "Light/";
					flag = true;
				}
			}
			else if (cc == 1)
			{
				BlockData bounded4 = w.GetBounded((int)aPos.x, (int)aPos.z - 3);
				if (bounded4.st > 2 && style != "Dense/")
				{
					style = "Dense/";
					flag = true;
				}
				else if (bounded4.st < 3 && style != "Light/")
				{
					style = "Light/";
					flag = true;
				}
			}
			else if (cc == 5)
			{
				BlockData bounded5 = w.GetBounded((int)aPos.x + 3, (int)aPos.z + 3);
				if (bounded5.st > 2 && style != "Dense/")
				{
					style = "Dense/";
					flag = true;
				}
				else if (bounded5.st < 3 && style != "Light/")
				{
					style = "Light/";
					flag = true;
				}
			}
			else if (cc == 6)
			{
				BlockData bounded6 = w.GetBounded((int)aPos.x + 3, (int)aPos.z - 3);
				if (bounded6.st > 2 && style != "Dense/")
				{
					style = "Dense/";
					flag = true;
				}
				else if (bounded6.st < 3 && style != "Light/")
				{
					style = "Light/";
					flag = true;
				}
			}
			else if (cc == 7)
			{
				BlockData bounded7 = w.GetBounded((int)aPos.x - 3, (int)aPos.z - 3);
				if (bounded7.st > 2 && style != "Dense/")
				{
					style = "Dense/";
					flag = true;
				}
				else if (bounded7.st < 3 && style != "Light/")
				{
					style = "Light/";
					flag = true;
				}
			}
			else if (cc == 8)
			{
				BlockData bounded8 = w.GetBounded((int)aPos.x - 3, (int)aPos.z + 3);
				if (bounded8.st > 2 && style != "Dense/")
				{
					style = "Dense/";
					flag = true;
				}
				else if (bounded8.st < 3 && style != "Light/")
				{
					style = "Light/";
					flag = true;
				}
			}
			else if (cc == 14 && tile == "precurve")
			{
				BlockData bounded9 = w.GetBounded((int)aPos.x + 3, (int)aPos.z + 3);
				if (bounded9.st > 2 && style != "Dense/")
				{
					style = "Dense/";
					flag = true;
				}
				else if (bounded9.st < 3 && style != "Light/")
				{
					style = "Light/";
					flag = true;
				}
			}
			if (flag)
			{
				w.GetBounded((int)aPos.x, (int)aPos.z).o.expiredRoads++;
			}
		}
		float num = 0f;
		treeLine = false;
		if (stopType == StopType.NONE)
		{
			if (style == "Dense/")
			{
				num += 0.25f;
			}
			if (tile == "sidetree" || tile == "straighttree")
			{
				num += 0.5f;
				treeLine = true;
			}
			if (subType == "Street/")
			{
				num -= 0.5f;
			}
			if (rType == RoadType.AVENUE)
			{
				num += 0.75f;
			}
			else if (rType == RoadType.ROAD)
			{
				num += 0.5f;
			}
			else if (rType == RoadType.HIGHWAY)
			{
				num += 1.5f;
			}
			else if (rType == RoadType.RAIL)
			{
				num += 3f;
			}
			else if (rType == RoadType.UNDERGROUND)
			{
				num += 5f;
			}
			else if (rType == RoadType.PATH)
			{
				num += 4f;
			}
			maintenance = num;
		}
		else if (stopType == StopType.BUS)
		{
			maintenance = 75f - 0.01f * (float)StopUsers;
		}
		else
		{
			maintenance = 125f - 0.01f * (float)StopUsers;
		}
	}

	public GameObject RebuildRoad()
	{
		if (!cleanedUp)
		{
			MeshFilter component = GetComponent<MeshFilter>();
			MeshRenderer component2 = GetComponent<MeshRenderer>();
			if (component != null)
			{
				Object.Destroy(component);
			}
			if (component2 != null)
			{
				Object.Destroy(component2);
			}
			cleanedUp = true;
		}
		path = "Roads/" + subType + style + tile;
		GameObject gameObject = Resources.Load<GameObject>("Roads/" + subType + style + tile);
		if (gameObject == null)
		{
			Debug.Log("Road type could not be rebuilt: Roads/" + subType + style + tile);
			return null;
		}
		GameObject gameObject2 = Object.Instantiate(gameObject);
		gameObject2.transform.Rotate(0f, Orientation, 0f, Space.Self);
		return gameObject2;
	}

	private void Reload()
	{
		w = World.staticWorld;
		r = w.eco.r;
		waypoint = GetComponent<Waypoint>();
		reload = false;
		if (rType == RoadType.ROAD || rType == RoadType.AVENUE)
		{
			w.Roads.Add(this);
			r.rD[(int)wPos.x, 1, (int)wPos.z] = this;
			if (stopType == StopType.BUS)
			{
				r.busStops.Add(this);
			}
			if (isExit)
			{
				World.exitRoads.Add(this);
			}
			else if (isEntrance)
			{
				World.entranceRoads.Add(this);
			}
			gameObject.layer = 2;
		}
		else if (rType == RoadType.RAIL)
		{
			w.Rails.Add(this);
			r.rG[(int)wPos.x, 1, (int)wPos.z] = this;
			if (stopType == StopType.TRAIN)
			{
				r.trainStops.Add(this);
			}
			gameObject.layer = 2;
		}
		else if (rType == RoadType.PATH)
		{
			w.Roads.Add(this);
			r.rD[(int)wPos.x, 1, (int)wPos.z] = this;
		}
		else if (rType == RoadType.UNDERGROUND)
		{
			w.Underground.Add(this);
			r.rG[(int)wPos.x, 0, (int)wPos.z] = this;
			if (stopType == StopType.TRAIN)
			{
				r.trainStops.Add(this);
			}
			gameObject.layer = 14;
		}
		else
		{
			if (rType != RoadType.HIGHWAY)
			{
				Debug.Log("Unknown road type");
				return;
			}
			w.Roads.Add(this);
			r.rD[(int)wPos.x, 2, (int)wPos.z] = this;
			if (stopType == StopType.BUS)
			{
				r.busStops.Add(this);
			}
			if (isExit)
			{
				World.exitRoads.Add(this);
			}
			else if (isEntrance)
			{
				World.entranceRoads.Add(this);
			}
			gameObject.layer = 2;
		}
		BlockData blockData = w.blockData[(int)wPos.x, (int)wPos.z];
		if (blockData == null || blockData.o == null)
		{
			return;
		}
		World.updates.Add(blockData.o);
		if (rType == RoadType.AVENUE || rType == RoadType.HIGHWAY)
		{
			blockData.o.lavenues.Add(this);
		}
		else
		{
			blockData.o.lroads.Add(this);
		}
		if (waypoint == null)
		{
			return;
		}
		waypoint.occupers.Clear();
		waypoint.free = true;
		if (tile == "curve_in_light")
		{
			StopLight component = r.GetComponent<StopLight>();
			if (!(component == null))
			{
				return;
			}
			component = gameObject.AddComponent<StopLight>();
			Transform[] componentsInChildren = base.transform.GetComponentsInChildren<Transform>(includeInactive: true);
			foreach (Transform transform in componentsInChildren)
			{
				if (transform.gameObject.name == "Green")
				{
					component.greenLight = transform.gameObject;
				}
				else if (transform.gameObject.name == "Orange")
				{
					component.orangeLight = transform.gameObject;
				}
				else if (transform.gameObject.name == "Red")
				{
					component.redLight = transform.gameObject;
				}
			}
		}
		else
		{
			if (!(tile == "curvein"))
			{
				return;
			}
			StopLight component2 = r.GetComponent<StopLight>();
			if (!(component2 == null))
			{
				return;
			}
			component2 = gameObject.AddComponent<StopLight>();
			Transform[] componentsInChildren = base.transform.GetComponentsInChildren<Transform>(includeInactive: true);
			foreach (Transform transform2 in componentsInChildren)
			{
				if (transform2.gameObject.name == "G")
				{
					component2.greenLight = transform2.gameObject;
				}
				else if (transform2.gameObject.name == "O")
				{
					component2.orangeLight = transform2.gameObject;
				}
				else if (transform2.gameObject.name == "R")
				{
					component2.redLight = transform2.gameObject;
				}
			}
		}
	}
}
