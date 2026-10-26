using UnityEngine;

public class RCC_Spawner : MonoBehaviour
{
	private void Start()
	{
		int num = PlayerPrefs.GetInt("SelectedRCCVehicle", 0);
		RCC.SpawnRCC(RCC_DemoVehicles.Instance.vehicles[num], transform.position, transform.rotation, registerAsPlayerVehicle: true, isControllable: true, isEngineRunning: true);
	}
}
