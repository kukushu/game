using System.Collections;
using UnityEngine;

public class BoatSpawner : MonoBehaviour
{
	private int spawnDir;

	public int Rotation;

	private ServiceCreator sc;

	public World w;

	private void Start()
	{
		StartCoroutine(WaitForInitialization());
	}

	private IEnumerator WaitForInitialization()
	{
		yield return new WaitForSecondsRealtime(5f);
		sc = GetComponentInParent<ServiceCreator>();
		if (sc == null)
		{
			Debug.LogError("Can't initialize boat spawner! Check serviceCreator ref.");
			yield break;
		}
		if (w == null)
		{
			GameObject gameObject = GameObject.Find("World");
			w = gameObject.GetComponent<World>();
		}
		if (sc.Orientation == 0)
		{
			spawnDir = 1;
		}
		else if (sc.Orientation == 90)
		{
			spawnDir = 0;
		}
		else if (sc.Orientation == 180)
		{
			spawnDir = 3;
		}
		else
		{
			spawnDir = 1;
		}
		StartCoroutine(SpawnBoat());
	}

	private IEnumerator SpawnBoat()
	{
		while (true)
		{
			yield return new WaitForSeconds(10f);
			if (sc != null && sc.carsOut < 2 && w != null)
			{
				float value = Random.value;
				string path;
				if (value < 0.33f)
				{
					path = "Vehicles/BoatBlue";
				}
				else
				{
					path = ((!(value < 0.66f)) ? "Vehicles/BoatWhite" : "Vehicles/BoatRed");
				}
				GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(path));
				gameObject.name = "Voilier";
				Vector3 position = new Vector3(transform.position.x, 30.02f, transform.position.z);
				gameObject.transform.position = position;
				Boat component = gameObject.GetComponent<Boat>();
				component.boatSize = 2;
				component.sc = sc;
				component.w = w;
				component.Direction = spawnDir;
				sc.carsOut++;
			}
		}
	}
}
