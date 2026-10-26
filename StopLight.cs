using System.Collections;
using UnityEngine;

public class StopLight : MonoBehaviour
{
	private int Orientation;

	private int _intersection;

	public float uniqueR;

	public GameObject greenLight;

	public GameObject orangeLight;

	public GameObject redLight;

	private bool green;

	private bool orange;

	private bool red;

	private bool initialized;

	public RoadCreator baseRc;

	public RoadCreator rc;

	public RoadCreator rcMid;

	private Roading roading;

	public AudioSource audioSource;

	private void Start()
	{
		initialized = false;
		StartCoroutine(WaitForInitialization());
	}

	private void Update()
	{
		if (_intersection != TrafficManager.intersection && initialized)
		{
			SwitchLight();
		}
	}

	private IEnumerator WaitForInitialization()
	{
		yield return new WaitForSecondsRealtime(2f);
		uniqueR = Random.value / 2f;
		Initialize();
	}

	private void SwitchLight()
	{
		if (rc == null)
		{
			return;
		}
		if (Orientation == TrafficManager.intersection)
		{
			red = !red;
			redLight.SetActive(red);
			green = !green;
			greenLight.SetActive(green);
			rc.isRed = false;
			if (rcMid != null)
			{
				rcMid.isRed = false;
			}
			StartCoroutine(OrangeLight());
		}
		else if (Orientation == TrafficManager.lastintersection && !rc.isRed)
		{
			orange = !orange;
			orangeLight.SetActive(orange);
			red = !red;
			redLight.SetActive(red);
			rc.isRed = true;
			if (rcMid != null)
			{
				rcMid.isRed = true;
			}
		}
		_intersection = TrafficManager.intersection;
	}

