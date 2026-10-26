using UnityEngine;

public class UnluckAnimatedMesh : MonoBehaviour
{
	public MeshFilter[] meshCache;

	[HideInInspector]
	public Transform meshCached;

	public Transform meshContainerFBX;

	public float playSpeed = 1f;

	public float playSpeedRandom;

	public bool randomSpeedLoop;

	private float currentSpeed;

	[HideInInspector]
	public float currentFrame;

	[HideInInspector]
	public int meshCacheCount;

	[HideInInspector]
	public MeshFilter meshFilter;

	[HideInInspector]
	public Renderer rendererComponent;

	public float updateInterval = 0.05f;

	public bool randomRotateX;

	public bool randomRotateY;

	public bool randomRotateZ;

	public bool randomStartFrame = true;

	public bool randomRotateLoop;

	public bool loop = true;

	public bool pingPong;

	public bool playOnAwake = true;

	public Vector2 randomStartDelay = new Vector2(0f, 0f);

	private float startDelay;

	private float startDelayCounter;

	public static float updateSeed;

	private bool pingPongToggle;

	public Transform transformCache;

	public float delta;

	public void Start()
	{
		transformCache = transform;
		CheckIfMeshHasChanged();
		startDelay = Random.Range(randomStartDelay.x, randomStartDelay.y);
		updateSeed += 0.0005f;
		if (playOnAwake)
		{
			Invoke("Play", updateInterval + updateSeed);
		}
		if (updateSeed >= updateInterval)
		{
			updateSeed = 0f;
		}
		if (rendererComponent == null)
		{
			GetRequiredComponents();
		}
	}

	public void Play()
	{
		CancelInvoke();
		if (randomStartFrame)
		{
			currentFrame = (float)meshCacheCount * Random.value;
		}
		else
		{
			currentFrame = 0f;
		}
		meshFilter.sharedMesh = meshCache[(int)currentFrame].sharedMesh;
		enabled = true;
		RandomizePlaySpeed();
		RandomRotate();
	}

	public void RandomRotate()
	{
		if (randomRotateX)
		{
			Quaternion localRotation = transformCache.localRotation;
			Vector3 eulerAngles = localRotation.eulerAngles;
			eulerAngles.x = Random.Range(0, 360);
			localRotation.eulerAngles = eulerAngles;
			transformCache.localRotation = localRotation;
		}
		if (randomRotateY)
		{
			Quaternion localRotation2 = transformCache.localRotation;
			Vector3 eulerAngles2 = localRotation2.eulerAngles;
			eulerAngles2.y = Random.Range(0, 360);
			localRotation2.eulerAngles = eulerAngles2;
			transformCache.localRotation = localRotation2;
		}
		if (randomRotateZ)
		{
			Quaternion localRotation3 = transformCache.localRotation;
			Vector3 eulerAngles3 = localRotation3.eulerAngles;
			eulerAngles3.z = Random.Range(0, 360);
			localRotation3.eulerAngles = eulerAngles3;
			transformCache.localRotation = localRotation3;
		}
	}

	public void GetRequiredComponents()
	{
		rendererComponent = GetComponent<Renderer>();
	}

	public void RandomizePlaySpeed()
	{
		if (playSpeedRandom > 0f)
		{
			currentSpeed = Random.Range(playSpeed - playSpeedRandom, playSpeed + playSpeedRandom);
		}
		else
		{
			currentSpeed = playSpeed;
		}
	}

	public void FillCacheArray()
	{
		GetRequiredComponents();
		if (transformCache == null)
		{
			transformCache = transform;
		}
		meshFilter = transformCache.GetComponent<MeshFilter>();
		meshCacheCount = meshContainerFBX.childCount;
		meshCached = meshContainerFBX;
		meshCache = new MeshFilter[meshCacheCount];
		for (int i = 0; i < meshCacheCount; i++)
		{
			meshCache[i] = meshContainerFBX.GetChild(i).GetComponent<MeshFilter>();
		}
		currentFrame = (float)meshCacheCount * Random.value;
		meshFilter.sharedMesh = meshCache[(int)currentFrame].sharedMesh;
	}

	public void CheckIfMeshHasChanged()
	{
		if (meshCached != meshContainerFBX && meshContainerFBX != null)
		{
			FillCacheArray();
		}
	}

	public void Update()
	{
		delta = Time.deltaTime;
		startDelayCounter += delta;
		if (startDelayCounter > startDelay)
		{
			rendererComponent.enabled = true;
			Animate();
		}
		if (!enabled)
		{
			rendererComponent.enabled = false;
		}
	}

	public bool PingPongFrame()
	{
		if (pingPongToggle)
		{
			currentFrame += currentSpeed * delta;
		}
		else
		{
			currentFrame -= currentSpeed * delta;
		}
		if (currentFrame <= 0f)
		{
			currentFrame = 0f;
			pingPongToggle = true;
			return true;
		}
		if (currentFrame >= (float)meshCacheCount)
		{
			pingPongToggle = false;
			currentFrame = meshCacheCount - 1;
			return true;
		}
		return false;
	}

	public bool NextFrame()
	{
		currentFrame += currentSpeed * delta;
		if (currentFrame > (float)(meshCacheCount + 1))
		{
			currentFrame = 0f;
			if (!loop)
			{
				enabled = false;
			}
			return true;
		}
		if (currentFrame >= (float)meshCacheCount)
		{
			currentFrame = (float)meshCacheCount - currentFrame;
			if (!loop)
			{
				enabled = false;
			}
			return true;
		}
		return false;
	}

	public void RandomizePropertiesAfterLoop()
	{
		if (randomSpeedLoop)
		{
			RandomizePlaySpeed();
		}
		if (randomRotateLoop)
		{
			RandomRotate();
		}
	}

	public void Animate()
	{
		if (rendererComponent.isVisible)
		{
			if (pingPong && PingPongFrame())
			{
				RandomizePropertiesAfterLoop();
			}
			else if (!pingPong && NextFrame())
			{
				RandomizePropertiesAfterLoop();
			}
			meshFilter.sharedMesh = meshCache[(int)currentFrame].sharedMesh;
		}
	}
}
