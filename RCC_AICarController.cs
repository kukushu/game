using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(RCC_CarControllerV3))]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/AI/RCC AI Car Controller")]
public class RCC_AICarController : MonoBehaviour
{
	public enum NavigationMode
	{
		FollowWaypoints,
		ChaseTarget,
		FollowTarget
	}

	public delegate void onRCCAISpawned(RCC_AICarController RCCAI);

	public delegate void onRCCAIDestroyed(RCC_AICarController RCCAI);

	internal RCC_CarControllerV3 carController;

	public RCC_AIWaypointsContainer waypointsContainer;

	public int currentWaypointIndex;

	public Transform targetChase;

	public string targetTag = "Player";

	public NavigationMode navigationMode;

	[Range(5f, 30f)]
	public float raycastLength = 3f;

	[Range(10f, 90f)]
	public float raycastAngle = 30f;

	public LayerMask obstacleLayers = -1;

	public GameObject obstacle;

	public bool useRaycasts = true;

	private float rayInput;

	private bool raycasting;

	private float resetTime;

	private bool reversingNow;

	public float steerInput;

	public float throttleInput;

	public float brakeInput;

	public float handbrakeInput;

	public bool limitSpeed;

	public float maximumSpeed = 100f;

	public bool smoothedSteer = true;

	public int lap;

	public int totalWaypointPassed;

	public int nextWaypointPassDistance = 20;

	public int chaseDistance = 200;

	public int startFollowDistance = 300;

	public int stopFollowDistance = 30;

	public bool ignoreWaypointNow;

	private NavMeshAgent navigator;

	private SphereCollider detector;

	public List<Transform> targetsInZone = new List<Transform>();

	public static event onRCCAISpawned OnRCCAISpawned;

	public static event onRCCAIDestroyed OnRCCAIDestroyed;

	private void Awake()
	{
		carController = GetComponent<RCC_CarControllerV3>();
		carController.externalController = true;
		if (!waypointsContainer)
		{
			waypointsContainer = Object.FindObjectOfType(typeof(RCC_AIWaypointsContainer)) as RCC_AIWaypointsContainer;
		}
		GameObject gameObject = new GameObject("Navigator");
		gameObject.transform.SetParent(transform, worldPositionStays: false);
		navigator = gameObject.AddComponent<NavMeshAgent>();
		navigator.radius = 1f;
		navigator.speed = 1f;
		navigator.angularSpeed = 100000f;
		navigator.acceleration = 100000f;
		navigator.height = 1f;
		navigator.avoidancePriority = 0;
		GameObject gameObject2 = new GameObject("Detector");
		gameObject2.transform.SetParent(transform, worldPositionStays: false);
		gameObject2.layer = LayerMask.NameToLayer("Ignore Raycast");
		detector = gameObject2.gameObject.AddComponent<SphereCollider>();
		detector.isTrigger = true;
		detector.radius = 10f;
	}

	private void OnEnable()
	{
		if (OnRCCAISpawned != null)
		{
			OnRCCAISpawned(this);
		}
	}

	private void Update()
	{
		if (carController.canControl)
		{
			navigator.transform.localPosition = Vector3.zero;
			navigator.transform.localPosition += Vector3.forward * carController.FrontLeftWheelCollider.transform.localPosition.z;
			CheckTargets();
		}
	}

	private void FixedUpdate()
	{
		if (carController.canControl)
		{
			if (useRaycasts)
			{
				FixedRaycasts();
			}
			Navigation();
			CheckReset();
			FeedRCC();
		}
	}

