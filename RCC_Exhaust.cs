using UnityEngine;

[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Misc/RCC Exhaust")]
public class RCC_Exhaust : RCC_Core
{
	private RCC_Settings RCCSettingsInstance;

	private RCC_CarControllerV3 carController;

	private ParticleSystem particle;

	private ParticleSystem.EmissionModule emission;

	public ParticleSystem flame;

	private ParticleSystem.EmissionModule subEmission;

	private Light flameLight;

	private LensFlare lensFlare;

	public float flareBrightness = 1f;

	private float finalFlareBrightness;

	public float flameTime;

	private AudioSource flameSource;

	public Color flameColor = Color.red;

	public Color boostFlameColor = Color.blue;

	public bool previewFlames;

	public float minEmission = 5f;

	public float maxEmission = 50f;

	public float minSize = 2.5f;

	public float maxSize = 5f;

	public float minSpeed = 0.5f;

	public float maxSpeed = 5f;

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

	private void Start()
	{
		if (RCCSettings.dontUseAnyParticleEffects)
		{
			Object.Destroy(gameObject);
			return;
		}
		carController = GetComponentInParent<RCC_CarControllerV3>();
		particle = GetComponent<ParticleSystem>();
		emission = particle.emission;
		if ((bool)flame)
		{
			subEmission = flame.emission;
			flameLight = flame.GetComponentInChildren<Light>();
			flameSource = RCC_Core.NewAudioSource(RCCSettings.audioMixer, gameObject, "Exhaust Flame AudioSource", 10f, 25f, 0.5f, RCCSettings.exhaustFlameClips[0], loop: false, playNow: false, destroyAfterFinished: false);
			flameLight.renderMode = ((!RCCSettings.useLightsAsVertexLights) ? LightRenderMode.ForcePixel : LightRenderMode.ForceVertex);
		}
		lensFlare = GetComponentInChildren<LensFlare>();
		if ((bool)flameLight && flameLight.flare != null)
		{
			flameLight.flare = null;
		}
	}

	private void Update()
	{
		if ((bool)carController && (bool)particle)
		{
			Smoke();
			Flame();
			if ((bool)lensFlare)
			{
				LensFlare();
			}
		}
	}

	private void Smoke()
	{
		if (carController.engineRunning)
		{
			ParticleSystem.MainModule main = particle.main;
			if (carController.speed < 20f)
			{
				if (!emission.enabled)
				{
					emission.enabled = true;
				}
				if (carController.throttleInput > 0.35f)
				{
					emission.rateOverTime = maxEmission;
					main.startSpeed = maxSpeed;
					main.startSize = maxSize;
				}
				else
				{
					emission.rateOverTime = minEmission;
					main.startSpeed = minSpeed;
					main.startSize = minSize;
				}
			}
			else if (emission.enabled)
			{
				emission.enabled = false;
			}
		}
		else if (emission.enabled)
		{
			emission.enabled = false;
		}
	}

	private void Flame()
	{
		if (carController.engineRunning)
		{
			ParticleSystem.MainModule main = flame.main;
			if (carController.throttleInput >= 0.25f)
			{
				flameTime = 0f;
			}
			if ((carController.useExhaustFlame && carController.engineRPM >= 5000f && carController.engineRPM <= 5500f && carController.throttleInput <= 0.25f && flameTime <= 0.5f) || carController.boostInput >= 0.75f || previewFlames)
			{
				flameTime += Time.deltaTime;
				subEmission.enabled = true;
				if ((bool)flameLight)
				{
					flameLight.intensity = flameSource.pitch * 3f * Random.Range(0.25f, 1f);
				}
				if (carController.boostInput >= 0.75f && (bool)flame)
				{
					main.startColor = boostFlameColor;
					flameLight.color = main.startColor.color;
				}
				else
				{
					main.startColor = flameColor;
					flameLight.color = main.startColor.color;
				}
				if (!flameSource.isPlaying)
				{
					flameSource.clip = RCCSettings.exhaustFlameClips[Random.Range(0, RCCSettings.exhaustFlameClips.Length)];
					flameSource.Play();
				}
			}
			else
			{
				subEmission.enabled = false;
				if ((bool)flameLight)
				{
					flameLight.intensity = 0f;
				}
				if (flameSource.isPlaying)
				{
					flameSource.Stop();
				}
			}
		}
		else
		{
			if (emission.enabled)
			{
				emission.enabled = false;
			}
			subEmission.enabled = false;
			if ((bool)flameLight)
			{
				flameLight.intensity = 0f;
			}
			if (flameSource.isPlaying)
			{
				flameSource.Stop();
			}
		}
	}

	private void LensFlare()
	{
		if ((bool)RCC_SceneManager.Instance.activePlayerCamera)
		{
			float num = Vector3.Distance(transform.position, RCC_SceneManager.Instance.activePlayerCamera.thisCam.transform.position);
			float num2 = Vector3.Angle(transform.forward, RCC_SceneManager.Instance.activePlayerCamera.thisCam.transform.position - transform.position);
			if (num2 != 0f)
			{
				finalFlareBrightness = flareBrightness * (4f / num) * ((100f - 1.11f * num2) / 100f) / 2f;
			}
			lensFlare.brightness = finalFlareBrightness * flameLight.intensity;
			lensFlare.color = flameLight.color;
		}
	}
}
