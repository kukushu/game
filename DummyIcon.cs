using UnityEngine;

public class DummyIcon : MonoBehaviour
{
	private float speed = 1f;

	private void Update()
	{
		transform.Rotate(0f, speed, 0f, Space.Self);
	}
}
