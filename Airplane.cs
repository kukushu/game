using System.Collections;
using UnityEngine;

public class Airplane : MonoBehaviour
{
	public Collider planeCol;

	public Rigidbody planeRB;

	private float Speed;

	private bool isAccelerating;

	private bool isClimbing;

	public int Rotation;

	public float height;

	private float maxHeight = 100f;

	private Vector3 Target;

	public Vector3 Destination;

	private int Life = 2000;

	public float acceleration = 2f;

	public float altitudeGain = 7f;

	public AudioSource audioSource;

	private void Start()
	{
		Destination = transform.position;
		audioSource.volume = SettingsStatic.Carsvolume * 3f;
		StartCoroutine(FlightPath());
	}

	private void FixedUpdate()
	{
		Life--;
		if (Life < 0)
		{
			Object.Destroy(gameObject);
		}
		if (isAccelerating)
		{
			Speed += acceleration * Time.deltaTime;
		}
		if (isClimbing && height < maxHeight)
		{
			height += altitudeGain * Time.deltaTime;
		}
		Destination = transform.position + Target;
		Destination.y = transform.position.y + height;
		transform.position = Vector3.MoveTowards(transform.position, Destination, Speed * Time.deltaTime);
	}

	private IEnumerator FlightPath()
	{
		if (Rotation == -90)
		{
			Rotation = 270;
		}
		if (Rotation == 0)
		{
			Target += new Vector3(-1f, 0f, 0f);
		}
		else if (Rotation == 180)
		{
			Target += new Vector3(1f, 0f, 0f);
		}
		else if (Rotation == 90)
		{
			Target += new Vector3(0f, 0f, 1f);
		}
		else if (Rotation == 270)
		{
			Target += new Vector3(0f, 0f, -1f);
		}
		yield return new WaitForSeconds(1f);
		isAccelerating = true;
		yield return new WaitForSeconds(4.5f);
		isClimbing = true;
		yield return new WaitForSeconds(2f);
		isAccelerating = false;
		yield return new WaitForSeconds(4f);
		isClimbing = false;
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (!(height < 0.1f))
		{
			((GameObject)Object.Instantiate(Resources.Load("Construction/Explosion"))).transform.position = gameObject.transform.position;
			Object.Destroy(gameObject);
			Messages.messages.NewAlertMessage(8);
		}
	}
}
