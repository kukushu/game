using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(WheelCollider))]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Main/RCC Wheel Collider")]
public class RCC_WheelCollider : RCC_Core
{
	private RCC_Settings RCCSettingsInstance;

	private RCC_GroundMaterials RCCGroundMaterialsInstance;

	private WheelCollider _wheelCollider;

	private RCC_CarControllerV3 _carController;

	private Rigidbody _rigid;

	private List<RCC_WheelCollider> allWheelColliders = new List<RCC_WheelCollider>();

	public Transform wheelModel;

	public WheelHit wheelHit;

	public bool isGrounded;

	[Space]
	public bool canPower;

	[Range(-1f, 1f)]
	public float powerMultiplier = 1f;

	public bool canSteer;

	[Range(-1f, 1f)]
	public float steeringMultiplier = 1f;

	public bool canBrake;

	[Range(0f, 1f)]
	public float brakingMultiplier = 1f;

	public bool canHandbrake;

	[Range(0f, 1f)]
	public float handbrakeMultiplier = 1f;

	[Space]
	public float width = 0.275f;

	public float offset = 0.05f;

	internal float wheelRPM2Speed;

	[Range(-5f, 5f)]
	public float camber;

	[Range(-5f, 5f)]
	public float caster;

	[Range(-5f, 5f)]
	public float toe;

	internal float damagedCamber;

	internal float damagedCaster;

	internal float damagedToe;

	private RCC_SkidmarksManager skidmarksManager;

	private int lastSkidmark = -1;

	private float wheelSlipAmountForward;

	private float wheelSlipAmountSideways;

	private float totalSlip;

	private WheelFrictionCurve forwardFrictionCurve;

	private WheelFrictionCurve sidewaysFrictionCurve;

	private AudioSource audioSource;

	private AudioClip audioClip;

	private float audioVolume = 1f;

	private int groundIndex;

	internal List<ParticleSystem> allWheelParticles = new List<ParticleSystem>();

	internal ParticleSystem.EmissionModule emission;

	internal float tractionHelpedSidewaysStiffness = 1f;

	private float minForwardStiffness = 0.75f;

	private float maxForwardStiffness = 1f;

	private float minSidewaysStiffness = 0.75f;

	private float maxSidewaysStiffness = 1f;

	private TerrainData mTerrainData;

	private int alphamapWidth;

	private int alphamapHeight;

	private float[,,] mSplatmapData;

	private float mNumTextures;

	public float bumpForce;

	public float oldForce;

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

	private RCC_GroundMaterials RCCGroundMaterials
	{
		get
		{
			if (RCCGroundMaterialsInstance == null)
			{
				RCCGroundMaterialsInstance = RCC_GroundMaterials.Instance;
			}
			return RCCGroundMaterialsInstance;
		}
	}

	public WheelCollider wheelCollider
	{
		get
		{
			if (_wheelCollider == null)
			{
				_wheelCollider = GetComponent<WheelCollider>();
			}
			return _wheelCollider;
		}
	}

	public RCC_CarControllerV3 carController
	{
		get
		{
			if (_carController == null)
			{
				_carController = GetComponentInParent<RCC_CarControllerV3>();
			}
			return _carController;
		}
	}

	public Rigidbody rigid
	{
		get
		{
			if (_rigid == null)
			{
				_rigid = carController.gameObject.GetComponent<Rigidbody>();
			}
			return _rigid;
		}
	}

	private RCC_GroundMaterials physicsMaterials => RCCGroundMaterials;

	private RCC_GroundMaterials.GroundMaterialFrictions[] physicsFrictions => RCCGroundMaterials.frictions;