	private void Navigation()
	{
		float num = Mathf.Clamp(transform.InverseTransformDirection(navigator.desiredVelocity).x * 1.5f, -1f, 1f);
		switch (navigationMode)
		{
		case NavigationMode.FollowWaypoints:
		{
			if (!waypointsContainer)
			{
				Debug.LogError("Waypoints Container Couldn't Found!");
				Stop();
				return;
			}
			if ((bool)waypointsContainer && waypointsContainer.waypoints.Count < 1)
			{
				Debug.LogError("Waypoints Container Doesn't Have Any Waypoints!");
				Stop();
				return;
			}
			RCC_Waypoint rCC_Waypoint = waypointsContainer.waypoints[currentWaypointIndex];
			float pathLength2 = GetPathLength(navigator.path);
			if (!navigator.hasPath)
			{
				navigator.SetDestination(waypointsContainer.waypoints[currentWaypointIndex].transform.position);
			}
			if (pathLength2 != 0f && pathLength2 < (float)nextWaypointPassDistance)
			{
				currentWaypointIndex++;
				totalWaypointPassed++;
				if (currentWaypointIndex >= waypointsContainer.waypoints.Count)
				{
					currentWaypointIndex = 0;
					lap++;
				}
				if (navigator.isOnNavMesh)
				{
					navigator.SetDestination(waypointsContainer.waypoints[currentWaypointIndex].transform.position);
				}
			}
			if (!reversingNow)
			{
				throttleInput = ((pathLength2 < (float)nextWaypointPassDistance * (carController.speed / 30f)) ? Mathf.Clamp01(rCC_Waypoint.targetSpeed - carController.speed) : 1f);
				throttleInput *= Mathf.Clamp01(Mathf.Lerp(10f, 0f, carController.speed / maximumSpeed));
				brakeInput = ((pathLength2 < (float)nextWaypointPassDistance * (carController.speed / 30f)) ? Mathf.Clamp01(carController.speed - rCC_Waypoint.targetSpeed) : 0f);
				handbrakeInput = 0f;
				if (carController.speed > 30f)
				{
					throttleInput -= Mathf.Abs(num) / 3f;
					brakeInput += Mathf.Abs(num) / 3f;
				}
			}
			break;
		}
		case NavigationMode.ChaseTarget:
			detector.radius = chaseDistance;
			if (!targetChase)
			{
				Stop();
				return;
			}
			if (navigator.isOnNavMesh)
			{
				navigator.SetDestination(targetChase.position);
			}
			if (!reversingNow)
			{
				throttleInput = 1f;
				throttleInput *= Mathf.Clamp01(Mathf.Lerp(10f, 0f, carController.speed / maximumSpeed));
				brakeInput = 0f;
				handbrakeInput = 0f;
				if (carController.speed > 30f)
				{
					throttleInput -= Mathf.Abs(num) / 3f;
					brakeInput += Mathf.Abs(num) / 3f;
				}
			}
			break;
		case NavigationMode.FollowTarget:
		{
			detector.radius = startFollowDistance;
			if (!targetChase)
			{
				Stop();
				return;
			}
			if (navigator.isOnNavMesh)
			{
				navigator.SetDestination(targetChase.position);
			}
			float pathLength = GetPathLength(navigator.path);
			if (!reversingNow)
			{
				throttleInput = ((pathLength < (float)stopFollowDistance * Mathf.Lerp(1f, 5f, carController.speed / 50f)) ? Mathf.Lerp(-5f, 1f, pathLength / ((float)stopFollowDistance / 1f)) : 1f);
				throttleInput *= Mathf.Clamp01(Mathf.Lerp(10f, 0f, carController.speed / maximumSpeed));
				brakeInput = ((pathLength < (float)stopFollowDistance * Mathf.Lerp(1f, 5f, carController.speed / 50f)) ? Mathf.Lerp(5f, 0f, pathLength / ((float)stopFollowDistance / 1f)) : 0f);
				handbrakeInput = 0f;
				if (carController.speed > 30f)
				{
					throttleInput -= Mathf.Abs(num) / 3f;
					brakeInput += Mathf.Abs(num) / 3f;
				}
				if (throttleInput < 0.05f)
				{
					throttleInput = 0f;
				}
				if (brakeInput < 0.05f)
				{
					brakeInput = 0f;
				}
			}
			break;
		}
		}
		steerInput = (ignoreWaypointNow ? rayInput : (num + rayInput));
		steerInput = Mathf.Clamp(steerInput, -1f, 1f) * (float)carController.direction;
		throttleInput = Mathf.Clamp01(throttleInput);
		brakeInput = Mathf.Clamp01(brakeInput);
		handbrakeInput = Mathf.Clamp01(handbrakeInput);
		if (reversingNow)
		{
			throttleInput = 0f;
			brakeInput = 1f;
			handbrakeInput = 0f;
		}
		else if (carController.speed < 5f && brakeInput >= 0.5f)
		{
			brakeInput = 0f;
			handbrakeInput = 1f;
		}
	}

	private void CheckReset()
	{
		if (navigationMode == NavigationMode.FollowTarget && GetPathLength(navigator.path) < (float)startFollowDistance)
		{
			reversingNow = false;
			resetTime = 0f;
			return;
		}
		if (carController.speed <= 5f && transform.InverseTransformDirection(carController.rigid.velocity).z < 1f)
		{
			resetTime += Time.deltaTime;
		}
		if (resetTime >= 2f)
		{
			reversingNow = true;
		}
		if (resetTime >= 4f || carController.speed >= 25f)
		{
			reversingNow = false;
			resetTime = 0f;
		}
	}

