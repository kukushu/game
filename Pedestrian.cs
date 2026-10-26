using System.Collections;
using AnimationInstancing;
using UnityEngine;

public class Pedestrian : MonoBehaviour
{
	public float speed = 0.24f;

	public Vector3 currentPos = Vector3.zero;

	public int life = 64;

	public bool destroyLaunched;

	private float pOffset;

	public byte listN;

	public byte pClass;

	public Roading roading;

	public RoadCreator currentRC;

	public LotCreator lotOwner;

	public GameObject transform1;

	public GameObject transform2;

	public global::AnimationInstancing.AnimationInstancing animationInst;

	public Vector3 Destination;

	public Vector3 Offset = Vector3.zero;

	public Vector2 Direction = new Vector2(1f, 1f);

	private float sidewalk;

	private Transform camTransform;

	private Transform trensform;

	private int count;

	private Vector3 screenPos;

	private bool onScreen;

	public int secInvisible;

	private void OnEnable()
	{
		life = 60 + Random.Range(-20, 20);
		destroyLaunched = false;
		trensform = GetComponent<Transform>();
		StartCoroutine(WaitForRC());
	}

	private IEnumerator WaitForRC()
	{
		int count = 0;
		while (currentRC == null)
		{
			count++;
			if (count > 10)
			{
				Debug.Log("NO RC! ERROR!");
				yield break;
			}
			yield return null;
		}
		pOffset = Random.Range(-0.08f, 0.08f);
		TrafficManager.peds++;
		if (!Indexer.copterMode)
		{
			camTransform = roading.cam.transform;
		}
		else if (roading.w.indexer.carCamera.activeSelf)
		{
			camTransform = roading.w.indexer.cameraCar.transform;
		}
		InitialPosition();
		yield return new WaitForEndOfFrame();
		if (transform1 != null)
		{
			Object.Destroy(transform1);
		}
		if (transform2 != null)
		{
			Object.Destroy(transform2);
		}
	}

	private void InitialPosition()
	{
		FindInitialPosition();
		trensform.position = new Vector3(currentRC.wPos.x + Offset.x, currentRC.wPos.y + 0.08f, currentRC.wPos.z + Offset.z);
		Destination = trensform.position;
	}

	public void UpdatePedestrian()
	{
		currentPos = trensform.position;
		count++;
		if (count > 30)
		{
			CheckVisibility();
		}
		UpdateDestination();
		trensform.LookAt(Destination);
		trensform.position = Vector3.MoveTowards(trensform.position, Destination, speed * Time.deltaTime);
	}

	public void CheckVisibility()
	{
		if (!Indexer.copterMode)
		{
			screenPos = roading.cam.WorldToScreenPoint(transform.position);
		}
		else if (roading.w.indexer.carCamera.activeSelf)
		{
			screenPos = roading.w.indexer.cameraCar.WorldToScreenPoint(transform.position);
		}
		onScreen = screenPos.x > 0f && screenPos.x < (float)Screen.width && screenPos.y > 0f && screenPos.y < (float)Screen.height;
		if (camTransform != null && Vector3.Distance(camTransform.position, trensform.position) > (float)SettingsStatic.pedsDistance)
		{
			onScreen = false;
		}
		if (onScreen)
		{
			animationInst.PlayAnimation(0);
		}
		else
		{
			secInvisible++;
			animationInst.Stop();
		}
		count = 0;
	}

	public void DestroyPed()
	{
		if (!destroyLaunched)
		{
			destroyLaunched = true;
			if (listN == 1)
			{
				TrafficManager.peds_A.Remove(this);
			}
			else
			{
				TrafficManager.peds_B.Remove(this);
			}
			if (pClass == 0)
			{
				TrafficManager.lowPeds.Add(this);
			}
			else if (pClass == 1)
			{
				TrafficManager.midPeds.Add(this);
			}
			else
			{
				TrafficManager.hiPeds.Add(this);
			}
			lotOwner.pedsOut--;
			TrafficManager.peds--;
			currentRC = null;
			gameObject.SetActive(value: false);
		}
	}

