using UnityEngine;

public class TextLookAt : MonoBehaviour
{
	private Camera cam;

	private void Start()
	{
		cam = Camera.main;
	}

	private void Update()
	{
		Vector3 forward = cam.transform.forward;
		transform.rotation = Quaternion.LookRotation(forward);
	}
}
