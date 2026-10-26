using UnityEngine;

public class RCC_Teleporter : MonoBehaviour
{
	public Transform spawnPoint;

	private void OnTriggerEnter(Collider col)
	{
		if (!col.isTrigger)
		{
			RCC_CarControllerV3 componentInParent = col.gameObject.GetComponentInParent<RCC_CarControllerV3>();
			if ((bool)componentInParent)
			{
				RCC.Transport(componentInParent, spawnPoint.position, spawnPoint.rotation);
			}
		}
	}
}
