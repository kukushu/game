using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Main/RCC Scene Manager")]
public class RCC_SceneManager : MonoBehaviour
{
	public enum RecordMode
	{
		Neutral,
		Play,
		Record
	}

	public delegate void onControllerChanged();

	public delegate void onBehaviorChanged();

	public delegate void onVehicleChanged();

	private static RCC_SceneManager instance;

	public RCC_CarControllerV3 activePlayerVehicle;

	private RCC_CarControllerV3 lastActivePlayerVehicle;

	public RCC_Camera activePlayerCamera;

	public RCC_UIDashboardDisplay activePlayerCanvas;

	public Camera activeMainCamera;

	public bool registerFirstVehicleAsPlayer = true;

	public bool disableUIWhenNoPlayerVehicle;

	public bool loadCustomizationAtFirst = true;

	public List<RCC_Recorder> allRecorders = new List<RCC_Recorder>();

	public RecordMode recordMode;

	private float orgTimeScale = 1f;

	public List<RCC_CarControllerV3> allVehicles = new List<RCC_CarControllerV3>();

	public static RCC_SceneManager Instance
	{
		get
		{
			if (instance == null)
			{
				instance = Object.FindObjectOfType<RCC_SceneManager>();
				if (instance == null)
				{
					instance = new GameObject("_RCCSceneManager").AddComponent<RCC_SceneManager>();
				}
			}
			return instance;
		}
	}

	public static event onControllerChanged OnControllerChanged;

	public static event onBehaviorChanged OnBehaviorChanged;

	public static event onVehicleChanged OnVehicleChanged;

	private void Awake()
	{
		if (RCC_Settings.Instance.overrideFPS)
		{
			Application.targetFrameRate = RCC_Settings.Instance.maxFPS;
		}
		if (RCC_Settings.Instance.useTelemetry)
		{
			Object.Instantiate(RCC_Settings.Instance.RCCTelemetry, Vector3.zero, Quaternion.identity);
		}
		RCC_Camera.OnBCGCameraSpawned += RCC_Camera_OnBCGCameraSpawned;
		RCC_CarControllerV3.OnRCCPlayerSpawned += RCC_CarControllerV3_OnRCCSpawned;
		RCC_AICarController.OnRCCAISpawned += RCC_AICarController_OnRCCAISpawned;
		RCC_CarControllerV3.OnRCCPlayerDestroyed += RCC_CarControllerV3_OnRCCPlayerDestroyed;
		RCC_AICarController.OnRCCAIDestroyed += RCC_AICarController_OnRCCAIDestroyed;
		activePlayerCanvas = Object.FindObjectOfType<RCC_UIDashboardDisplay>();
		orgTimeScale = Time.timeScale;
		if (RCC_Settings.Instance.lockAndUnlockCursor)
		{
			Cursor.lockState = CursorLockMode.Locked;
		}
		XRSettings.enabled = RCC_Settings.Instance.useVR;
	}

	private void RCC_CarControllerV3_OnRCCSpawned(RCC_CarControllerV3 RCC)
	{
		if (!allVehicles.Contains(RCC))
		{
			allVehicles.Add(RCC);
			allRecorders = new List<RCC_Recorder>();
			allRecorders.AddRange(gameObject.GetComponentsInChildren<RCC_Recorder>());
			RCC_Recorder rCC_Recorder = null;
			if (allRecorders != null && allRecorders.Count > 0)
			{
				for (int i = 0; i < allRecorders.Count; i++)
				{
					if (allRecorders[i] != null && allRecorders[i].carController == RCC)
					{
						rCC_Recorder = allRecorders[i];
						break;
					}
				}
			}
			if (rCC_Recorder == null)
			{
				rCC_Recorder = gameObject.AddComponent<RCC_Recorder>();
				rCC_Recorder.carController = RCC;
			}
		}
		StartCoroutine(CheckMissingRecorders());
		if (registerFirstVehicleAsPlayer)
		{
			RegisterPlayer(RCC);
		}
	}

	private void RCC_AICarController_OnRCCAISpawned(RCC_AICarController RCCAI)
	{
		if (!allVehicles.Contains(RCCAI.carController))
		{
			allVehicles.Add(RCCAI.carController);
			allRecorders = new List<RCC_Recorder>();
			allRecorders.AddRange(gameObject.GetComponentsInChildren<RCC_Recorder>());
			RCC_Recorder rCC_Recorder = null;
			if (allRecorders != null && allRecorders.Count > 0)
			{
				for (int i = 0; i < allRecorders.Count; i++)
				{
					if (allRecorders[i] != null && allRecorders[i].carController == RCCAI.carController)
					{
						rCC_Recorder = allRecorders[i];
						break;
					}
				}
			}
			if (rCC_Recorder == null)
			{
				rCC_Recorder = gameObject.AddComponent<RCC_Recorder>();
				rCC_Recorder.carController = RCCAI.carController;
			}
		}
		StartCoroutine(CheckMissingRecorders());
	}

