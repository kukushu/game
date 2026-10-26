using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class RCC_Core : MonoBehaviour
{
	public static AudioSource NewAudioSource(AudioMixerGroup audioMixer, GameObject go, string audioName, float minDistance, float maxDistance, float volume, AudioClip audioClip, bool loop, bool playNow, bool destroyAfterFinished)
	{
		GameObject gameObject = new GameObject(audioName);
		if ((bool)go.transform.Find("All Audio Sources"))
		{
			gameObject.transform.SetParent(go.transform.Find("All Audio Sources"));
		}
		else
		{
			GameObject gameObject2 = new GameObject("All Audio Sources");
			gameObject2.transform.SetParent(go.transform, worldPositionStays: false);
			gameObject.transform.SetParent(gameObject2.transform, worldPositionStays: false);
		}
		gameObject.transform.position = go.transform.position;
		gameObject.transform.rotation = go.transform.rotation;
		gameObject.AddComponent<AudioSource>();
		AudioSource component = gameObject.GetComponent<AudioSource>();
		if ((bool)audioMixer)
		{
			component.outputAudioMixerGroup = audioMixer;
		}
		component.minDistance = minDistance;
		component.maxDistance = maxDistance;
		component.volume = volume;
		component.clip = audioClip;
		component.loop = loop;
		component.dopplerLevel = 0.5f;
		if (minDistance == 0f && maxDistance == 0f)
		{
			component.spatialBlend = 0f;
		}
		else
		{
			component.spatialBlend = 1f;
		}
		if (playNow)
		{
			component.playOnAwake = true;
			component.Play();
		}
		else
		{
			component.playOnAwake = false;
		}
		if (destroyAfterFinished)
		{
			if ((bool)audioClip)
			{
				Object.Destroy(gameObject, audioClip.length);
			}
			else
			{
				Object.Destroy(gameObject);
			}
		}
		return component;
	}

	public static AudioSource NewAudioSource(GameObject go, string audioName, float minDistance, float maxDistance, float volume, AudioClip audioClip, bool loop, bool playNow, bool destroyAfterFinished)
	{
		GameObject gameObject = new GameObject(audioName);
		if ((bool)go.transform.Find("All Audio Sources"))
		{
			gameObject.transform.SetParent(go.transform.Find("All Audio Sources"));
		}
		else
		{
			GameObject gameObject2 = new GameObject("All Audio Sources");
			gameObject2.transform.SetParent(go.transform, worldPositionStays: false);
			gameObject.transform.SetParent(gameObject2.transform, worldPositionStays: false);
		}
		gameObject.transform.position = go.transform.position;
		gameObject.transform.rotation = go.transform.rotation;
		gameObject.AddComponent<AudioSource>();
		AudioSource component = gameObject.GetComponent<AudioSource>();
		component.minDistance = minDistance;
		component.maxDistance = maxDistance;
		component.volume = volume;
		component.clip = audioClip;
		component.loop = loop;
		component.dopplerLevel = 0.5f;
		if (minDistance == 0f && maxDistance == 0f)
		{
			component.spatialBlend = 0f;
		}
		else
		{
			component.spatialBlend = 1f;
		}
		if (playNow)
		{
			component.playOnAwake = true;
			component.Play();
		}
		else
		{
			component.playOnAwake = false;
		}
		if (destroyAfterFinished)
		{
			if ((bool)audioClip)
			{
				Object.Destroy(gameObject, audioClip.length);
			}
			else
			{
				Object.Destroy(gameObject);
			}
		}
		return component;
	}

	public static AudioSource NewAudioSource(AudioMixerGroup audioMixer, GameObject go, Vector3 localPosition, string audioName, float minDistance, float maxDistance, float volume, AudioClip audioClip, bool loop, bool playNow, bool destroyAfterFinished)
	{
		GameObject gameObject = new GameObject(audioName);
		if ((bool)go.transform.Find("All Audio Sources"))
		{
			gameObject.transform.SetParent(go.transform.Find("All Audio Sources"));
		}
		else
		{
			GameObject gameObject2 = new GameObject("All Audio Sources");
			gameObject2.transform.SetParent(go.transform, worldPositionStays: false);
			gameObject.transform.SetParent(gameObject2.transform, worldPositionStays: false);
		}
		gameObject.transform.position = go.transform.position;
		gameObject.transform.rotation = go.transform.rotation;
		gameObject.transform.localPosition = localPosition;
		gameObject.AddComponent<AudioSource>();
		AudioSource component = gameObject.GetComponent<AudioSource>();
		if ((bool)audioMixer)
		{
			component.outputAudioMixerGroup = audioMixer;
		}
		component.minDistance = minDistance;
		component.maxDistance = maxDistance;
		component.volume = volume;
		component.clip = audioClip;
		component.loop = loop;
		component.dopplerLevel = 0.5f;
		if (minDistance == 0f && maxDistance == 0f)
		{
			component.spatialBlend = 0f;
		}
		else
		{
			component.spatialBlend = 1f;
		}
		if (playNow)
		{
			component.playOnAwake = true;
			component.Play();
		}
		else
		{
			component.playOnAwake = false;
		}
		if (destroyAfterFinished)
		{
			if ((bool)audioClip)
			{
				Object.Destroy(gameObject, audioClip.length);
			}
			else
			{
				Object.Destroy(gameObject);
			}
		}
		return component;
	}

	public static AudioSource NewAudioSource(GameObject go, Vector3 localPosition, string audioName, float minDistance, float maxDistance, float volume, AudioClip audioClip, bool loop, bool playNow, bool destroyAfterFinished)
	{
		GameObject gameObject = new GameObject(audioName);
		if ((bool)go.transform.Find("All Audio Sources"))
		{
			gameObject.transform.SetParent(go.transform.Find("All Audio Sources"));
		}
		else
		{
			GameObject gameObject2 = new GameObject("All Audio Sources");
			gameObject2.transform.SetParent(go.transform, worldPositionStays: false);
			gameObject.transform.SetParent(gameObject2.transform, worldPositionStays: false);
		}
		gameObject.transform.position = go.transform.position;
		gameObject.transform.rotation = go.transform.rotation;
		gameObject.transform.localPosition = localPosition;
		gameObject.AddComponent<AudioSource>();
		AudioSource component = gameObject.GetComponent<AudioSource>();
		component.minDistance = minDistance;
		component.maxDistance = maxDistance;
		component.volume = volume;
		component.clip = audioClip;
		component.loop = loop;
		component.dopplerLevel = 0.5f;
		if (minDistance == 0f && maxDistance == 0f)
		{
			component.spatialBlend = 0f;
		}
		else
		{
			component.spatialBlend = 1f;
		}
		if (playNow)
		{
			component.playOnAwake = true;
			component.Play();
		}
		else
		{
			component.playOnAwake = false;
		}
		if (destroyAfterFinished)
		{
			if ((bool)audioClip)
			{
				Object.Destroy(gameObject, audioClip.length);
			}
			else
			{
				Object.Destroy(gameObject);
			}
		}
		return component;
	}

	public static void NewHighPassFilter(AudioSource source, float freq, int level)
	{
		if (!(source == null))
		{
			AudioHighPassFilter audioHighPassFilter = source.gameObject.AddComponent<AudioHighPassFilter>();
			audioHighPassFilter.cutoffFrequency = freq;
			audioHighPassFilter.highpassResonanceQ = level;
		}
	}

	public static void NewLowPassFilter(AudioSource source, float freq)
	{
		if (!(source == null))
		{
			source.gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency = freq;
		}
	}

	public void CreateWheelColliders(RCC_CarControllerV3 carController)
	{
		List<Transform> list = new List<Transform>();
		list.Add(carController.FrontLeftWheelTransform);
		list.Add(carController.FrontRightWheelTransform);
		list.Add(carController.RearLeftWheelTransform);
		list.Add(carController.RearRightWheelTransform);
		if (carController.ExtraRearWheelsTransform.Length != 0 && (bool)carController.ExtraRearWheelsTransform[0])
		{
			Transform[] extraRearWheelsTransform = carController.ExtraRearWheelsTransform;
			foreach (Transform item in extraRearWheelsTransform)
			{
				list.Add(item);
			}
		}
		if (list != null && list[0] == null)
		{
			Debug.LogError("You haven't choosen your Wheel Models. Please select all of your Wheel Models before creating Wheel Colliders. Script needs to know their sizes and positions, aye?");
			return;
		}
		Quaternion rotation = transform.rotation;
		transform.rotation = Quaternion.identity;
		GameObject gameObject = new GameObject("Wheel Colliders");
		gameObject.transform.SetParent(transform, worldPositionStays: false);
		gameObject.transform.localRotation = Quaternion.identity;
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.transform.localScale = Vector3.one;
		foreach (Transform item2 in list)
		{
			GameObject gameObject2 = new GameObject(item2.transform.name);
			gameObject2.transform.position = RCC_GetBounds.GetBoundsCenter(item2.transform);
			gameObject2.transform.rotation = transform.rotation;
			gameObject2.transform.name = item2.transform.name;
			gameObject2.transform.SetParent(gameObject.transform);
			gameObject2.transform.localScale = Vector3.one;
			gameObject2.AddComponent<WheelCollider>();
			Bounds bounds = default;
			Renderer[] componentsInChildren = item2.GetComponentsInChildren<Renderer>();
			foreach (Renderer renderer in componentsInChildren)
			{
				if (renderer != GetComponent<Renderer>() && renderer.bounds.size.z > bounds.size.z)
				{
					bounds = renderer.bounds;
				}
			}
			gameObject2.GetComponent<WheelCollider>().radius = bounds.extents.y / transform.localScale.y;
			gameObject2.AddComponent<RCC_WheelCollider>();
			JointSpring suspensionSpring = gameObject2.GetComponent<WheelCollider>().suspensionSpring;
			suspensionSpring.spring = 40000f;
			suspensionSpring.damper = 1500f;
			suspensionSpring.targetPosition = 0.5f;
			gameObject2.GetComponent<WheelCollider>().suspensionSpring = suspensionSpring;
			gameObject2.GetComponent<WheelCollider>().suspensionDistance = 0.2f;
			gameObject2.GetComponent<WheelCollider>().forceAppPointDistance = 0f;
			gameObject2.GetComponent<WheelCollider>().mass = 40f;
			gameObject2.GetComponent<WheelCollider>().wheelDampingRate = 1f;
			WheelFrictionCurve sidewaysFriction = gameObject2.GetComponent<WheelCollider>().sidewaysFriction;
			WheelFrictionCurve forwardFriction = gameObject2.GetComponent<WheelCollider>().forwardFriction;
			forwardFriction.extremumSlip = 0.4f;
			forwardFriction.extremumValue = 1f;
			forwardFriction.asymptoteSlip = 0.8f;
			forwardFriction.asymptoteValue = 0.6f;
			forwardFriction.stiffness = 1f;
			sidewaysFriction.extremumSlip = 0.25f;
			sidewaysFriction.extremumValue = 1f;
			sidewaysFriction.asymptoteSlip = 0.5f;
			sidewaysFriction.asymptoteValue = 0.8f;
			sidewaysFriction.stiffness = 1f;
			gameObject2.GetComponent<WheelCollider>().sidewaysFriction = sidewaysFriction;
			gameObject2.GetComponent<WheelCollider>().forwardFriction = forwardFriction;
		}
		RCC_WheelCollider[] array = new RCC_WheelCollider[list.Count];
		array = GetComponentsInChildren<RCC_WheelCollider>();
		carController.FrontLeftWheelCollider = array[0];
		carController.FrontRightWheelCollider = array[1];
		carController.RearLeftWheelCollider = array[2];
		carController.RearRightWheelCollider = array[3];
		carController.ExtraRearWheelsCollider = new RCC_WheelCollider[carController.ExtraRearWheelsTransform.Length];
		for (int j = 0; j < carController.ExtraRearWheelsTransform.Length; j++)
		{
			carController.ExtraRearWheelsCollider[j] = array[j + 4];
		}
		transform.rotation = rotation;
	}

	public void SetBehavior(RCC_CarControllerV3 carController)
	{
		if (RCC_Settings.Instance.selectedBehaviorType != null)
		{
			RCC_Settings.BehaviorType selectedBehaviorType = RCC_Settings.Instance.selectedBehaviorType;
			carController.steeringHelper = selectedBehaviorType.steeringHelper;
			carController.tractionHelper = selectedBehaviorType.tractionHelper;
			carController.angularDragHelper = selectedBehaviorType.angularDragHelper;
			carController.useCounterSteering = selectedBehaviorType.counterSteering;
			carController.ABS = selectedBehaviorType.ABS;
			carController.ESP = selectedBehaviorType.ESP;
			carController.TCS = selectedBehaviorType.TCS;
			carController.highspeedsteerAngle = Mathf.Clamp(carController.highspeedsteerAngle, selectedBehaviorType.highSpeedSteerAngleMinimum, selectedBehaviorType.highSpeedSteerAngleMaximum);
			carController.highspeedsteerAngleAtspeed = Mathf.Clamp(carController.highspeedsteerAngleAtspeed, selectedBehaviorType.highSpeedSteerAngleAtspeedMinimum, selectedBehaviorType.highSpeedSteerAngleAtspeedMaximum);
			carController.counterSteeringFactor = Mathf.Clamp(carController.counterSteeringFactor, selectedBehaviorType.counterSteeringMinimum, selectedBehaviorType.counterSteeringMaximum);
			carController.steerHelperAngularVelStrength = Mathf.Clamp(carController.steerHelperAngularVelStrength, selectedBehaviorType.steerHelperAngularVelStrengthMinimum, selectedBehaviorType.steerHelperAngularVelStrengthMaximum);
			carController.steerHelperLinearVelStrength = Mathf.Clamp(carController.steerHelperLinearVelStrength, selectedBehaviorType.steerHelperLinearVelStrengthMinimum, selectedBehaviorType.steerHelperLinearVelStrengthMaximum);
			carController.tractionHelperStrength = Mathf.Clamp(carController.tractionHelperStrength, selectedBehaviorType.tractionHelperStrengthMinimum, selectedBehaviorType.tractionHelperStrengthMaximum);
			carController.antiRollFrontHorizontal = Mathf.Clamp(carController.antiRollFrontHorizontal, selectedBehaviorType.antiRollFrontHorizontalMinimum, float.PositiveInfinity);
			carController.antiRollRearHorizontal = Mathf.Clamp(carController.antiRollRearHorizontal, selectedBehaviorType.antiRollRearHorizontalMinimum, float.PositiveInfinity);
			carController.gearShiftingDelay = Mathf.Clamp(carController.gearShiftingDelay, 0f, selectedBehaviorType.gearShiftingDelayMaximum);
			carController.rigid.angularDrag = Mathf.Clamp(carController.rigid.angularDrag, selectedBehaviorType.angularDrag, 1f);
			carController.angularDragHelperStrength = Mathf.Clamp(carController.angularDragHelperStrength, selectedBehaviorType.angularDragHelperMinimum, selectedBehaviorType.angularDragHelperMaximum);
		}
	}
}
