using UnityEngine;

public class RTS_Camera : MonoBehaviour
{
	private string horizontalAxisName;

	private string verticalAxisName;

	public float keyboardMovementSpeed;

	public float mouseMovementSpeed;

	public bool edgeInput_bool;

	public bool limitMovement_bool;

	public float keyboardZoomingSensity;

	public float scrollwheelSensity;

	private float maxHeight = 250f;

	private float terrainHeight = 5f;

	private float followerMinHeight = 30f;

	private KeyCode ZoomIn = KeyCode.Z;

	private KeyCode ZoomOut = KeyCode.X;

	private KeyCode RotateLeft = KeyCode.Q;

	private KeyCode RotateRight = KeyCode.E;

	private KeyCode RotateUp = KeyCode.R;

	private KeyCode RotateDown = KeyCode.F;

	public float KeyboardRotateSensity;

	public float MouseRotateSensity;

	public GameObject follower;

	public string rotateXAxisName;

	public string rotateYAxisName;

	public Vector3 LookAt;

	public int boundary;

	private Vector3 _moveVector;

	public AudioSource audioSourceHigh;

	public AudioSource audioSourceLow;

	public AudioSource audioSourceOcean;

	public World world;

	public LayerMask groundLayers;

	public LayerMask undergroundLayers;

	private float Rotation;

	private float Tilt;

	private Camera camera;

	public float Distance;

	public float dist;

	public static bool showingUnderground;

	public bool freeze;

	public float ySmooth = 4f;

	public float zSmooth = 6f;

	public float terrainDistance;

	private float oceanVolume;

	private float oceanNow;

	private void Awake()
	{
		follower = GameObject.Find("FollowHelper");
		camera = GetComponent<Camera>();
	}

	private void Start()
	{
		SetAzerty();
		if (world != null)
		{
			Distance = 225f;
			dist = 225f;
			Rotation = 45f;
			LookAt = new Vector3(World.worldSize * 16 / 2, 30f, World.worldSize * 16 / 2);
		}
		else
		{
			Distance = 250f;
			dist = 250f;
			Rotation = 0f;
			LookAt = new Vector3(160f, 30f, 160f);
		}
		Tilt = 45f;
		follower.transform.position = LookAt;
		GetComponent<Camera>().cullingMask = groundLayers;
		float[] array = new float[32];
		array[13] = SettingsStatic.carsDistance;
		array[11] = SettingsStatic.propsDistance;
		array[12] = SettingsStatic.pedsDistance;
		array[15] = SettingsStatic.propsDistance * 2;
		array[16] = SettingsStatic.propsDistance + 50;
		camera.layerCullDistances = array;
		camera.cullingMask &= -513;
		QualitySettings.shadowDistance = 300f;
	}

	public void GoUnderground()
	{
		GetComponent<Camera>().cullingMask = undergroundLayers;
		showingUnderground = true;
	}

	public void GoSurface()
	{
		GetComponent<Camera>().cullingMask = groundLayers;
		showingUnderground = false;
	}

	public void SetAzerty()
	{
		if (SettingsStatic.azerty)
		{
			horizontalAxisName = "HorizontalFR";
			verticalAxisName = "VerticalFR";
			ZoomIn = KeyCode.W;
			ZoomOut = KeyCode.X;
			RotateLeft = KeyCode.A;
			RotateRight = KeyCode.E;
		}
		else
		{
			horizontalAxisName = "Horizontal";
			verticalAxisName = "Vertical";
			ZoomIn = KeyCode.Z;
			ZoomOut = KeyCode.X;
			RotateLeft = KeyCode.Q;
			RotateRight = KeyCode.E;
		}
	}