	private void RCC_Camera_OnBCGCameraSpawned(GameObject BCGCamera)
	{
		activePlayerCamera = BCGCamera.GetComponent<RCC_Camera>();
	}

	private void RCC_CarControllerV3_OnRCCPlayerDestroyed(RCC_CarControllerV3 RCC)
	{
		if (allVehicles.Contains(RCC))
		{
			allVehicles.Remove(RCC);
		}
		StartCoroutine(CheckMissingRecorders());
	}

	private void RCC_AICarController_OnRCCAIDestroyed(RCC_AICarController RCCAI)
	{
		if (allVehicles.Contains(RCCAI.carController))
		{
			allVehicles.Remove(RCCAI.carController);
		}
		StartCoroutine(CheckMissingRecorders());
	}

	private void Update()
	{
		if ((bool)activePlayerVehicle)
		{
			if (activePlayerVehicle != lastActivePlayerVehicle && OnVehicleChanged != null)
			{
				OnVehicleChanged();
			}
			lastActivePlayerVehicle = activePlayerVehicle;
		}
		if (disableUIWhenNoPlayerVehicle && (bool)activePlayerCanvas)
		{
			CheckCanvas();
		}
		if (Input.GetKeyDown(RCC_Settings.Instance.recordKB))
		{
			RCC.StartStopRecord();
		}
		if (Input.GetKeyDown(RCC_Settings.Instance.playbackKB))
		{
			RCC.StartStopReplay();
		}
		if (Input.GetKey(RCC_Settings.Instance.slowMotionKB))
		{
			Time.timeScale = 0.2f;
		}
		if (Input.GetKeyUp(RCC_Settings.Instance.slowMotionKB))
		{
			Time.timeScale = orgTimeScale;
		}
		if (Input.GetButtonDown("Cancel"))
		{
			Cursor.lockState = CursorLockMode.None;
		}
		activeMainCamera = Camera.main;
		if (allRecorders != null && allRecorders.Count > 0)
		{
			switch (allRecorders[0].mode)
			{
			case RCC_Recorder.Mode.Neutral:
				recordMode = RecordMode.Neutral;
				break;
			case RCC_Recorder.Mode.Play:
				recordMode = RecordMode.Play;
				break;
			case RCC_Recorder.Mode.Record:
				recordMode = RecordMode.Record;
				break;
			}
		}
	}

	public void Record()
	{
		if (allRecorders != null && allRecorders.Count > 0)
		{
			for (int i = 0; i < allRecorders.Count; i++)
			{
				allRecorders[i].Record();
			}
		}
	}

	public void Play()
	{
		if (allRecorders != null && allRecorders.Count > 0)
		{
			for (int i = 0; i < allRecorders.Count; i++)
			{
				allRecorders[i].Play();
			}
		}
	}

	public void Stop()
	{
		if (allRecorders != null && allRecorders.Count > 0)
		{
			for (int i = 0; i < allRecorders.Count; i++)
			{
				allRecorders[i].Stop();
			}
		}
	}

	private IEnumerator CheckMissingRecorders()
	{
		yield return new WaitForFixedUpdate();
		allRecorders = new List<RCC_Recorder>();
		allRecorders.AddRange(gameObject.GetComponentsInChildren<RCC_Recorder>());
		if (allRecorders != null && allRecorders.Count > 0)
		{
			for (int i = 0; i < allRecorders.Count; i++)
			{
				if (allRecorders[i].carController == null)
				{
					Object.Destroy(allRecorders[i]);
				}
			}
		}
		yield return new WaitForFixedUpdate();
		allRecorders = new List<RCC_Recorder>();
		allRecorders.AddRange(gameObject.GetComponentsInChildren<RCC_Recorder>());
	}

	public void RegisterPlayer(RCC_CarControllerV3 playerVehicle)
	{
		activePlayerVehicle = playerVehicle;
		if ((bool)activePlayerCamera)
		{
			activePlayerCamera.SetTarget(activePlayerVehicle.gameObject);
		}
		if (loadCustomizationAtFirst)
		{
			RCC_Customization.LoadStats(Instance.activePlayerVehicle);
		}
		if ((bool)Object.FindObjectOfType<RCC_CustomizerExample>())
		{
			Object.FindObjectOfType<RCC_CustomizerExample>().CheckUIs();
		}
	}

