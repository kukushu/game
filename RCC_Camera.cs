using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Camera/RCC Camera")]
public class RCC_Camera : MonoBehaviour
{
	public enum CameraMode
	{
		TPS,
		FPS,
		WHEEL,
		FIXED,
		CINEMATIC,
		TOP
	}

	public delegate void onBCGCameraSpawned(GameObject BCGCamera);

	private RCC_Settings RCCSettingsInstance;

	public bool isRendering = true;

	public RCC_CarControllerV3 playerCar;

	private Rigidbody playerRigid;

	private float playerSpeed;

	private Vector3 playerVelocity = new Vector3(0f, 0f, 0f);

	public Camera thisCam;

	public GameObject pivot;

	public CameraMode cameraMode;

	public CameraMode lastCameraMode;

	public bool useTopCameraMode;

	public bool useHoodCameraMode = true;

	public bool useOrbitInTPSCameraMode = true;

	public bool useOrbitInHoodCameraMode = true;

	public bool useWheelCameraMode = true;

	public bool useFixedCameraMode = true;

	public bool useCinematicCameraMode = true;

	public bool useOrthoForTopCamera;

	public bool useOcclusion = true;

	public LayerMask occlusionLayerMask = -1;

	public bool useAutoChangeCamera;

	private float autoChangeCameraTimer;

	public Vector3 topCameraAngle = new Vector3(45f, 45f, 0f);

	private float distanceOffset;

	public float maximumZDistanceOffset = 10f;

	public float topCameraDistance = 100f;

	private Vector3 targetPosition = Vector3.zero;

	private int direction = 1;

	private int lastDirection = 1;

	public float TPSDistance = 6f;

	public float TPSHeight = 2f;

	public float TPSHeightDamping = 10f;

	public float TPSRotationDamping = 5f;

	public float TPSTiltMaximum = 15f;

	public float TPSTiltMultiplier = 2f;

	private float TPSTiltAngle;

	public float TPSYawAngle;

	public float TPSPitchAngle = 7f;

	public float TPSOffsetX;

	public float TPSOffsetY = 0.5f;

	public bool TPSAutoFocus = true;

	public bool TPSAutoReverse = true;

	public bool TPSCollision = true;

	public Vector3 TPSOffset = new Vector3(0f, 0f, 0.25f);

	public Vector3 TPSStartRotation = new Vector3(0f, 0f, 0f);

	internal float targetFieldOfView = 60f;

	public float TPSMinimumFOV = 50f;

	public float TPSMaximumFOV = 70f;

	public float hoodCameraFOV = 60f;

	public float wheelCameraFOV = 60f;

	public float minimumOrtSize = 10f;

	public float maximumOrtSize = 20f;

	internal int cameraSwitchCount;

	private RCC_HoodCamera hoodCam;

	private RCC_WheelCamera wheelCam;

	private RCC_FixedCamera fixedCam;

	private RCC_CinematicCamera cinematicCam;

	private Vector3 collisionVector = Vector3.zero;

	private Vector3 collisionPos = Vector3.zero;

	private Quaternion collisionRot = Quaternion.identity;

	private Quaternion orbitRotation = Quaternion.identity;

	internal float orbitX;

	internal float orbitY;

	public float minOrbitY = -20f;

	public float maxOrbitY = 80f;

	public float orbitXSpeed = 50f;

	public float orbitYSpeed = 50f;

	public float orbitSmooth = 10f;

	public bool orbitReset;

	private float orbitResetTimer;

	private float oldOrbitX;

	public float oldOrbitY;

	private Quaternion currentRotation = Quaternion.identity;

	private Quaternion wantedRotation = Quaternion.identity;

	private float currentHeight;

	private float wantedHeight;

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

	public static event onBCGCameraSpawned OnBCGCameraSpawned;

	private void Awake()
	{
		thisCam = GetComponentInChildren<Camera>();
		GameObject gameObject = new GameObject("Camera Direction");
		gameObject.transform.SetParent(thisCam.transform, worldPositionStays: false);
		gameObject.transform.localPosition = new Vector3(0f, 0f, 10f);
		gameObject.transform.localRotation = Quaternion.identity;
	}