	private void Update()
	{
		if (freeze)
		{
			return;
		}
		float axisRaw = Input.GetAxisRaw(horizontalAxisName);
		float axisRaw2 = Input.GetAxisRaw(verticalAxisName);
		if (Mathf.Abs(axisRaw) > Mathf.Epsilon || Mathf.Abs(axisRaw2) > Mathf.Epsilon)
		{
			float num = 1f + (transform.position.y - terrainHeight);
			if (num < 0.5f)
			{
				num = 0.5f;
			}
			Vector3 normalized = new Vector3(axisRaw, 0f, axisRaw2).normalized;
			normalized = num * keyboardMovementSpeed * normalized;
			LookAt += Quaternion.Euler(0f, Rotation, 0f) * normalized;
			BlockInteraction.following = false;
		}
		float num2 = 5 + (int)(transform.position.y - terrainHeight) * 2;
		if (num2 < 5f)
		{
			num2 = 5f;
		}
		float num3 = Input.GetAxis("Mouse ScrollWheel") * scrollwheelSensity * num2;
		if (num3 > 0f && Distance > 1.5f)
		{
			dist = Distance - num3;
		}
		else if (num3 < 0f && Distance < maxHeight)
		{
			dist = Distance - num3;
		}
		if (dist < 1.5f)
		{
			dist = 1.5f;
		}
		if (Input.GetKey(ZoomIn) || Input.GetKey(KeyCode.KeypadPlus))
		{
			float num4 = keyboardZoomingSensity * Time.unscaledDeltaTime;
			dist = Distance - num4;
		}
		if ((Input.GetKey(ZoomOut) && Distance < maxHeight) || (Input.GetKey(KeyCode.KeypadMinus) && Distance < maxHeight))
		{
			float num5 = keyboardZoomingSensity * Time.unscaledDeltaTime;
			dist = Distance + num5;
		}
		if (Input.GetKey(RotateRight))
		{
			Rotation += KeyboardRotateSensity;
		}
		if (Input.GetKey(RotateLeft))
		{
			Rotation -= KeyboardRotateSensity;
		}
		if (Input.GetKey(RotateUp))
		{
			Tilt += KeyboardRotateSensity;
		}
		if (Input.GetKey(RotateDown))
		{
			Tilt -= KeyboardRotateSensity;
		}
		Distance = Mathf.Lerp(Distance, dist, 4f * Time.unscaledDeltaTime);
		Quaternion quaternion = Quaternion.Euler(Tilt, Rotation, 0f);
		Vector3 vector = new Vector3(0f, 0f, 0f - Distance);
		Vector3 position = quaternion * vector + follower.transform.position;
		transform.rotation = quaternion;
		follower.transform.rotation = quaternion;
		transform.position = position;
		if (Input.GetKey(KeyCode.Mouse1))
		{
			float num6 = 1 + (int)(transform.position.y - terrainHeight);
			if (num6 < 1f)
			{
				num6 = 1f;
			}
			float axis = Input.GetAxis("Mouse X");
			float axis2 = Input.GetAxis("Mouse Y");
			Vector3 normalized2 = new Vector3(axis, 0f, axis2).normalized;
			normalized2 = num6 * mouseMovementSpeed * normalized2 / 150f;
			LookAt += Quaternion.Euler(0f, Rotation, 0f) * normalized2;
		}
		if (Input.GetKey(KeyCode.Mouse2) || (Input.GetKey(KeyCode.Mouse0) && !Indexer.sliderOnScreen && Indexer.Index == 0))
		{
			float axisRaw3 = Input.GetAxisRaw(rotateXAxisName);
			float axisRaw4 = Input.GetAxisRaw(rotateYAxisName);
			Rotation += axisRaw3 * MouseRotateSensity;
			Tilt -= axisRaw4 * MouseRotateSensity;
			Quaternion quaternion2 = Quaternion.Euler(Tilt, Rotation, 0f);
			Vector3 vector2 = new Vector3(0f, 0f, 0f - Distance);
			Vector3 position2 = quaternion2 * vector2 + follower.transform.position;
			transform.rotation = quaternion2;
			transform.position = position2;
		}
		if (edgeInput_bool)
		{
			if (Input.mousePosition.y > (float)(Screen.height - 2))
			{
				_moveVector += new Vector3(0f, 0f, mouseMovementSpeed / 7f);
			}
			else if (Input.mousePosition.y < 2f)
			{
				_moveVector += new Vector3(0f, 0f, -1f * mouseMovementSpeed / 7f);
			}
			if (Input.mousePosition.x > (float)(Screen.width - 2))
			{
				_moveVector += new Vector3(mouseMovementSpeed / 7f, 0f, 0f);
			}
			else if (Input.mousePosition.x < 2f)
			{
				_moveVector += new Vector3((0f - mouseMovementSpeed) / 7f, 0f, 0f);
			}
			LookAt += Quaternion.Euler(0f, Rotation, 0f) * _moveVector;
			follower.transform.position = Vector3.Lerp(follower.transform.position, LookAt, mouseMovementSpeed / 4f * Time.unscaledDeltaTime);
			_moveVector = Vector3.zero;
		}
		LookAt.y = Mathf.Lerp(LookAt.y, followerMinHeight, ySmooth * Time.unscaledDeltaTime);
		follower.transform.position = Vector3.Lerp(follower.transform.position, LookAt, zSmooth * Time.unscaledDeltaTime);
		if (limitMovement_bool)
		{
			LookAt = new Vector3(Mathf.Clamp(LookAt.x, -10f, boundary), LookAt.y, Mathf.Clamp(LookAt.z, -10f, boundary));
			follower.transform.position = new Vector3(Mathf.Clamp(follower.transform.position.x, -10f, boundary), follower.transform.position.y, Mathf.Clamp(follower.transform.position.z, -10f, boundary));
		}
		if (world != null)
		{
			AdjustShadowAndSounds();
		}
		else
		{
			followerMinHeight = 45f;
			terrainHeight = 30f;
			QualitySettings.shadowDistance = 350f;
		}
		if (transform.position.y - terrainHeight < 0f)
		{
			transform.position = new Vector3(transform.position.x, terrainHeight, transform.position.z);
		}
	}

