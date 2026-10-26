using UnityEngine;

public class FollowTargetCamera : MonoBehaviour
{
	public Transform Target;

	public float PositionFolowForce = 5f;

	public float RotationFolowForce = 5f;

	private void Start()
	{
	}

	private void FixedUpdate()
	{
		Vector3 forward = Vector3.forward;
		Vector3 vector = Target.rotation * Vector3.forward;
		vector.y = 0f;
		if (vector.magnitude > 0f)
		{
			forward = vector / vector.magnitude;
		}
		transform.position = Vector3.Lerp(transform.position, Target.position, PositionFolowForce * Time.deltaTime);
		transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(forward), RotationFolowForce * Time.deltaTime);
	}
}