	private void OnEnable()
	{
		if (OnBCGCameraSpawned != null)
		{
			OnBCGCameraSpawned(gameObject);
		}
		RCC_CarControllerV3.OnRCCPlayerCollision += RCC_CarControllerV3_OnRCCPlayerCollision;
	}

	private void RCC_CarControllerV3_OnRCCPlayerCollision(RCC_CarControllerV3 RCC, Collision collision)
	{
		Collision(collision);
	}

	private void GetTarget()
	{
		if ((bool)playerCar)
		{
			if (TPSAutoFocus)
			{
				StartCoroutine(AutoFocus());
			}
			playerRigid = playerCar.GetComponent<Rigidbody>();
			hoodCam = playerCar.GetComponentInChildren<RCC_HoodCamera>();
			wheelCam = playerCar.GetComponentInChildren<RCC_WheelCamera>();
			fixedCam = Object.FindObjectOfType<RCC_FixedCamera>();
			cinematicCam = Object.FindObjectOfType<RCC_CinematicCamera>();
			ResetCamera();
		}
	}

	public void SetTarget(GameObject player)
	{
		playerCar = player.GetComponent<RCC_CarControllerV3>();
		GetTarget();
	}

	public void RemoveTarget()
	{
		transform.SetParent(null);
		playerCar = null;
		playerRigid = null;
	}

	private void Update()
	{
		if (!isRendering)
		{
			if (thisCam.gameObject.activeInHierarchy)
			{
				thisCam.gameObject.SetActive(value: false);
			}
			return;
		}
		if (!thisCam.gameObject.activeInHierarchy)
		{
			thisCam.gameObject.SetActive(value: true);
		}
		if (!playerCar || !playerRigid)
		{
			GetTarget();
			return;
		}
		Inputs();
		playerSpeed = Mathf.Lerp(playerSpeed, playerCar.speed, Time.deltaTime * 5f);
		playerVelocity = playerCar.transform.InverseTransformDirection(playerRigid.velocity);
		thisCam.fieldOfView = Mathf.Lerp(thisCam.fieldOfView, targetFieldOfView, Time.deltaTime * 5f);
	}

	private void LateUpdate()
	{
		if (!playerCar || !playerRigid || !playerCar.gameObject.activeSelf)
		{
			return;
		}
		switch (cameraMode)
		{
		case CameraMode.TPS:
			TPS();
			if (useOrbitInTPSCameraMode)
			{
				ORBIT();
			}
			break;
		case CameraMode.FPS:
			FPS();
			if (useOrbitInHoodCameraMode)
			{
				ORBIT();
			}
			break;
		case CameraMode.WHEEL:
			WHEEL();
			break;
		case CameraMode.FIXED:
			FIXED();
			break;
		case CameraMode.CINEMATIC:
			CINEMATIC();
			break;
		case CameraMode.TOP:
			TOP();
			break;
		}
		if (lastCameraMode != cameraMode)
		{
			ResetCamera();
		}
		lastCameraMode = cameraMode;
		autoChangeCameraTimer += Time.deltaTime;
		if (useAutoChangeCamera && autoChangeCameraTimer > 10f)
		{
			autoChangeCameraTimer = 0f;
			ChangeCamera();
		}
	}