	private void AdjustShadowAndSounds()
	{
		terrainHeight = world.GetBounded((int)transform.position.x, (int)transform.position.z).h + 1f;
		followerMinHeight = world.GetBounded((int)follower.transform.position.x, (int)follower.transform.position.z).h + 0.01f;
		if (followerMinHeight < 30.5f)
		{
			followerMinHeight = 30.5f;
		}
		if (terrainHeight < 30.75f)
		{
			oceanVolume = 1.2f * SettingsStatic.NatureVolume;
		}
		else
		{
			oceanVolume = 0f;
		}
		oceanNow = Mathf.Lerp(oceanNow, oceanVolume, Time.unscaledDeltaTime * 1f);
		terrainDistance = Mathf.Clamp(transform.position.y - terrainHeight, 0f, 120f);
		audioSourceHigh.volume = (terrainDistance - 30f) / 130f * SettingsStatic.NatureVolume;
		float num = (40f - terrainDistance) / 70f * SettingsStatic.NatureVolume;
		audioSourceLow.volume = num - oceanNow;
		audioSourceOcean.volume = num - (1f - oceanNow);
	}

	public void BlockCamera()
	{
		freeze = true;
		edgeInput_bool = false;
	}

	public void UnblockCamera()
	{
		freeze = false;
		edgeInput_bool = SettingsStatic.edgeScroll;
	}

	public void UpdateSettings()
	{
		GetComponent<Camera>().cullingMask = groundLayers;
		float[] array = new float[32];
		array[13] = SettingsStatic.carsDistance;
		array[11] = SettingsStatic.propsDistance;
		array[12] = SettingsStatic.pedsDistance;
		array[15] = SettingsStatic.propsDistance * 2;
		array[16] = SettingsStatic.propsDistance + 50;
		camera.layerCullDistances = array;
	}
}
