using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrainWP : MonoBehaviour
{
	public enum Direction
	{
		upX,
		downX,
		upZ,
		downZ
	}

	public int kph;

	public float Torque;

	private float MaxTorque = 1.6f;

	private float LimitedTorque = 1.2f;

	private float Acceleration = 0.01f;

	private float Handling = 100f;

	public Vector3 currentPos = Vector3.zero;

	public Vector3 destination = Vector3.zero;

	public bool isBus;

	public Roading roading;

	public RoadCreator currentRC;

	public RoadCreator nextRC;

	public float Distance;

	public Vector3 Offset = Vector3.zero;

	public bool isStopped;

	public string RFS = "none";

	private bool destroying;

	private Waypoint forward;

	private Waypoint next;

	public List<RoadCreator> path = new List<RoadCreator>();

	public int pathPos;

	private AudioSource audioSource;

	public bool isHead;

	public TrainWP headWagon;

	public TrainWP wagon1;

	public TrainWP wagon2;

	private Vector3 z_ = new Vector3(0f, 0f, 0.245f);

	private Vector3 _z = new Vector3(0f, 0f, -0.245f);

	private Vector3 x_ = new Vector3(0.245f, 0f, 0f);

	private Vector3 _x = new Vector3(-0.245f, 0f, 0f);

	public Direction dir;

	public int life = 500;

	private int delay;

	private void Start()
	{
		FirstRoad();
		audioSource = GetComponent<AudioSource>();
	}

	private void FirstRoad()
	{
		currentRC = path[pathPos];
		if (isHead)
		{
			if (currentRC == null || path[pathPos - 1] == null)
			{
				DestroyWholeTrain();
				return;
			}
			if (currentRC.waypoint.occupers.Count > 0 || path[pathPos - 2].waypoint.occupers.Count > 0)
			{
				DestroyWholeTrain();
				return;
			}
		}
		Vector3 wPos = currentRC.wPos;
		currentPos = currentRC.wPos;
		next = currentRC.waypoint;
		forward = path[pathPos - 1].waypoint;
		next.free = false;
		UpdateDestination();
		UpdateDirection();
		InitialRotation();
		transform.position = new Vector3(wPos.x + Offset.x, wPos.y, wPos.z + Offset.z);
		Offset = Vector3.zero;
		CheckNextRoad();
	}

	public void UpdateTrain()
	{
		kph = Mathf.RoundToInt((transform.position - currentPos).magnitude * 3.6f * 400f);
		currentPos = transform.position;
		if (isHead)
		{
			audioSource.volume = Torque * 0.75f;
			audioSource.pitch = Torque;
		}
		if (!isStopped)
		{
			CheckNextRoad();
		}
		else if (isStopped && isHead)
		{
			delay++;
			if (delay > 120)
			{
				if (isHead)
				{
					DestroyWholeTrain();
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
		else
		{
			Torque = Mathf.Lerp(Torque, LimitedTorque, Acceleration);
			Quaternion to = Quaternion.LookRotation(destination - transform.position);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, to, (Handling + Torque * 100f) * Time.deltaTime);
			transform.Translate(Vector3.forward * Torque * Time.deltaTime);
		}
	}

	private void CheckNextRoad()
	{
		if (isHead && next == null)
		{
			Debug.Log("Road was destroyed");
			DestroyWholeTrain();
			return;
		}
		Distance = Vector3.Distance(currentPos, destination);
		if (!(Distance < 0.15f))
		{
			return;
		}
		List<TrainWP> trainsOccupers = path[pathPos - 1].waypoint.trainsOccupers;
		RoadCreator nextR = (nextRC = path[pathPos - 1]);
		if (trainsOccupers.Count > 0 && isHead)
		{
			if (isHead && path[pathPos - 1] == null)
			{
				DestroyWholeTrain();
				return;
			}
			foreach (TrainWP item in trainsOccupers.ToList())
			{
				if (item == null)
				{
					path[pathPos - 1].waypoint.trainsOccupers.Remove(item);
				}
				else if (item != this && item.dir == dir)
				{
					StopTrain(stop: true);
					RFS = "B Lane Ahead Busy";
					return;
				}
			}
			GoToNextRoad(nextR);
			RFS = "Train ahead on opposite track, drive.";
		}
		else if ((!isHead && !headWagon.isStopped) || isHead)
		{
			GoToNextRoad(nextR);
			RFS = "Normal Driving";
		}
		else
		{
			RFS = "Unknown!";
		}
	}

	private void GoToNextRoad(RoadCreator nextR)
	{
		if (isHead)
		{
			StopTrain(stop: false);
		}
		if (next == null)
		{
			if (isHead)
			{
				DestroyWholeTrain();
			}
			else
			{
				headWagon.DestroyWholeTrain();
			}
			return;
		}
		next.trainsOccupers.Remove(this);
		next.free = true;
		next = nextR.waypoint;
		next.free = false;
		next.trainsOccupers.Add(this);
		currentRC = nextR;
		pathPos--;
		life = 500;
		UpdateDirection();
		UpdateDestination();
		if (isHead && pathPos <= 1)
		{
			DestroyWholeTrain();
		}
	}

	private void UpdateDestination()
	{
		if ((isHead && next == null) || (isHead && forward == null))
		{
			DestroyWholeTrain();
			return;
		}
		if (dir == Direction.upX)
		{
			Offset = z_;
		}
		else if (dir == Direction.downX)
		{
			Offset = _z;
		}
		else if (dir == Direction.upZ)
		{
			Offset = _x;
		}
		else if (dir == Direction.downZ)
		{
			Offset = x_;
		}
		destination = next.next.position + Offset;
	}

	private void UpdateDirection()
	{
		if (next == null || pathPos - 1 < 0 || path[pathPos - 1] == null)
		{
			Debug.Log("Road was destroyed");
			DestroyWholeTrain();
			return;
		}
		float x = next.next.position.x;
		float z = next.next.position.z;
		float x2 = path[pathPos - 1].waypoint.next.position.x;
		float z2 = path[pathPos - 1].waypoint.next.position.z;
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
		Direction direction2 = dir;
		if (direction != direction2)
		{
			LimitedTorque = 0.35f;
		}
		else
		{
			LimitedTorque = MaxTorque;
		}
	}

	private void InitialRotation()
	{
		int num;
		if (dir == Direction.upX)
		{
			num = 90;
			Offset = z_;
		}
		else if (dir == Direction.downX)
		{
			num = 270;
			Offset = _z;
		}
		else if (dir == Direction.upZ)
		{
			num = 0;
			Offset = _x;
		}
		else if (dir == Direction.downZ)
		{
			num = 180;
			Offset = x_;
		}
		else
		{
			num = 180;
			Offset = x_;
		}
		gameObject.transform.Rotate(0f, num, 0f, Space.Self);
	}

	private void StopTrain(bool stop)
	{
		isStopped = stop;
		if (isHead)
		{
			wagon1.isStopped = stop;
			wagon2.isStopped = stop;
		}
	}

	private void DestroyWholeTrain()
	{
		if (!destroying)
		{
			destroying = true;
			if (next != null)
			{
				next.free = true;
			}
			if (currentRC != null)
			{
				currentRC.waypoint.trainsOccupers.Remove(this);
			}
			TrafficManager.nTrains--;
			TrafficManager.trains.Remove(this);
			if (wagon1 != null)
			{
				wagon1.DestroyWagon();
			}
			if (wagon2 != null)
			{
				wagon2.DestroyWagon();
			}
			Object.Destroy(gameObject);
		}
	}

	public void DestroyWagon()
	{
		if (next != null)
		{
			next.free = true;
		}
		if (currentRC != null && currentRC.waypoint != null)
		{
			currentRC.waypoint.trainsOccupers.Remove(this);
		}
		Object.Destroy(gameObject);
	}
}