	private void FixedRaycasts()
	{
		int[] array = new int[5]
		{
			0,
			Mathf.FloorToInt(raycastAngle / 3f),
			Mathf.FloorToInt(raycastAngle / 1f),
			-Mathf.FloorToInt(raycastAngle / 1f),
			-Mathf.FloorToInt(raycastAngle / 3f)
		};
		Vector3 position = transform.position;
		position += transform.forward * carController.FrontLeftWheelCollider.transform.localPosition.z;
		rayInput = 0f;
		bool flag = false;
		for (int i = 0; i < array.Length; i++)
		{
			Debug.DrawRay(position, Quaternion.AngleAxis(array[i], transform.up) * transform.forward * raycastLength, Color.white);
			if (!Physics.Raycast(position, Quaternion.AngleAxis(array[i], transform.up) * transform.forward, out var hitInfo, raycastLength, obstacleLayers) || hitInfo.collider.isTrigger || !(hitInfo.transform.root != transform))
			{
				continue;
			}
			switch (navigationMode)
			{
			case NavigationMode.FollowWaypoints:
				Debug.DrawRay(position, Quaternion.AngleAxis(array[i], transform.up) * transform.forward * raycastLength, Color.red);
				flag = true;
				if (i != 0)
				{
					rayInput -= Mathf.Lerp(Mathf.Sign(array[i]), 0f, hitInfo.distance / raycastLength);
				}
				break;
			case NavigationMode.ChaseTarget:
				if ((bool)targetChase && hitInfo.transform != targetChase && !hitInfo.transform.IsChildOf(targetChase))
				{
					Debug.DrawRay(position, Quaternion.AngleAxis(array[i], transform.up) * transform.forward * raycastLength, Color.red);
					flag = true;
					if (i != 0)
					{
						rayInput -= Mathf.Lerp(Mathf.Sign(array[i]), 0f, hitInfo.distance / raycastLength);
					}
				}
				break;
			case NavigationMode.FollowTarget:
				Debug.DrawRay(position, Quaternion.AngleAxis(array[i], transform.up) * transform.forward * raycastLength, Color.red);
				flag = true;
				if (i != 0)
				{
					rayInput -= Mathf.Lerp(Mathf.Sign(array[i]), 0f, hitInfo.distance / raycastLength);
				}
				break;
			}
			if (flag)
			{
				obstacle = hitInfo.transform.gameObject;
			}
			else
			{
				obstacle = null;
			}
		}
		raycasting = flag;
		rayInput = Mathf.Clamp(rayInput, -1f, 1f);
		if (raycasting && Mathf.Abs(rayInput) > 0.5f)
		{
			ignoreWaypointNow = true;
		}
		else
		{
			ignoreWaypointNow = false;
		}
	}

	private void FeedRCC()
	{
		if (!carController.changingGear && !carController.cutGas)
		{
			carController.throttleInput = ((carController.direction == 1) ? Mathf.Clamp01(throttleInput) : Mathf.Clamp01(brakeInput));
		}
		else
		{
			carController.throttleInput = 0f;
		}
		if (!carController.changingGear && !carController.cutGas)
		{
			carController.brakeInput = ((carController.direction == 1) ? Mathf.Clamp01(brakeInput) : Mathf.Clamp01(throttleInput));
		}
		else
		{
			carController.brakeInput = 0f;
		}
		if (smoothedSteer)
		{
			carController.steerInput = Mathf.Lerp(carController.steerInput, steerInput, Time.deltaTime * 20f);
		}
		else
		{
			carController.steerInput = steerInput;
		}
		carController.handbrakeInput = handbrakeInput;
	}

	private void Stop()
	{
		throttleInput = 0f;
		brakeInput = 0f;
		steerInput = 0f;
		handbrakeInput = 1f;
	}

	private void CheckTargets()
	{
		for (int i = 0; i < targetsInZone.Count; i++)
		{
			if (targetsInZone[i] == null)
			{
				targetsInZone.RemoveAt(i);
			}
			if (!targetsInZone[i].gameObject.activeInHierarchy)
			{
				targetsInZone.RemoveAt(i);
			}
			else if (Vector3.Distance(transform.position, targetsInZone[i].transform.position) > detector.radius * 1.25f)
			{
				targetsInZone.RemoveAt(i);
			}
		}
		if (targetsInZone.Count > 0)
		{
			targetChase = GetClosestEnemy(targetsInZone.ToArray());
		}
		else
		{
			targetChase = null;
		}
	}

	private void OnTriggerEnter(Collider col)
	{
		if (col.transform.root.CompareTag(targetTag) && !targetsInZone.Contains(col.transform.root))
		{
			targetsInZone.Add(col.transform.root);
		}
	}

	private Transform GetClosestEnemy(Transform[] enemies)
	{
		Transform result = null;
		float num = float.PositiveInfinity;
		Vector3 position = base.transform.position;
		foreach (Transform transform in enemies)
		{
			float sqrMagnitude = (transform.position - position).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = transform;
			}
		}
		return result;
	}

	public static bool GetPath(NavMeshPath path, Vector3 fromPos, Vector3 toPos, int passableMask)
	{
		path.ClearCorners();
		if (!NavMesh.CalculatePath(fromPos, toPos, passableMask, path))
		{
			return false;
		}
		return true;
	}

	public static float GetPathLength(NavMeshPath path)
	{
		float num = 0f;
		if (path.status != NavMeshPathStatus.PathInvalid && path.corners.Length > 1)
		{
			for (int i = 1; i < path.corners.Length; i++)
			{
				num += Vector3.Distance(path.corners[i - 1], path.corners[i]);
			}
		}
		return num;
	}

	private void OnDisable()
	{
		if (OnRCCAIDestroyed != null)
		{
			OnRCCAIDestroyed(this);
		}
	}
}
