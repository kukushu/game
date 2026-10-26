using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrafficManager : MonoBehaviour
{
	public GameObject tCars;

	public static int cars = 0;

	public int pCars;

	public int nCars;

	public static int peds = 0;

	public int nPeds;

	public int pTrains;

	public static int nTrains = 0;

	public static int intersection = 90;

	public static int lastintersection = 0;

	public static int RedLightInterval = 15;

	public float carSpawnEvery = 2f;

	public float pedsSpawnEvery = 2f;

	public World world;

	public Roading roading;

	public Commute commute;

	public static List<VehicleWP> vehicles = new List<VehicleWP>();

	public static List<TrainWP> trains = new List<TrainWP>();

	public static List<Pedestrian> peds_A = new List<Pedestrian>();

	public static List<Pedestrian> peds_B = new List<Pedestrian>();

	public static List<Pedestrian> lowPeds = new List<Pedestrian>();

	public static List<Pedestrian> midPeds = new List<Pedestrian>();

	public static List<Pedestrian> hiPeds = new List<Pedestrian>();

	private int count;

	private void Start()
	{
		vehicles.Clear();
		trains.Clear();
		peds_A.Clear();
		peds_B.Clear();
		lowPeds.Clear();
		midPeds.Clear();
		hiPeds.Clear();
		StartCoroutine(ClockRedLight());
		StartCoroutine(TrafficUpdate());
		StartCoroutine(PedestrianUpdate());
	}

	private void Update()
	{
		if (Time.timeScale < 1f)
		{
			return;
		}
		foreach (VehicleWP item in vehicles.ToList())
		{
			if (item == null || !item.rend.isVisible)
			{
				if (!item.silent)
				{
					item.audioSource.enabled = false;
				}
			}
			else
			{
				item.UpdateVehicle();
			}
		}
		foreach (TrainWP item2 in trains.ToList())
		{
			if (!(item2 == null))
			{
				item2.UpdateTrain();
			}
		}
	}

	private IEnumerator PedestrianUpdate()
	{
		while (true)
		{
			foreach (Pedestrian item in peds_A.ToList())
			{
				if (!(item == null))
				{
					if (item.life < 10)
					{
						item.DestroyPed();
					}
					else
					{
						item.UpdatePedestrian();
					}
				}
			}
			yield return null;
			foreach (Pedestrian item2 in peds_B.ToList())
			{
				if (!(item2 == null))
				{
					if (item2.life < 10)
					{
						item2.DestroyPed();
					}
					else
					{
						item2.UpdatePedestrian();
					}
				}
			}
			yield return null;
			nPeds = peds;
		}
	}

	public void LotSpawnSpecialVehicle(LotCreator lc, string vehicle)
	{
		if (lc.roadPath == null)
		{
			Debug.Log(lc.gameObject.name + " road path is null!");
			lc.commuteState = "invalidcommute";
			return;
		}
		RoadCreator roadCreator = lc.roadPath[lc.roadPath.Count - 1];
		if (roadCreator == null)
		{
			return;
		}
		if (roadCreator.waypoint == null)
		{
			Debug.Log("Start Waypoint Inexistant");
		}
		else if (roadCreator.waypoint.free)
		{
			cars++;
			nCars++;
			pCars = cars;
			lc.carsOut++;
			GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(vehicle));
			gameObject.name = "bus_" + nCars;
			VehicleWP component = gameObject.GetComponent<VehicleWP>();
			component.roading = roading;
			component.path = lc.roadPath.ToList();
			component.lotOwner = lc;
			float num = 0.9f;
			if (nCars > 500)
			{
				num = 0.1f;
			}
			else if (nCars > 250)
			{
				num = 0.4f;
			}
			else if (nCars > 100)
			{
				num = 0.6f;
			}
			if (num > Random.value)
			{
				component.silent = true;
			}
			vehicles.Add(component);
			gameObject.transform.parent = tCars.transform;
		}
	}

	public void ServiceSpawnSpecialVehicle(ServiceCreator sc, string vehicle)
	{
		if (world.ResidentialBuildings.Count < 2)
		{
			return;
		}
		LotCreator lotCreator = world.ResidentialBuildings[Random.Range(0, world.ResidentialBuildings.Count)];
		RoadCreator ifNotNull = roading.GetIfNotNull(sc.Entry.x, 1, sc.Entry.y);
		if (ifNotNull == null)
		{
			return;
		}
		List<RoadCreator> roadPath = lotCreator.roadPath;
		commute.FindPath(ifNotNull, lotCreator.RoadEntry, lotCreator, byCar: true);
		List<RoadCreator> roadPath2 = lotCreator.roadPath;
		if (roadPath2 != null && roadPath2.Count >= 3 && lotCreator.validCommute)
		{
			RoadCreator roadCreator = roadPath2[roadPath2.Count - 1];
			if (!(roadCreator == null) && !(roadCreator.waypoint == null) && roadCreator.waypoint.free)
			{
				cars++;
				nCars++;
				pCars = cars;
				sc.carsOut++;
				GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(vehicle));
				gameObject.name = "special_" + nCars;
				VehicleWP component = gameObject.GetComponent<VehicleWP>();
				component.roading = roading;
				component.path = roadPath2.ToList();
				component.serviceOwner = sc;
				component.siren.volume = SettingsStatic.Carsvolume * 1.1f;
				vehicles.Add(component);
				gameObject.transform.parent = tCars.transform;
				lotCreator.roadPath = roadPath;
			}
		}
	}

	public void SendSpecialVehicleOnPath(ServiceCreator sc, LotCreator lc, string vehicle, FireCoroutine fireco)
	{
		if (world.ResidentialBuildings.Count < 2)
		{
			return;
		}
		RoadCreator ifNotNull = roading.GetIfNotNull(sc.Entry.x, 1, sc.Entry.y);
		if (ifNotNull == null)
		{
			return;
		}
		commute.FindPath(ifNotNull, lc.RoadEntry, lc, byCar: true);
		List<RoadCreator> roadPath = lc.roadPath;
		if (roadPath == null)
		{
			return;
		}
		List<RoadCreator> roadPath2 = lc.roadPath;
		if (roadPath.Count >= 3 && lc.validCommute)
		{
			RoadCreator roadCreator = roadPath[roadPath.Count - 1];
			if (!(roadCreator == null) && !(roadCreator.waypoint == null) && roadCreator.waypoint.free)
			{
				cars++;
				nCars++;
				pCars = cars;
				sc.carsOut++;
				GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(vehicle));
				gameObject.name = "fire";
				VehicleWP component = gameObject.GetComponent<VehicleWP>();
				component.roading = roading;
				component.path = roadPath.ToList();
				component.serviceOwner = sc;
				component.siren.volume = SettingsStatic.Carsvolume * 1.25f;
				component.life = 5000;
				vehicles.Add(component);
				fireco.vehicle = component;
				fireco.firetruckenroute = true;
				gameObject.transform.parent = tCars.transform;
				lc.roadPath = roadPath2;
			}
		}
	}

	public void LotSpawnCar(LotCreator lc)
	{
		if (lc.roadPath == null)
		{
			Debug.Log(lc.gameObject.name + " road path is null!");
			lc.commuteState = "invalidcommute";
			return;
		}
		RoadCreator roadCreator = lc.roadPath[lc.roadPath.Count - 1];
		if (roadCreator == null)
		{
			return;
		}
		if (roadCreator.waypoint == null)
		{
			Debug.Log("Start Waypoint Inexistant");
		}
		else if (roadCreator.waypoint.free)
		{
			string path;
			if (lc.lClass != LotCreator.LotClass.LOW)
			{
				if (lc.lClass != LotCreator.LotClass.MIDDLE && lc.lClass != LotCreator.LotClass.MIXRETAIL)
				{
					if (lc.lClass == LotCreator.LotClass.UPPER || lc.lClass == LotCreator.LotClass.MIXOFFICE)
					{
						path = Dico.Cars_UC[Random.Range(0, Dico.Cars_UC.Length)];
					}
					else if (lc.lClass == LotCreator.LotClass.MANUFACTURE)
					{
						path = Dico.Cars_M[Random.Range(0, Dico.Cars_M.Length)];
					}
					else if (lc.lClass == LotCreator.LotClass.HIGHTECH)
					{
						path = Dico.Cars_HT[Random.Range(0, Dico.Cars_HT.Length)];
					}
					else
					{
						path = ((lc.lClass != LotCreator.LotClass.OFFICE) ? Dico.Cars_R[Random.Range(0, Dico.Cars_R.Length)] : Dico.Cars_O[Random.Range(0, Dico.Cars_O.Length)]);
					}
				}
				else
				{
					path = Dico.Cars_MC[Random.Range(0, Dico.Cars_MC.Length)];
				}
			}
			else
			{
				path = Dico.Cars_LC[Random.Range(0, Dico.Cars_LC.Length)];
			}
			cars++;
			nCars++;
			pCars = cars;
			lc.carsOut++;
			GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(path));
			gameObject.name = "car_" + nCars;
			VehicleWP component = gameObject.GetComponent<VehicleWP>();
			component.roading = roading;
			component.path = lc.roadPath.ToList();
			component.lotOwner = lc;
			float num = 0.9f;
			if (nCars > 250)
			{
				num = 0.1f;
			}
			else if (nCars > 100)
			{
				num = 0.4f;
			}
			else if (nCars > 50)
			{
				num = 0.6f;
			}
			if (num > Random.value)
			{
				component.silent = true;
			}
			vehicles.Add(component);
			gameObject.transform.parent = tCars.transform;
		}
	}

	public void LotSpawnAirTaxi(LotCreator lc)
	{
		if (lc.roadPath == null)
		{
			Debug.Log(lc.gameObject.name + " road path is null!");
			lc.commuteState = "invalidcommute";
			return;
		}
		RoadCreator roadCreator = lc.roadPath[lc.roadPath.Count - 1];
		if (roadCreator == null)
		{
			return;
		}
		if (roadCreator.waypoint == null)
		{
			Debug.Log("Start Waypoint Inexistant");
			return;
		}
		string path = ((lc.lType != LotCreator.LotType.RESIDENTIAL && lc.lType != LotCreator.LotType.MIXED) ? Dico.Drones_IND[Random.Range(0, Dico.Drones_IND.Length)] : Dico.Drones_RES[Random.Range(0, Dico.Drones_RES.Length)]);
		cars++;
		nCars++;
		pCars = cars;
		lc.carsOut++;
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(path));
		gameObject.name = "AT_" + nCars;
		VehicleWP component = gameObject.GetComponent<VehicleWP>();
		component.roading = roading;
		component.path = lc.roadPath.ToList();
		component.lotOwner = lc;
		component.isDrone = true;
		float num = 0.9f;
		if (nCars > 125)
		{
			num = 0.05f;
		}
		else if (nCars > 50)
		{
			num = 0.2f;
		}
		else if (nCars > 20)
		{
			num = 0.4f;
		}
		if (num > Random.value)
		{
			component.silent = true;
		}
		vehicles.Add(component);
		gameObject.transform.parent = tCars.transform;
	}

	public void SpawnTrain(LotCreator lc)
	{
		RoadCreator roadCreator = lc.roadPath[lc.roadPath.Count - 1];
		if (!(roadCreator == null) && !(roadCreator.waypoint == null) && roadCreator.waypoint.free && lc.roadPath.Count >= 4 && (roadCreator.rType == RoadCreator.RoadType.RAIL || roadCreator.rType == RoadCreator.RoadType.UNDERGROUND))
		{
			nTrains++;
			GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Vehicles/S_LRT"));
			gameObject.name = "head_" + nCars;
			gameObject.transform.parent = tCars.transform;
			TrainWP component = gameObject.GetComponent<TrainWP>();
			component.roading = roading;
			component.isHead = true;
			component.path = lc.roadPath.ToList();
			component.pathPos = component.path.Count - 3;
			trains.Add(component);
			GameObject gameObject2 = (GameObject)Object.Instantiate(Resources.Load("Vehicles/S_LRT"));
			gameObject2.name = "w2_" + nCars;
			gameObject2.transform.parent = tCars.transform;
			TrainWP component2 = gameObject2.GetComponent<TrainWP>();
			component2.roading = roading;
			component2.isHead = false;
			component2.path = lc.roadPath.ToList();
			component2.pathPos = component2.path.Count - 2;
			trains.Add(component2);
			GameObject gameObject3 = (GameObject)Object.Instantiate(Resources.Load("Vehicles/S_LRT"));
			gameObject3.name = "w3_" + nCars;
			gameObject3.transform.parent = tCars.transform;
			TrainWP component3 = gameObject3.GetComponent<TrainWP>();
			component3.roading = roading;
			component3.isHead = false;
			component3.path = lc.roadPath.ToList();
			component3.pathPos = component3.path.Count - 1;
			trains.Add(component3);
			component.wagon1 = component2;
			component.wagon2 = component3;
			component2.headWagon = component;
			component3.headWagon = component;
		}
	}

	public void SpawnPed(LotCreator lc)
	{
		if (lc.RoadEntry == null)
		{
			return;
		}
		string text = "Pedestrian/Prepared/man_casual";
		bool flag = false;
		byte b = (byte)((lc.lClass != LotCreator.LotClass.LOW && lc.lClass != LotCreator.LotClass.MANUFACTURE) ? ((lc.lClass == LotCreator.LotClass.MIDDLE || lc.lClass == LotCreator.LotClass.MIXRETAIL) ? 1 : 2) : 0);
		if (b == 0 && lowPeds.Count > 0)
		{
			lowPeds[0].roading = roading;
			lowPeds[0].currentRC = lc.RoadEntry;
			lowPeds[0].lotOwner = lc;
			lowPeds[0].gameObject.SetActive(value: true);
			if (lowPeds[0].listN == 1)
			{
				peds_A.Add(lowPeds[0]);
			}
			else
			{
				peds_B.Add(lowPeds[0]);
			}
			lowPeds.Remove(lowPeds[0]);
			flag = true;
		}
		else if (b == 0)
		{
			text = Dico.Peds_Low[Random.Range(0, Dico.Peds_Low.Length)];
		}
		if (b == 1 && midPeds.Count > 0)
		{
			midPeds[0].roading = roading;
			midPeds[0].currentRC = lc.RoadEntry;
			midPeds[0].lotOwner = lc;
			midPeds[0].gameObject.SetActive(value: true);
			if (midPeds[0].listN == 1)
			{
				peds_A.Add(midPeds[0]);
			}
			else
			{
				peds_B.Add(midPeds[0]);
			}
			midPeds.Remove(midPeds[0]);
			flag = true;
		}
		else if (b == 1)
		{
			text = Dico.Peds_Middle[Random.Range(0, Dico.Peds_Low.Length)];
		}
		if (b == 2 && hiPeds.Count > 0)
		{
			hiPeds[0].roading = roading;
			hiPeds[0].currentRC = lc.RoadEntry;
			hiPeds[0].lotOwner = lc;
			hiPeds[0].gameObject.SetActive(value: true);
			if (hiPeds[0].listN == 1)
			{
				peds_A.Add(hiPeds[0]);
			}
			else
			{
				peds_B.Add(hiPeds[0]);
			}
			hiPeds.Remove(hiPeds[0]);
			flag = true;
		}
		else if (b == 2)
		{
			text = Dico.Peds_All[Random.Range(0, Dico.Peds_Low.Length)];
		}
		if (!flag)
		{
			GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Pedestrian/Prepared/" + text));
			gameObject.name = text;
			Pedestrian component = gameObject.GetComponent<Pedestrian>();
			component.roading = roading;
			component.currentRC = lc.RoadEntry;
			component.lotOwner = lc;
			component.pClass = b;
			gameObject.transform.parent = tCars.transform;
			if (Random.value < 0.5f)
			{
				peds_A.Add(component);
				component.listN = 1;
			}
			else
			{
				peds_B.Add(component);
				component.listN = 2;
			}
		}
		lc.pedsOut++;
	}

	private IEnumerator ClockRedLight()
	{
		while (true)
		{
			intersection += 90;
			if (intersection > 359)
			{
				intersection = 0;
			}
			lastintersection += 90;
			if (lastintersection > 359)
			{
				lastintersection = 0;
			}
			yield return new WaitForSeconds(RedLightInterval);
		}
	}

	private IEnumerator TrafficUpdate()
	{
		while (true)
		{
			if (world.Roads.Count > 0)
			{
				float totTraffic = 0f;
				foreach (RoadCreator item in world.Roads.ToList())
				{
					if (item != null)
					{
						item.TrafficRecount(roading.w);
						totTraffic += (float)item.TrafficPercent;
					}
					count++;
					if (count > 16)
					{
						count = 0;
						yield return null;
					}
				}
				foreach (RoadCreator item2 in world.Rails.Concat(world.Underground).ToList())
				{
					if (item2 != null)
					{
						item2.TrafficRecount(roading.w);
					}
					count++;
					if (count > 32)
					{
						count = 0;
						yield return null;
					}
				}
				world.eco.Congestion = totTraffic / (float)(world.Roads.Count + 1);
			}
			yield return new WaitForEndOfFrame();
			if (world.ResidentialBuildings.Count > 0)
			{
				foreach (LotCreator item3 in world.ResidentialBuildings.ToList())
				{
					if (item3.commuteState == "at" || item3.commuteState == "wc" || item3.roadPath == null)
					{
						continue;
					}
					foreach (RoadCreator item4 in item3.roadPath)
					{
						if (item4 != null)
						{
							item4.TrafficSlipsal += Mathf.RoundToInt((float)item3.Occupants * 0.7f);
						}
					}
					count++;
					if (count > 30)
					{
						count = 0;
						yield return null;
					}
				}
			}
			if (world.MixedBuildings.Count > 0)
			{
				foreach (LotCreator item5 in world.MixedBuildings.ToList())
				{
					if (item5.commuteState == "at" || item5.commuteState == "wc" || item5.roadPath == null)
					{
						continue;
					}
					foreach (RoadCreator item6 in item5.roadPath)
					{
						if (item6 != null)
						{
							item6.TrafficSlipsal += Mathf.RoundToInt((float)item5.Occupants * 0.35f);
						}
					}
					count++;
					if (count > 30)
					{
						count = 0;
						yield return null;
					}
				}
			}
			if (world.IndustrialBuildings.Count > 0)
			{
				foreach (LotCreator item7 in world.IndustrialBuildings.ToList())
				{
					if (item7.commuteState == "at" || item7.commuteState == "wc" || item7.roadPath == null)
					{
						continue;
					}
					foreach (RoadCreator item8 in item7.roadPath)
					{
						if (item8 != null)
						{
							item8.TrafficSlipsal += Mathf.RoundToInt((float)item7.Occupants * 1.75f);
						}
					}
					count++;
					if (count > 30)
					{
						count = 0;
						yield return null;
					}
				}
			}
			if (world.CommercialBuildings.Count <= 0)
			{
				continue;
			}
			foreach (LotCreator item9 in world.CommercialBuildings.ToList())
			{
				if (item9.commuteState == "at" || item9.commuteState == "wc" || item9.roadPath == null)
				{
					continue;
				}
				foreach (RoadCreator item10 in item9.roadPath)
				{
					if (item10 != null)
					{
						item10.TrafficSlipsal += item9.Occupants;
					}
				}
				count++;
				if (count > 30)
				{
					count = 0;
					yield return null;
				}
			}
		}
	}
}