	private void Initialize()
	{
		baseRc = GetComponent<RoadCreator>();
		if (baseRc == null)
		{
			Debug.Log("Can not find rc component");
		}
		bool flag = false;
		if (baseRc.rType == RoadCreator.RoadType.AVENUE)
		{
			flag = true;
		}
		Orientation = (int)baseRc.Orientation;
		roading = World.staticWorld.buldoze.roading;
		if (baseRc.cc == 5)
		{
			rc = roading.rD[(int)baseRc.aPos.x + 1, 1, (int)baseRc.aPos.z];
			if (flag)
			{
				rcMid = roading.rD[(int)baseRc.aPos.x + 1, 1, (int)baseRc.aPos.z - 1];
			}
		}
		else if (baseRc.cc == 6)
		{
			rc = roading.rD[(int)baseRc.aPos.x, 1, (int)baseRc.aPos.z - 1];
			if (flag)
			{
				rcMid = roading.rD[(int)baseRc.aPos.x - 1, 1, (int)baseRc.aPos.z - 1];
			}
		}
		else if (baseRc.cc == 7)
		{
			rc = roading.rD[(int)baseRc.aPos.x - 1, 1, (int)baseRc.aPos.z];
			if (flag)
			{
				rcMid = roading.rD[(int)baseRc.aPos.x - 1, 1, (int)baseRc.aPos.z + 1];
			}
		}
		else if (baseRc.cc == 8)
		{
			rc = roading.rD[(int)baseRc.aPos.x, 1, (int)baseRc.aPos.z + 1];
			if (flag)
			{
				rcMid = roading.rD[(int)baseRc.aPos.x + 1, 1, (int)baseRc.aPos.z + 1];
			}
		}
		if (rc == null)
		{
			return;
		}
		if (Orientation == 270)
		{
			rc.dir = RoadCreator.Direction.downZ;
			if (flag)
			{
				rcMid.dir = RoadCreator.Direction.downZ;
			}
		}
		else if (Orientation == 0)
		{
			rc.dir = RoadCreator.Direction.downX;
			if (flag)
			{
				rcMid.dir = RoadCreator.Direction.downX;
			}
		}
		else if (Orientation == 90)
		{
			rc.dir = RoadCreator.Direction.upZ;
			if (flag)
			{
				rcMid.dir = RoadCreator.Direction.upZ;
			}
		}
		else
		{
			rc.dir = RoadCreator.Direction.upX;
			if (flag)
			{
				rcMid.dir = RoadCreator.Direction.upX;
			}
		}
		if (greenLight == null)
		{
			new GameObject().transform.parent = transform;
			greenLight = new GameObject();
			new GameObject().transform.parent = transform;
			redLight = new GameObject();
			new GameObject().transform.parent = transform;
			orangeLight = new GameObject();
		}
		greenLight.SetActive(value: false);
		orangeLight.SetActive(value: false);
		redLight.SetActive(value: false);
		if (Orientation == TrafficManager.intersection)
		{
			orange = !orange;
			orangeLight.SetActive(orange);
			rc.isRed = false;
			if (rcMid != null)
			{
				rcMid.isRed = false;
			}
		}
		else
		{
			red = !red;
			redLight.SetActive(red);
			rc.isRed = true;
			if (rcMid != null)
			{
				rcMid.isRed = true;
			}
		}
		_intersection = TrafficManager.intersection;
		audioSource = GetComponent<AudioSource>();
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
		}
		audioSource.playOnAwake = false;
		audioSource.volume = SettingsStatic.CityVolume * 0.5f;
		audioSource.maxDistance = 65f;
		audioSource.spatialBlend = 1f;
		audioSource.rolloffMode = AudioRolloffMode.Custom;
		audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, roading.w.distanceCurve);
		if (uniqueR > 0.125f)
		{
			audioSource.enabled = false;
		}
		ReloadLightMaterial();
		initialized = true;
	}

	private IEnumerator OrangeLight()
	{
		yield return new WaitForSeconds(8f);
		if (audioSource.enabled)
		{
			float num = Random.value / 2f + uniqueR * 4f;
			if (audioSource.enabled && num < 0.75f && !rc.waypoint.free)
			{
				RandomHonk(num + uniqueR);
			}
		}
		orange = !orange;
		orangeLight.SetActive(orange);
		green = !green;
		greenLight.SetActive(green);
	}

	private void RandomHonk(float r)
	{
		if (r < 0.04f)
		{
			audioSource.clip = roading.la.HonkA;
		}
		else if (r < 0.08f)
		{
			audioSource.clip = roading.la.HonkB;
		}
		else if (r < 0.12f)
		{
			audioSource.clip = roading.la.HonkC;
		}
		else if (r < 0.16f)
		{
			audioSource.clip = roading.la.HonkD;
		}
		else if (r < 0.2f)
		{
			audioSource.clip = roading.la.HonkE;
		}
		else if (r < 0.24f)
		{
			audioSource.clip = roading.la.HonkF;
		}
		else if (r < 0.28f)
		{
			audioSource.clip = roading.la.HonkG;
		}
		else if (r < 0.32f)
		{
			audioSource.clip = roading.la.HonkH;
		}
		else if (r < 0.38f)
		{
			audioSource.clip = roading.la.BrownNoise;
		}
		else if (r < 0.44f)
		{
			audioSource.clip = roading.la.BrownNoise2;
		}
		else if (r < 0.5f)
		{
			audioSource.clip = roading.la.BrownNoise3;
		}
		else if (r < 0.56f)
		{
			audioSource.clip = roading.la.AccelerationA;
		}
		else if (r < 0.62f)
		{
			audioSource.clip = roading.la.AccelerationB;
		}
		else if (r < 0.68f)
		{
			audioSource.clip = roading.la.AccelerationC;
		}
		else if (r < 0.74f)
		{
			audioSource.clip = roading.la.AccelerationD;
		}
		else if (r < 0.8f)
		{
			audioSource.clip = roading.la.PedsA;
		}
		else if (r < 0.86f)
		{
			audioSource.clip = roading.la.PedsB;
		}
		else if (r < 0.92f)
		{
			audioSource.clip = roading.la.CarAlarm;
		}
		else
		{
			audioSource.clip = roading.la.Brakes;
		}
		audioSource.Play();
	}

	private void ReloadLightMaterial()
	{
		SpriteRenderer[] componentsInChildren = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].sharedMaterial = roading.trafficLightMat;
		}
	}
}