	public void RegisterPlayer(RCC_CarControllerV3 playerVehicle, bool isControllable)
	{
		activePlayerVehicle = playerVehicle;
		activePlayerVehicle.SetCanControl(isControllable);
		if ((bool)activePlayerCamera)
		{
			activePlayerCamera.SetTarget(activePlayerVehicle.gameObject);
		}
		if ((bool)Object.FindObjectOfType<RCC_CustomizerExample>())
		{
			Object.FindObjectOfType<RCC_CustomizerExample>().CheckUIs();
		}
	}

	public void RegisterPlayer(RCC_CarControllerV3 playerVehicle, bool isControllable, bool engineState)
	{
		activePlayerVehicle = playerVehicle;
		activePlayerVehicle.SetCanControl(isControllable);
		activePlayerVehicle.SetEngine(engineState);
		if ((bool)activePlayerCamera)
		{
			activePlayerCamera.SetTarget(activePlayerVehicle.gameObject);
		}
		if ((bool)Object.FindObjectOfType<RCC_CustomizerExample>())
		{
			Object.FindObjectOfType<RCC_CustomizerExample>().CheckUIs();
		}
	}

	public void DeRegisterPlayer()
	{
		if ((bool)activePlayerVehicle)
		{
			activePlayerVehicle.SetCanControl(state: false);
		}
		activePlayerVehicle = null;
		if ((bool)activePlayerCamera)
		{
			activePlayerCamera.RemoveTarget();
		}
	}

	public void CheckCanvas()
	{
		if (!activePlayerVehicle || !activePlayerVehicle.canControl || !activePlayerVehicle.gameObject.activeInHierarchy || !activePlayerVehicle.enabled)
		{
			activePlayerCanvas.SetDisplayType(RCC_UIDashboardDisplay.DisplayType.Off);
		}
		else if (activePlayerCanvas.displayType != RCC_UIDashboardDisplay.DisplayType.Customization)
		{
			activePlayerCanvas.displayType = RCC_UIDashboardDisplay.DisplayType.Full;
		}
	}

	public static void SetBehavior(int behaviorIndex)
	{
		RCC_Settings.Instance.overrideBehavior = true;
		RCC_Settings.Instance.behaviorSelectedIndex = behaviorIndex;
		if (OnBehaviorChanged != null)
		{
			OnBehaviorChanged();
		}
	}

	public static void SetController(int controllerIndex)
	{
		RCC_Settings.Instance.controllerSelectedIndex = controllerIndex;
		switch (controllerIndex)
		{
		case 0:
			RCC_Settings.Instance.selectedControllerType = RCC_Settings.ControllerType.Keyboard;
			break;
		case 1:
			RCC_Settings.Instance.selectedControllerType = RCC_Settings.ControllerType.Mobile;
			break;
		case 2:
			RCC_Settings.Instance.selectedControllerType = RCC_Settings.ControllerType.XBox360One;
			break;
		case 3:
			RCC_Settings.Instance.selectedControllerType = RCC_Settings.ControllerType.PS4;
			break;
		case 4:
			RCC_Settings.Instance.selectedControllerType = RCC_Settings.ControllerType.LogitechSteeringWheel;
			break;
		case 5:
			RCC_Settings.Instance.selectedControllerType = RCC_Settings.ControllerType.Custom;
			break;
		}
		if (OnControllerChanged != null)
		{
			OnControllerChanged();
		}
	}

	public void ChangeCamera()
	{
		if ((bool)Instance.activePlayerCamera)
		{
			Instance.activePlayerCamera.ChangeCamera();
		}
	}

	public void Transport(Vector3 position, Quaternion rotation)
	{
		if ((bool)activePlayerVehicle)
		{
			activePlayerVehicle.rigid.velocity = Vector3.zero;
			activePlayerVehicle.rigid.angularVelocity = Vector3.zero;
			activePlayerVehicle.transform.position = position;
			activePlayerVehicle.transform.rotation = rotation;
		}
	}

	public void Transport(RCC_CarControllerV3 vehicle, Vector3 position, Quaternion rotation)
	{
		if ((bool)vehicle)
		{
			vehicle.rigid.velocity = Vector3.zero;
			vehicle.rigid.angularVelocity = Vector3.zero;
			vehicle.transform.position = position;
			vehicle.transform.rotation = rotation;
		}
	}

	private void OnDisable()
	{
		RCC_Camera.OnBCGCameraSpawned -= RCC_Camera_OnBCGCameraSpawned;
		RCC_CarControllerV3.OnRCCPlayerSpawned -= RCC_CarControllerV3_OnRCCSpawned;
		RCC_AICarController.OnRCCAISpawned -= RCC_AICarController_OnRCCAISpawned;
		RCC_CarControllerV3.OnRCCPlayerDestroyed -= RCC_CarControllerV3_OnRCCPlayerDestroyed;
		RCC_AICarController.OnRCCAIDestroyed -= RCC_AICarController_OnRCCAIDestroyed;
	}
}
