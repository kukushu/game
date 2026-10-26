using System.Collections;
using UnityEngine;

public class FireCoroutine : MonoBehaviour
{
	public LotCreator lotOnFire;

	public AudioSource audioSource;

	public AudioSource collapse;

	public VehicleWP vehicle;

	public bool firetruckenroute;

	private bool fireStopped;

	private GameObject fireTruckOnScene;

	private void Start()
	{
		audioSource.volume = SettingsStatic.CityVolume;
		if (!lotOnFire.isRubles)
		{
			StartCoroutine(Process());
		}
	}

	private IEnumerator Process()
	{
		yield return new WaitForFixedUpdate();
		if (lotOnFire == null)
		{
			Object.Destroy(transform.gameObject);
			yield break;
		}
		lotOnFire.isRubles = true;
		if (!lotOnFire.isDecayed)
		{
			lotOnFire.DecayLot(toParking: false);
		}
		for (int i = 0; i < 2000; i++)
		{
			if (i < 500 && firetruckenroute && vehicle == null)
			{
				InstantiateExtinguisher();
				firetruckenroute = false;
			}
			if (i == 500 && firetruckenroute)
			{
				if (vehicle != null)
				{
					vehicle.DestroyCar();
				}
				InstantiateExtinguisher();
				firetruckenroute = false;
			}
			if (i == 1000 && fireStopped)
			{
				if (fireTruckOnScene != null)
				{
					Object.Destroy(fireTruckOnScene);
				}
				Object.Destroy(transform.gameObject);
				yield break;
			}
			if (i == 1760 && !fireStopped)
			{
				if (lotOnFire == null)
				{
					Object.Destroy(transform.gameObject);
					yield break;
				}
				StartCoroutine(lotOnFire.RublesTransform());
				collapse.volume = SettingsStatic.CityVolume;
				collapse.Play();
			}
			yield return new WaitForFixedUpdate();
		}
		yield return new WaitForSeconds(5f);
		Object.Destroy(fireTruckOnScene);
		Object.Destroy(transform.gameObject);
	}

	private void InstantiateExtinguisher()
	{
		if (lotOnFire == null)
		{
			Object.Destroy(transform.gameObject);
			return;
		}
		_ = lotOnFire.Entry;
		fireStopped = true;
		lotOnFire.isRubles = false;
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Vehicles/Extinguisher"));
		gameObject.transform.position = new Vector3(lotOnFire.Entry.x, lotOnFire.wP.y + 0.504f, lotOnFire.Entry.y);
		gameObject.transform.Rotate(0f, lotOnFire.Orientation, 0f, Space.Self);
		gameObject.transform.parent = lotOnFire.gameObject.transform;
		fireTruckOnScene = gameObject;
	}
}
