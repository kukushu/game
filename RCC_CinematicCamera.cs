using UnityEngine;

[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Camera/RCC Cinematic Camera")]
public class RCC_CinematicCamera : MonoBehaviour
{
	public GameObject pivot;

	private Vector3 targetPosition;

	public float targetFOV = 60f;

	private void Start()
	{
		if (!pivot)
		{
			pivot = new GameObject("Pivot");
			pivot.transform.SetParent(transform, worldPositionStays: false);
			pivot.transform.localPosition = Vector3.zero;
			pivot.transform.localRotation = Quaternion.identity;
		}
	}

	private void Update()
	{
		if ((bool)RCC_SceneManager.Instance.activePlayerCamera)
		{
			Transform transform = null;
			if ((bool)RCC_SceneManager.Instance.activePlayerCamera.playerCar)
			{
				transform = RCC_SceneManager.Instance.activePlayerCamera.playerCar.transform;
			}
			if (!(transform == null))
			{
				base.transform.rotation = Quaternion.Slerp(base.transform.rotation, Quaternion.Euler(base.transform.eulerAngles.x, transform.eulerAngles.y + 180f, base.transform.eulerAngles.z), Time.deltaTime * 3f);
				targetPosition = transform.position;
				targetPosition -= base.transform.rotation * Vector3.forward * 10f;
				base.transform.position = targetPosition;
			}
		}
	}
}
