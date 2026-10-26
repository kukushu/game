using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Main/RCC Realistic Car Controller V3")]
[RequireComponent(typeof(Rigidbody))]
public class RCC_CarControllerV3 : RCC_Core
{
	[Serializable]
	public class ConfigureVehicleSubsteps
	{
		public float speedThreshold = 10f;

		public int stepsBelowThreshold = 5;

		public int stepsAboveThreshold = 5;
	}

	public enum SteeringWheelRotateAround
	{
		XAxis,
		YAxis,
		ZAxis
	}

	public enum WheelType
	{
		FWD,
		RWD,
		AWD,
		BIASED
	}

	[Serializable]
	public class Gear
	{
		public float maxRatio;

		public int maxSpeed;

		public int targetSpeedForNextGear;

		public void SetGear(float ratio, int speed, int targetSpeed)
		{
			maxRatio = ratio;
			maxSpeed = speed;
			targetSpeedForNextGear = targetSpeed;
		}
	}

	public enum AudioType
	{
		OneSource,
		TwoSource,
		ThreeSource,
		Off
	}

	public enum IndicatorsOn
	{
		Off,
		Right,
		Left,
		All
	}

	private struct originalMeshVerts
	{
		public Vector3[] meshVerts;
	}

	public delegate void onRCCPlayerSpawned(RCC_CarControllerV3 RCC);

	public delegate void onRCCPlayerDestroyed(RCC_CarControllerV3 RCC);

	public delegate void onRCCPlayerCollision(RCC_CarControllerV3 RCC, Collision collision);

	private RCC_Settings RCCSettingsInstance;

	public bool canControl = true;

	public bool isGrounded;

	public Transform FrontLeftWheelTransform;

	public Transform FrontRightWheelTransform;

	public Transform RearLeftWheelTransform;

	public Transform RearRightWheelTransform;

	public Transform[] ExtraRearWheelsTransform;

	public RCC_WheelCollider FrontLeftWheelCollider;

	public RCC_WheelCollider FrontRightWheelCollider;

	public RCC_WheelCollider RearLeftWheelCollider;

	public RCC_WheelCollider RearRightWheelCollider;

	public RCC_WheelCollider[] ExtraRearWheelsCollider;

	public RCC_WheelCollider[] allWheelColliders;

	public bool overrideWheels = true;

	public int poweredWheels;

	public bool applyEngineTorqueToExtraRearWheelColliders = true;

	public ConfigureVehicleSubsteps configureVehicleSubsteps = new ConfigureVehicleSubsteps();

	public Transform SteeringWheel;

	private Quaternion orgSteeringWheelRot;

	public SteeringWheelRotateAround steeringWheelRotateAround;

	public float steeringWheelAngleMultiplier = 11f;

	public WheelType wheelTypeChoise = WheelType.RWD;

	public bool externalController;

	public Rigidbody rigid;

	public Transform COM;

	public float brakeTorque = 2000f;

	public float steerAngle = 40f;

	public float highspeedsteerAngle = 5f;

	public float highspeedsteerAngleAtspeed = 200f;

	public float antiRollFrontHorizontal = 1000f;

	public float antiRollRearHorizontal = 1000f;

	public float antiRollVertical;

	public float downForce = 25f;

	public bool useCounterSteering = true;

	[Range(0f, 1f)]
	public float counterSteeringFactor = 0.5f;

	public float speed;

	public float maxspeed = 240f;

	private float resetTime;

	private float orgSteerAngle;

	public AnimationCurve engineTorqueCurve = new AnimationCurve();

	public bool autoGenerateEngineRPMCurve = true;

	public float maxEngineTorque = 250f;

	public float maxEngineTorqueAtRPM = 4500f;

	public float minEngineRPM = 1000f;

	public float maxEngineRPM = 7000f;

	public float engineRPM;

	[Range(0.02f, 0.4f)]
	public float engineInertia = 0.15f;

	public bool useRevLimiter = true;

	public bool useExhaustFlame = true;

	public bool engineRunning;

	private float oldEngineTorque;

	private float oldMaxTorqueAtRPM;

	private float oldMinEngineRPM;

	private float oldMaxEngineRPM;

	public bool useFuelConsumption;

	public float fuelTankCapacity = 62f;

	public float fuelTank = 62f;

	public float fuelConsumptionRate = 0.1f;

	public bool useEngineHeat;

	public float engineHeat = 15f;

	public float engineCoolingWaterThreshold = 60f;

	public float engineHeatRate = 1f;

	public float engineCoolRate = 1f;

	public Gear[] gears;

	public int totalGears = 6;

	public int currentGear;

	public bool NGear;

	public float finalRatio = 3.23f;

	[Range(0f, 0.5f)]
	public float gearShiftingDelay = 0.35f;

	[Range(0.25f, 1f)]
	public float gearShiftingThreshold = 0.75f;

	[Range(0.1f, 0.9f)]
	public float clutchInertia = 0.25f;

	public float gearShiftUpRPM = 5250f;

	public float gearShiftDownRPM = 3000f;

	public bool changingGear;

	public int direction = 1;

	internal bool canGoReverseNow;

	public float launched;

	internal bool semiAutomaticGear;

	public AudioType audioType;

	public bool autoCreateEngineOffSounds = true;

	private AudioSource engineStartSound;

	public AudioClip engineStartClip;

	internal AudioSource engineSoundHigh;

	public AudioClip engineClipHigh;

	private AudioSource engineSoundMed;

	public AudioClip engineClipMed;

	private AudioSource engineSoundLow;

	public AudioClip engineClipLow;

	private AudioSource engineSoundIdle;

	public AudioClip engineClipIdle;

	private AudioSource gearShiftingSound;

	internal AudioSource engineSoundHighOff;

	public AudioClip engineClipHighOff;

	internal AudioSource engineSoundMedOff;

	public AudioClip engineClipMedOff;

	internal AudioSource engineSoundLowOff;

	public AudioClip engineClipLowOff;

	private AudioSource crashSound;

	private AudioSource reversingSound;

	private AudioSource windSound;

	private AudioSource brakeSound;

	private AudioSource NOSSound;

	private AudioSource turboSound;

	private AudioSource blowSound;

	[Range(0f, 1f)]
	public float minEngineSoundPitch = 0.75f;

	[Range(1f, 2f)]
	public float maxEngineSoundPitch = 1.75f;

	[Range(0f, 1f)]
	public float minEngineSoundVolume = 0.05f;

	[Range(0f, 1f)]
	public float maxEngineSoundVolume = 0.85f;

	[Range(0f, 1f)]
	public float idleEngineSoundVolume = 0.85f;

	public Vector3 engineSoundPosition = new Vector3(0f, 0f, 1.5f);

	public Vector3 gearSoundPosition = new Vector3(0f, -0.5f, 0.5f);

	public Vector3 turboSoundPosition = new Vector3(0f, 0f, 1.5f);

	public Vector3 exhaustSoundPosition = new Vector3(0f, -0.5f, 2f);

	public Vector3 windSoundPosition = new Vector3(0f, 0f, 2f);

	public RCC_Inputs inputs;

	[HideInInspector]
	public float throttleInput;

	[HideInInspector]
	public float brakeInput;

	[HideInInspector]
	public float steerInput;

	[HideInInspector]
	public float clutchInput;

	[HideInInspector]
	public float handbrakeInput;

	[HideInInspector]
	public float boostInput;

	[HideInInspector]
	public float fuelInput;

	[HideInInspector]
	public bool cutGas;

	private bool permanentGas;

	public bool lowBeamHeadLightsOn;

	public bool highBeamHeadLightsOn;

	public IndicatorsOn indicatorsOn;

	public float indicatorTimer;

	public bool useDamage = true;

	public bool useWheelDamage;

	public float wheelDamageRadius = 1f;

	public float wheelDamageMultiplier = 0.1f;

	private originalMeshVerts[] originalMeshData;

	public MeshFilter[] deformableMeshFilters;

	public LayerMask damageFilter = -1;

	public float randomizeVertices = 1f;

	public float damageRadius = 0.5f;

	private float minimumVertDistanceForDamagedMesh = 0.002f;

	public bool repairNow;

	public bool repaired = true;

	public float maximumDamage = 0.5f;

	private float minimumCollisionForce = 1f;

	public float damageMultiplier = 1f;

	public int maximumContactSparkle = 5;

	private List<ParticleSystem> contactSparkeList = new List<ParticleSystem>();

	private GameObject allContactParticles;

	public RCC_DetachablePart[] detachableParts;

	private Vector3 localVector;

	private Quaternion rot = Quaternion.identity;

	private float oldRotation;

	public Transform velocityDirection;

	public Transform steeringDirection;

	public float velocityAngle;

	private float angle;

	private float angularVelo;

	public bool ABS = true;

	public bool TCS = true;

	public bool ESP = true;

	public bool steeringHelper = true;

	public bool tractionHelper = true;

	public bool angularDragHelper;

	[Range(0.05f, 0.5f)]
	public float ABSThreshold = 0.35f;

	[Range(0.05f, 1f)]
	public float TCSStrength = 1f;