	private void Inputs()
	{
		switch (RCCSettings.selectedControllerType)
		{
		case RCC_Settings.ControllerType.Keyboard:
			if (Input.GetKeyDown(RCCSettings.changeCameraKB))
			{
				ChangeCamera();
			}
			orbitX += Input.GetAxis(RCCSettings.mouseXInput) * orbitXSpeed * 0.02f;
			orbitY -= Input.GetAxis(RCCSettings.mouseYInput) * orbitYSpeed * 0.02f;
			break;
		case RCC_Settings.ControllerType.XBox360One:
			if (Input.GetButtonDown(RCCSettings.Xbox_changeCameraKB))
			{
				ChangeCamera();
			}
			orbitX += Input.GetAxis(RCCSettings.Xbox_mouseXInput) * orbitXSpeed * 0.01f;
			orbitY -= Input.GetAxis(RCCSettings.Xbox_mouseYInput) * orbitYSpeed * 0.01f;
			break;
		case RCC_Settings.ControllerType.PS4:
			if (Input.GetButtonDown(RCCSettings.PS4_changeCameraKB))
			{
				ChangeCamera();
			}
			orbitX += Input.GetAxis(RCCSettings.PS4_mouseXInput) * orbitXSpeed * 0.01f;
			orbitY -= Input.GetAxis(RCCSettings.PS4_mouseYInput) * orbitYSpeed * 0.01f;
			break;
		case RCC_Settings.ControllerType.Mobile:
		case RCC_Settings.ControllerType.LogitechSteeringWheel:
			break;
		}
	}

	public void ChangeCamera()
	{
		cameraSwitchCount++;
		if (cameraSwitchCount >= 6)
		{
			cameraSwitchCount = 0;
		}
		switch (cameraSwitchCount)
		{
		case 0:
			cameraMode = CameraMode.TPS;
			break;
		case 1:
			if (useHoodCameraMode && (bool)hoodCam)
			{
				cameraMode = CameraMode.FPS;
			}
			else
			{
				ChangeCamera();
			}
			break;
		case 2:
			if (useWheelCameraMode && (bool)wheelCam)
			{
				cameraMode = CameraMode.WHEEL;
			}
			else
			{
				ChangeCamera();
			}
			break;
		case 3:
			if (useFixedCameraMode && (bool)fixedCam)
			{
				cameraMode = CameraMode.FIXED;
			}
			else
			{
				ChangeCamera();
			}
			break;
		case 4:
			if (useCinematicCameraMode && (bool)cinematicCam)
			{
				cameraMode = CameraMode.CINEMATIC;
			}
			else
			{
				ChangeCamera();
			}
			break;
		case 5:
			if (useTopCameraMode)
			{
				cameraMode = CameraMode.TOP;
			}
			else
			{
				ChangeCamera();
			}
			break;
		}
	}

	public void ChangeCamera(CameraMode mode)
	{
		cameraMode = mode;
	}

	private void FPS()
	{
		transform.rotation = Quaternion.Lerp(transform.rotation, playerCar.transform.rotation * orbitRotation, Time.deltaTime * 50f);
	}

	private void WHEEL()
	{
		if (useOcclusion && Occluding(playerCar.transform.position))
		{
			ChangeCamera(CameraMode.TPS);
		}
	}

