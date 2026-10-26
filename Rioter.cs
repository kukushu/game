using System.Collections;
using UnityEngine;

public class Rioter : MonoBehaviour
{
	private float Speed = 0.003f;

	private float MaxSpeed = 0.003f;

	public Vector3 currentPos = Vector3.zero;

	private int life = 25;

	public bool isViolent;

	private int Delay = 3;

	public Roading roading;

	public RoadCreator currentRC;

	public Vector3 Destination;

	public Vector3 worldDestination;

	public Vector3 Offset;

	public Vector2 Direction = new Vector2(1f, 1f);

	private Vector3 g = new Vector3(0f, -0.5f, 0f);

	private Animator anim;

	public bool isStopped;

	public Collider pedCollider;

	public Rigidbody manRB;

	private float rx;

	private float ry;

	private void Start()
	{
		Offset += new Vector3(Random.Range(-0.1f, 0.1f), 0f, Random.Range(-0.1f, 0.1f));
		rx = Random.Range(0.7f, 0.9f);
		if (Random.value > 0.5f)
		{
			rx = 0f - rx;
		}
		ry = Random.Range(0.7f, 0.9f);
		if (Random.value > 0.5f)
		{
			ry = 0f - ry;
		}
		anim = GetComponent<Animator>();
		anim.Play("Walk");
		InitialPosition();
		StartCoroutine(AnimateRioter());
	}

	private void InitialPosition()
	{
		Vector3 wPos = currentRC.wPos;
		currentPos = currentRC.wPos;
		Destination.x = currentPos.x;
		Destination.z = currentPos.z;
		transform.position = new Vector3(wPos.x + Offset.x, wPos.y + 0.08f, wPos.z + Offset.z);
	}

	private void FixedUpdate()
	{
		manRB.velocity = g;
		currentPos = gameObject.transform.position;
		UpdateDestination();
		Walk();
	}

	private void Walk()
	{
		worldDestination = Destination + Offset;
		transform.LookAt(worldDestination);
		if (!isStopped)
		{
			anim.SetBool("isWalking", value: true);
			anim.SetBool("isWaving", value: false);
			Speed = MaxSpeed;
			transform.position = Vector3.MoveTowards(transform.position, worldDestination, Speed);
		}
		if (life < 1)
		{
			Object.Destroy(gameObject);
			Rioting.Rioters--;
		}
	}

	private void UpdateDestination()
	{
		if (Mathf.Round(currentPos.x) != Mathf.Ceil(Destination.x) || Mathf.Round(currentPos.z) != Mathf.Ceil(Destination.z))
		{
			return;
		}
		if (life <= 0)
		{
			Object.Destroy(gameObject);
			Rioting.Rioters--;
			return;
		}
		FindDestination(currentRC);
		int num = (int)Mathf.Round(Destination.x);
		int num2 = (int)Mathf.Round(Destination.z);
		RoadCreator roadCreator = roading.rD[num, 1, num2];
		if (roadCreator != null)
		{
			currentRC = roadCreator;
			if (Destination == Vector3.zero)
			{
				Debug.Log("Return statement not working? Road not existing in switch case.");
			}
		}
		else
		{
			Object.Destroy(gameObject);
			Rioting.Rioters--;
			Debug.Log("Couldn't find next destination. Road not existing in switch case.");
		}
	}

	private void FindDestination(RoadCreator rc)
	{
		Vector3 destination = Vector3.zero;
		int num = (int)Mathf.Round(currentPos.x);
		int num2 = (int)Mathf.Round(currentPos.z);
		RoadCreator roadCreator = roading.rD[(int)((float)num + Direction.x), 1, (int)((float)num2 + Direction.y)];
		if (roadCreator == null)
		{
			if (Direction.x == -1f)
			{
				Direction.x = 1f;
			}
			else if (Direction.x == 1f)
			{
				Direction.x = -1f;
			}
			else if (Direction.y == -1f)
			{
				Direction.y = 1f;
			}
			else if (Direction.y == 1f)
			{
				Direction.y = -1f;
			}
			else
			{
				Debug.Log("Demi tour not possible");
				Object.Destroy(gameObject);
				Rioting.Rioters--;
			}
			roadCreator = roading.rD[(int)((float)num + Direction.x), 1, (int)((float)num2 + Direction.y)];
			destination = roadCreator.wPos;
		}
		else if (roadCreator != rc)
		{
			destination = roadCreator.wPos;
		}
		else
		{
			Debug.Log("Bug in the rioter script");
			Object.Destroy(gameObject);
			Rioting.Rioters--;
		}
		Destination = destination;
	}

	private IEnumerator AnimateRioter()
	{
		while (true)
		{
			yield return new WaitForSeconds(Delay);
			isStopped = true;
			anim.SetBool("isWaving", value: true);
			anim.SetBool("isWalking", value: false);
			if (isViolent)
			{
				ThrowMolotov();
			}
			yield return new WaitForSeconds(Delay * 4);
			isStopped = false;
		}
	}

	private void ThrowMolotov()
	{
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Pedestrian/MolotovActiv"));
		Molotov component = gameObject.GetComponent<Molotov>();
		gameObject.transform.position = transform.position;
		component.rb.useGravity = true;
		Vector3 vector = new Vector3(75f, 5f, 75f);
		component.rb.AddForce(new Vector3(rx * vector.x, 3f, ry * vector.z));
	}
}
