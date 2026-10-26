using UnityEngine;

public class Heightmapper : MonoBehaviour
{
	public static Texture2D Land;

	public static float GetRegionHeight(int x, int z)
	{
		Color pixel = Land.GetPixel(x, z);
		if ((double)pixel.r < 0.038)
		{
			return 26.6f + pixel.r * 64f / 2f;
		}
		if ((double)pixel.r < 0.04)
		{
			return 27.75f;
		}
		return 27.5f + pixel.r * 64f / 6f;
	}

	public static float GetCityHeight(int x, int z)
	{
		return 27.25f + Land.GetPixel(x, z).r * 64f;
	}
}