	private void Start()
	{
		allWheelColliders = carController.GetComponentsInChildren<RCC_WheelCollider>().ToList();
		GetTerrainData();
		CheckBehavior();
		if (!RCCSettings.dontUseSkidmarks)
		{
			if ((bool)UnityEngine.Object.FindObjectOfType<RCC_SkidmarksManager>())
			{
				skidmarksManager = UnityEngine.Object.FindObjectOfType<RCC_SkidmarksManager>();
			}
			else
			{
				skidmarksManager = UnityEngine.Object.Instantiate(RCCSettings.skidmarksManager, Vector3.zero, Quaternion.identity);
			}
		}
		if (RCCSettings.useFixedWheelColliders)
		{
			wheelCollider.mass = rigid.mass / 15f;
		}
		audioSource = RCC_Core.NewAudioSource(RCCSettings.audioMixer, carController.gameObject, "Skid Sound AudioSource", 5f, 50f, 0f, audioClip, loop: true, playNow: true, destroyAfterFinished: false);
		audioSource.transform.position = transform.position;
		if (!RCCSettings.dontUseAnyParticleEffects)
		{
			for (int i = 0; i < RCCGroundMaterials.frictions.Length; i++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(RCCGroundMaterials.frictions[i].groundParticles, transform.position, transform.rotation);
				emission = gameObject.GetComponent<ParticleSystem>().emission;
				emission.enabled = false;
				gameObject.transform.SetParent(transform, worldPositionStays: false);
				gameObject.transform.localPosition = Vector3.zero;
				gameObject.transform.localRotation = Quaternion.identity;
				allWheelParticles.Add(gameObject.GetComponent<ParticleSystem>());
			}
		}
		GameObject gameObject2 = new GameObject("Pivot_" + wheelModel.transform.name);
		gameObject2.transform.position = RCC_GetBounds.GetBoundsCenter(wheelModel.transform);
		gameObject2.transform.rotation = transform.rotation;
		gameObject2.transform.SetParent(wheelModel.transform.parent, worldPositionStays: true);
		if (gameObject2.transform.localPosition.x > 0f)
		{
			wheelModel.transform.position += transform.right * offset;
		}
		else
		{
			wheelModel.transform.position -= transform.right * offset;
		}
		wheelModel.SetParent(gameObject2.transform, worldPositionStays: true);
		wheelModel = gameObject2.transform;
		if (carController.overrideWheels)
		{
			if (this == carController.FrontLeftWheelCollider || this == carController.FrontRightWheelCollider)
			{
				canSteer = true;
				canBrake = true;
				brakingMultiplier = 1f;
			}
			if (this == carController.RearLeftWheelCollider || this == carController.RearRightWheelCollider)
			{
				canHandbrake = true;
				canBrake = true;
				brakingMultiplier = 0.5f;
			}
		}
	}

	private void OnEnable()
	{
		RCC_SceneManager.OnBehaviorChanged += CheckBehavior;
	}

	private void CheckBehavior()
	{
		forwardFrictionCurve = wheelCollider.forwardFriction;
		sidewaysFrictionCurve = wheelCollider.sidewaysFriction;
		RCC_Settings.BehaviorType selectedBehaviorType = RCCSettings.selectedBehaviorType;
		if (selectedBehaviorType != null)
		{
			forwardFrictionCurve = SetFrictionCurves(forwardFrictionCurve, selectedBehaviorType.forwardExtremumSlip, selectedBehaviorType.forwardExtremumValue, selectedBehaviorType.forwardAsymptoteSlip, selectedBehaviorType.forwardAsymptoteValue);
			sidewaysFrictionCurve = SetFrictionCurves(sidewaysFrictionCurve, selectedBehaviorType.sidewaysExtremumSlip, selectedBehaviorType.sidewaysExtremumValue, selectedBehaviorType.sidewaysAsymptoteSlip, selectedBehaviorType.sidewaysAsymptoteValue);
		}
		wheelCollider.forwardFriction = forwardFrictionCurve;
		wheelCollider.sidewaysFriction = sidewaysFrictionCurve;
	}

	private void Update()
	{
		if (carController.enabled)
		{
			WheelAlign();
		}
	}