	[Range(0.05f, 0.5f)]
	public float ESPThreshold = 0.5f;

	[Range(0.05f, 1f)]
	public float ESPStrength = 0.25f;

	[Range(0f, 1f)]
	public float steerHelperLinearVelStrength = 0.1f;

	[Range(0f, 1f)]
	public float steerHelperAngularVelStrength = 0.1f;

	[Range(0f, 1f)]
	public float tractionHelperStrength = 0.1f;

	[Range(0f, 1f)]
	public float angularDragHelperStrength = 0.1f;

	public bool ABSAct;

	public bool TCSAct;

	public bool ESPAct;

	public float frontSlip;

	public float rearSlip;

	public bool underSteering;

	public bool overSteering;

	internal bool driftingNow;

	internal float driftAngle;

	public float turboBoost;

	public float NoS = 100f;

	private float NoSConsumption = 25f;

	private float NoSRegenerateTime = 10f;

	public bool useNOS;

	public bool useTurbo;

	public RCC_TruckTrailer attachedTrailer;

	private RCC_Settings RCCSettings
	{
		get
		{
			if (RCCSettingsInstance == null)
			{
				RCCSettingsInstance = RCC_Settings.Instance;
				return RCCSettingsInstance;
			}
			return RCCSettingsInstance;
		}
	}

	public bool runEngineAtAwake => RCCSettings.runEngineAtAwake;

	public bool autoReverse
	{
		get
		{
			if (!externalController)
			{
				return RCCSettings.autoReverse;
			}
			return true;
		}
	}

	public bool automaticGear
	{
		get
		{
			if (!externalController)
			{
				return RCCSettings.useAutomaticGear;
			}
			return true;
		}
	}

	public bool useAutomaticClutch => RCCSettingsInstance.useAutomaticClutch;

	private AudioClip[] gearShiftingClips => RCCSettings.gearShiftingClips;

	private AudioClip[] crashClips => RCCSettings.crashClips;

	private AudioClip reversingClip => RCCSettings.reversingClip;

	private AudioClip windClip => RCCSettings.windClip;

	private AudioClip brakeClip => RCCSettings.brakeClip;

	private AudioClip NOSClip => RCCSettings.NOSClip;

	private AudioClip turboClip => RCCSettings.turboClip;

	private AudioClip[] blowClip => RCCSettings.blowoutClip;

	public GameObject contactSparkle => RCCSettings.contactParticles;

	public static event onRCCPlayerSpawned OnRCCPlayerSpawned;

	public static event onRCCPlayerDestroyed OnRCCPlayerDestroyed;

	public static event onRCCPlayerCollision OnRCCPlayerCollision;

	private void Awake()
	{
		if (RCCSettings.overrideFixedTimeStep)
		{
			Time.fixedDeltaTime = RCCSettings.fixedTimeStep;
		}
		rigid = GetComponent<Rigidbody>();
		rigid.maxAngularVelocity = RCCSettings.maxAngularVelocity;
		gearShiftingThreshold = Mathf.Clamp(gearShiftingThreshold, 0.25f, 1f);
		if (engineInertia > 0.4f)
		{
			engineInertia = 0.15f;
		}
		engineInertia = Mathf.Clamp(engineInertia, 0.02f, 0.4f);
		if (autoGenerateEngineRPMCurve)
		{
			CreateEngineCurve();
		}
		allWheelColliders = GetComponentsInChildren<RCC_WheelCollider>();
		GetComponentInChildren<WheelCollider>().ConfigureVehicleSubsteps(configureVehicleSubsteps.speedThreshold, configureVehicleSubsteps.stepsBelowThreshold, configureVehicleSubsteps.stepsAboveThreshold);
		FrontLeftWheelCollider.wheelModel = FrontLeftWheelTransform;
		FrontRightWheelCollider.wheelModel = FrontRightWheelTransform;
		RearLeftWheelCollider.wheelModel = RearLeftWheelTransform;
		RearRightWheelCollider.wheelModel = RearRightWheelTransform;
		for (int i = 0; i < ExtraRearWheelsCollider.Length; i++)
		{
			ExtraRearWheelsCollider[i].wheelModel = ExtraRearWheelsTransform[i];
		}
		orgSteerAngle = steerAngle;
		allContactParticles = new GameObject("All Contact Particles");
		allContactParticles.transform.SetParent(transform, worldPositionStays: false);
		CreateAudios();
		if (useDamage)
		{
			InitDamage();
		}
		CheckBehavior();
		if (runEngineAtAwake || externalController)
		{
			engineRunning = true;
			fuelInput = 1f;
		}
	}

	private void OnEnable()
	{
		changingGear = false;
		StartCoroutine(RCCPlayerSpawned());
		RCC_SceneManager.OnBehaviorChanged += CheckBehavior;
	}

	private IEnumerator RCCPlayerSpawned()
	{
		yield return new WaitForEndOfFrame();
		if (!externalController && OnRCCPlayerSpawned != null)
		{
			OnRCCPlayerSpawned(this);
		}
	}

	public void CreateWheelColliders()
	{
		CreateWheelColliders(this);
	}