	private void TPS()
	{
		if (lastDirection != playerCar.direction)
		{
			direction = playerCar.direction;
			orbitX = 0f;
			orbitY = 0f;
		}
		lastDirection = playerCar.direction;
		if (TPSAutoReverse)
		{
			wantedRotation = playerCar.transform.rotation * Quaternion.AngleAxis((direction != 1) ? 180 : 0, Vector3.up);
		}
		else
		{
			wantedRotation = playerCar.transform.rotation;
		}
		switch (RCCSettings.selectedControllerType)
		{
		case RCC_Settings.ControllerType.Keyboard:
			if (Input.GetKey(RCCSettings.lookBackKB))
			{
				wantedRotation *= Quaternion.AngleAxis(180f, Vector3.up);
			}
			break;
		case RCC_Settings.ControllerType.XBox360One:
			if (Input.GetButton(RCCSettings.Xbox_lookBackKB))
			{
				wantedRotation *= Quaternion.AngleAxis(180f, Vector3.up);
			}
			break;
		}
		wantedHeight = playerCar.transform.position.y + TPSHeight + TPSOffsetY;
		currentHeight = Mathf.Lerp(currentHeight, wantedHeight, TPSHeightDamping * Time.fixedDeltaTime);
		if (Time.time > 1f)
		{
			currentRotation = Quaternion.Lerp(currentRotation, wantedRotation, 1f - Mathf.Exp((0f - TPSRotationDamping) * Time.deltaTime));
		}
		else
		{
			currentRotation = wantedRotation;
		}
		TPSTiltAngle = Mathf.Lerp(0f, TPSTiltMaximum * Mathf.Clamp(0f - playerVelocity.x, -1f, 1f), Mathf.Abs(playerVelocity.x) / 50f);
		TPSTiltAngle *= TPSTiltMultiplier;
		targetPosition = playerCar.transform.position;
		targetPosition -= currentRotation * orbitRotation * Vector3.forward * TPSDistance;
		targetPosition += Vector3.up * (TPSHeight * Mathf.Lerp(1f, 0.75f, playerRigid.velocity.magnitude * 3.6f / 100f));
		transform.position = targetPosition;
		Vector3 position = playerCar.transform.position;
		if (TPSOffset != Vector3.zero)
		{
			position += playerCar.transform.rotation * TPSOffset;
		}
		transform.LookAt(position);
		transform.position = transform.position + transform.right * TPSOffsetX + transform.up * TPSOffsetY;
		transform.rotation *= Quaternion.Euler(TPSPitchAngle * Mathf.Lerp(1f, 0.75f, playerSpeed / 100f), 0f, Mathf.Clamp(0f - TPSTiltAngle, 0f - TPSTiltMaximum, TPSTiltMaximum) + TPSYawAngle);
		collisionPos = Vector3.Lerp(new Vector3(collisionPos.x, collisionPos.y, collisionPos.z), Vector3.zero, Time.deltaTime * 5f);
		if (Time.deltaTime != 0f)
		{
			collisionRot = Quaternion.Lerp(collisionRot, Quaternion.identity, Time.deltaTime * 5f);
		}
		pivot.transform.localPosition = Vector3.Lerp(pivot.transform.localPosition, collisionPos, Time.deltaTime * 10f);
		pivot.transform.localRotation = Quaternion.Lerp(pivot.transform.localRotation, collisionRot, Time.deltaTime * 10f);
		targetFieldOfView = Mathf.Lerp(TPSMinimumFOV, TPSMaximumFOV, Mathf.Abs(playerSpeed) / 150f);
		if (useOcclusion)
		{
			OccludeRay(playerCar.transform.position);
		}
	}

	private void FIXED()
	{
		if (fixedCam.transform.parent != null)
		{
			fixedCam.transform.SetParent(null);
		}
		if (useOcclusion && Occluding(playerCar.transform.position))
		{
			fixedCam.ChangePosition();
		}
	}

	private void TOP()
	{
		if ((bool)playerCar && (bool)playerRigid)
		{
			thisCam.orthographic = useOrthoForTopCamera;
			distanceOffset = Mathf.Lerp(0f, maximumZDistanceOffset, Mathf.Abs(playerSpeed) / 100f);
			targetFieldOfView = Mathf.Lerp(minimumOrtSize, maximumOrtSize, Mathf.Abs(playerSpeed) / 100f);
			thisCam.orthographicSize = targetFieldOfView;
			targetPosition = playerCar.transform.position;
			targetPosition += playerCar.transform.rotation * Vector3.forward * distanceOffset;
			transform.position = targetPosition;
			transform.rotation = Quaternion.Euler(topCameraAngle);
			pivot.transform.localPosition = new Vector3(0f, 0f, 0f - topCameraDistance);
		}
	}

	private void ORBIT()
	{
		orbitY = Mathf.Clamp(orbitY, minOrbitY, maxOrbitY);
		if (orbitX < -360f)
		{
			orbitX += 360f;
		}
		if (orbitX > 360f)
		{
			orbitX -= 360f;
		}
		orbitRotation = Quaternion.Lerp(orbitRotation, Quaternion.Euler(orbitY, orbitX, 0f), orbitSmooth * Time.deltaTime);
		if (oldOrbitX != orbitX)
		{
			oldOrbitX = orbitX;
			orbitResetTimer = 2f;
		}
		if (oldOrbitY != orbitY)
		{
			oldOrbitY = orbitY;
			orbitResetTimer = 2f;
		}
		if (orbitResetTimer > 0f)
		{
			orbitResetTimer -= Time.deltaTime;
		}
		Mathf.Clamp(orbitResetTimer, 0f, 2f);
		if (orbitReset && playerSpeed >= 25f && orbitResetTimer <= 0f)
		{
			orbitX = 0f;
			orbitY = 0f;
		}
	}

