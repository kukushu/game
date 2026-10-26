using System.Collections.Generic;
using UnityEngine;

public class Boat : MonoBehaviour
{
	public int boatSize;

	public World w;

	public ServiceCreator sc;

	public int Direction;

	public float forwardSpeed = 0.6f;

	private int life = 500;

	private readonly float waterLevel = 30.02f;

	private int delay;

	private BlockData x_;

	private BlockData _x;

	private BlockData z_;

	private BlockData _z;

	public Vector3 ahead = new Vector3(1f, 0f, 1f);

	private void Start()
	{
		x_ = w.GetIfNotNull((int)transform.position.x + boatSize, (int)transform.position.z);
		_x = w.GetIfNotNull((int)transform.position.x - boatSize, (int)transform.position.z);
		z_ = w.GetIfNotNull((int)transform.position.x, (int)transform.position.z + boatSize);
		_z = w.GetIfNotNull((int)transform.position.x, (int)transform.position.z - boatSize);
		if (x_ == null && _x == null && z_ == null && _z == null)
		{
			Debug.Log("All positions are null");
		}
		if (Direction == 0 && x_ != null)
		{
			ahead = new Vector3(x_.wP.x, waterLevel, x_.wP.y);
		}
		else if (Direction == 1 && _x != null)
		{
			ahead = new Vector3(_x.wP.x, waterLevel, _x.wP.y);
		}
		else if (Direction == 2 && z_ != null)
		{
			ahead = new Vector3(z_.wP.x, waterLevel, z_.wP.y);
		}
		else if (Direction == 3 && _z != null)
		{
			ahead = new Vector3(_z.wP.x, waterLevel, _z.wP.y);
		}
	}

	private void Update()
	{
		delay++;
		if (delay >= 10)
		{
			delay = 0;
			life--;
			CheckDirection();
		}
		MoveToward();
	}

	private void CheckDirection()
	{
		x_ = w.GetIfNotNull((int)transform.position.x + boatSize, (int)transform.position.z);
		_x = w.GetIfNotNull((int)transform.position.x - boatSize, (int)transform.position.z);
		z_ = w.GetIfNotNull((int)transform.position.x, (int)transform.position.z + boatSize);
		_z = w.GetIfNotNull((int)transform.position.x, (int)transform.position.z - boatSize);
		if (Direction == 0)
		{
			if (x_ == null)
			{
				DestroyBoat();
			}
			else if (x_.h > 29.5f || x_.b)
			{
				ChangeDirection();
			}
			else
			{
				ahead = new Vector3(x_.wP.x + boatSize, waterLevel, transform.position.z);
			}
		}
		else if (Direction == 1)
		{
			if (_x == null)
			{
				DestroyBoat();
			}
			else if (_x.h > 29.5f || _x.b)
			{
				ChangeDirection();
			}
			else
			{
				ahead = new Vector3(_x.wP.x - boatSize, waterLevel, transform.position.z);
			}
		}
		else if (Direction == 2)
		{
			if (z_ == null)
			{
				DestroyBoat();
			}
			else if (z_.h > 29.5f || z_.b)
			{
				ChangeDirection();
			}
			else
			{
				ahead = new Vector3(transform.position.x, waterLevel, z_.wP.y + boatSize);
			}
		}
		else if (Direction == 3)
		{
			if (_z == null)
			{
				DestroyBoat();
			}
			else if (_z.h > 29.5f || _z.b)
			{
				ChangeDirection();
			}
			else
			{
				ahead = new Vector3(transform.position.x, waterLevel, _z.wP.y - boatSize);
			}
		}
	}

	private void ChangeDirection()
	{
		if (life < 0)
		{
			DestroyBoat();
			return;
		}
		List<BlockData> list = new List<BlockData>();
		if (x_ != null && x_.h < 29.75f && !x_.b)
		{
			list.Add(x_);
		}
		if (_x != null && _x.h < 29.75f && !_x.b)
		{
			list.Add(_x);
		}
		if (z_ != null && z_.h < 29.75f && !z_.b)
		{
			list.Add(z_);
		}
		if (_z != null && _z.h < 29.75f && !_z.b)
		{
			list.Add(_z);
		}
		if (list.Count < 1)
		{
			DestroyBoat();
		}
		else
		{
			BlockData blockData = list[Random.Range(0, list.Count)];
			if (blockData == x_)
			{
				Direction = 0;
				ahead = new Vector3(x_.wP.x + boatSize, waterLevel, transform.position.z);
			}
			else if (blockData == _x)
			{
				Direction = 1;
				ahead = new Vector3(_x.wP.x - boatSize, waterLevel, transform.position.z);
			}
			else if (blockData == z_)
			{
				Direction = 2;
				ahead = new Vector3(transform.position.x, waterLevel, z_.wP.y + boatSize);
			}
			else if (blockData == _z)
			{
				Direction = 3;
				ahead = new Vector3(transform.position.x, waterLevel, _z.wP.y - boatSize);
			}
		}
		list.Clear();
	}

	private void MoveToward()
	{
		Quaternion to = Quaternion.LookRotation(ahead - transform.position);
		if (Mathf.Abs(transform.rotation.y - to.y) > 0.01f)
		{
			forwardSpeed = 0.1f;
		}
		else
		{
			forwardSpeed = 0.6f;
		}
		transform.rotation = Quaternion.RotateTowards(transform.rotation, to, 30f * Time.deltaTime);
		transform.Translate(forwardSpeed * Time.deltaTime * Vector3.forward);
	}

	private void DestroyBoat()
	{
		if (sc != null)
		{
			sc.carsOut--;
		}
		Object.Destroy(gameObject);
	}
}
