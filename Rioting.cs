using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Rioting : MonoBehaviour
{
	public float Violence = 0.1f;

	public static int Rioters = 0;

	public static int MaxRioters = 500;

	public static Vector3 riotPosition;

	public Vector3 force = new Vector3(75f, 210f, 75f);

	public int riotDuration = 120;

	private int spreadRadius = 2;

	public int response;

	public Roading roading;

	public GameObject tCars;

	public Messages messages;

	private List<GameObject> props = new List<GameObject>();

	private List<GameObject> citizens = new List<GameObject>();

	private List<GameObject> rioterBank = new List<GameObject>();

	private bool isBusy;

	private bool onGoingRiot;

	public void NewRiot(int rioters, string LOCpol, int LOCreason, float violence)
	{
		MaxRioters = rioters;
		if (Rioters >= MaxRioters || isBusy || !World.dataReady || onGoingRiot)
		{
			return;
		}
		RoadCreator roadCreator = FindLocation();
		if (!(roadCreator == null))
		{
			riotDuration = 120;
			riotPosition = roadCreator.wPos;
			response = 0;
			if (violence < 10f)
			{
				Violence = 0.5f;
			}
			else if (violence < 30f)
			{
				Violence = 0.1f;
			}
			else
			{
				Violence = 0.01f;
			}
			Debug.Log("Violence= " + violence);
			StartCoroutine(OrganizeRiot(roadCreator));
			onGoingRiot = true;
			messages.NewRiotMessage(rioters, LOCpol, LOCreason, Violence);
			if (!messages.achievements[8] && rioters > 249 && violence < 5f)
			{
				SteamAchievements.script.UnlockAchievement("riot");
				messages.achievements[8] = true;
			}
		}
	}

	private IEnumerator OrganizeRiot(RoadCreator rc)
	{
		isBusy = true;
		StartCoroutine(Clock());
		for (int x = (int)rc.aPos.x - spreadRadius; x <= (int)rc.aPos.x + spreadRadius; x++)
		{
			for (int z = (int)rc.aPos.z - spreadRadius; z <= (int)rc.aPos.z + spreadRadius; z++)
			{
				RoadCreator ifNotNull = roading.GetIfNotNull(x, 1, z);
				if (!(ifNotNull == null) && (ifNotNull.rType == RoadCreator.RoadType.AVENUE || ifNotNull.rType == RoadCreator.RoadType.ROAD))
				{
					StartCoroutine(SpreadRiotTo(ifNotNull));
					yield return new WaitForSeconds(2f);
				}
			}
		}
		isBusy = false;
	}

	private void SpawnProp(Vector3 pos)
	{
		int num = Random.Range(0, 360);
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(Dico.Prop_Riot[Random.Range(0, Dico.Prop_Riot.Length)]));
		gameObject.transform.position = pos;
		gameObject.transform.Rotate(0f, num, 0f, Space.Self);
		props.Add(gameObject);
	}

	private void SpawnRioter(Vector2 pos, RoadCreator _rc)
	{
		if (rioterBank.Count > 0)
		{
			GameObject gameObject = rioterBank[0];
			gameObject.SetActive(value: true);
			gameObject.transform.position = new Vector3(_rc.wPos.x + pos.x, _rc.wPos.y, _rc.wPos.z + pos.y);
			citizens.Add(gameObject);
			rioterBank.Remove(gameObject);
		}
		else
		{
			int num = Random.Range(0, 360);
			string text = Dico.Rioter[Random.Range(0, Dico.Rioter.Length)];
			GameObject gameObject2 = (GameObject)Object.Instantiate(Resources.Load("Pedestrian/Rioter/" + text));
			gameObject2.name = text;
			gameObject2.transform.position = new Vector3(_rc.wPos.x + pos.x, _rc.wPos.y, _rc.wPos.z + pos.y);
			gameObject2.transform.Rotate(0f, num, 0f, Space.Self);
			gameObject2.transform.parent = tCars.transform;
			citizens.Add(gameObject2);
		}
	}

	private IEnumerator SpreadRiotTo(RoadCreator rc)
	{
		if (rc == null || Rioters > MaxRioters || rc.waypoint == null || ((rc.cc < 5 || rc.cc > 16) && rc.tile != "center" && rc.tile != "side" && rc.tile != "straight" && rc.tile != "curvecenter"))
		{
			yield break;
		}
		int r = Random.Range(15, 30);
		if (r < 25)
		{
			SpawnProp(new Vector3(rc.wPos.x + Random.Range(-0.5f, 0.5f), rc.wPos.y, rc.wPos.z + Random.Range(-0.5f, 0.5f)));
		}
		for (int i = 0; i <= r; i++)
		{
			Vector2 pos = Dico.RioterPos[Random.Range(0, Dico.RioterPos.Count)];
			pos += new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f));
			SpawnRioter(pos, rc);
			Rioters++;
			if (Rioters > MaxRioters)
			{
				break;
			}
			yield return new WaitForSeconds(0.3f);
		}
	}

	private RoadCreator FindLocation()
	{
		for (int i = 0; i < 40; i++)
		{
			RoadCreator roadCreator = roading.w.Roads[Random.Range(0, roading.w.Roads.Count)];
			if (roadCreator.cc >= 5 && roadCreator.cc <= 16 && roadCreator.TrafficSlipsal >= 0 && roadCreator.rType != RoadCreator.RoadType.HIGHWAY && roadCreator.rType != RoadCreator.RoadType.PATH && roadCreator.rType != RoadCreator.RoadType.RAIL)
			{
				return roadCreator;
			}
			if (i == 39)
			{
				Debug.Log("Could not find location for riot");
				return null;
			}
		}
		return null;
	}

	private IEnumerator Clock()
	{
		for (int i = 0; i < riotDuration; i++)
		{
			float value = Random.value;
			if (Violence + value > 1f)
			{
				ThrowMolotov();
			}
			yield return new WaitForSeconds(1f);
		}
		StartCoroutine(StopRiot());
	}

	private void ThrowMolotov()
	{
		if (citizens.Count < 10)
		{
			return;
		}
		GameObject gameObject = citizens[Random.Range(0, citizens.Count)];
		if (!(gameObject == null))
		{
			GameObject gameObject2 = (GameObject)Object.Instantiate(Resources.Load("Pedestrian/MolotovActiv"));
			Molotov component = gameObject2.GetComponent<Molotov>();
			gameObject2.transform.position = gameObject.transform.position;
			component.rb.useGravity = true;
			gameObject2.transform.parent = tCars.transform;
			component.w = roading.w;
			float num = Random.Range(0.5f, 2f);
			if (Random.value > 0.5f)
			{
				num = 0f - num;
			}
			float num2 = Random.Range(0.5f, 2f);
			if (Random.value > 0.5f)
			{
				num2 = 0f - num2;
			}
			float y = Random.Range(160, 260);
			Vector3 vector = force;
			component.rb.AddForce(new Vector3(num * vector.x, y, num2 * vector.z));
		}
	}

	private IEnumerator StopRiot()
	{
		Debug.Log("Riot Stopping...");
		foreach (GameObject item in props.ToList())
		{
			if (!(item == null))
			{
				Object.Destroy(item);
				yield return new WaitForSeconds(2f);
			}
		}
		foreach (GameObject item2 in citizens.ToList())
		{
			if (!(item2 == null))
			{
				rioterBank.Add(item2);
				item2.SetActive(value: false);
				yield return new WaitForSeconds(0.1f);
			}
		}
		messages.RiotBox.SetActive(value: false);
		riotPosition = Vector3.zero;
		props.Clear();
		citizens.Clear();
		Rioters = 0;
		yield return new WaitForEndOfFrame();
		onGoingRiot = false;
	}
}