	private void CreateAudios()
	{
		switch (audioType)
		{
		case AudioType.OneSource:
			engineSoundHigh = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound High AudioSource", 5f, 50f, 0f, engineClipHigh, loop: true, playNow: true, destroyAfterFinished: false);
			if (autoCreateEngineOffSounds)
			{
				engineSoundHighOff = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound High Off AudioSource", 5f, 50f, 0f, engineClipHigh, loop: true, playNow: true, destroyAfterFinished: false);
				RCC_Core.NewLowPassFilter(engineSoundHighOff, 3000f);
			}
			else
			{
				engineSoundHighOff = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound High Off AudioSource", 5f, 50f, 0f, engineClipHighOff, loop: true, playNow: true, destroyAfterFinished: false);
			}
			break;
		case AudioType.TwoSource:
			engineSoundHigh = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound High AudioSource", 5f, 50f, 0f, engineClipHigh, loop: true, playNow: true, destroyAfterFinished: false);
			engineSoundLow = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Low AudioSource", 5f, 25f, 0f, engineClipLow, loop: true, playNow: true, destroyAfterFinished: false);
			if (autoCreateEngineOffSounds)
			{
				engineSoundHighOff = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound High Off AudioSource", 5f, 50f, 0f, engineClipHigh, loop: true, playNow: true, destroyAfterFinished: false);
				engineSoundLowOff = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Low Off AudioSource", 5f, 25f, 0f, engineClipLow, loop: true, playNow: true, destroyAfterFinished: false);
				RCC_Core.NewLowPassFilter(engineSoundHighOff, 3000f);
				RCC_Core.NewLowPassFilter(engineSoundLowOff, 3000f);
			}
			else
			{
				engineSoundHighOff = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound High Off AudioSource", 5f, 50f, 0f, engineClipHighOff, loop: true, playNow: true, destroyAfterFinished: false);
				engineSoundLowOff = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Low Off AudioSource", 5f, 25f, 0f, engineClipLowOff, loop: true, playNow: true, destroyAfterFinished: false);
			}
			break;
		case AudioType.ThreeSource:
			engineSoundHigh = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound High AudioSource", 5f, 50f, 0f, engineClipHigh, loop: true, playNow: true, destroyAfterFinished: false);
			engineSoundMed = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Medium AudioSource", 5f, 50f, 0f, engineClipMed, loop: true, playNow: true, destroyAfterFinished: false);
			engineSoundLow = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Low AudioSource", 5f, 25f, 0f, engineClipLow, loop: true, playNow: true, destroyAfterFinished: false);
			if (autoCreateEngineOffSounds)
			{
				engineSoundHighOff = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound High Off AudioSource", 5f, 50f, 0f, engineClipHigh, loop: true, playNow: true, destroyAfterFinished: false);
				engineSoundMedOff = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Medium Off AudioSource", 5f, 50f, 0f, engineClipMed, loop: true, playNow: true, destroyAfterFinished: false);
				engineSoundLowOff = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Low Off AudioSource", 5f, 25f, 0f, engineClipLow, loop: true, playNow: true, destroyAfterFinished: false);
				if ((bool)engineSoundHighOff)
				{
					RCC_Core.NewLowPassFilter(engineSoundHighOff, 3000f);
				}
				if ((bool)engineSoundMedOff)
				{
					RCC_Core.NewLowPassFilter(engineSoundMedOff, 3000f);
				}
				if ((bool)engineSoundLowOff)
				{
					RCC_Core.NewLowPassFilter(engineSoundLowOff, 3000f);
				}
			}
			else
			{
				engineSoundHighOff = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound High Off AudioSource", 5f, 50f, 0f, engineClipHighOff, loop: true, playNow: true, destroyAfterFinished: false);
				engineSoundMedOff = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Medium Off AudioSource", 5f, 50f, 0f, engineClipMedOff, loop: true, playNow: true, destroyAfterFinished: false);
				engineSoundLowOff = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Low Off AudioSource", 5f, 25f, 0f, engineClipLowOff, loop: true, playNow: true, destroyAfterFinished: false);
			}
			break;
		}
		engineSoundIdle = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Idle AudioSource", 5f, 25f, 0f, engineClipIdle, loop: true, playNow: true, destroyAfterFinished: false);
		reversingSound = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, gearSoundPosition, "Reverse Sound AudioSource", 10f, 50f, 0f, reversingClip, loop: true, playNow: false, destroyAfterFinished: false);
		windSound = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, windSoundPosition, "Wind Sound AudioSource", 1f, 10f, 0f, windClip, loop: true, playNow: true, destroyAfterFinished: false);
		brakeSound = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, "Brake Sound AudioSource", 1f, 10f, 0f, brakeClip, loop: true, playNow: true, destroyAfterFinished: false);
		if (useNOS)
		{
			NOSSound = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, exhaustSoundPosition, "NOS Sound AudioSource", 5f, 10f, 0.5f, NOSClip, loop: true, playNow: false, destroyAfterFinished: false);
		}
		if (useNOS || useTurbo)
		{
			blowSound = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, exhaustSoundPosition, "NOS Blow", 1f, 10f, 0.5f, null, loop: false, playNow: false, destroyAfterFinished: false);
		}
		if (useTurbo)
		{
			turboSound = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, turboSoundPosition, "Turbo Sound AudioSource", 0.1f, 0.5f, 0f, turboClip, loop: true, playNow: true, destroyAfterFinished: false);
			RCC_Core.NewHighPassFilter(turboSound, 10000f, 10);
		}
	}

	private void CheckBehavior()
	{
		if (RCCSettings.selectedBehaviorType != null)
		{
			SetBehavior(this);
		}
	}

	public void CreateEngineCurve()
	{
		engineTorqueCurve = new AnimationCurve();
		engineTorqueCurve.AddKey(0f, 0f);
		engineTorqueCurve.AddKey(maxEngineTorqueAtRPM, maxEngineTorque);
		engineTorqueCurve.AddKey(maxEngineRPM, maxEngineTorque / 1.5f);
		oldEngineTorque = maxEngineTorque;
		oldMaxTorqueAtRPM = maxEngineTorqueAtRPM;
		oldMinEngineRPM = minEngineRPM;
		oldMaxEngineRPM = maxEngineRPM;
	}

	public void InitGears()
	{
		gears = new Gear[totalGears];
		float[] array = new float[gears.Length];
		int[] array2 = new int[gears.Length];
		int[] array3 = new int[gears.Length];
		if (gears.Length == 1)
		{
			array = new float[1] { 1f };
		}
		if (gears.Length == 2)
		{
			array = new float[2] { 2f, 1f };
		}
		if (gears.Length == 3)
		{
			array = new float[3] { 2f, 1.5f, 1f };
		}
		if (gears.Length == 4)
		{
			array = new float[4] { 2.86f, 1.62f, 1f, 0.72f };
		}
		if (gears.Length == 5)
		{
			array = new float[5] { 4.23f, 2.52f, 1.66f, 1.22f, 1f };
		}
		if (gears.Length == 6)
		{
			array = new float[6] { 4.35f, 2.5f, 1.66f, 1.23f, 1f, 0.85f };
		}
		if (gears.Length == 7)
		{
			array = new float[7] { 4.5f, 2.5f, 1.66f, 1.23f, 1f, 0.9f, 0.8f };
		}
		if (gears.Length == 8)
		{
			array = new float[8] { 4.6f, 2.5f, 1.86f, 1.43f, 1.23f, 1.05f, 0.9f, 0.72f };
		}
		for (int i = 0; i < gears.Length; i++)
		{
			array2[i] = (int)(maxspeed / (float)gears.Length * (float)(i + 1));
			array3[i] = (int)Mathf.Lerp(0f, maxspeed * Mathf.Lerp(0f, 1f, gearShiftingThreshold), (float)(i + 1) / (float)gears.Length);
		}
		for (int j = 0; j < gears.Length; j++)
		{
			gears[j] = new Gear();
			gears[j].SetGear(array[j], array2[j], array3[j]);
		}
	}

	private void InitDamage()
	{
		if (deformableMeshFilters.Length == 0)
		{
			MeshFilter[] componentsInChildren = GetComponentsInChildren<MeshFilter>();
			List<MeshFilter> list = new List<MeshFilter>();
			MeshFilter[] array = componentsInChildren;
			foreach (MeshFilter meshFilter in array)
			{
				if (!meshFilter.mesh.isReadable)
				{
					Debug.LogError("Not deformable mesh detected. Mesh of the " + meshFilter.transform.name + " isReadable is false; Read/Write must be enabled in import settings for this model!");
				}
				else if (!meshFilter.transform.IsChildOf(FrontLeftWheelTransform) && !meshFilter.transform.IsChildOf(FrontRightWheelTransform) && !meshFilter.transform.IsChildOf(RearLeftWheelTransform) && !meshFilter.transform.IsChildOf(RearRightWheelTransform))
				{
					list.Add(meshFilter);
				}
			}
			deformableMeshFilters = list.ToArray();
		}
		LoadOriginalMeshData();
		if ((bool)contactSparkle)
		{
			for (int j = 0; j < maximumContactSparkle; j++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(contactSparkle, transform.position, Quaternion.identity);
				gameObject.transform.SetParent(allContactParticles.transform);
				contactSparkeList.Add(gameObject.GetComponent<ParticleSystem>());
				ParticleSystem.EmissionModule emission = gameObject.GetComponent<ParticleSystem>().emission;
				emission.enabled = false;
			}
		}
		detachableParts = base.gameObject.GetComponentsInChildren<RCC_DetachablePart>();
	}

	public void KillOrStartEngine()
	{
		if (engineRunning)
		{
			KillEngine();
		}
		else
		{
			StartEngine();
		}
	}

	public void StartEngine()
	{
		if (!engineRunning)
		{
			StartCoroutine(StartEngineDelayed());
		}
	}

	public void StartEngine(bool instantStart)
	{
		if (instantStart)
		{
			fuelInput = 1f;
			engineRunning = true;
		}
		else
		{
			StartCoroutine(StartEngineDelayed());
		}
	}

	public IEnumerator StartEngineDelayed()
	{
		if (!engineRunning)
		{
			engineStartSound = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, engineSoundPosition, "Engine Start AudioSource", 1f, 10f, 1f, engineStartClip, loop: false, playNow: true, destroyAfterFinished: true);
			if (engineStartSound.isPlaying)
			{
				engineStartSound.Play();
			}
			yield return new WaitForSeconds(1f);
			engineRunning = true;
			fuelInput = 1f;
		}
		yield return new WaitForSeconds(1f);
	}

	public void KillEngine()
	{
		fuelInput = 0f;
		engineRunning = false;
	}

	private void LoadOriginalMeshData()
	{
		originalMeshData = new originalMeshVerts[deformableMeshFilters.Length];
		for (int i = 0; i < deformableMeshFilters.Length; i++)
		{
			originalMeshData[i].meshVerts = deformableMeshFilters[i].mesh.vertices;
		}
	}

	public void Repair()
	{
		if (repaired || !repairNow)
		{
			return;
		}
		repaired = true;
		for (int i = 0; i < deformableMeshFilters.Length; i++)
		{
			if (!(deformableMeshFilters[i] != null))
			{
				continue;
			}
			Vector3[] vertices = deformableMeshFilters[i].mesh.vertices;
			if (originalMeshData == null)
			{
				LoadOriginalMeshData();
			}
			for (int j = 0; j < vertices.Length; j++)
			{
				vertices[j] += (originalMeshData[i].meshVerts[j] - vertices[j]) * (Time.deltaTime * 2f);
				if ((originalMeshData[i].meshVerts[j] - vertices[j]).magnitude >= minimumVertDistanceForDamagedMesh)
				{
					repaired = false;
				}
			}
			deformableMeshFilters[i].mesh.vertices = vertices;
			deformableMeshFilters[i].mesh.RecalculateNormals();
			deformableMeshFilters[i].mesh.RecalculateBounds();
		}
		for (int k = 0; k < allWheelColliders.Length; k++)
		{
			allWheelColliders[k].damagedCamber = Mathf.Lerp(allWheelColliders[k].damagedCamber, 0f, Time.deltaTime * 2f);
			allWheelColliders[k].damagedCaster = Mathf.Lerp(allWheelColliders[k].damagedCaster, 0f, Time.deltaTime * 2f);
			allWheelColliders[k].damagedToe = Mathf.Lerp(allWheelColliders[k].damagedToe, 0f, Time.deltaTime * 2f);
			if (Mathf.Abs(allWheelColliders[k].damagedCamber) > 0.05f || Mathf.Abs(allWheelColliders[k].damagedToe) > 0.05f || Mathf.Abs(allWheelColliders[k].damagedCaster) > 0.05f)
			{
				repaired = false;
				continue;
			}
			allWheelColliders[k].damagedCamber = 0f;
			allWheelColliders[k].damagedCaster = 0f;
			allWheelColliders[k].damagedToe = 0f;
		}
		if (repaired)
		{
			repairNow = false;
		}
	}

	private void DeformMesh(Mesh mesh, Vector3[] originalMesh, Collision collision, float cos, Transform meshTransform, Quaternion rot)
	{
		Vector3[] vertices = mesh.vertices;
		ContactPoint[] contacts = collision.contacts;
		foreach (ContactPoint contactPoint in contacts)
		{
			Vector3 vector = meshTransform.InverseTransformPoint(contactPoint.point);
			for (int j = 0; j < vertices.Length; j++)
			{
				if ((vector - vertices[j]).magnitude < damageRadius)
				{
					vertices[j] += rot * (localVector * (damageRadius - (vector - vertices[j]).magnitude) / damageRadius * cos + new Vector3(Mathf.Sin(vertices[j].y * 1000f), Mathf.Sin(vertices[j].z * 1000f), Mathf.Sin(vertices[j].x * 100f)).normalized * (randomizeVertices / 500f));
					if (maximumDamage > 0f && (vertices[j] - originalMesh[j]).magnitude > maximumDamage)
					{
						vertices[j] = originalMesh[j] + (vertices[j] - originalMesh[j]).normalized * maximumDamage;
					}
				}
			}
		}
		mesh.vertices = vertices;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
	}

	private void CollisionParticles(Vector3 contactPoint)
	{
		for (int i = 0; i < contactSparkeList.Count; i++)
		{
			if (!contactSparkeList[i].isPlaying)
			{
				contactSparkeList[i].transform.position = contactPoint;
				ParticleSystem.EmissionModule emission = contactSparkeList[i].emission;
				emission.enabled = true;
				contactSparkeList[i].Play();
				break;
			}
		}
	}

	private void OtherVisuals()
	{
		if ((bool)SteeringWheel)
		{
			if (orgSteeringWheelRot.eulerAngles == Vector3.zero)
			{
				orgSteeringWheelRot = SteeringWheel.transform.localRotation;
			}
			switch (steeringWheelRotateAround)
			{
			case SteeringWheelRotateAround.XAxis:
				SteeringWheel.transform.localRotation = orgSteeringWheelRot * Quaternion.AngleAxis(steerInput * steerAngle * (0f - steeringWheelAngleMultiplier), Vector3.right);
				break;
			case SteeringWheelRotateAround.YAxis:
				SteeringWheel.transform.localRotation = orgSteeringWheelRot * Quaternion.AngleAxis(steerInput * steerAngle * (0f - steeringWheelAngleMultiplier), Vector3.up);
				break;
			case SteeringWheelRotateAround.ZAxis:
				SteeringWheel.transform.localRotation = orgSteeringWheelRot * Quaternion.AngleAxis(steerInput * steerAngle * (0f - steeringWheelAngleMultiplier), Vector3.forward);
				break;
			}
		}
	}

	private void Update()
	{
		Inputs();
		if (brakeInput > 0.9f && transform.InverseTransformDirection(rigid.velocity).z < 0.25f && canGoReverseNow && automaticGear && !semiAutomaticGear && !changingGear && direction != -1)
		{
			StartCoroutine(ChangeGear(-1));
		}
		else if (throttleInput < 0.1f && transform.InverseTransformDirection(rigid.velocity).z > -0.25f && direction == -1 && !changingGear && automaticGear && !semiAutomaticGear)
		{
			StartCoroutine(ChangeGear(0));
		}
		Audio();
		ResetCar();
		if (useDamage)
		{
			Repair();
		}
		OtherVisuals();
		indicatorTimer += Time.deltaTime;
		if (throttleInput >= 0.05f)
		{
			launched += throttleInput * Time.deltaTime;
		}
		else
		{
			launched -= Time.deltaTime;
		}
		launched = Mathf.Clamp01(launched);
	}

	private void Inputs()
	{
		if (canControl)
		{
			if (!externalController)
			{
				inputs = RCC_InputManager.GetInputs();
				if (!automaticGear || semiAutomaticGear)
				{
					if (!changingGear && !cutGas)
					{
						throttleInput = inputs.throttleInput;
					}
					else
					{
						throttleInput = 0f;
					}
				}
				else if (!changingGear && !cutGas)
				{
					throttleInput = ((direction == 1) ? Mathf.Clamp01(inputs.throttleInput) : Mathf.Clamp01(inputs.brakeInput));
				}
				else
				{
					throttleInput = 0f;
				}
				if (!automaticGear || semiAutomaticGear)
				{
					brakeInput = Mathf.Clamp01(inputs.brakeInput);
				}
				else if (!cutGas)
				{
					brakeInput = ((direction == 1) ? Mathf.Clamp01(inputs.brakeInput) : Mathf.Clamp01(inputs.throttleInput));
				}
				else
				{
					brakeInput = 0f;
				}
				steerInput = inputs.steerInput;
				boostInput = inputs.boostInput;
				handbrakeInput = inputs.handbrakeInput;
				if (!useAutomaticClutch)
				{
					clutchInput = inputs.clutchInput;
				}
				GetExternalInputs();
			}
		}
		else if (!externalController)
		{
			throttleInput = 0f;
			brakeInput = 0f;
			steerInput = 0f;
			boostInput = 0f;
			handbrakeInput = 1f;
		}
		if (fuelInput <= 0f)
		{
			throttleInput = 0f;
		}
		if (changingGear || cutGas)
		{
			throttleInput = 0f;
		}
		if (!useNOS || NoS < 5f || throttleInput < 0.75f)
		{
			boostInput = 0f;
		}
		if (useCounterSteering)
		{
			steerInput += driftAngle * counterSteeringFactor;
		}
		throttleInput = Mathf.Clamp01(throttleInput);
		brakeInput = Mathf.Clamp01(brakeInput);
		steerInput = Mathf.Clamp(steerInput, -1f, 1f);
		boostInput = Mathf.Clamp01(boostInput);
		handbrakeInput = Mathf.Clamp01(handbrakeInput);
	}

	private void GetExternalInputs()
	{
		switch (RCCSettings.selectedControllerType)
		{
		case RCC_Settings.ControllerType.Keyboard:
			if (RCC_InputManager.GetKeyDown(RCCSettings.lowBeamHeadlightsKB))
			{
				lowBeamHeadLightsOn = !lowBeamHeadLightsOn;
			}
			if (RCC_InputManager.GetKeyDown(RCCSettings.highBeamHeadlightsKB))
			{
				highBeamHeadLightsOn = true;
			}
			else if (RCC_InputManager.GetKeyUp(RCCSettings.highBeamHeadlightsKB))
			{
				highBeamHeadLightsOn = false;
			}
			if (RCC_InputManager.GetKeyDown(RCCSettings.startEngineKB))
			{
				KillOrStartEngine();
			}
			if (RCC_InputManager.GetKeyDown(RCCSettings.trailerAttachDetach))
			{
				DetachTrailer();
			}
			if (RCC_InputManager.GetKeyDown(RCCSettings.rightIndicatorKB))
			{
				if (indicatorsOn != IndicatorsOn.Right)
				{
					indicatorsOn = IndicatorsOn.Right;
				}
				else
				{
					indicatorsOn = IndicatorsOn.Off;
				}
			}
			if (RCC_InputManager.GetKeyDown(RCCSettings.leftIndicatorKB))
			{
				if (indicatorsOn != IndicatorsOn.Left)
				{
					indicatorsOn = IndicatorsOn.Left;
				}
				else
				{
					indicatorsOn = IndicatorsOn.Off;
				}
			}
			if (RCC_InputManager.GetKeyDown(RCCSettings.hazardIndicatorKB))
			{
				if (indicatorsOn != IndicatorsOn.All)
				{
					indicatorsOn = IndicatorsOn.Off;
					indicatorsOn = IndicatorsOn.All;
				}
				else
				{
					indicatorsOn = IndicatorsOn.Off;
				}
			}
			if (RCC_InputManager.GetKeyDown(RCCSettings.NGear))
			{
				NGear = true;
			}
			if (RCC_InputManager.GetKeyUp(RCCSettings.NGear))
			{
				NGear = false;
			}
			if (!automaticGear)
			{
				if (RCC_InputManager.GetKeyDown(RCCSettings.shiftGearUp))
				{
					GearShiftUp();
				}
				if (RCC_InputManager.GetKeyDown(RCCSettings.shiftGearDown))
				{
					GearShiftDown();
				}
			}
			break;
		case RCC_Settings.ControllerType.XBox360One:
		{
			if (!string.IsNullOrEmpty(RCCSettingsInstance.Xbox_lowBeamHeadlightsKB) && RCC_InputManager.GetButtonDown(RCCSettings.Xbox_lowBeamHeadlightsKB))
			{
				lowBeamHeadLightsOn = !lowBeamHeadLightsOn;
			}
			if (!string.IsNullOrEmpty(RCCSettingsInstance.Xbox_highBeamHeadlightsKB))
			{
				if (RCC_InputManager.GetButtonDown(RCCSettings.Xbox_highBeamHeadlightsKB))
				{
					highBeamHeadLightsOn = true;
				}
				else if (RCC_InputManager.GetButtonUp(RCCSettings.Xbox_highBeamHeadlightsKB))
				{
					highBeamHeadLightsOn = false;
				}
			}
			if (!string.IsNullOrEmpty(RCCSettingsInstance.Xbox_startEngineKB) && RCC_InputManager.GetButtonDown(RCCSettings.Xbox_startEngineKB))
			{
				KillOrStartEngine();
			}
			if (!string.IsNullOrEmpty(RCCSettingsInstance.Xbox_trailerAttachDetach) && RCC_InputManager.GetButtonDown(RCCSettings.Xbox_trailerAttachDetach))
			{
				DetachTrailer();
			}
			float num3 = 0f;
			if (!string.IsNullOrEmpty(RCCSettingsInstance.Xbox_indicatorKB))
			{
				num3 = Input.GetAxis(RCCSettings.Xbox_indicatorKB);
			}
			if (!string.IsNullOrEmpty(RCCSettingsInstance.Xbox_hazardIndicatorKB))
			{
				Input.GetAxis(RCCSettings.Xbox_hazardIndicatorKB);
			}
			if (0f >= 0.5f)
			{
				if (indicatorsOn != IndicatorsOn.All)
				{
					indicatorsOn = IndicatorsOn.All;
				}
			}
			else if (num3 >= 0.5f)
			{
				if (indicatorsOn != IndicatorsOn.Right)
				{
					indicatorsOn = IndicatorsOn.Right;
				}
			}
			else if (num3 <= -0.5f)
			{
				if (indicatorsOn != IndicatorsOn.Left)
				{
					indicatorsOn = IndicatorsOn.Left;
				}
			}
			else
			{
				indicatorsOn = IndicatorsOn.Off;
			}
			if (!automaticGear)
			{
				if (!string.IsNullOrEmpty(RCCSettingsInstance.Xbox_shiftGearUp) && RCC_InputManager.GetButtonDown(RCCSettings.Xbox_shiftGearUp))
				{
					GearShiftUp();
				}
				if (!string.IsNullOrEmpty(RCCSettingsInstance.Xbox_shiftGearDown) && RCC_InputManager.GetButtonDown(RCCSettings.Xbox_shiftGearDown))
				{
					GearShiftDown();
				}
			}
			break;
		}
		case RCC_Settings.ControllerType.PS4:
		{
			if (!string.IsNullOrEmpty(RCCSettingsInstance.PS4_lowBeamHeadlightsKB) && RCC_InputManager.GetButtonDown(RCCSettings.PS4_lowBeamHeadlightsKB))
			{
				lowBeamHeadLightsOn = !lowBeamHeadLightsOn;
			}
			if (!string.IsNullOrEmpty(RCCSettingsInstance.PS4_highBeamHeadlightsKB))
			{
				if (RCC_InputManager.GetButtonDown(RCCSettings.PS4_highBeamHeadlightsKB))
				{
					highBeamHeadLightsOn = true;
				}
				else if (RCC_InputManager.GetButtonUp(RCCSettings.PS4_highBeamHeadlightsKB))
				{
					highBeamHeadLightsOn = false;
				}
			}
			if (!string.IsNullOrEmpty(RCCSettingsInstance.PS4_startEngineKB) && RCC_InputManager.GetButtonDown(RCCSettings.PS4_startEngineKB))
			{
				KillOrStartEngine();
			}
			if (!string.IsNullOrEmpty(RCCSettingsInstance.PS4_trailerAttachDetach) && RCC_InputManager.GetButtonDown(RCCSettings.PS4_trailerAttachDetach))
			{
				DetachTrailer();
			}
			float num = 0f;
			if (!string.IsNullOrEmpty(RCCSettingsInstance.PS4_indicatorKB))
			{
				num = Input.GetAxis(RCCSettings.PS4_indicatorKB);
			}
			float num2 = 0f;
			if (!string.IsNullOrEmpty(RCCSettingsInstance.PS4_hazardIndicatorKB))
			{
				num2 = Input.GetAxis(RCCSettings.PS4_hazardIndicatorKB);
			}
			if (num2 >= 0.5f)
			{
				if (indicatorsOn != IndicatorsOn.All)
				{
					indicatorsOn = IndicatorsOn.All;
				}
			}
			else if (num >= 0.5f)
			{
				if (indicatorsOn != IndicatorsOn.Right)
				{
					indicatorsOn = IndicatorsOn.Right;
				}
			}
			else if (num <= -0.5f)
			{
				if (indicatorsOn != IndicatorsOn.Left)
				{
					indicatorsOn = IndicatorsOn.Left;
				}
			}
			else
			{
				indicatorsOn = IndicatorsOn.Off;
			}
			if (!automaticGear)
			{
				if (!string.IsNullOrEmpty(RCCSettingsInstance.PS4_shiftGearUp) && RCC_InputManager.GetButtonDown(RCCSettings.PS4_shiftGearUp))
				{
					GearShiftUp();
				}
				if (!string.IsNullOrEmpty(RCCSettingsInstance.PS4_shiftGearDown) && RCC_InputManager.GetButtonDown(RCCSettings.PS4_shiftGearDown))
				{
					GearShiftDown();
				}
			}
			break;
		}
		}
		if (permanentGas)
		{
			throttleInput = 1f;
		}
	}

	private void FixedUpdate()
	{
		if ((autoGenerateEngineRPMCurve && oldEngineTorque != maxEngineTorque) || oldMaxTorqueAtRPM != maxEngineTorqueAtRPM || minEngineRPM != oldMinEngineRPM || maxEngineRPM != oldMaxEngineRPM)
		{
			CreateEngineCurve();
		}
		oldEngineTorque = maxEngineTorque;
		oldMaxTorqueAtRPM = maxEngineTorqueAtRPM;
		oldMinEngineRPM = minEngineRPM;
		oldMaxEngineRPM = maxEngineRPM;
		if (gears == null || gears.Length == 0)
		{
			MonoBehaviour.print("Gear can not be 0! Recreating gears...");
			gears = new Gear[totalGears];
			InitGears();
		}
		int num = 0;
		for (int i = 0; i < allWheelColliders.Length; i++)
		{
			if (allWheelColliders[i].canPower)
			{
				num++;
			}
		}
		poweredWheels = num;
		Engine();
		EngineSounds();
		Wheels();
		if (canControl)
		{
			GearBox();
			if (useAutomaticClutch)
			{
				Clutch();
			}
		}
		AntiRollBars();
		CheckGrounded();
		DriftVariables();
		RevLimiter();
		Turbo();
		NOS();
		if (useFuelConsumption)
		{
			Fuel();
		}
		if (useEngineHeat)
		{
			EngineHeat();
		}
		if (steeringHelper)
		{
			SteerHelper();
		}
		if (tractionHelper)
		{
			TractionHelper();
		}
		if (angularDragHelper)
		{
			AngularDragHelper();
		}
		if (ESP)
		{
			ESPCheck(FrontLeftWheelCollider.wheelCollider.steerAngle);
		}
		if (RCCSettings.selectedBehaviorType != null && RCCSettings.selectedBehaviorType.applyRelativeTorque && isGrounded)
		{
			rigid.AddRelativeTorque(Vector3.up * (steerInput * throttleInput * (float)direction) * Mathf.Lerp(1.5f, 0.5f, speed / 100f), ForceMode.Acceleration);
		}
		rigid.centerOfMass = transform.InverseTransformPoint(COM.transform.position);
		rigid.AddRelativeForce(Vector3.down * (speed * downForce), ForceMode.Force);
	}

	private void Engine()
	{
		speed = rigid.velocity.magnitude * 3.6f;
		steerAngle = Mathf.Lerp(orgSteerAngle, highspeedsteerAngle, speed / highspeedsteerAngleAtspeed);
		float num = 0f;
		for (int i = 0; i < allWheelColliders.Length; i++)
		{
			if (allWheelColliders[i].canPower)
			{
				num += allWheelColliders[i].wheelCollider.rpm;
			}
		}
		float currentVelocity = 0f;
		engineRPM = Mathf.SmoothDamp(engineRPM, (Mathf.Lerp(minEngineRPM, maxEngineRPM + 500f, clutchInput * throttleInput) + Mathf.Abs(num / Mathf.Clamp(poweredWheels, 1f, float.PositiveInfinity)) * finalRatio * gears[currentGear].maxRatio * (1f - clutchInput)) * fuelInput, ref currentVelocity, engineInertia);
		engineRPM = Mathf.Clamp(engineRPM, 0f, maxEngineRPM + 500f);
		if (autoReverse)
		{
			canGoReverseNow = true;
		}
		else if (brakeInput < 0.5f && speed < 5f)
		{
			canGoReverseNow = true;
		}
		else if (brakeInput > 0f && transform.InverseTransformDirection(rigid.velocity).z > 1f)
		{
			canGoReverseNow = false;
		}
	}

	private void Audio()
	{
		windSound.volume = Mathf.Lerp(0f, RCCSettings.maxWindSoundVolume, speed / 300f);
		windSound.pitch = UnityEngine.Random.Range(0.9f, 1f);
		if (direction == 1)
		{
			brakeSound.volume = Mathf.Lerp(0f, RCCSettings.maxBrakeSoundVolume, Mathf.Clamp01((FrontLeftWheelCollider.wheelCollider.brakeTorque + FrontRightWheelCollider.wheelCollider.brakeTorque) / (brakeTorque * 2f)) * Mathf.Lerp(0f, 1f, FrontLeftWheelCollider.wheelCollider.rpm / 50f));
		}
		else
		{
			brakeSound.volume = 0f;
		}
	}

	private void ESPCheck(float steering)
	{
		frontSlip = FrontLeftWheelCollider.wheelHit.sidewaysSlip + FrontRightWheelCollider.wheelHit.sidewaysSlip;
		rearSlip = RearLeftWheelCollider.wheelHit.sidewaysSlip + RearRightWheelCollider.wheelHit.sidewaysSlip;
		if (Mathf.Abs(frontSlip) >= ESPThreshold)
		{
			underSteering = true;
		}
		else
		{
			underSteering = false;
		}
		if (Mathf.Abs(rearSlip) >= ESPThreshold)
		{
			overSteering = true;
		}
		else
		{
			overSteering = false;
		}
		if (overSteering || underSteering)
		{
			ESPAct = true;
		}
		else
		{
			ESPAct = false;
		}
	}

	private void EngineSounds()
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		num = ((!(engineRPM < maxEngineRPM / 2f)) ? Mathf.Lerp(1f, 0.25f, engineRPM / maxEngineRPM) : Mathf.Lerp(0f, 1f, engineRPM / (maxEngineRPM / 2f)));
		num2 = ((!(engineRPM < maxEngineRPM / 2f)) ? Mathf.Lerp(1f, 0.5f, engineRPM / maxEngineRPM) : Mathf.Lerp(-0.5f, 1f, engineRPM / (maxEngineRPM / 2f)));
		num3 = Mathf.Lerp(-1f, 1f, engineRPM / maxEngineRPM);
		num = Mathf.Clamp01(num) * maxEngineSoundVolume;
		num2 = Mathf.Clamp01(num2) * maxEngineSoundVolume;
		num3 = Mathf.Clamp01(num3) * maxEngineSoundVolume;
		float num4 = Mathf.Clamp(throttleInput, 0f, 1f);
		float pitch = Mathf.Lerp(minEngineSoundPitch, maxEngineSoundPitch, engineRPM / maxEngineRPM) * (engineRunning ? 1f : 0f);
		switch (audioType)
		{
		case AudioType.OneSource:
			engineSoundHigh.volume = num4 * maxEngineSoundVolume;
			engineSoundHigh.pitch = pitch;
			engineSoundHighOff.volume = (1f - num4) * maxEngineSoundVolume;
			engineSoundHighOff.pitch = pitch;
			if ((bool)engineSoundIdle)
			{
				engineSoundIdle.volume = Mathf.Lerp(engineRunning ? idleEngineSoundVolume : 0f, 0f, engineRPM / maxEngineRPM);
				engineSoundIdle.pitch = pitch;
			}
			if (!engineSoundHigh.isPlaying)
			{
				engineSoundHigh.Play();
			}
			if (!engineSoundIdle.isPlaying)
			{
				engineSoundIdle.Play();
			}
			break;
		case AudioType.TwoSource:
			engineSoundHigh.volume = num3 * num4;
			engineSoundHigh.pitch = pitch;
			engineSoundLow.volume = num * num4;
			engineSoundLow.pitch = pitch;
			engineSoundHighOff.volume = num3 * (1f - num4);
			engineSoundHighOff.pitch = pitch;
			engineSoundLowOff.volume = num * (1f - num4);
			engineSoundLowOff.pitch = pitch;
			if ((bool)engineSoundIdle)
			{
				engineSoundIdle.volume = Mathf.Lerp(engineRunning ? idleEngineSoundVolume : 0f, 0f, engineRPM / maxEngineRPM);
				engineSoundIdle.pitch = pitch;
			}
			if (!engineSoundLow.isPlaying)
			{
				engineSoundLow.Play();
			}
			if (!engineSoundHigh.isPlaying)
			{
				engineSoundHigh.Play();
			}
			if (!engineSoundIdle.isPlaying)
			{
				engineSoundIdle.Play();
			}
			break;
		case AudioType.ThreeSource:
			engineSoundHigh.volume = num3 * num4;
			engineSoundHigh.pitch = pitch;
			engineSoundMed.volume = num2 * num4;
			engineSoundMed.pitch = pitch;
			engineSoundLow.volume = num * num4;
			engineSoundLow.pitch = pitch;
			engineSoundHighOff.volume = num3 * (1f - num4);
			engineSoundHighOff.pitch = pitch;
			engineSoundMedOff.volume = num2 * (1f - num4);
			engineSoundMedOff.pitch = pitch;
			engineSoundLowOff.volume = num * (1f - num4);
			engineSoundLowOff.pitch = pitch;
			if ((bool)engineSoundIdle)
			{
				engineSoundIdle.volume = Mathf.Lerp(engineRunning ? idleEngineSoundVolume : 0f, 0f, engineRPM / maxEngineRPM);
				engineSoundIdle.pitch = pitch;
			}
			if (!engineSoundLow.isPlaying)
			{
				engineSoundLow.Play();
			}
			if (!engineSoundMed.isPlaying)
			{
				engineSoundMed.Play();
			}
			if (!engineSoundHigh.isPlaying)
			{
				engineSoundHigh.Play();
			}
			if (!engineSoundIdle.isPlaying)
			{
				engineSoundIdle.Play();
			}
			break;
		}
	}

	private void Wheels()
	{
		for (int i = 0; i < allWheelColliders.Length; i++)
		{
			if (allWheelColliders[i].canPower)
			{
				allWheelColliders[i].ApplyMotorTorque((float)direction * allWheelColliders[i].powerMultiplier * (1f - clutchInput) * throttleInput * (1f + boostInput) * (engineTorqueCurve.Evaluate(engineRPM) * gears[currentGear].maxRatio * finalRatio) / Mathf.Clamp(poweredWheels, 1f, float.PositiveInfinity));
			}
			if (allWheelColliders[i].canSteer)
			{
				allWheelColliders[i].ApplySteering(steerInput * allWheelColliders[i].steeringMultiplier, steerAngle);
			}
			bool flag = false;
			if (!flag && handbrakeInput > 0.5f)
			{
				flag = true;
				if (allWheelColliders[i].canHandbrake)
				{
					allWheelColliders[i].ApplyBrakeTorque(brakeTorque * handbrakeInput * allWheelColliders[i].handbrakeMultiplier);
				}
			}
			if (!flag && brakeInput >= 0.05f)
			{
				flag = true;
				if (allWheelColliders[i].canBrake)
				{
					allWheelColliders[i].ApplyBrakeTorque(brakeInput * brakeTorque * allWheelColliders[i].brakingMultiplier);
				}
			}
			if (ESPAct)
			{
				flag = true;
			}
			if (!flag)
			{
				allWheelColliders[i].ApplyBrakeTorque(0f);
			}
			if (!allWheelColliders[i].canPower)
			{
				allWheelColliders[i].ApplyMotorTorque(0f);
			}
			if (!allWheelColliders[i].canBrake)
			{
				allWheelColliders[i].ApplyBrakeTorque(0f);
			}
			if (!allWheelColliders[i].canSteer)
			{
				allWheelColliders[i].ApplySteering(0f, 0f);
			}
		}
	}

	private void AntiRollBars()
	{
		float num = 1f;
		float num2 = 1f;
		bool flag = FrontLeftWheelCollider.isGrounded;
		if (flag)
		{
			num = (0f - FrontLeftWheelCollider.transform.InverseTransformPoint(FrontLeftWheelCollider.wheelHit.point).y - FrontLeftWheelCollider.wheelCollider.radius) / FrontLeftWheelCollider.wheelCollider.suspensionDistance;
		}
		bool flag2 = FrontRightWheelCollider.isGrounded;
		if (flag2)
		{
			num2 = (0f - FrontRightWheelCollider.transform.InverseTransformPoint(FrontRightWheelCollider.wheelHit.point).y - FrontRightWheelCollider.wheelCollider.radius) / FrontRightWheelCollider.wheelCollider.suspensionDistance;
		}
		float num3 = (num - num2) * antiRollFrontHorizontal;
		if (flag)
		{
			rigid.AddForceAtPosition(FrontLeftWheelCollider.transform.up * (0f - num3), FrontLeftWheelCollider.transform.position);
		}
		if (flag2)
		{
			rigid.AddForceAtPosition(FrontRightWheelCollider.transform.up * num3, FrontRightWheelCollider.transform.position);
		}
		float num4 = 1f;
		float num5 = 1f;
		bool flag3 = RearLeftWheelCollider.isGrounded;
		if (flag3)
		{
			num4 = (0f - RearLeftWheelCollider.transform.InverseTransformPoint(RearLeftWheelCollider.wheelHit.point).y - RearLeftWheelCollider.wheelCollider.radius) / RearLeftWheelCollider.wheelCollider.suspensionDistance;
		}
		bool flag4 = RearRightWheelCollider.isGrounded;
		if (flag4)
		{
			num5 = (0f - RearRightWheelCollider.transform.InverseTransformPoint(RearRightWheelCollider.wheelHit.point).y - RearRightWheelCollider.wheelCollider.radius) / RearRightWheelCollider.wheelCollider.suspensionDistance;
		}
		float num6 = (num4 - num5) * antiRollRearHorizontal;
		if (flag3)
		{
			rigid.AddForceAtPosition(RearLeftWheelCollider.transform.up * (0f - num6), RearLeftWheelCollider.transform.position);
		}
		if (flag4)
		{
			rigid.AddForceAtPosition(RearRightWheelCollider.transform.up * num6, RearRightWheelCollider.transform.position);
		}
		float num7 = (num - num4) * antiRollVertical;
		if (flag)
		{
			rigid.AddForceAtPosition(FrontLeftWheelCollider.transform.up * (0f - num7), FrontLeftWheelCollider.transform.position);
		}
		if (flag3)
		{
			rigid.AddForceAtPosition(RearLeftWheelCollider.transform.up * num7, RearLeftWheelCollider.transform.position);
		}
		float num8 = (num2 - num5) * antiRollVertical;
		if (flag2)
		{
			rigid.AddForceAtPosition(FrontRightWheelCollider.transform.up * (0f - num8), FrontRightWheelCollider.transform.position);
		}
		if (flag4)
		{
			rigid.AddForceAtPosition(RearRightWheelCollider.transform.up * num8, RearRightWheelCollider.transform.position);
		}
	}

	public void CheckGrounded()
	{
		bool flag = true;
		for (int i = 0; i < allWheelColliders.Length; i++)
		{
			if (!allWheelColliders[i].wheelCollider.isGrounded)
			{
				flag = false;
			}
		}
		isGrounded = flag;
	}

	private void SteerHelper()
	{
		if (!isGrounded)
		{
			return;
		}
		if (!steeringDirection || !velocityDirection)
		{
			if (!steeringDirection)
			{
				GameObject gameObject = new GameObject("Steering Direction");
				gameObject.transform.SetParent(transform, worldPositionStays: false);
				steeringDirection = gameObject.transform;
				gameObject.transform.localPosition = new Vector3(1f, 2f, 0f);
				gameObject.transform.localScale = new Vector3(0.1f, 0.1f, 3f);
			}
			if (!velocityDirection)
			{
				GameObject gameObject2 = new GameObject("Velocity Direction");
				gameObject2.transform.SetParent(transform, worldPositionStays: false);
				velocityDirection = gameObject2.transform;
				gameObject2.transform.localPosition = new Vector3(-1f, 2f, 0f);
				gameObject2.transform.localScale = new Vector3(0.1f, 0.1f, 3f);
			}
			return;
		}
		for (int i = 0; i < allWheelColliders.Length; i++)
		{
			if (allWheelColliders[i].wheelHit.normal == Vector3.zero)
			{
				return;
			}
		}
		velocityAngle = rigid.angularVelocity.y * Mathf.Clamp(transform.InverseTransformDirection(rigid.velocity).z, -1f, 1f) * 57.29578f;
		velocityDirection.localRotation = Quaternion.Lerp(velocityDirection.localRotation, Quaternion.AngleAxis(Mathf.Clamp(velocityAngle / 3f, -45f, 45f), Vector3.up), Time.fixedDeltaTime * 20f);
		steeringDirection.localRotation = Quaternion.Euler(0f, FrontLeftWheelCollider.wheelCollider.steerAngle, 0f);
		int num = 1;
		num = ((steeringDirection.localRotation.y > velocityDirection.localRotation.y) ? 1 : (-1));
		float num2 = Quaternion.Angle(velocityDirection.localRotation, steeringDirection.localRotation) * (float)num;
		rigid.AddRelativeTorque(Vector3.up * (num2 * (Mathf.Clamp(transform.InverseTransformDirection(rigid.velocity).z, -10f, 10f) / 1000f) * steerHelperAngularVelStrength), ForceMode.VelocityChange);
		if (Mathf.Abs(oldRotation - transform.eulerAngles.y) < 10f)
		{
			Quaternion quaternion = Quaternion.AngleAxis((transform.eulerAngles.y - oldRotation) * (steerHelperLinearVelStrength / 2f), Vector3.up);
			rigid.velocity = quaternion * rigid.velocity;
		}
		oldRotation = transform.eulerAngles.y;
	}

	private void TractionHelper()
	{
		if (isGrounded)
		{
			Vector3 velocity = rigid.velocity;
			velocity -= transform.up * Vector3.Dot(velocity, transform.up);
			velocity.Normalize();
			angle = 0f - Mathf.Asin(Vector3.Dot(Vector3.Cross(transform.forward, velocity), transform.up));
			angularVelo = rigid.angularVelocity.y;
			if (angle * FrontLeftWheelCollider.wheelCollider.steerAngle < 0f)
			{
				FrontLeftWheelCollider.tractionHelpedSidewaysStiffness = 1f - Mathf.Clamp01(tractionHelperStrength * Mathf.Abs(angularVelo));
			}
			else
			{
				FrontLeftWheelCollider.tractionHelpedSidewaysStiffness = 1f;
			}
			if (angle * FrontRightWheelCollider.wheelCollider.steerAngle < 0f)
			{
				FrontRightWheelCollider.tractionHelpedSidewaysStiffness = 1f - Mathf.Clamp01(tractionHelperStrength * Mathf.Abs(angularVelo));
			}
			else
			{
				FrontRightWheelCollider.tractionHelpedSidewaysStiffness = 1f;
			}
		}
	}

	private void AngularDragHelper()
	{
		rigid.angularDrag = Mathf.Lerp(0f, 10f, speed * angularDragHelperStrength / 1000f);
	}

	private void Clutch()
	{
		float num = 0f;
		for (int i = 0; i < allWheelColliders.Length; i++)
		{
			if (allWheelColliders[i].canPower)
			{
				num += allWheelColliders[i].wheelCollider.rpm;
			}
		}
		if (currentGear == 0)
		{
			if (launched >= 0.25f)
			{
				clutchInput = Mathf.Lerp(clutchInput, Mathf.Lerp(1f, Mathf.Lerp(clutchInertia, 0f, num / Mathf.Clamp(poweredWheels, 1f, float.PositiveInfinity) / (float)gears[0].targetSpeedForNextGear), Mathf.Abs(throttleInput)), Time.fixedDeltaTime * 5f);
			}
			else
			{
				clutchInput = Mathf.Lerp(clutchInput, 1f / speed, Time.fixedDeltaTime * 5f);
			}
		}
		else if (changingGear)
		{
			clutchInput = Mathf.Lerp(clutchInput, 1f, Time.fixedDeltaTime * 5f);
		}
		else
		{
			clutchInput = Mathf.Lerp(clutchInput, 0f, Time.fixedDeltaTime * 5f);
		}
		if (cutGas || handbrakeInput >= 0.1f)
		{
			clutchInput = 1f;
		}
		if (NGear)
		{
			clutchInput = 1f;
		}
		clutchInput = Mathf.Clamp01(clutchInput);
	}

	private void GearBox()
	{
		if (automaticGear)
		{
			if (currentGear < gears.Length - 1 && !changingGear && direction == 1 && speed >= (float)gears[currentGear].targetSpeedForNextGear && engineRPM >= gearShiftUpRPM)
			{
				if (!semiAutomaticGear)
				{
					StartCoroutine(ChangeGear(currentGear + 1));
				}
				else if (semiAutomaticGear && direction != -1)
				{
					StartCoroutine(ChangeGear(currentGear + 1));
				}
			}
			if (currentGear > 0 && !changingGear && direction != -1 && speed < (float)gears[currentGear - 1].targetSpeedForNextGear && engineRPM <= gearShiftDownRPM)
			{
				StartCoroutine(ChangeGear(currentGear - 1));
			}
		}
		if (direction == -1)
		{
			if (!reversingSound.isPlaying)
			{
				reversingSound.Play();
			}
			reversingSound.volume = Mathf.Lerp(0f, 1f, speed / (float)gears[0].maxSpeed);
			reversingSound.pitch = reversingSound.volume;
		}
		else
		{
			if (reversingSound.isPlaying)
			{
				reversingSound.Stop();
			}
			reversingSound.volume = 0f;
			reversingSound.pitch = 0f;
		}
	}

	public IEnumerator ChangeGear(int gear)
	{
		changingGear = true;
		if (RCCSettings.useTelemetry)
		{
			MonoBehaviour.print("Shifted to: " + gear);
		}
		yield return new WaitForSeconds(gearShiftingDelay);
		if (gear == -1)
		{
			currentGear = 0;
			if (!NGear)
			{
				direction = -1;
			}
			else
			{
				direction = 0;
			}
		}
		else
		{
			currentGear = gear;
			if (!NGear)
			{
				direction = 1;
			}
			else
			{
				direction = 0;
			}
		}
		changingGear = false;
	}

	public void GearShiftUp()
	{
		if (currentGear < gears.Length - 1 && !changingGear)
		{
			if (direction != -1)
			{
				StartCoroutine(ChangeGear(currentGear + 1));
			}
			else
			{
				StartCoroutine(ChangeGear(0));
			}
		}
	}

	public void GearShiftTo(int gear)
	{
		if (gear >= -1 && gear < gears.Length && gear != currentGear)
		{
			StartCoroutine(ChangeGear(gear));
		}
	}

	public void GearShiftDown()
	{
		if (currentGear >= 0)
		{
			StartCoroutine(ChangeGear(currentGear - 1));
		}
	}

	private void RevLimiter()
	{
		if (useRevLimiter && engineRPM >= maxEngineRPM)
		{
			cutGas = true;
		}
		else if (engineRPM < maxEngineRPM * 0.95f)
		{
			cutGas = false;
		}
	}

	private void NOS()
	{
		if (!useNOS)
		{
			return;
		}
		if (!NOSSound)
		{
			NOSSound = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, exhaustSoundPosition, "NOS Sound AudioSource", 5f, 10f, 0.5f, NOSClip, loop: true, playNow: false, destroyAfterFinished: false);
		}
		if (!blowSound)
		{
			blowSound = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, exhaustSoundPosition, "NOS Blow", 1f, 10f, 0.5f, null, loop: false, playNow: false, destroyAfterFinished: false);
		}
		if (boostInput >= 0.8f && throttleInput >= 0.8f && NoS > 5f)
		{
			NoS -= NoSConsumption * Time.fixedDeltaTime;
			NoSRegenerateTime = 0f;
			if (!NOSSound.isPlaying)
			{
				NOSSound.Play();
			}
			return;
		}
		if (NoS < 100f && NoSRegenerateTime > 3f)
		{
			NoS += NoSConsumption / 1.5f * Time.fixedDeltaTime;
		}
		NoSRegenerateTime += Time.fixedDeltaTime;
		if (NOSSound.isPlaying)
		{
			NOSSound.Stop();
			blowSound.clip = blowClip[UnityEngine.Random.Range(0, blowClip.Length)];
			blowSound.Play();
		}
	}

	private void Turbo()
	{
		if (useTurbo)
		{
			if (!turboSound)
			{
				turboSound = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, turboSoundPosition, "Turbo Sound AudioSource", 0.1f, 0.5f, 0f, turboClip, loop: true, playNow: true, destroyAfterFinished: false);
				RCC_Core.NewHighPassFilter(turboSound, 10000f, 10);
			}
			turboBoost = Mathf.Lerp(turboBoost, Mathf.Clamp(Mathf.Pow(throttleInput, 10f) * 30f + Mathf.Pow(engineRPM / maxEngineRPM, 10f) * 30f, 0f, 30f), Time.fixedDeltaTime * 10f);
			if (turboBoost >= 25f && turboBoost < turboSound.volume * 30f && !blowSound.isPlaying)
			{
				blowSound.clip = RCCSettings.blowoutClip[UnityEngine.Random.Range(0, RCCSettings.blowoutClip.Length)];
				blowSound.Play();
			}
			turboSound.volume = Mathf.Lerp(turboSound.volume, turboBoost / 30f, Time.fixedDeltaTime * 5f);
			turboSound.pitch = Mathf.Lerp(Mathf.Clamp(turboSound.pitch, 2f, 3f), turboBoost / 30f * 2f, Time.fixedDeltaTime * 5f);
		}
	}

	private void Fuel()
	{
		fuelTank -= engineRPM / 10000f * fuelConsumptionRate * Time.fixedDeltaTime;
		fuelTank = Mathf.Clamp(fuelTank, 0f, fuelTankCapacity);
	}

	private void EngineHeat()
	{
		engineHeat += engineRPM / 10000f * engineHeatRate * Time.fixedDeltaTime;
		if (engineHeat > engineCoolingWaterThreshold)
		{
			engineHeat -= engineCoolRate * Time.fixedDeltaTime;
		}
		engineHeat -= engineCoolRate / 10f * Time.fixedDeltaTime;
		engineHeat = Mathf.Clamp(engineHeat, 15f, 120f);
	}

	private void DriftVariables()
	{
		if (Mathf.Abs(RearRightWheelCollider.wheelHit.sidewaysSlip) > 0.25f)
		{
			driftingNow = true;
		}
		else
		{
			driftingNow = false;
		}
		if (speed > 10f)
		{
			driftAngle = RearRightWheelCollider.wheelHit.sidewaysSlip * 0.75f;
		}
		else
		{
			driftAngle = 0f;
		}
	}

	private void ResetCar()
	{
		if (speed < 5f && !rigid.isKinematic && RCCSettings.autoReset && transform.eulerAngles.z < 300f && transform.eulerAngles.z > 60f)
		{
			resetTime += Time.deltaTime;
			if (resetTime > 3f)
			{
				transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
				transform.position = new Vector3(transform.position.x, transform.position.y + 0.25f, transform.position.z);
				repairNow = true;
				Repair();
				resetTime = 0f;
			}
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		CollisionParticles(collision.contacts[0].point);
		if (collision.contacts.Length < 1 || collision.relativeVelocity.magnitude < minimumCollisionForce)
		{
			return;
		}
		if (OnRCCPlayerCollision != null && this == RCC_SceneManager.Instance.activePlayerVehicle)
		{
			OnRCCPlayerCollision(this, collision);
		}
		if (!useDamage || ((1 << collision.gameObject.layer) & (int)damageFilter) == 0)
		{
			return;
		}
		CollisionParticles(collision.contacts[0].point);
		Vector3 relativeVelocity = collision.relativeVelocity;
		relativeVelocity *= 1f - Mathf.Abs(Vector3.Dot(transform.up, collision.contacts[0].normal));
		float num = Mathf.Abs(Vector3.Dot(collision.contacts[0].normal, relativeVelocity.normalized));
		if (!(relativeVelocity.magnitude * num >= minimumCollisionForce))
		{
			return;
		}
		repaired = false;
		localVector = transform.InverseTransformDirection(relativeVelocity) * (damageMultiplier / 50f);
		if (originalMeshData == null)
		{
			LoadOriginalMeshData();
		}
		for (int i = 0; i < deformableMeshFilters.Length; i++)
		{
			if (deformableMeshFilters[i] != null)
			{
				DeformMesh(deformableMeshFilters[i].mesh, originalMeshData[i].meshVerts, collision, num, deformableMeshFilters[i].transform, rot);
			}
		}
		if (useWheelDamage)
		{
			for (int j = 0; j < allWheelColliders.Length; j++)
			{
				if (allWheelColliders[j].transform.InverseTransformPoint(collision.contacts[0].point).magnitude <= wheelDamageRadius)
				{
					allWheelColliders[j].damagedToe += UnityEngine.Random.Range(0f - relativeVelocity.magnitude * num, relativeVelocity.magnitude * num) * wheelDamageMultiplier;
					allWheelColliders[j].damagedCamber += UnityEngine.Random.Range(0f - relativeVelocity.magnitude * num, relativeVelocity.magnitude * num) * wheelDamageMultiplier;
					allWheelColliders[j].damagedCaster += UnityEngine.Random.Range(0f - relativeVelocity.magnitude * num, relativeVelocity.magnitude * num) * wheelDamageMultiplier;
				}
			}
		}
		if (detachableParts != null && detachableParts.Length >= 1)
		{
			for (int k = 0; k < detachableParts.Length; k++)
			{
				detachableParts[k].OnCollision(collision);
			}
		}
		if (crashClips.Length != 0 && collision.contacts[0].thisCollider.gameObject.transform != transform.parent)
		{
			crashSound = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, "Crash Sound AudioSource", 5f, 20f, RCCSettings.maxCrashSoundVolume, crashClips[UnityEngine.Random.Range(0, crashClips.Length)], loop: false, playNow: true, destroyAfterFinished: true);
			if (!crashSound.isPlaying)
			{
				crashSound.Play();
			}
		}
	}

	private void OnDrawGizmos()
	{
	}

	public void PreviewSmokeParticle(bool state)
	{
		canControl = state;
		permanentGas = state;
		rigid.isKinematic = state;
	}

	public void DetachTrailer()
	{
		if ((bool)attachedTrailer)
		{
			attachedTrailer.DetachTrailer();
		}
	}

	private void OnDestroy()
	{
		if (OnRCCPlayerDestroyed != null)
		{
			OnRCCPlayerDestroyed(this);
		}
		if (canControl && (bool)gameObject.GetComponentInChildren<RCC_Camera>())
		{
			gameObject.GetComponentInChildren<RCC_Camera>().transform.SetParent(null);
		}
	}

	public void SetCanControl(bool state)
	{
		canControl = state;
	}

	public void SetEngine(bool state)
	{
		if (state)
		{
			StartEngine();
		}
		else
		{
			KillEngine();
		}
	}

	private void OnDisable()
	{
		RCC_SceneManager.OnBehaviorChanged -= CheckBehavior;
	}
}
