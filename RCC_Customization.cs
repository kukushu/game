using UnityEngine;

public class RCC_Customization : MonoBehaviour
{
	public static void SetCustomizationMode(RCC_CarControllerV3 vehicle, bool state)
	{
		if (!vehicle)
		{
			Debug.LogError("Player vehicle is not selected for customization! Use RCC_Customization.SetCustomizationMode(playerVehicle, true/false); for enabling / disabling customization mode for player vehicle.");
			return;
		}
		RCC_Camera activePlayerCamera = RCC_SceneManager.Instance.activePlayerCamera;
		RCC_UIDashboardDisplay activePlayerCanvas = RCC_SceneManager.Instance.activePlayerCanvas;
		if (state)
		{
			vehicle.SetCanControl(state: false);
			if ((bool)activePlayerCamera)
			{
				activePlayerCamera.ChangeCamera(RCC_Camera.CameraMode.TPS);
			}
			if ((bool)activePlayerCanvas)
			{
				activePlayerCanvas.SetDisplayType(RCC_UIDashboardDisplay.DisplayType.Customization);
			}
			return;
		}
		SetSmokeParticle(vehicle, state: false);
		SetExhaustFlame(vehicle, state: false);
		vehicle.SetCanControl(state: true);
		if ((bool)activePlayerCamera)
		{
			activePlayerCamera.ChangeCamera(RCC_Camera.CameraMode.TPS);
		}
		if ((bool)activePlayerCanvas)
		{
			activePlayerCanvas.SetDisplayType(RCC_UIDashboardDisplay.DisplayType.Full);
		}
	}

	public static void OverrideRCC(RCC_CarControllerV3 vehicle)
	{
		CheckVehicle(vehicle);
	}

	public static void SetSmokeParticle(RCC_CarControllerV3 vehicle, bool state)
	{
		if (CheckVehicle(vehicle))
		{
			vehicle.PreviewSmokeParticle(state);
		}
	}

	public static void SetSmokeColor(RCC_CarControllerV3 vehicle, int indexOfGroundMaterial, Color color)
	{
		if (!CheckVehicle(vehicle))
		{
			return;
		}
		RCC_WheelCollider[] componentsInChildren = vehicle.GetComponentsInChildren<RCC_WheelCollider>();
		foreach (RCC_WheelCollider rCC_WheelCollider in componentsInChildren)
		{
			for (int j = 0; j < rCC_WheelCollider.allWheelParticles.Count; j++)
			{
				ParticleSystem.MainModule main = rCC_WheelCollider.allWheelParticles[j].main;
				color.a = main.startColor.color.a;
				main.startColor = color;
			}
		}
	}

	public static void SetHeadlightsColor(RCC_CarControllerV3 vehicle, Color color)
	{
		if (!CheckVehicle(vehicle))
		{
			return;
		}
		vehicle.lowBeamHeadLightsOn = true;
		RCC_Light[] componentsInChildren = vehicle.GetComponentsInChildren<RCC_Light>();
		foreach (RCC_Light rCC_Light in componentsInChildren)
		{
			if (rCC_Light.lightType == RCC_Light.LightType.HeadLight || rCC_Light.lightType == RCC_Light.LightType.HighBeamHeadLight || rCC_Light.lightType == RCC_Light.LightType.ParkLight)
			{
				rCC_Light.GetComponent<Light>().color = color;
			}
		}
	}