	private void FixedUpdate()
	{
		isGrounded = wheelCollider.GetGroundHit(out wheelHit);
		groundIndex = GetGroundMaterialIndex();
		float num = 6.28f * wheelCollider.radius;
		wheelRPM2Speed = num * wheelCollider.rpm * 60f;
		wheelRPM2Speed = Mathf.Clamp(wheelRPM2Speed / 1000f, 0f, float.PositiveInfinity);
		if (carController.overrideWheels)
		{
			switch (carController.wheelTypeChoise)
			{
			case RCC_CarControllerV3.WheelType.AWD:
				canPower = true;
				break;
			case RCC_CarControllerV3.WheelType.BIASED:
				canPower = true;
				break;
			case RCC_CarControllerV3.WheelType.FWD:
				if (this == carController.FrontLeftWheelCollider || this == carController.FrontRightWheelCollider)
				{
					canPower = true;
				}
				else
				{
					canPower = false;
				}
				break;
			case RCC_CarControllerV3.WheelType.RWD:
				if (this == carController.RearLeftWheelCollider || this == carController.RearRightWheelCollider)
				{
					canPower = true;
				}
				else
				{
					canPower = false;
				}
				break;
			}
		}
		Frictions();
		SkidMarks();
		Audio();
		Particles();
		if (!carController.enabled || !carController.ESP || !(carController.brakeInput < 0.5f) || !(carController.handbrakeInput < 0.5f))
		{
			return;
		}
		if (carController.underSteering)
		{
			if (this == carController.FrontLeftWheelCollider)
			{
				ApplyBrakeTorque(carController.brakeTorque * carController.ESPStrength * Mathf.Clamp(0f - carController.rearSlip, 0f, float.PositiveInfinity));
			}
			if (this == carController.FrontRightWheelCollider)
			{
				ApplyBrakeTorque(carController.brakeTorque * carController.ESPStrength * Mathf.Clamp(carController.rearSlip, 0f, float.PositiveInfinity));
			}
		}
		if (carController.overSteering)
		{
			if (this == carController.RearLeftWheelCollider)
			{
				ApplyBrakeTorque(carController.brakeTorque * carController.ESPStrength * Mathf.Clamp(0f - carController.frontSlip, 0f, float.PositiveInfinity));
			}
			if (this == carController.RearRightWheelCollider)
			{
				ApplyBrakeTorque(carController.brakeTorque * carController.ESPStrength * Mathf.Clamp(carController.frontSlip, 0f, float.PositiveInfinity));
			}
		}
	}

	private void WheelAlign()
	{
		if (!wheelModel)
		{
			Debug.LogError(transform.name + " wheel of the " + carController.transform.name + " is missing wheel model. This wheel is disabled");
			enabled = false;
			return;
		}
		Vector3 pos = Vector3.zero;
		Quaternion quat = Quaternion.identity;
		wheelCollider.GetWorldPose(out pos, out quat);
		wheelModel.transform.position = pos;
		wheelModel.transform.rotation = quat;
		if (transform.localPosition.x < 0f)
		{
			wheelModel.transform.position += transform.right * offset;
		}
		else
		{
			wheelModel.transform.position -= transform.right * offset;
		}
		if (transform.localPosition.x < 0f)
		{
			wheelModel.transform.RotateAround(wheelModel.transform.position, transform.forward, 0f - camber - damagedCamber);
		}
		else
		{
			wheelModel.transform.RotateAround(wheelModel.transform.position, transform.forward, camber + damagedCamber);
		}
		if (transform.localPosition.x < 0f)
		{
			wheelModel.transform.RotateAround(wheelModel.transform.position, transform.right, 0f - caster - damagedCaster);
		}
		else
		{
			wheelModel.transform.RotateAround(wheelModel.transform.position, transform.right, caster + damagedCaster);
		}
	}