	public void OnDrag(PointerEventData pointerData)
	{
		orbitX += pointerData.delta.x * orbitXSpeed / 1000f;
		orbitY -= pointerData.delta.y * orbitYSpeed / 1000f;
		orbitResetTimer = 0f;
	}

	public void OnDrag(float x, float y)
	{
		orbitX += x * orbitXSpeed / 10f;
		orbitY -= y * orbitYSpeed / 10f;
		orbitResetTimer = 0f;
	}

	private void CINEMATIC()
	{
		if (cinematicCam.transform.parent != null)
		{
			cinematicCam.transform.SetParent(null);
		}
		targetFieldOfView = cinematicCam.targetFOV;
		if (useOcclusion && Occluding(playerCar.transform.position))
		{
			ChangeCamera(CameraMode.TPS);
		}
	}

	public void Collision(Collision collision)
	{
		if (enabled && isRendering && cameraMode == CameraMode.TPS && TPSCollision)
		{
			Vector3 relativeVelocity = collision.relativeVelocity;
			relativeVelocity *= 1f - Mathf.Abs(Vector3.Dot(transform.up, collision.contacts[0].normal));
			float num = Mathf.Abs(Vector3.Dot(collision.contacts[0].normal, relativeVelocity.normalized));
			if (relativeVelocity.magnitude * num >= 5f)
			{
				collisionVector = transform.InverseTransformDirection(relativeVelocity) / 30f;
				collisionPos -= collisionVector * 5f;
				collisionRot = Quaternion.Euler(new Vector3((0f - collisionVector.z) * 10f, (0f - collisionVector.y) * 10f, (0f - collisionVector.x) * 10f));
				targetFieldOfView = thisCam.fieldOfView - Mathf.Clamp(collision.relativeVelocity.magnitude, 0f, 15f);
			}
		}
	}

	private void ResetCamera()
	{
		if ((bool)fixedCam)
		{
			fixedCam.canTrackNow = false;
		}
		TPSTiltAngle = 0f;
		collisionPos = Vector3.zero;
		collisionRot = Quaternion.identity;
		thisCam.transform.localPosition = Vector3.zero;
		thisCam.transform.localRotation = Quaternion.identity;
		pivot.transform.localPosition = collisionPos;
		pivot.transform.localRotation = collisionRot;
		orbitX = TPSStartRotation.y;
		orbitY = TPSStartRotation.x;
		if (TPSStartRotation != Vector3.zero)
		{
			TPSStartRotation = Vector3.zero;
		}
		thisCam.orthographic = false;
		switch (cameraMode)
		{
		case CameraMode.TPS:
			transform.SetParent(null);
			targetFieldOfView = TPSMinimumFOV;
			break;
		case CameraMode.FPS:
			transform.SetParent(hoodCam.transform, worldPositionStays: false);
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			targetFieldOfView = hoodCameraFOV;
			hoodCam.FixShake();
			break;
		case CameraMode.WHEEL:
			transform.SetParent(wheelCam.transform, worldPositionStays: false);
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			targetFieldOfView = wheelCameraFOV;
			break;
		case CameraMode.FIXED:
			transform.SetParent(fixedCam.transform, worldPositionStays: false);
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			targetFieldOfView = 60f;
			fixedCam.canTrackNow = true;
			break;
		case CameraMode.CINEMATIC:
			transform.SetParent(cinematicCam.pivot.transform, worldPositionStays: false);
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			targetFieldOfView = 30f;
			break;
		case CameraMode.TOP:
			transform.SetParent(null);
			targetFieldOfView = minimumOrtSize;
			pivot.transform.localPosition = Vector3.zero;
			pivot.transform.localRotation = Quaternion.identity;
			targetPosition = playerCar.transform.position;
			targetPosition += playerCar.transform.rotation * Vector3.forward * distanceOffset;
			transform.position = playerCar.transform.position;
			break;
		}
	}