	private void UpdateDestination()
	{
		if (!(Vector3.Distance(currentPos, Destination) < 0.05f))
		{
			return;
		}
		life--;
		RoadCreator roadCreator = FindDestination(currentRC);
		if (roadCreator == null)
		{
			life = 0;
			return;
		}
		if (Direction.x == 1f)
		{
			Offset.x = 0.5f;
		}
		else if (Direction.x == -1f)
		{
			Offset.x = -0.5f;
		}
		else if (Direction.y == 1f)
		{
			Offset.z = 0.5f;
		}
		else
		{
			Offset.z = -0.5f;
		}
		if (roadCreator.tile == "side25in" || roadCreator.tile == "straight25in" || roadCreator.tile == "side25" || roadCreator.tile == "straight25")
		{
			if (roadCreator.Orientation == 270f || roadCreator.Orientation == 0f)
			{
				if (Direction.y == 1f || Direction.x == 1f)
				{
					Offset.y = 0.25f;
				}
				else
				{
					Offset.y = 0f;
				}
			}
			else if (Direction.y == 1f || Direction.x == 1f)
			{
				Offset.y = 0f;
			}
			else
			{
				Offset.y = 0.25f;
			}
		}
		else if (roadCreator.tile == "side50in" || roadCreator.tile == "straight50in" || roadCreator.tile == "side50" || roadCreator.tile == "straight50")
		{
			if (roadCreator.Orientation == 270f || roadCreator.Orientation == 0f)
			{
				if (Direction.y == 1f || Direction.x == 1f)
				{
					Offset.y = 0.5f;
				}
				else
				{
					Offset.y = 0f;
				}
			}
			else if (Direction.y == 1f || Direction.x == 1f)
			{
				Offset.y = 0f;
			}
			else
			{
				Offset.y = 0.5f;
			}
		}
		else
		{
			Offset.y = 0f;
		}
		Offset.y += sidewalk;
		Destination = roadCreator.wPos + Offset;
		currentRC = roadCreator;
		if (life < 10)
		{
			life = 0;
		}
		else if (currentPos.y < 20f)
		{
			life = 0;
		}
	}

