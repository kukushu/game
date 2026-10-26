using System.Collections;
using UnityEngine;

public class WeatherController : MonoBehaviour
{
	public static int CloudsAlive;

	public int MaxClouds = 5;

	private int worldSize = 8;

	private void Start()
	{
		worldSize = World.worldSize;
		StartCoroutine(CreateCloud());
	}

	private IEnumerator CreateCloud()
	{
		while (true)
		{
			if (CloudsAlive < MaxClouds)
			{
				GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Skybox/HigherClouds"));
				CloudsAlive++;
				float z = Random.Range(0f, worldSize * 16);
				gameObject.transform.position = new Vector3(-10f, 30f, z);
			}
			yield return new WaitForSeconds(10f);
		}
	}
}