	private void SkidMarks()
	{
		if (isGrounded)
		{
			wheelSlipAmountForward = Mathf.Abs(wheelHit.forwardSlip);
			wheelSlipAmountSideways = Mathf.Abs(wheelHit.sidewaysSlip);
		}
		else
		{
			wheelSlipAmountForward = 0f;
			wheelSlipAmountSideways = 0f;
		}
		totalSlip = Mathf.Lerp(totalSlip, (wheelSlipAmountSideways + wheelSlipAmountForward) / 2f, Time.fixedDeltaTime * 5f);
		if (RCCSettings.dontUseSkidmarks)
		{
			return;
		}
		if (totalSlip > physicsFrictions[groundIndex].slip)
		{
			Vector3 pos = wheelHit.point + 2f * rigid.velocity * Time.deltaTime;
			if (rigid.velocity.magnitude > 1f)
			{
				lastSkidmark = skidmarksManager.AddSkidMark(pos, wheelHit.normal, totalSlip - physicsFrictions[groundIndex].slip, lastSkidmark, groundIndex, width);
			}
			else
			{
				lastSkidmark = -1;
			}
		}
		else
		{
			lastSkidmark = -1;
		}
	}

	private void Frictions()
	{
		float handbrakeInput = carController.handbrakeInput;
		handbrakeInput = (((!(this == carController.RearLeftWheelCollider) && !(this == carController.RearRightWheelCollider)) || !(handbrakeInput > 0.75f)) ? 1f : 0.5f);
		forwardFrictionCurve.stiffness = physicsFrictions[groundIndex].forwardStiffness;
		sidewaysFrictionCurve.stiffness = physicsFrictions[groundIndex].sidewaysStiffness * handbrakeInput * tractionHelpedSidewaysStiffness;
		if (RCCSettings.selectedBehaviorType != null && RCCSettings.selectedBehaviorType.applyExternalWheelFrictions)
		{
			Drift();
		}
		wheelCollider.forwardFriction = forwardFrictionCurve;
		wheelCollider.sidewaysFriction = sidewaysFrictionCurve;
		wheelCollider.wheelDampingRate = physicsFrictions[groundIndex].damp;
		audioClip = physicsFrictions[groundIndex].groundSound;
		audioVolume = physicsFrictions[groundIndex].volume;
	}

	private void Particles()
	{
		if (RCCSettings.dontUseAnyParticleEffects)
		{
			return;
		}
		for (int i = 0; i < allWheelParticles.Count; i++)
		{
			if (totalSlip > physicsFrictions[groundIndex].slip)
			{
				if (i != groundIndex)
				{
					ParticleSystem.EmissionModule emissionModule = allWheelParticles[i].emission;
					emissionModule.enabled = false;
				}
				else
				{
					ParticleSystem.EmissionModule emissionModule2 = allWheelParticles[i].emission;
					emissionModule2.enabled = true;
				}
			}
			else
			{
				ParticleSystem.EmissionModule emissionModule3 = allWheelParticles[i].emission;
				emissionModule3.enabled = false;
			}
		}
	}

	private void Drift()
	{
		Vector3 vector = transform.InverseTransformDirection(rigid.velocity);
		float num = vector.x * vector.x / 10f;
		num += Mathf.Abs(wheelHit.forwardSlip * wheelHit.forwardSlip) * 1f;
		if (wheelCollider == carController.FrontLeftWheelCollider.wheelCollider || wheelCollider == carController.FrontRightWheelCollider.wheelCollider)
		{
			forwardFrictionCurve.extremumValue = Mathf.Clamp(1f - num, 0.5f, maxForwardStiffness);
			forwardFrictionCurve.asymptoteValue = Mathf.Clamp(0.75f - num / 2f, 0.5f, minForwardStiffness);
		}
		else
		{
			forwardFrictionCurve.extremumValue = Mathf.Clamp(1f - num, 1f, maxForwardStiffness);
			forwardFrictionCurve.asymptoteValue = Mathf.Clamp(0.75f - num / 2f, 1.2f, minForwardStiffness);
		}
		if (wheelCollider == carController.FrontLeftWheelCollider.wheelCollider || wheelCollider == carController.FrontRightWheelCollider.wheelCollider)
		{
			sidewaysFrictionCurve.extremumValue = Mathf.Clamp(1f - num, 0.4f, maxSidewaysStiffness);
			sidewaysFrictionCurve.asymptoteValue = Mathf.Clamp(0.75f - num / 2f, 0.4f, minSidewaysStiffness);
		}
		else
		{
			sidewaysFrictionCurve.extremumValue = Mathf.Clamp(1f - num, 0.375f, maxSidewaysStiffness);
			sidewaysFrictionCurve.asymptoteValue = Mathf.Clamp(0.75f - num / 2f, 0.375f, minSidewaysStiffness);
		}
	}

