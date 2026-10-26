using UnityEngine;

[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Camera/RCC Fixed Camera")]
public class RCC_FixedCamera : MonoBehaviour
{
	private Vector3 targetPosition;

	public float maxDistance = 50f;

	private float distance;

	public float minimumFOV = 20f;

	public float maximumFOV = 60f;

	public bool canTrackNow;

	private void LateUpdate()
	{
		if (!canTrackNow || !RCC_SceneManager.Instance.activePlayerCamera)
		{
			return;
		}
		Transform transform = null;
		float speed = RCC_SceneManager.Instance.activePlayerCamera.playerCar.speed;
		transform = RCC_SceneManager.Instance.activePlayerCamera.playerCar.transform;
		if (!(transform == null))
		{
			distance = Vector3.Distance(base.transform.position, transform.position);
			RCC_SceneManager.Instance.activePlayerCamera.targetFieldOfView = Mathf.Lerp((distance > maxDistance / 10f) ? maximumFOV : 70f, minimumFOV, distance * 1.5f / maxDistance);
			targetPosition = transform.transform.position;
			targetPosition += transform.transform.rotation * Vector3.forward * (speed * 0.05f);
			base.transform.Translate(-transform.forward * speed / 50f * Time.deltaTime);
			base.transform.LookAt(targetPosition);
			if (distance > maxDistance)
			{
				ChangePosition();
			}
		}
	}

	public void ChangePosition()
	{
		if (!canTrackNow || !RCC_SceneManager.Instance.activePlayerCamera)
		{
			return;
		}
		Transform transform = null;
		if ((bool)RCC_SceneManager.Instance.activePlayerCamera.playerCar)
		{
			transform = RCC_SceneManager.Instance.activePlayerCamera.playerCar.transform;
		}
		if (!(transform == null))
		{
			float num = Random.Range(-15f, 15f);
			if (Physics.Raycast(transform.position, Quaternion.AngleAxis(num, transform.up) * transform.forward, out var hitInfo, maxDistance) && !hitInfo.transform.IsChildOf(transform) && !hitInfo.collider.isTrigger)
			{
				base.transform.position = hitInfo.point;
				base.transform.LookAt(transform.position + new Vector3(0f, Mathf.Clamp(num, 0.5f, 5f), 0f));
				base.transform.position += base.transform.rotation * Vector3.forward * 5f;
			}
			else
			{
				base.transform.position = transform.position + new Vector3(0f, Mathf.Clamp(num, 0f, 5f), 0f);
				base.transform.position += Quaternion.AngleAxis(num, transform.up) * transform.forward * (maxDistance * 0.9f);
			}
		}
	}
}
