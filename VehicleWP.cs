using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VehicleWP : MonoBehaviour
{
	public enum Direction
	{
		upX,
		downX,
		upZ,
		downZ
	}

	public float Torque;

	private float MaxTorque = 1.2f;

	private float LimitedTorque = 1.2f;

	private float Acceleration = 0.02f;

	private float Handling = 125f;

	private float gear;

	private float distanceTrigger = 0.35f;

	public Vector3 currentPos = Vector3.zero;

	public Vector3 destination = Vector3.zero;

	public bool isDrone;

	public Vector3 droneAltitude = Vector3.zero;

	public bool isBus;

	public bool emergency;

	public LotCreator lotOwner;

	public ServiceCreator serviceOwner;

	public Roading roading;

	public RoadCreator currentRC;

	public float Distance;

	public Vector3 Offset = Vector3.zero;

	private Vector3 laneOffset = Vector3.zero;

	public bool isStopped;

	private bool destroying;

	private Waypoint forward;

	private Waypoint next;

	public List<RoadCreator> path = new List<RoadCreator>();

	public int pathPos;

	private bool atInter;

	public AudioSource audioSource;

	public AudioSource siren;

	public bool B_lane;

	public bool C_lane;

	public bool silent;

	public Direction dir;

	private Vector3[] off = new Vector3[4]
	{
		new Vector3(0.5f, 0f, -0.25f),
		new Vector3(-0.5f, 0f, 0.25f),
		new Vector3(0.25f, 0f, 0.5f),
		new Vector3(-0.25f, 0f, -0.5f)
	};

	public int life = 500;

	private int delay;

	public Renderer rend;

	private Transform trensform;

	private void Start()
	{
		float value = Random.value;
		float value2 = Random.value;
		if (value < 0.4f)
		{
			B_lane = true;
		}
		if (value2 < 0.2f)
		{
			C_lane = true;
		}
		if (isDrone)
		{
			droneAltitude.y = 8f + Random.value;
			distanceTrigger = 0.8f;
			MaxTorque = Random.Range(0.5f, 1f);
			Handling = 10f;
		}
		audioSource = GetComponent<AudioSource>();
		trensform = GetComponent<Transform>();
		if (silent)
		{
			Object.Destroy(audioSource);
		}
		else
		{
			audioSource.volume = SettingsStatic.Carsvolume;
		}
		FirstRoad();
	}

	private void FirstRoad()
	{
		pathPos = path.Count - 1;
		currentRC = path[pathPos];
		if (currentRC == null || path[pathPos - 1] == null)
		{
			TrafficManager.cars--;
			TrafficManager.vehicles.Remove(this);
			Object.Destroy(gameObject);
			return;
		}
		if (!currentRC.waypoint.free || !path[pathPos - 1].waypoint.free)
		{
			TrafficManager.cars--;
			TrafficManager.vehicles.Remove(this);
			Object.Destroy(gameObject);
			return;
		}
		Vector3 wPos = currentRC.wPos;
		currentPos = currentRC.wPos;
		next = currentRC.waypoint;
		forward = path[pathPos - 1].waypoint;
		if (!isDrone)
		{
			next.free = false;
		}
		UpdateDestination();
		InitialRotation();
		if (isDrone)
		{
			trensform.position = new Vector3(wPos.x + Offset.x, wPos.y, wPos.z + Offset.z) + droneAltitude;
		}
		else
		{
			trensform.position = new Vector3(wPos.x + Offset.x, wPos.y, wPos.z + Offset.z);
		}
		Offset = Vector3.zero;
		CheckNextRoad();
	}

	public void UpdateVehicle()
	{
		currentPos = trensform.position;
		if (!silent)
		{
			if (Torque > 0.1f)
			{
				if (isDrone)
				{
					audioSource.enabled = true;
					audioSource.pitch = 0.3f + Torque;
					audioSource.volume = Torque * SettingsStatic.Carsvolume;
				}
				else
				{
					audioSource.enabled = true;
					gear += (1.4f - Torque) / 200f;
					if (gear > 1f)
					{
						gear = Torque / 2f;
					}
					if (Torque > MaxTorque - 0.1f)
					{
						audioSource.pitch = 0.25f + gear * 1.5f;
					}
					else
					{
						audioSource.pitch = 0.25f + Torque * 1.5f;
					}
					audioSource.volume = Torque * SettingsStatic.Carsvolume;
				}
			}
			else
			{
				gear = 0f;
				audioSource.enabled = false;
			}
		}
		if (!isStopped)
		{
			CheckNextRoad();
		}
		else
		{
			delay++;
			if (delay > 30)
			{
				if (life < 0)
				{
					DestroyCar();
					return;
				}
				CheckNextRoad();
				delay = 0;
			}
		}
		if (isStopped)
		{
			Torque -= 0.01f;
			if (Torque < 0f)
			{
				Torque = 0f;
			}
		}
		else if (isDrone)
		{
			Torque = Mathf.Lerp(Torque, LimitedTorque, Acceleration);
			Quaternion to = Quaternion.LookRotation((destination - trensform.position) / 2f);
			trensform.rotation = Quaternion.RotateTowards(trensform.rotation, to, (Handling + Torque * 50f) * Time.deltaTime);
			trensform.Translate(Time.deltaTime * Torque * Vector3.forward);
		}
		else
		{
			Torque = Mathf.Lerp(Torque, LimitedTorque, Acceleration);
			Quaternion to2 = Quaternion.LookRotation(destination - trensform.position);
			trensform.rotation = Quaternion.RotateTowards(trensform.rotation, to2, (Handling + Torque * 100f) * Time.deltaTime);
			trensform.Translate(Time.deltaTime * Torque * Vector3.forward);
		}
	}

	private void CheckNextRoad()
	{
		if (next == null)
		{
			DestroyCar();
			return;
		}
		Distance = Vector3.Distance(currentPos, destination);
		if (!(Distance < distanceTrigger))
		{
			return;
		}
		List<VehicleWP> list = path[pathPos - 1].waypoint.occupers.ToList();
		RoadCreator roadCreator = path[pathPos - 1];
		if (pathPos - 2 < 0)
		{
			DestroyCar();
			return;
		}
		RoadCreator roadCreator2 = path[pathPos - 2];
		forward = roadCreator2.waypoint;
		if (roadCreator2 == null)
		{
			DestroyCar();
		}
		else if (forward == null)
		{
			DestroyCar();
		}
		else if (isDrone)
		{
			GoToNextRoad(roadCreator);
		}
		else if (!roadCreator.isRed && list.Count > 0)
		{
			if (emergency && roadCreator.isIntersec)
			{
				siren.volume = SettingsStatic.Carsvolume * 1.25f;
			}
			if (roadCreator.rType == RoadCreator.RoadType.ROAD || roadCreator.isAccess)
			{
				isStopped = true;
				if (currentRC.cc > 4)
				{
					life -= 20;
				}
				return;
			}
			foreach (VehicleWP item in list)
			{
				if (item == null)
				{
					path[pathPos - 1].waypoint.occupers.Remove(item);
				}
				else if (item.B_lane == B_lane && item != this)
				{
					isStopped = true;
					if (currentRC.cc > 4)
					{
						life -= 20;
					}
					return;
				}
			}
			GoToNextRoad(roadCreator);
		}
		else if (roadCreator.isRed)
		{
			if (roadCreator.dir == (RoadCreator.Direction)dir && !emergency)
			{
				isStopped = true;
			}
			else
			{
				GoToNextRoad(roadCreator);
			}
		}
		else
		{
			GoToNextRoad(roadCreator);
			if (emergency)
			{
				siren.volume = 0f;
			}
		}
	}

	private void GoToNextRoad(RoadCreator nextR)
	{
		isStopped = false;
		if (!isDrone)
		{
			next.occupers.Remove(this);
			next.free = true;
		}
		next = nextR.waypoint;
		next.free = false;
		if (!isDrone)
		{
			next.occupers.Add(this);
		}
		currentRC = nextR;
		pathPos--;
		life = 500;
		UpdateOffset();
		UpdateDestination();
		if (pathPos <= 3)
		{
			DestroyCar();
		}
	}

	private void UpdateDestination()
	{
		if (next == null || forward == null)
		{
			DestroyCar();
			return;
		}
		if (currentRC.rType != RoadCreator.RoadType.ROAD)
		{
			if (B_lane)
			{
				if (currentRC.cc == 1)
				{
					laneOffset = new Vector3(0f, 0f, 0.5f);
				}
				else if (currentRC.cc == 2)
				{
					laneOffset = new Vector3(0f, 0f, -0.5f);
				}
				else if (currentRC.cc == 3)
				{
					laneOffset = new Vector3(-0.5f, 0f, 0f);
				}
				else if (currentRC.cc == 4)
				{
					laneOffset = new Vector3(0.5f, 0f, 0f);
				}
				if (currentRC.tile == "precurve" || currentRC.tile == "curveout" || currentRC.isAccess)
				{
					laneOffset = Vector3.zero;
				}
			}
		}
		else
		{
			laneOffset = Vector3.zero;
		}
		if (isDrone)
		{
			destination = next.next.position + Offset + laneOffset + droneAltitude;
		}
		else
		{
			destination = next.next.position + Offset + laneOffset;
		}
		if (atInter && (path[pathPos - 1].wPos.y > currentRC.wPos.y || path[pathPos - 1].wPos.x > currentRC.wPos.x))
		{
			if (isDrone)
			{
				destination = new Vector3(destination.x, path[pathPos - 1].wPos.y, destination.z) + droneAltitude;
			}
			else
			{
				destination = new Vector3(destination.x, path[pathPos - 1].wPos.y, destination.z);
			}
		}
	}

	private void UpdateDirection()
	{
		if (next == null || pathPos - 3 < 0 || path[pathPos - 3] == null || next.next == null || path[pathPos - 3].waypoint == null)
		{
			DestroyCar();
			return;
		}
		float x = next.next.position.x;
		float z = next.next.position.z;
		float x2 = path[pathPos - 3].waypoint.next.position.x;
		float z2 = path[pathPos - 3].waypoint.next.position.z;
		float f = x - x2;
		float f2 = z - z2;
		Direction direction = dir;
		if (Mathf.Abs(f) > Mathf.Abs(f2))
		{
			if (x > x2)
			{
				dir = Direction.downX;
			}
			else
			{
				dir = Direction.upX;
			}
		}
		else if (z > z2)
		{
			dir = Direction.downZ;
		}
		else
		{
			dir = Direction.upZ;
		}
		if (direction != dir)
		{
			LimitedTorque = MaxTorque * 0.7f;
		}
		else
		{
			LimitedTorque = MaxTorque;
		}
	}

	private void UpdateOffset()
	{
		if (currentRC.cc < 5)
		{
			Offset = Vector3.zero;
			atInter = false;
			UpdateDirection();
		}
		else if (currentRC.cc == 17 || currentRC.cc == 18)
		{
			UpdateDirection();
			atInter = false;
			Offset = off[(int)dir];
		}
		else
		{
			UpdateDirection();
			atInter = true;
			Offset = Vector3.zero;
		}
	}

	private void InitialRotation()
	{
		Vector3 offset = new Vector3(0f, 0f, 0.25f);
		Vector3 offset2 = new Vector3(0f, 0f, -0.25f);
		Vector3 offset3 = new Vector3(0.25f, 0f, 0f);
		Vector3 offset4 = new Vector3(-0.25f, 0f, 0f);
		float value = Random.value;
		int num;
		if (currentRC.cc == 1)
		{
			num = 90;
			Offset = offset;
			dir = Direction.upX;
		}
		else if (currentRC.cc == 2)
		{
			num = 270;
			Offset = offset2;
			dir = Direction.downX;
		}
		else if (currentRC.cc == 3)
		{
			num = 0;
			Offset = offset4;
			dir = Direction.upZ;
		}
		else if (currentRC.cc == 4)
		{
			num = 180;
			Offset = offset3;
			dir = Direction.downZ;
		}
		else if (currentRC.cc == 17)
		{
			if (value < 0.5f)
			{
				num = 180;
				Offset = offset4;
			}
			else
			{
				num = 0;
				Offset = offset3;
			}
		}
		else if (currentRC.cc == 18)
		{
			if (value < 0.5f)
			{
				num = 90;
				Offset = offset2;
			}
			else
			{
				num = 270;
				Offset = offset;
			}
		}
		else
		{
			num = 180;
			Offset = offset3;
		}
		trensform.Rotate(0f, num, 0f, Space.Self);
	}

	public void DeployEmergency()
	{
	}

	public void DestroyCar()
	{
		if (!destroying)
		{
			if (emergency && name == "fire")
			{
				DeployEmergency();
			}
			destroying = true;
			if (!isDrone)
			{
				next.free = true;
				currentRC.waypoint.occupers.Remove(this);
			}
			TrafficManager.cars--;
			TrafficManager.vehicles.Remove(this);
			if (lotOwner != null)
			{
				lotOwner.carsOut--;
			}
			else if (serviceOwner != null)
			{
				serviceOwner.carsOut--;
			}
			Object.Destroy(gameObject);
		}
	}
}