	private void Audio()
	{
		if (totalSlip > physicsFrictions[groundIndex].slip)
		{
			if (audioSource.clip != audioClip)
			{
				audioSource.clip = audioClip;
			}
			if (!audioSource.isPlaying)
			{
				audioSource.Play();
			}
			if (rigid.velocity.magnitude > 1f)
			{
				audioSource.volume = Mathf.Lerp(0f, audioVolume, totalSlip - 0f);
				audioSource.pitch = Mathf.Lerp(1f, 0.8f, audioSource.volume);
			}
			else
			{
				audioSource.volume = 0f;
			}
		}
		else
		{
			audioSource.volume = 0f;
			if (audioSource.volume <= 0.05f && audioSource.isPlaying)
			{
				audioSource.Stop();
			}
		}
		bumpForce = wheelHit.force - oldForce;
		if (bumpForce >= 5000f)
		{
			RCC_Core.NewAudioSource(RCCSettings.audioMixer, carController.gameObject, "Bump Sound AudioSource", 5f, 50f, (bumpForce - 5000f) / 3000f, RCCSettingsInstance.bumpClip, loop: false, playNow: true, destroyAfterFinished: true).pitch = UnityEngine.Random.Range(0.9f, 1.1f);
		}
		oldForce = wheelHit.force;
	}

	private bool isSkidding()
	{
		for (int i = 0; i < allWheelColliders.Count; i++)
		{
			if (allWheelColliders[i].totalSlip > physicsFrictions[groundIndex].slip)
			{
				return true;
			}
		}
		return false;
	}

	public void ApplyMotorTorque(float torque)
	{
		if (carController.TCS)
		{
			if (Mathf.Abs(wheelCollider.rpm) >= 100f)
			{
				if (wheelHit.forwardSlip > physicsFrictions[groundIndex].slip)
				{
					carController.TCSAct = true;
					torque -= Mathf.Clamp(torque * wheelHit.forwardSlip * carController.TCSStrength, 0f, float.PositiveInfinity);
				}
				else
				{
					carController.TCSAct = false;
					torque += Mathf.Clamp(torque * wheelHit.forwardSlip * carController.TCSStrength, float.NegativeInfinity, 0f);
				}
			}
			else
			{
				carController.TCSAct = false;
			}
		}
		if (OverTorque())
		{
			torque = 0f;
		}
		if (Mathf.Abs(torque) > 0.15f)
		{
			wheelCollider.motorTorque = torque;
		}
		else
		{
			wheelCollider.motorTorque = 0f;
		}
	}

	public void ApplySteering(float steerInput, float angle)
	{
		if (steerInput > 0f)
		{
			if (transform.localPosition.x < 0f)
			{
				wheelCollider.steerAngle = (float)Math.PI / 180f * angle * 2.55f * (57.29578f * Mathf.Atan(17f / 45f) * steerInput);
			}
			else
			{
				wheelCollider.steerAngle = (float)Math.PI / 180f * angle * 2.55f * (57.29578f * Mathf.Atan(17f / 35f) * steerInput);
			}
		}
		else if (steerInput < 0f)
		{
			if (transform.localPosition.x < 0f)
			{
				wheelCollider.steerAngle = (float)Math.PI / 180f * angle * 2.55f * (57.29578f * Mathf.Atan(17f / 35f) * steerInput);
			}
			else
			{
				wheelCollider.steerAngle = (float)Math.PI / 180f * angle * 2.55f * (57.29578f * Mathf.Atan(17f / 45f) * steerInput);
			}
		}
		else
		{
			wheelCollider.steerAngle = 0f;
		}
		if (transform.localPosition.x < 0f)
		{
			wheelCollider.steerAngle += toe + damagedToe;
		}
		else
		{
			wheelCollider.steerAngle -= toe + damagedToe;
		}
	}