	private RoadCreator FindDestination(RoadCreator rc)
	{
		int num = (int)rc.wPos.x;
		int num2 = (int)rc.wPos.z;
		Vector3 offset = new Vector3(0f, 0f, -0.35f + pOffset);
		Vector3 offset2 = new Vector3(0f, 0f, 0.35f + pOffset);
		Vector3 offset3 = new Vector3(-0.35f + pOffset, 0f, 0f);
		Vector3 offset4 = new Vector3(0.35f + pOffset, 0f, 0f);
		RoadCreator roadCreator = null;
		if (rc == null)
		{
			return null;
		}
		sidewalk = 0.02f;
		switch (rc.cc)
		{
		case 0:
			roadCreator = roading.GetIfNotNull((int)((float)num + Direction.x), 1, (int)((float)num2 + Direction.y));
			break;
		case 1:
			if (rc.tile == "curveout")
			{
				if (Direction.y == -1f)
				{
					Direction = new Vector2(1f, 0f);
					Offset = offset;
				}
				else if (Direction.x == -1f)
				{
					Direction = new Vector2(0f, 1f);
					Offset = offset3;
				}
			}
			else if (rc.tile == "precurve")
			{
				Direction = new Vector2(1f, 0f);
				Offset = offset;
			}
			roadCreator = roading.GetIfNotNull((int)((float)num + Direction.x), 1, (int)((float)num2 + Direction.y));
			break;
		case 2:
			if (rc.tile == "curveout")
			{
				if (Direction.y == 1f)
				{
					Direction = new Vector2(-1f, 0f);
					Offset = offset2;
				}
				else if (Direction.x == 1f)
				{
					Direction = new Vector2(0f, -1f);
					Offset = offset4;
				}
			}
			else if (rc.tile == "precurve")
			{
				Direction = new Vector2(-1f, 0f);
				Offset = offset2;
			}
			roadCreator = roading.GetIfNotNull((int)((float)num + Direction.x), 1, (int)((float)num2 + Direction.y));
			break;
		case 3:
			if (rc.tile == "curveout")
			{
				if (Direction.y == -1f)
				{
					Direction = new Vector2(-1f, 0f);
					Offset = offset;
				}
				else if (Direction.x == 1f)
				{
					Direction = new Vector2(0f, 1f);
					Offset = offset4;
				}
			}
			else if (rc.tile == "precurve")
			{
				Direction = new Vector2(0f, 1f);
				Offset = offset4;
			}
			roadCreator = roading.GetIfNotNull((int)((float)num + Direction.x), 1, (int)((float)num2 + Direction.y));
			break;
		case 4:
			if (rc.tile == "curveout")
			{
				if (Direction.y == 1f)
				{
					Direction = new Vector2(1f, 0f);
					Offset = offset2;
				}
				else if (Direction.x == -1f)
				{
					Direction = new Vector2(0f, -1f);
					Offset = offset3;
				}
			}
			else if (rc.tile == "precurve")
			{
				Direction = new Vector2(0f, -1f);
				Offset = offset3;
			}
			roadCreator = roading.GetIfNotNull((int)((float)num + Direction.x), 1, (int)((float)num2 + Direction.y));
			break;
		case 5:
			sidewalk = 0f;
			roadCreator = roading.GetIfNotNull((int)((float)num + Direction.x), 1, (int)((float)num2 + Direction.y));
			break;
		case 6:
			sidewalk = 0f;
			roadCreator = roading.GetIfNotNull((int)((float)num + Direction.x), 1, (int)((float)num2 + Direction.y));
			break;
		case 7:
			sidewalk = 0f;
			roadCreator = roading.GetIfNotNull((int)((float)num + Direction.x), 1, (int)((float)num2 + Direction.y));
			break;
		case 8:
			sidewalk = 0f;
			roadCreator = roading.GetIfNotNull((int)((float)num + Direction.x), 1, (int)((float)num2 + Direction.y));
			break;
		case 9:
			sidewalk = 0f;
			if (Direction.y == -1f)
			{
				roadCreator = roading.GetIfNotNull(num, 1, num2 - 1);
				Offset = offset4;
			}
			else
			{
				roadCreator = roading.GetIfNotNull(num, 1, num2 + 1);
				Offset = offset4;
			}
			break;
		case 10:
			sidewalk = 0f;
			if (Direction.x == -1f)
			{
				roadCreator = roading.GetIfNotNull(num - 1, 1, num2);
				Offset = offset;
			}
			else
			{
				roadCreator = roading.GetIfNotNull(num + 1, 1, num2);
				Offset = offset;
			}
			break;
		case 11:
			sidewalk = 0f;
			if (Direction.y == -1f)
			{
				roadCreator = roading.GetIfNotNull(num, 1, num2 - 1);
				Offset = offset3;
			}
			else
			{
				roadCreator = roading.GetIfNotNull(num, 1, num2 + 1);
				Offset = offset3;
			}
			break;
		case 12:
			sidewalk = 0f;
			if (Direction.x == -1f)
			{
				roadCreator = roading.GetIfNotNull(num - 1, 1, num2);
				Offset = offset2;
			}
			else
			{
				roadCreator = roading.GetIfNotNull(num + 1, 1, num2);
				Offset = offset2;
			}
			break;
		}
		if (roadCreator == null || roadCreator == rc || life < 0)
		{
			life = 0;
			return null;
		}
		return roadCreator;
	}

	private void FindInitialPosition()
	{
		Vector3 offset = new Vector3(0f, 0f, -0.35f + pOffset);
		Vector3 offset2 = new Vector3(0f, 0f, 0.35f + pOffset);
		Vector3 offset3 = new Vector3(-0.35f + pOffset, 0f, 0f);
		Vector3 offset4 = new Vector3(0.35f + pOffset, 0f, 0f);
		float value = Random.value;
		if (currentRC.cc == 1)
		{
			if (value < 0.5f)
			{
				Offset = offset;
				Direction = new Vector2(1f, 0f);
			}
			else
			{
				Offset = offset;
				Direction = new Vector2(-1f, 0f);
			}
		}
		else if (currentRC.cc == 2)
		{
			if (value < 0.5f)
			{
				Offset = offset2;
				Direction = new Vector2(1f, 0f);
			}
			else
			{
				Offset = offset2;
				Direction = new Vector2(-1f, 0f);
			}
		}
		else if (currentRC.cc == 3)
		{
			if (value < 0.5f)
			{
				Offset = offset4;
				Direction = new Vector2(0f, 1f);
			}
			else
			{
				Offset = offset4;
				Direction = new Vector2(0f, -1f);
			}
		}
		else if (value < 0.5f)
		{
			Offset = offset3;
			Direction = new Vector2(0f, 1f);
		}
		else
		{
			Offset = offset3;
			Direction = new Vector2(0f, -1f);
		}
	}
}