	public static void SetExhaustFlame(RCC_CarControllerV3 vehicle, bool state)
	{
		if (CheckVehicle(vehicle))
		{
			RCC_Exhaust[] componentsInChildren = vehicle.GetComponentsInChildren<RCC_Exhaust>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].previewFlames = state;
			}
		}
	}

	public static void SetFrontCambers(RCC_CarControllerV3 vehicle, float camberAngle)
	{
		if (!CheckVehicle(vehicle))
		{
			return;
		}
		RCC_WheelCollider[] componentsInChildren = vehicle.GetComponentsInChildren<RCC_WheelCollider>();
		foreach (RCC_WheelCollider rCC_WheelCollider in componentsInChildren)
		{
			if (rCC_WheelCollider == vehicle.FrontLeftWheelCollider || rCC_WheelCollider == vehicle.FrontRightWheelCollider)
			{
				rCC_WheelCollider.camber = camberAngle;
			}
		}
		OverrideRCC(vehicle);
	}

	public static void SetRearCambers(RCC_CarControllerV3 vehicle, float camberAngle)
	{
		if (!CheckVehicle(vehicle))
		{
			return;
		}
		RCC_WheelCollider[] componentsInChildren = vehicle.GetComponentsInChildren<RCC_WheelCollider>();
		foreach (RCC_WheelCollider rCC_WheelCollider in componentsInChildren)
		{
			if (rCC_WheelCollider != vehicle.FrontLeftWheelCollider && rCC_WheelCollider != vehicle.FrontRightWheelCollider)
			{
				rCC_WheelCollider.camber = camberAngle;
			}
		}
		OverrideRCC(vehicle);
	}

	public static void ChangeWheels(RCC_CarControllerV3 vehicle, GameObject wheel, bool applyRadius)
	{
		if (!CheckVehicle(vehicle))
		{
			return;
		}
		for (int i = 0; i < vehicle.allWheelColliders.Length; i++)
		{
			if ((bool)vehicle.allWheelColliders[i].wheelModel.GetComponent<MeshRenderer>())
			{
				vehicle.allWheelColliders[i].wheelModel.GetComponent<MeshRenderer>().enabled = false;
			}
			foreach (Transform componentInChild in vehicle.allWheelColliders[i].wheelModel.GetComponentInChildren<Transform>())
			{
				componentInChild.gameObject.SetActive(value: false);
			}
			GameObject gameObject = Object.Instantiate(wheel, vehicle.allWheelColliders[i].wheelModel.position, vehicle.allWheelColliders[i].wheelModel.rotation, vehicle.allWheelColliders[i].wheelModel);
			if (vehicle.allWheelColliders[i].wheelModel.localPosition.x > 0f)
			{
				gameObject.transform.localScale = new Vector3(gameObject.transform.localScale.x * -1f, gameObject.transform.localScale.y, gameObject.transform.localScale.z);
			}
			if (applyRadius)
			{
				vehicle.allWheelColliders[i].wheelCollider.radius = RCC_GetBounds.MaxBoundsExtent(wheel.transform);
			}
		}
		OverrideRCC(vehicle);
	}

	public static void SetFrontSuspensionsTargetPos(RCC_CarControllerV3 vehicle, float targetPosition)
	{
		if (CheckVehicle(vehicle))
		{
			targetPosition = Mathf.Clamp01(targetPosition);
			JointSpring suspensionSpring = vehicle.FrontLeftWheelCollider.wheelCollider.suspensionSpring;
			suspensionSpring.targetPosition = 1f - targetPosition;
			vehicle.FrontLeftWheelCollider.wheelCollider.suspensionSpring = suspensionSpring;
			JointSpring suspensionSpring2 = vehicle.FrontRightWheelCollider.wheelCollider.suspensionSpring;
			suspensionSpring2.targetPosition = 1f - targetPosition;
			vehicle.FrontRightWheelCollider.wheelCollider.suspensionSpring = suspensionSpring2;
			OverrideRCC(vehicle);
		}
	}

	public static void SetRearSuspensionsTargetPos(RCC_CarControllerV3 vehicle, float targetPosition)
	{
		if (CheckVehicle(vehicle))
		{
			targetPosition = Mathf.Clamp01(targetPosition);
			JointSpring suspensionSpring = vehicle.RearLeftWheelCollider.wheelCollider.suspensionSpring;
			suspensionSpring.targetPosition = 1f - targetPosition;
			vehicle.RearLeftWheelCollider.wheelCollider.suspensionSpring = suspensionSpring;
			JointSpring suspensionSpring2 = vehicle.RearRightWheelCollider.wheelCollider.suspensionSpring;
			suspensionSpring2.targetPosition = 1f - targetPosition;
			vehicle.RearRightWheelCollider.wheelCollider.suspensionSpring = suspensionSpring2;
			OverrideRCC(vehicle);
		}
	}

	public static void SetAllSuspensionsTargetPos(RCC_CarControllerV3 vehicle, float targetPosition)
	{
		if (CheckVehicle(vehicle))
		{
			targetPosition = Mathf.Clamp01(targetPosition);
			JointSpring suspensionSpring = vehicle.RearLeftWheelCollider.wheelCollider.suspensionSpring;
			suspensionSpring.targetPosition = 1f - targetPosition;
			vehicle.RearLeftWheelCollider.wheelCollider.suspensionSpring = suspensionSpring;
			JointSpring suspensionSpring2 = vehicle.RearRightWheelCollider.wheelCollider.suspensionSpring;
			suspensionSpring2.targetPosition = 1f - targetPosition;
			vehicle.RearRightWheelCollider.wheelCollider.suspensionSpring = suspensionSpring2;
			JointSpring suspensionSpring3 = vehicle.FrontLeftWheelCollider.wheelCollider.suspensionSpring;
			suspensionSpring3.targetPosition = 1f - targetPosition;
			vehicle.FrontLeftWheelCollider.wheelCollider.suspensionSpring = suspensionSpring3;
			JointSpring suspensionSpring4 = vehicle.FrontRightWheelCollider.wheelCollider.suspensionSpring;
			suspensionSpring4.targetPosition = 1f - targetPosition;
			vehicle.FrontRightWheelCollider.wheelCollider.suspensionSpring = suspensionSpring4;
			OverrideRCC(vehicle);
		}
	}

	public static void SetFrontSuspensionsDistances(RCC_CarControllerV3 vehicle, float distance)
	{
		if (CheckVehicle(vehicle))
		{
			if (distance <= 0f)
			{
				distance = 0.05f;
			}
			vehicle.FrontLeftWheelCollider.wheelCollider.suspensionDistance = distance;
			vehicle.FrontRightWheelCollider.wheelCollider.suspensionDistance = distance;
			OverrideRCC(vehicle);
		}
	}

	public static void SetRearSuspensionsDistances(RCC_CarControllerV3 vehicle, float distance)
	{
		if (!CheckVehicle(vehicle))
		{
			return;
		}
		if (distance <= 0f)
		{
			distance = 0.05f;
		}
		vehicle.RearLeftWheelCollider.wheelCollider.suspensionDistance = distance;
		vehicle.RearRightWheelCollider.wheelCollider.suspensionDistance = distance;
		if (vehicle.ExtraRearWheelsCollider != null && vehicle.ExtraRearWheelsCollider.Length != 0)
		{
			RCC_WheelCollider[] extraRearWheelsCollider = vehicle.ExtraRearWheelsCollider;
			for (int i = 0; i < extraRearWheelsCollider.Length; i++)
			{
				extraRearWheelsCollider[i].wheelCollider.suspensionDistance = distance;
			}
		}
		OverrideRCC(vehicle);
	}

	public static void SetDrivetrainMode(RCC_CarControllerV3 vehicle, RCC_CarControllerV3.WheelType mode)
	{
		if (CheckVehicle(vehicle))
		{
			vehicle.wheelTypeChoise = mode;
			OverrideRCC(vehicle);
		}
	}

	public static void SetGearShiftingThreshold(RCC_CarControllerV3 vehicle, float targetValue)
	{
		if (CheckVehicle(vehicle))
		{
			vehicle.gearShiftingThreshold = targetValue;
			OverrideRCC(vehicle);
		}
	}

	public static void SetClutchThreshold(RCC_CarControllerV3 vehicle, float targetValue)
	{
		if (CheckVehicle(vehicle))
		{
			vehicle.clutchInertia = targetValue;
			OverrideRCC(vehicle);
		}
	}

	public static void SetCounterSteering(RCC_CarControllerV3 vehicle, bool state)
	{
		if (CheckVehicle(vehicle))
		{
			vehicle.useCounterSteering = state;
			OverrideRCC(vehicle);
		}
	}

	public static void SetNOS(RCC_CarControllerV3 vehicle, bool state)
	{
		if (CheckVehicle(vehicle))
		{
			vehicle.useNOS = state;
			OverrideRCC(vehicle);
		}
	}

	public static void SetTurbo(RCC_CarControllerV3 vehicle, bool state)
	{
		if (CheckVehicle(vehicle))
		{
			vehicle.useTurbo = state;
			OverrideRCC(vehicle);
		}
	}

	public static void SetUseExhaustFlame(RCC_CarControllerV3 vehicle, bool state)
	{
		if (CheckVehicle(vehicle))
		{
			vehicle.useExhaustFlame = state;
			OverrideRCC(vehicle);
		}
	}

	public static void SetRevLimiter(RCC_CarControllerV3 vehicle, bool state)
	{
		if (CheckVehicle(vehicle))
		{
			vehicle.useRevLimiter = state;
			OverrideRCC(vehicle);
		}
	}

	public static void SetFrontSuspensionsSpringForce(RCC_CarControllerV3 vehicle, float targetValue)
	{
		if (CheckVehicle(vehicle))
		{
			JointSpring suspensionSpring = vehicle.FrontLeftWheelCollider.GetComponent<WheelCollider>().suspensionSpring;
			suspensionSpring.spring = targetValue;
			vehicle.FrontLeftWheelCollider.GetComponent<WheelCollider>().suspensionSpring = suspensionSpring;
			vehicle.FrontRightWheelCollider.GetComponent<WheelCollider>().suspensionSpring = suspensionSpring;
			OverrideRCC(vehicle);
		}
	}

	public static void SetRearSuspensionsSpringForce(RCC_CarControllerV3 vehicle, float targetValue)
	{
		if (CheckVehicle(vehicle))
		{
			JointSpring suspensionSpring = vehicle.RearLeftWheelCollider.GetComponent<WheelCollider>().suspensionSpring;
			suspensionSpring.spring = targetValue;
			vehicle.RearLeftWheelCollider.GetComponent<WheelCollider>().suspensionSpring = suspensionSpring;
			vehicle.RearRightWheelCollider.GetComponent<WheelCollider>().suspensionSpring = suspensionSpring;
			OverrideRCC(vehicle);
		}
	}

	public static void SetFrontSuspensionsSpringDamper(RCC_CarControllerV3 vehicle, float targetValue)
	{
		if (CheckVehicle(vehicle))
		{
			JointSpring suspensionSpring = vehicle.FrontLeftWheelCollider.GetComponent<WheelCollider>().suspensionSpring;
			suspensionSpring.damper = targetValue;
			vehicle.FrontLeftWheelCollider.GetComponent<WheelCollider>().suspensionSpring = suspensionSpring;
			vehicle.FrontRightWheelCollider.GetComponent<WheelCollider>().suspensionSpring = suspensionSpring;
			OverrideRCC(vehicle);
		}
	}

	public static void SetRearSuspensionsSpringDamper(RCC_CarControllerV3 vehicle, float targetValue)
	{
		if (CheckVehicle(vehicle))
		{
			JointSpring suspensionSpring = vehicle.RearLeftWheelCollider.GetComponent<WheelCollider>().suspensionSpring;
			suspensionSpring.damper = targetValue;
			vehicle.RearLeftWheelCollider.GetComponent<WheelCollider>().suspensionSpring = suspensionSpring;
			vehicle.RearRightWheelCollider.GetComponent<WheelCollider>().suspensionSpring = suspensionSpring;
			OverrideRCC(vehicle);
		}
	}

	public static void SetMaximumSpeed(RCC_CarControllerV3 vehicle, float targetValue)
	{
		if (CheckVehicle(vehicle))
		{
			vehicle.maxspeed = Mathf.Clamp(targetValue, 10f, 400f);
			OverrideRCC(vehicle);
		}
	}

	public static void SetMaximumTorque(RCC_CarControllerV3 vehicle, float targetValue)
	{
		if (CheckVehicle(vehicle))
		{
			vehicle.maxEngineTorque = Mathf.Clamp(targetValue, 5f, 50000f);
			OverrideRCC(vehicle);
		}
	}

	public static void SetMaximumBrake(RCC_CarControllerV3 vehicle, float targetValue)
	{
		if (CheckVehicle(vehicle))
		{
			vehicle.brakeTorque = Mathf.Clamp(targetValue, 0f, 50000f);
			OverrideRCC(vehicle);
		}
	}

	public static void Repair(RCC_CarControllerV3 vehicle)
	{
		if (CheckVehicle(vehicle))
		{
			vehicle.repairNow = true;
		}
	}

	public static void SetESP(RCC_CarControllerV3 vehicle, bool state)
	{
		if (CheckVehicle(vehicle))
		{
			vehicle.ESP = state;
		}
	}

	public static void SetABS(RCC_CarControllerV3 vehicle, bool state)
	{
		if (CheckVehicle(vehicle))
		{
			vehicle.ABS = state;
		}
	}

	public static void SetTCS(RCC_CarControllerV3 vehicle, bool state)
	{
		if (CheckVehicle(vehicle))
		{
			vehicle.TCS = state;
		}
	}

	public static void SetSH(RCC_CarControllerV3 vehicle, bool state)
	{
		if (CheckVehicle(vehicle))
		{
			vehicle.steeringHelper = state;
		}
	}

	public static void SetSHStrength(RCC_CarControllerV3 vehicle, float value)
	{
		if (CheckVehicle(vehicle))
		{
			vehicle.steeringHelper = true;
			vehicle.steerHelperLinearVelStrength = value;
			vehicle.steerHelperAngularVelStrength = value;
		}
	}

	public static void SetTransmission(bool automatic)
	{
		RCC_Settings.Instance.useAutomaticGear = automatic;
	}

	public static void SaveStats(RCC_CarControllerV3 vehicle)
	{
		if (!CheckVehicle(vehicle))
		{
			return;
		}
		PlayerPrefs.SetFloat(vehicle.transform.name + "_FrontCamber", vehicle.FrontLeftWheelCollider.camber);
		PlayerPrefs.SetFloat(vehicle.transform.name + "_RearCamber", vehicle.RearLeftWheelCollider.camber);
		PlayerPrefs.SetFloat(vehicle.transform.name + "_FrontSuspensionsDistance", vehicle.FrontLeftWheelCollider.wheelCollider.suspensionDistance);
		PlayerPrefs.SetFloat(vehicle.transform.name + "_RearSuspensionsDistance", vehicle.RearLeftWheelCollider.wheelCollider.suspensionDistance);
		PlayerPrefs.SetFloat(vehicle.transform.name + "_FrontSuspensionsSpring", vehicle.FrontLeftWheelCollider.wheelCollider.suspensionSpring.spring);
		PlayerPrefs.SetFloat(vehicle.transform.name + "_RearSuspensionsSpring", vehicle.RearLeftWheelCollider.wheelCollider.suspensionSpring.spring);
		PlayerPrefs.SetFloat(vehicle.transform.name + "_FrontSuspensionsDamper", vehicle.FrontLeftWheelCollider.wheelCollider.suspensionSpring.damper);
		PlayerPrefs.SetFloat(vehicle.transform.name + "_RearSuspensionsDamper", vehicle.RearLeftWheelCollider.wheelCollider.suspensionSpring.damper);
		PlayerPrefs.SetFloat(vehicle.transform.name + "_MaximumSpeed", vehicle.maxspeed);
		PlayerPrefs.SetFloat(vehicle.transform.name + "_MaximumBrake", vehicle.brakeTorque);
		PlayerPrefs.SetFloat(vehicle.transform.name + "_MaximumTorque", vehicle.maxEngineTorque);
		PlayerPrefs.SetString(vehicle.transform.name + "_DrivetrainMode", vehicle.wheelTypeChoise.ToString());
		PlayerPrefs.SetFloat(vehicle.transform.name + "_GearShiftingThreshold", vehicle.gearShiftingThreshold);
		PlayerPrefs.SetFloat(vehicle.transform.name + "_ClutchingThreshold", vehicle.clutchInertia);
		RCC_PlayerPrefsX.SetBool(vehicle.transform.name + "_CounterSteering", vehicle.useCounterSteering);
		RCC_Light[] componentsInChildren = vehicle.GetComponentsInChildren<RCC_Light>();
		foreach (RCC_Light rCC_Light in componentsInChildren)
		{
			if (rCC_Light.lightType == RCC_Light.LightType.HeadLight)
			{
				RCC_PlayerPrefsX.SetColor(vehicle.transform.name + "_HeadlightsColor", rCC_Light.GetComponentInChildren<Light>().color);
				break;
			}
		}
		ParticleSystem.MainModule main = vehicle.RearLeftWheelCollider.allWheelParticles[0].main;
		RCC_PlayerPrefsX.SetColor(vehicle.transform.name + "_WheelsSmokeColor", main.startColor.color);
		RCC_PlayerPrefsX.SetBool(vehicle.transform.name + "_ABS", vehicle.ABS);
		RCC_PlayerPrefsX.SetBool(vehicle.transform.name + "_ESP", vehicle.ESP);
		RCC_PlayerPrefsX.SetBool(vehicle.transform.name + "_TCS", vehicle.TCS);
		RCC_PlayerPrefsX.SetBool(vehicle.transform.name + "_SH", vehicle.steeringHelper);
		RCC_PlayerPrefsX.SetBool(vehicle.transform.name + "NOS", vehicle.useNOS);
		RCC_PlayerPrefsX.SetBool(vehicle.transform.name + "Turbo", vehicle.useTurbo);
		RCC_PlayerPrefsX.SetBool(vehicle.transform.name + "ExhaustFlame", vehicle.useExhaustFlame);
		RCC_PlayerPrefsX.SetBool(vehicle.transform.name + "RevLimiter", vehicle.useRevLimiter);
	}

	public static void LoadStats(RCC_CarControllerV3 vehicle)
	{
		if (CheckVehicle(vehicle))
		{
			SetFrontCambers(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_FrontCamber", vehicle.FrontLeftWheelCollider.camber));
			SetRearCambers(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_RearCamber", vehicle.RearLeftWheelCollider.camber));
			SetFrontSuspensionsDistances(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_FrontSuspensionsDistance", vehicle.FrontLeftWheelCollider.wheelCollider.suspensionDistance));
			SetRearSuspensionsDistances(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_RearSuspensionsDistance", vehicle.RearLeftWheelCollider.wheelCollider.suspensionDistance));
			SetFrontSuspensionsSpringForce(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_FrontSuspensionsSpring", vehicle.FrontLeftWheelCollider.wheelCollider.suspensionSpring.spring));
			SetRearSuspensionsSpringForce(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_RearSuspensionsSpring", vehicle.RearLeftWheelCollider.wheelCollider.suspensionSpring.spring));
			SetFrontSuspensionsSpringDamper(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_FrontSuspensionsDamper", vehicle.FrontLeftWheelCollider.wheelCollider.suspensionSpring.damper));
			SetRearSuspensionsSpringDamper(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_RearSuspensionsDamper", vehicle.RearLeftWheelCollider.wheelCollider.suspensionSpring.damper));
			SetMaximumSpeed(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_MaximumSpeed", vehicle.maxspeed));
			SetMaximumBrake(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_MaximumBrake", vehicle.brakeTorque));
			SetMaximumTorque(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_MaximumTorque", vehicle.maxEngineTorque));
			switch (PlayerPrefs.GetString(vehicle.transform.name + "_DrivetrainMode", vehicle.wheelTypeChoise.ToString()))
			{
			case "FWD":
				vehicle.wheelTypeChoise = RCC_CarControllerV3.WheelType.FWD;
				break;
			case "RWD":
				vehicle.wheelTypeChoise = RCC_CarControllerV3.WheelType.RWD;
				break;
			case "AWD":
				vehicle.wheelTypeChoise = RCC_CarControllerV3.WheelType.AWD;
				break;
			}
			SetGearShiftingThreshold(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_GearShiftingThreshold", vehicle.gearShiftingThreshold));
			SetClutchThreshold(vehicle, PlayerPrefs.GetFloat(vehicle.transform.name + "_ClutchingThreshold", vehicle.clutchInertia));
			SetCounterSteering(vehicle, RCC_PlayerPrefsX.GetBool(vehicle.transform.name + "_CounterSteering", vehicle.useCounterSteering));
			SetABS(vehicle, RCC_PlayerPrefsX.GetBool(vehicle.transform.name + "_ABS", vehicle.ABS));
			SetESP(vehicle, RCC_PlayerPrefsX.GetBool(vehicle.transform.name + "_ESP", vehicle.ESP));
			SetTCS(vehicle, RCC_PlayerPrefsX.GetBool(vehicle.transform.name + "_TCS", vehicle.TCS));
			SetSH(vehicle, RCC_PlayerPrefsX.GetBool(vehicle.transform.name + "_SH", vehicle.steeringHelper));
			SetNOS(vehicle, RCC_PlayerPrefsX.GetBool(vehicle.transform.name + "NOS", vehicle.useNOS));
			SetTurbo(vehicle, RCC_PlayerPrefsX.GetBool(vehicle.transform.name + "Turbo", vehicle.useTurbo));
			SetUseExhaustFlame(vehicle, RCC_PlayerPrefsX.GetBool(vehicle.transform.name + "ExhaustFlame", vehicle.useExhaustFlame));
			SetRevLimiter(vehicle, RCC_PlayerPrefsX.GetBool(vehicle.transform.name + "RevLimiter", vehicle.useRevLimiter));
			if (PlayerPrefs.HasKey(vehicle.transform.name + "_WheelsSmokeColor"))
			{
				SetSmokeColor(vehicle, 0, RCC_PlayerPrefsX.GetColor(vehicle.transform.name + "_WheelsSmokeColor"));
			}
			if (PlayerPrefs.HasKey(vehicle.transform.name + "_HeadlightsColor"))
			{
				SetHeadlightsColor(vehicle, RCC_PlayerPrefsX.GetColor(vehicle.transform.name + "_HeadlightsColor"));
			}
			OverrideRCC(vehicle);
		}
	}

	public static void ResetStats(RCC_CarControllerV3 vehicle, RCC_CarControllerV3 defaultCar)
	{
		if (CheckVehicle(vehicle) && CheckVehicle(defaultCar))
		{
			SetFrontCambers(vehicle, defaultCar.FrontLeftWheelCollider.camber);
			SetRearCambers(vehicle, defaultCar.RearLeftWheelCollider.camber);
			SetFrontSuspensionsDistances(vehicle, defaultCar.FrontLeftWheelCollider.wheelCollider.suspensionDistance);
			SetRearSuspensionsDistances(vehicle, defaultCar.RearLeftWheelCollider.wheelCollider.suspensionDistance);
			SetFrontSuspensionsSpringForce(vehicle, defaultCar.FrontLeftWheelCollider.wheelCollider.suspensionSpring.spring);
			SetRearSuspensionsSpringForce(vehicle, defaultCar.RearLeftWheelCollider.wheelCollider.suspensionSpring.spring);
			SetFrontSuspensionsSpringDamper(vehicle, defaultCar.FrontLeftWheelCollider.wheelCollider.suspensionSpring.damper);
			SetRearSuspensionsSpringDamper(vehicle, defaultCar.RearLeftWheelCollider.wheelCollider.suspensionSpring.damper);
			SetMaximumSpeed(vehicle, defaultCar.maxspeed);
			SetMaximumBrake(vehicle, defaultCar.brakeTorque);
			SetMaximumTorque(vehicle, defaultCar.maxEngineTorque);
			switch (defaultCar.wheelTypeChoise.ToString())
			{
			case "FWD":
				vehicle.wheelTypeChoise = RCC_CarControllerV3.WheelType.FWD;
				break;
			case "RWD":
				vehicle.wheelTypeChoise = RCC_CarControllerV3.WheelType.RWD;
				break;
			case "AWD":
				vehicle.wheelTypeChoise = RCC_CarControllerV3.WheelType.AWD;
				break;
			}
			SetGearShiftingThreshold(vehicle, defaultCar.gearShiftingThreshold);
			SetClutchThreshold(vehicle, defaultCar.clutchInertia);
			SetCounterSteering(vehicle, defaultCar.useCounterSteering);
			SetABS(vehicle, defaultCar.ABS);
			SetESP(vehicle, defaultCar.ESP);
			SetTCS(vehicle, defaultCar.TCS);
			SetSH(vehicle, defaultCar.steeringHelper);
			SetNOS(vehicle, defaultCar.useNOS);
			SetTurbo(vehicle, defaultCar.useTurbo);
			SetUseExhaustFlame(vehicle, defaultCar.useExhaustFlame);
			SetRevLimiter(vehicle, defaultCar.useRevLimiter);
			SetSmokeColor(vehicle, 0, Color.white);
			SetHeadlightsColor(vehicle, Color.white);
			SaveStats(vehicle);
			OverrideRCC(vehicle);
		}
	}

	public static bool CheckVehicle(RCC_CarControllerV3 vehicle)
	{
		if (!vehicle)
		{
			Debug.LogError("Vehicle is missing!");
			return false;
		}
		return true;
	}
}
