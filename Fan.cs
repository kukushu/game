using UnityEngine;

public class Fan : MonoBehaviour
{
	public Vector3 speed = new Vector3(0f, 0f, 0.5f);

	private void Update()
	{
		transform.Rotate(speed, Space.Self);
	}
}
