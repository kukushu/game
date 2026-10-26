using UnityEngine;

public class RCC : MonoBehaviour
{
	public static RCC_CarControllerV3 SpawnRCC(RCC_CarControllerV3 vehiclePrefab, Vector3 position, Quaternion rotation, bool registerAsPlayerVehicle, bool isControllable, bool isEngineRunning)
	{
		RCC_CarControllerV3 rCC_CarControllerV = Object.Instantiate(vehiclePrefab, position, rotation);
		rCC_CarControllerV.gameObject.SetActive(value: true);
		rCC_CarControllerV.SetCanControl(isControllable);
		if (registerAsPlayerVehicle)
		{
			RCC_SceneManager.Instance.RegisterPlayer(rCC_CarControllerV);
		}
		if (isEngineRunning)
		{
			rCC_CarControllerV.StartEngine(instantStart: true);
		}
		else
		{
			rCC_CarControllerV.KillEngine();
		}
		return rCC_CarControllerV;
	}

	public static void RegisterPlayerVehicle(RCC_CarControllerV3 vehicle)
	{
		RCC_SceneManager.Instance.RegisterPlayer(vehicle);
	}

	public static void RegisterPlayerVehicle(RCC_CarControllerV3 vehicle, bool isControllable)
	{
		RCC_SceneManager.Instance.RegisterPlayer(vehicle, isControllable);
	}

	public static void RegisterPlayerVehicle(RCC_CarControllerV3 vehicle, bool isControllable, bool engineState)
	{
		RCC_SceneManager.Instance.RegisterPlayer(vehicle, isControllable, engineState);
	}

	public static void DeRegisterPlayerVehicle()
	{
		RCC_SceneManager.Instance.DeRegisterPlayer();
	}

	public static void SetControl(RCC_CarControllerV3 vehicle, bool isControllable)
	{
		vehicle.SetCanControl(isControllable);
	}

	public static void SetEngine(RCC_CarControllerV3 vehicle, bool engineState)
	{
		if (engineState)
		{
			vehicle.StartEngine();
		}
		else
		{
			vehicle.KillEngine();
		}
	}

	public static void SetMobileController(RCC_Settings.MobileController mobileController)
	{
		RCC_Settings.Instance.mobileController = mobileController;
		Debug.Log("Mobile Controller has been changed to " + mobileController);
	}

	public static void SetUnits()
	{
	}

	public static void SetAutomaticGear()
	{
	}

	public static void StartStopRecord()
	{
		RCC_SceneManager.Instance.Record();
	}

	public static void StartStopReplay()
	{
		RCC_SceneManager.Instance.Play();
	}

	public static void StopRecordReplay()
	{
		RCC_SceneManager.Instance.Stop();
	}

	public static void SetBehavior(int behaviorIndex)
	{
		RCC_SceneManager.SetBehavior(behaviorIndex);
		Debug.Log("Behavior has been changed to " + behaviorIndex);
	}

	public static void SetController(int controllerIndex)
	{
		RCC_SceneManager.SetController(controllerIndex);
		Debug.Log("Main Controller has been changed to " + controllerIndex);
	}

	public static void ChangeCamera()
	{
		RCC_SceneManager.Instance.ChangeCamera();
	}

	public static void Transport(Vector3 position, Quaternion rotation)
	{
		RCC_SceneManager.Instance.Transport(position, rotation);
	}

	public static void Transport(RCC_CarControllerV3 vehicle, Vector3 position, Quaternion rotation)
	{
		RCC_SceneManager.Instance.Transport(vehicle, position, rotation);
	}
}
