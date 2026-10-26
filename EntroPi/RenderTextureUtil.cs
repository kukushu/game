using UnityEngine;

namespace EntroPi;

public class RenderTextureUtil
{
	public static RenderTexture CreateRenderTexture(int resolution, int depth = 0, TextureWrapMode wrapMode = TextureWrapMode.Repeat, FilterMode filterMode = FilterMode.Bilinear)
	{
		return new RenderTexture(resolution, resolution, depth)
		{
			wrapMode = wrapMode,
			filterMode = filterMode
		};
	}

	public static void RecreateRenderTexture(ref RenderTexture renderTexture, int resolution)
	{
		renderTexture.Release();
		renderTexture = CreateRenderTexture(resolution);
	}

	public static void ClearRenderTexture(RenderTexture renderTexture, Color clearColor)
	{
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = renderTexture;
		GL.Clear(clearDepth: false, clearColor: true, clearColor);
		RenderTexture.active = active;
	}

	public static void SwapRenderTextures(ref RenderTexture renderTexture1, ref RenderTexture renderTexture2)
	{
		RenderTexture renderTexture3 = renderTexture1;
		renderTexture1 = renderTexture2;
		renderTexture2 = renderTexture3;
	}
}
