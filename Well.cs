using UnityEngine;

public class Well : MonoBehaviour
{
	public Vector3 speedUp = new Vector3(0f, 0f, 0.2f);

	public Vector3 speedDown = new Vector3(0f, 0f, -0.2f);

	public Vector3 speed;

	public float rotation;

	private void Start()
	{
		speed = speedUp;
	}

	private void FixedUpdate()
	{
		rotation = transform.rotation.eulerAngles.z;
		if (rotation > 342f)
		{
			speed = speedUp;
		}
		else if (rotation > 18f)
		{
			speed = speedDown;
		}
		transform.Rotate(speed, Space.Self);
	}
}
