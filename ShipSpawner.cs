using System.Collections;
using UnityEngine;

public class ShipSpawner : MonoBehaviour
{
	private int spawnDir;

	private int Rotation;

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
		switch (Mathf.RoundToInt(sc.transform.eulerAngles.y))
		{
		case 0:
			spawnDir = 0;
			Rotation = 90;
			break;
		case 90:
			spawnDir = 2;
			Rotation = 0;
			break;
		case 180:
			spawnDir = 1;
			Rotation = 270;
			break;
		default:
			spawnDir = 3;
			Rotation = 180;
			break;
		}
		StartCoroutine(SpawnBoat());
	}

	private IEnumerator SpawnBoat()
	{
		while (true)
		{
			yield return new WaitForSeconds(10f);
			if (sc != null && sc.carsOut < 1 && w != null)
			{
				GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Vehicles/Cargo"));
				gameObject.name = "Cargo";
				gameObject.transform.position = new Vector3(transform.position.x, 30.02f, transform.position.z);
				gameObject.transform.Rotate(new Vector3(0f, Rotation, 0f));
				Boat boatAI = gameObject.GetComponent<Boat>();
				boatAI.sc = sc;
				boatAI.w = w;
				boatAI.Direction = spawnDir;
				boatAI.boatSize = 8;
				boatAI.enabled = false;
				sc.carsOut++;
				yield return new WaitForSeconds(5f);
				boatAI.enabled = true;
			}
		}
	}
}
