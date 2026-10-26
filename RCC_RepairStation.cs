using UnityEngine;

public class RCC_RepairStation : MonoBehaviour
{
	private RCC_CarControllerV3 targetVehicle;

	private void OnTriggerStay(Collider col)
	{
		if (targetVehicle == null && (bool)col.gameObject.GetComponentInParent<RCC_CarControllerV3>())
		{
			targetVehicle = col.gameObject.GetComponentInParent<RCC_CarControllerV3>();
		}
		if ((bool)targetVehicle)
		{
			targetVehicle.repairNow = true;
		}
	}

	private void OnTriggerExit(Collider col)
	{
		if ((bool)col.gameObject.GetComponentInParent<RCC_CarControllerV3>())
		{
			targetVehicle = null;
		}
	}
}
