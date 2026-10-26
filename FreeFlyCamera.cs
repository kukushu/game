using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FreeFlyCamera : MonoBehaviour
{
	public float movementSpeed = 5f;

	public float movementMaxSpeed = 100f;

	public float rotationSpeed = 2f;

	public float reset_x;

	public float reset_y;

	public float reset_z;

	private float acceleration = 10f;

	private float totalRun = 1f;

	private float yaw;

	private float pitch;

	private Vector3 GetInputMovement()
	{
		Vector3 result = default;
		if (Input.GetKey(KeyCode.W))
		{
			result += new Vector3(0f, 0f, 1f);
		}
		if (Input.GetKey(KeyCode.S))
		{
			result += new Vector3(0f, 0f, -1f);
		}
		if (Input.GetKey(KeyCode.A))
		{
			result += new Vector3(-1f, 0f, 0f);
		}
		if (Input.GetKey(KeyCode.D))
		{
			result += new Vector3(1f, 0f, 0f);
		}
		if (Input.GetKey(KeyCode.PageUp))
		{
			result += new Vector3(0f, 1f, 0f);
		}
		if (Input.GetKey(KeyCode.PageDown))
		{
			result += new Vector3(0f, -1f, 0f);
		}
		return result;
	}

	private Quaternion GetInputRotation()
	{
		float num = yaw;
		float num2 = pitch;
		if (Input.GetKey(KeyCode.RightArrow))
		{
			num += rotationSpeed;
		}
		if (Input.GetKey(KeyCode.LeftArrow))
		{
			num -= rotationSpeed;
		}
		if (Input.GetKey(KeyCode.UpArrow))
		{
			num2 -= rotationSpeed;
		}
		if (Input.GetKey(KeyCode.DownArrow))
		{
			num2 += rotationSpeed;
		}
		if (Input.GetKey(KeyCode.Z))
		{
			num2 = 0f;
			num = 0f;
		}
		yaw = num;
		pitch = num2;
		return Quaternion.Euler(pitch, yaw, 0f);
	}

	private void Start()
	{
		Debug.Log(transform.eulerAngles);
		pitch = transform.eulerAngles.x;
		yaw = transform.eulerAngles.y;
	}

	private void Update()
	{
		if (Input.GetKey(KeyCode.X))
		{
			Vector3 position = new Vector3(reset_x, reset_y, reset_z);
			transform.position = position;
		}
		else
		{
			Vector3 inputMovement = GetInputMovement();
			if (Input.GetKey(KeyCode.Space))
			{
				totalRun += Time.deltaTime;
				inputMovement *= totalRun * acceleration;
				inputMovement.x = Mathf.Clamp(inputMovement.x, 0f - movementMaxSpeed, movementMaxSpeed);
				inputMovement.y = Mathf.Clamp(inputMovement.y, 0f - movementMaxSpeed, movementMaxSpeed);
				inputMovement.z = Mathf.Clamp(inputMovement.z, 0f - movementMaxSpeed, movementMaxSpeed);
			}
			else
			{
				totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
				inputMovement *= movementSpeed;
			}
			inputMovement *= Time.deltaTime;
			transform.Translate(inputMovement);
		}
		transform.rotation = GetInputRotation();
	}
}
