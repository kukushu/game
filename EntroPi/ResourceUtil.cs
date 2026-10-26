using UnityEngine;

namespace EntroPi;

public class ResourceUtil
{
	public static Material CreateMaterial(string shaderResource)
	{
		Material material = null;
		Shader shader = LoadShader(shaderResource);
		if (Debug.Verify(CheckShader(shader)))
		{
			material = new Material(shader);
			material.hideFlags = HideFlags.HideAndDontSave;
		}
		return material;
	}

	public static Shader LoadShader(string shaderResource)
	{
		return Resources.Load(shaderResource, typeof(Shader)) as Shader;
	}

	public static bool CheckShader(Shader shader)
	{
		if (shader != null)
		{
			return shader.isSupported;
		}
		return false;
	}
}
