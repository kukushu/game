using UnityEngine;

public class CameraControl : MonoBehaviour
{
	public float scrollSpeed;

	public float rotateSens;

	public float zoomSpeed;

	public bool invertedRotate;

	private Vector3 scrollAnchor;

	private Vector3 rotateAnchor;

	private void Start()
	{
	}

	private void Update()
	{
		Vector3 mousePosition = Input.mousePosition;
		if (Input.GetMouseButtonDown(1))
		{
			rotateAnchor = mousePosition;
		}
		if (Input.GetMouseButton(1))
		{
			Vector3 position = mousePosition - rotateAnchor;
			position = Camera.main.ScreenToViewportPoint(position);
			float num = 1f;
			if (invertedRotate)
			{
				num = -1f;
			}
			float x = (0f - position.y) * num;
			position.y = position.x * num;
			position.x = x;
			Camera.main.transform.eulerAngles = rotateSens * position;
		}
		if (Input.GetMouseButtonDown(2))
		{
			scrollAnchor = mousePosition;
		}
		if (Input.GetMouseButton(2))
		{
			Vector3 position2 = mousePosition - scrollAnchor;
			position2 = Camera.main.ScreenToViewportPoint(position2);
			Camera.main.transform.Translate(position2 * scrollSpeed);
		}
		Vector3 vector = new Vector3(0f, 0f, Input.mouseScrollDelta.y);
		Camera.main.transform.Translate(vector * zoomSpeed);
	}
}