	public void ApplyBrakeTorque(float torque)
	{
		if (carController.ABS && carController.handbrakeInput <= 0.1f)
		{
			if (Mathf.Abs(wheelHit.forwardSlip) * Mathf.Clamp01(torque) >= carController.ABSThreshold)
			{
				carController.ABSAct = true;
				torque = 0f;
			}
			else
			{
				carController.ABSAct = false;
			}
		}
		if (Mathf.Abs(torque) > 0.15f)
		{
			wheelCollider.brakeTorque = torque;
		}
		else
		{
			wheelCollider.brakeTorque = 0f;
		}
	}

	private bool OverTorque()
	{
		if (carController.speed > carController.maxspeed || !carController.engineRunning)
		{
			return true;
		}
		return false;
	}

	private void GetTerrainData()
	{
		if ((bool)Terrain.activeTerrain)
		{
			mTerrainData = Terrain.activeTerrain.terrainData;
			alphamapWidth = mTerrainData.alphamapWidth;
			alphamapHeight = mTerrainData.alphamapHeight;
			mSplatmapData = mTerrainData.GetAlphamaps(0, 0, alphamapWidth, alphamapHeight);
			mNumTextures = mSplatmapData.Length / (alphamapWidth * alphamapHeight);
		}
	}

	private Vector3 ConvertToSplatMapCoordinate(Vector3 playerPos)
	{
		Vector3 result = default;
		Terrain activeTerrain = Terrain.activeTerrain;
		Vector3 position = activeTerrain.transform.position;
		result.x = (playerPos.x - position.x) / activeTerrain.terrainData.size.x * (float)activeTerrain.terrainData.alphamapWidth;
		result.z = (playerPos.z - position.z) / activeTerrain.terrainData.size.z * (float)activeTerrain.terrainData.alphamapHeight;
		return result;
	}

	private int GetGroundMaterialIndex()
	{
		bool flag = false;
		if (wheelHit.point == Vector3.zero)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < physicsFrictions.Length; i++)
		{
			if (wheelHit.collider.sharedMaterial == physicsFrictions[i].groundMaterial)
			{
				flag = true;
				num = i;
			}
		}
		if (!flag)
		{
			for (int j = 0; j < RCCGroundMaterials.terrainFrictions.Length; j++)
			{
				if (!(wheelHit.collider.sharedMaterial == RCCGroundMaterials.terrainFrictions[j].groundMaterial))
				{
					continue;
				}
				Vector3 position = transform.position;
				Vector3 vector = ConvertToSplatMapCoordinate(position);
				float num2 = 0f;
				for (int k = 0; (float)k < mNumTextures; k++)
				{
					if (num2 < mSplatmapData[(int)vector.z, (int)vector.x, k])
					{
						num = k;
					}
				}
				num = RCCGroundMaterialsInstance.terrainFrictions[j].splatmapIndexes[num].index;
			}
		}
		return num;
	}

	public WheelFrictionCurve SetFrictionCurves(WheelFrictionCurve curve, float extremumSlip, float extremumValue, float asymptoteSlip, float asymptoteValue)
	{
		WheelFrictionCurve result = curve;
		result.extremumSlip = extremumSlip;
		result.extremumValue = extremumValue;
		result.asymptoteSlip = asymptoteSlip;
		result.asymptoteValue = asymptoteValue;
		return result;
	}

	private void OnDisable()
	{
		RCC_SceneManager.OnBehaviorChanged -= CheckBehavior;
	}

	private void OnDrawGizmos()
	{
	}
}
