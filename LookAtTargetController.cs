using UnityEngine;

public class LookAtTargetController : MonoBehaviour
{
	public Transform Target;

	public bool smooth = true;

	public float damping = 6f;

	private void Start()
	{
	}

	private void LateUpdate()
	{
		if (Target != null)
		{
			if (smooth)
			{
				Quaternion b = Quaternion.LookRotation(Target.position - transform.position);
				transform.rotation = Quaternion.Slerp(transform.rotation, b, Time.deltaTime * damping);
			}
			else
			{
				transform.LookAt(Target);
			}
		}
	}
}
