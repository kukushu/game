using System;
using UnityEngine;

namespace EntroPi;

[AddComponentMenu("EntroPi/Cloud Shadows/Cloud Shadows")]
[ExecuteInEditMode]
[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
public class CloudShadows : MonoBehaviour
{
	public enum ProjectMode
	{
		Mode3D,
		Mode2D
	}

	public const string PATH_PREVIEW_IN_EDITOR = "m_PreviewInEditor";

	public const string PATH_PROJECT_MODE = "m_ProjectMode";

	public const string PATH_CLOUD_LAYERS = "m_CloudLayers";

	public const string PATH_WORLD_SIZE = "m_WorldSize";

	public const string PATH_RENDER_TEXTURE_RESOLUTION = "m_RenderTextureResolution";

	public const string PATH_OPACITY_MULTIPLIER = "m_OpacityMultiplier";

	public const string PATH_COVERAGE_MODIFIER = "m_CoverageModifier";

	public const string PATH_SOFTNESS_MODIFIER = "m_SoftnessModifier";

	public const string PATH_SPEED_MULTIPLIER = "m_SpeedMultiplier";

	public const string PATH_DIRECTION_MODIFIER = "m_DirectionModifier";

	public const string PATH_HORIZON_ANGLE_THRESHOLD = "m_HorizonAngleThreshold";

	public const string PATH_HORIZON_ANGLE_FADE = "m_HorizonAngleFade";

	private const string SHADER_PATH_CLOUD_SHADOWS = "Shaders/CloudShadows";

	private const float WORLD_SIZE_MIN = 1f;

	private const float ROTATION_MODIFIER_LIMIT = 180f;

	private const float HORIZON_ANGLE_MAX = 90f;

	private const float HORIZON_ANGLE_FADE_MIN = 0.1f;

	[Tooltip("Project Mode")]
	[SerializeField]
	private ProjectMode m_ProjectMode;

	[Tooltip("Asset containing Cloud Layers")]
	[SerializeField]
	private CloudLayers m_CloudLayers;

	[Tooltip("Size in world units that the cloud layer textures will be projected on")]
	[SerializeField]
	private float m_WorldSize = 100f;

	[Tooltip("Resolution of texture used to render layers into")]
	[SerializeField]
	private int m_RenderTextureResolution = 1024;

	[Tooltip("Multiplier which influences the opacity of all cloud layers")]
	[SerializeField]
	[Range(0f, 1f)]
	private float m_OpacityMultiplier = 1f;

	[Tooltip("Modifies the coverage of all cloud layers")]
	[SerializeField]
	[Range(-1f, 1f)]
	private float m_CoverageModifier;

	[Tooltip("Modifies the softness of all cloud layers")]
	[SerializeField]
	[Range(-1f, 1f)]
	private float m_SoftnessModifier;

	[Tooltip("Multiplier which influences the speed of all cloud layers")]
	[SerializeField]
	private float m_SpeedMultiplier = 1f;

	[Tooltip("Modifies the direction of all cloud layers")]
	[Range(-180f, 180f)]
	[SerializeField]
	private float m_DirectionModifier;

	[Tooltip("The angle from the horizon at which the cloud shadows fade out completely")]
	[SerializeField]
	[Range(0f, 90f)]
	private float m_HorizonAngleThreshold = 10f;

	[Tooltip("The angle from the horizon over which the cloud shadows fade out")]
	[SerializeField]
	[Range(0.1f, 90f)]
	private float m_HorizonAngleFade = 10f;

	private Light m_Light;

	private Material m_CloudShadowMaterial;

	private RenderTexture m_RenderTexture1;

	private RenderTexture m_RenderTexture2;

	private int m_LayerTextureID;

	private int m_LayerTextureTransformID;

	private int m_LayerParamsID;

	private int m_LayerOpacityID;

	public ProjectMode Mode
	{
		get
		{
			return m_ProjectMode;
		}
		set
		{
			m_ProjectMode = value;
		}
	}

	public CloudLayers CloudLayers
	{
		get
		{
			return m_CloudLayers;
		}
		set
		{
			m_CloudLayers = value;
		}
	}

	public float WorldSize
	{
		get
		{
			return m_WorldSize;
		}
		set
		{
			m_WorldSize = Mathf.Max(value, 1f);
		}
	}

	public int RenderTextureResolution
	{
		get
		{
			return m_RenderTextureResolution;
		}
		set
		{
			m_RenderTextureResolution = value;
			UpdateRenderTextureResolution();
		}
	}

	public float OpacityMultiplier
	{
		get
		{
			return m_OpacityMultiplier;
		}
		set
		{
			m_OpacityMultiplier = Mathf.Clamp01(value);
		}
	}

	public float CoverageModifier
	{
		get
		{
			return m_CoverageModifier;
		}
		set
		{
			m_CoverageModifier = Mathf.Clamp(value, -1f, 1f);
		}
	}

	public float SoftnessModifier
	{
		get
		{
			return m_SoftnessModifier;
		}
		set
		{
			m_SoftnessModifier = Mathf.Clamp(value, -1f, 1f);
		}
	}

	public float SpeedMultiplier
	{
		get
		{
			return m_SpeedMultiplier;
		}
		set
		{
			m_SpeedMultiplier = value;
		}
	}

	public float DirectionModifier
	{
		get
		{
			return m_DirectionModifier;
		}
		set
		{
			m_DirectionModifier = Mathf.Clamp(value, -180f, 180f);
		}
	}

	public float HorizonAngleThreshold
	{
		get
		{
			return m_HorizonAngleThreshold;
		}
		set
		{
			m_HorizonAngleThreshold = Mathf.Clamp(value, 0f, 90f);
		}
	}

	public float HorizonAngleFade
	{
		get
		{
			return m_HorizonAngleFade;
		}
		set
		{
			m_HorizonAngleFade = Mathf.Clamp(value, 0.1f, 90f);
		}
	}

	public RenderTexture RenderTexture => m_RenderTexture1;

	private void OnValidate()
	{
		m_WorldSize = Mathf.Max(m_WorldSize, 1f);
		m_RenderTextureResolution = Mathf.Clamp(m_RenderTextureResolution, 2, 4096);
	}

	private void OnEnable()
	{
		m_Light = GetComponent<Light>();
		enabled &= Debug.Verify(m_Light.type == LightType.Directional, "Light type needs to be directional");
	}

	private void OnDisable()
	{
		DisableEffect();
	}

	private void Start()
	{
		m_LayerTextureID = Shader.PropertyToID("_LayerTex");
		m_LayerTextureTransformID = Shader.PropertyToID("_LayerTex_ST");
		m_LayerParamsID = Shader.PropertyToID("_LayerParams");
		m_LayerOpacityID = Shader.PropertyToID("_LayerOpacity");
		m_RenderTexture1 = RenderTextureUtil.CreateRenderTexture(m_RenderTextureResolution);
		m_RenderTexture2 = RenderTextureUtil.CreateRenderTexture(m_RenderTextureResolution);
		m_CloudShadowMaterial = ResourceUtil.CreateMaterial("Shaders/CloudShadows");
		UpdateLightProperties();
	}

	private void Update()
	{
		RenderCloudShadows();
	}

	private void UpdateRenderTextureResolution()
	{
		if (m_RenderTexture1 != null && m_RenderTexture1.width != m_RenderTextureResolution)
		{
			RenderTextureUtil.RecreateRenderTexture(ref m_RenderTexture1, m_RenderTextureResolution);
			RenderTextureUtil.RecreateRenderTexture(ref m_RenderTexture2, m_RenderTextureResolution);
		}
	}

	private void RenderCloudShadows()
	{
		RenderTextureUtil.ClearRenderTexture(m_RenderTexture1, new Color(0f, 0f, 0f, 1f));
		if (m_CloudLayers != null && m_OpacityMultiplier > 0f)
		{
			float angleToHorizon = Vector3.Angle(Vector3.up, transform.forward) - 90f;
			for (int i = 0; i < m_CloudLayers.LayerCount; i++)
			{
				CloudLayerData cloudLayerData = m_CloudLayers[i];
				UpdateCloudLayerDataAnimationOffset(cloudLayerData, m_WorldSize, m_SpeedMultiplier, m_DirectionModifier);
				if (cloudLayerData.IsVisible)
				{
					m_CloudShadowMaterial.SetTexture(m_LayerTextureID, cloudLayerData.Texture);
					m_CloudShadowMaterial.SetVector(m_LayerTextureTransformID, cloudLayerData.TextureTransform);
					m_CloudShadowMaterial.SetVector(m_LayerParamsID, ExtractCloudLayerParameters(cloudLayerData));
					m_CloudShadowMaterial.SetFloat(m_LayerOpacityID, CalculateCloudLayerOpacity(cloudLayerData, angleToHorizon));
					Graphics.Blit(m_RenderTexture1, m_RenderTexture2, m_CloudShadowMaterial, (int)cloudLayerData.BlendMode);
					RenderTextureUtil.SwapRenderTextures(ref m_RenderTexture1, ref m_RenderTexture2);
				}
			}
		}
		UpdateLightProperties();
	}

	private static void UpdateCloudLayerDataAnimationOffset(CloudLayerData cloudLayerData, float worldSize, float globalSpeedMultiplier, float globalDirectionModifier)
	{
		Vector2 vector = CalculateLayerDirection(cloudLayerData.DirectionAngle, globalDirectionModifier) * cloudLayerData.Speed * globalSpeedMultiplier * Time.deltaTime;
		vector /= worldSize;
		vector.x *= cloudLayerData.TextureTiling.x;
		vector.y *= cloudLayerData.TextureTiling.y;
		Vector2 animationOffset = cloudLayerData.AnimationOffset;
		animationOffset += vector;
		animationOffset.x = Mathf.Repeat(animationOffset.x, worldSize);
		animationOffset.y = Mathf.Repeat(animationOffset.y, worldSize);
		cloudLayerData.AnimationOffset = animationOffset;
	}

	private static Vector2 CalculateLayerDirection(float cloudLayerDirectionAngle, float globalDirectionModifier)
	{
		float f = (cloudLayerDirectionAngle + globalDirectionModifier) * ((float)Math.PI / 180f);
		Vector2 result = Vector3.zero;
		result.x = Mathf.Cos(f);
		result.y = Mathf.Sin(f);
		return result;
	}

	private Vector4 ExtractCloudLayerParameters(CloudLayerData cloudLayerData)
	{
		float z = Mathf.Clamp01(cloudLayerData.Coverage + m_CoverageModifier);
		float w = Mathf.Clamp01(cloudLayerData.Softness + m_SoftnessModifier);
		return new Vector4(cloudLayerData.AnimationOffset.x, cloudLayerData.AnimationOffset.y, z, w);
	}

	private float CalculateCloudLayerOpacity(CloudLayerData cloudLayerData, float angleToHorizon)
	{
		float num = 1f;
		if (m_ProjectMode == ProjectMode.Mode3D)
		{
			num = Mathf.Clamp01((angleToHorizon - m_HorizonAngleThreshold) / m_HorizonAngleFade);
		}
		num *= cloudLayerData.Opacity;
		return num * m_OpacityMultiplier;
	}

	private void UpdateLightProperties()
	{
		m_Light.cookie = m_RenderTexture1;
		m_Light.cookieSize = m_WorldSize;
	}

	private void DisableEffect()
	{
		if (m_RenderTexture1 != null && m_RenderTexture2 != null)
		{
			m_RenderTexture1.Release();
			m_RenderTexture2.Release();
		}
		m_Light.cookie = null;
	}
}
