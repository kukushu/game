using UnityEngine;

public class CraneOffshore : MonoBehaviour
{
	private bool invertRot;

	private float rotation;

	private void Start()
	{
	}

	private void FixedUpdate()
	{
		if (rotation > 90f)
		{
			invertRot = true;
		}
		else if (rotation < 0f)
		{
			invertRot = false;
		}
		if (!invertRot)
		{
			rotation += 0.06f;
			transform.Rotate(0f, 0.06f, 0f, Space.Self);
		}
		else
		{
			rotation -= 0.06f * Time.deltaTime;
			transform.Rotate(0f, -0.06f, 0f, Space.Self);
		}
	}
}
