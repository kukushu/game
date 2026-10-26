using System.Collections;
using UnityEngine;

public class PlaneSpawner : MonoBehaviour
{
	private int Delay;

	public int Rotation;

	private ServiceCreator sc;

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
			Debug.LogError("Can't initialize plane spawner! Check serviceCreator ref.");
			yield break;
		}
		int num = sc.MaxEmployees - sc.HiredEmployees;
		if (sc.Stage == 0)
		{
			Delay = Random.Range(80 - num, 90 - num);
		}
		else if (sc.Stage == 1)
		{
			Delay = Random.Range(70 - num, 100 - num);
		}
		else if (sc.Stage == 2)
		{
			Delay = Random.Range(60 - num, 90 - num);
		}
		else if (sc.Stage == 3)
		{
			Delay = Random.Range(50 - num, 80 - num);
		}
		else
		{
			Delay = Random.Range(50 - num, 60 - num);
		}
		if (Delay < 5)
		{
			Delay = 5;
		}
		StartCoroutine(SpawnPlane());
	}

	private IEnumerator SpawnPlane()
	{
		while (true)
		{
			yield return new WaitForSeconds(Delay);
			if (sc != null && sc.open)
			{
				GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Vehicles/PlaneA"));
				gameObject.name = "Plane";
				gameObject.transform.position = transform.position;
				float num = sc.Orientation - 90 + Rotation;
				gameObject.transform.Rotate(new Vector3(0f, num, 0f));
				Airplane component = gameObject.GetComponent<Airplane>();
				if (num == 360f)
				{
					num = 0f;
				}
				component.Rotation = (int)num;
			}
			yield return new WaitForSeconds(1f);
		}
	}
}