	public void ToggleCamera(bool state)
	{
		isRendering = state;
	}

	private void OccludeRay(Vector3 targetFollow)
	{
		RaycastHit hitInfo = default;
		if (Physics.Linecast(targetFollow, transform.position, out hitInfo, occlusionLayerMask) && !hitInfo.collider.isTrigger && !hitInfo.transform.IsChildOf(playerCar.transform))
		{
			Vector3 position = new Vector3(hitInfo.point.x + hitInfo.normal.x * 0.2f, hitInfo.point.y + hitInfo.normal.y * 0.2f, hitInfo.point.z + hitInfo.normal.z * 0.2f);
			transform.position = position;
		}
	}

	private bool Occluding(Vector3 targetFollow)
	{
		RaycastHit hitInfo = default;
		if (Physics.Linecast(targetFollow, transform.position, out hitInfo, ~(1 << LayerMask.NameToLayer(RCCSettings.RCCLayer))) && !hitInfo.collider.isTrigger && !hitInfo.transform.IsChildOf(playerCar.transform))
		{
			return true;
		}
		return false;
	}

	public IEnumerator AutoFocus()
	{
		float timer = 3f;
		while (timer > 0f)
		{
			timer -= Time.deltaTime;
			TPSDistance = Mathf.Lerp(TPSDistance, RCC_GetBounds.MaxBoundsExtent(playerCar.transform) * 2.55f, Time.deltaTime);
			TPSHeight = Mathf.Lerp(TPSHeight, RCC_GetBounds.MaxBoundsExtent(playerCar.transform) * 0.75f, Time.deltaTime);
			yield return null;
		}
	}

	public IEnumerator AutoFocus(Transform bounds)
	{
		float timer = 3f;
		while (timer > 0f)
		{
			timer -= Time.deltaTime;
			TPSDistance = Mathf.Lerp(TPSDistance, RCC_GetBounds.MaxBoundsExtent(bounds) * 2.55f, Time.deltaTime);
			TPSHeight = Mathf.Lerp(TPSHeight, RCC_GetBounds.MaxBoundsExtent(bounds) * 0.75f, Time.deltaTime);
			yield return null;
		}
	}

	public IEnumerator AutoFocus(Transform bounds1, Transform bounds2)
	{
		float timer = 3f;
		while (timer > 0f)
		{
			timer -= Time.deltaTime;
			TPSDistance = Mathf.Lerp(TPSDistance, RCC_GetBounds.MaxBoundsExtent(bounds1) * 2.55f + RCC_GetBounds.MaxBoundsExtent(bounds2) * 2.75f, Time.deltaTime);
			TPSHeight = Mathf.Lerp(TPSHeight, RCC_GetBounds.MaxBoundsExtent(bounds1) * 0.75f + RCC_GetBounds.MaxBoundsExtent(bounds2) * 0.6f, Time.deltaTime);
			yield return null;
		}
	}

	public IEnumerator AutoFocus(Transform bounds1, Transform bounds2, Transform bounds3)
	{
		float timer = 5f;
		while (timer > 0f)
		{
			timer -= Time.deltaTime;
			TPSDistance = Mathf.Lerp(TPSDistance, RCC_GetBounds.MaxBoundsExtent(bounds1) * 2.75f + RCC_GetBounds.MaxBoundsExtent(bounds2) * 2.75f + RCC_GetBounds.MaxBoundsExtent(bounds3) * 2.75f, Time.deltaTime);
			TPSHeight = Mathf.Lerp(TPSHeight, RCC_GetBounds.MaxBoundsExtent(bounds1) * 0.6f + RCC_GetBounds.MaxBoundsExtent(bounds2) * 0.6f + RCC_GetBounds.MaxBoundsExtent(bounds3) * 0.6f, Time.deltaTime);
			yield return null;
		}
	}

	private void OnDisable()
	{
		RCC_CarControllerV3.OnRCCPlayerCollision -= RCC_CarControllerV3_OnRCCPlayerCollision;
	}
}
