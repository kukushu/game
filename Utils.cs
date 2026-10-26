using UnityEngine;

public class Utils
{
	private static int maxHeight = 150;

	private static float smooth = 0.036f;

	private static float resourceSmooth = 0.01f;

	private static int octaves = 6;

	private static float persistence = 0.6f;

	private static int waterOffset = 20;

	private static float seed = 24352f;

	public static float fBM(float x, float z, float frequency)
	{
		float num = 0f;
		float num2 = 1f;
		float num3 = 0f;
		float num4 = seed;
		for (int i = 0; i < octaves; i++)
		{
			num += Mathf.PerlinNoise((x + num4) * frequency, (z + num4) * frequency) * num2;
			num3 += num2;
			num2 *= persistence;
			frequency *= 2f;
		}
		return num / num3;
	}

	public static float GenerateHeight(float x, float z)
	{
		float num = (float)(DataRegion.selectedCity.rX * WorldRegion.chunkSize * WorldRegion.worldSize) + x;
		float num2 = (float)(DataRegion.selectedCity.rZ * WorldRegion.chunkSize * WorldRegion.worldSize) + z;
		return Map(0f, maxHeight - 15, 0f, 1f, fBM(num * smooth, num2 * smooth, 1.33f)) + (float)waterOffset;
	}

	public static float GenerateResource(float x, float z)
	{
		float num = (float)(DataRegion.selectedCity.rX * WorldRegion.chunkSize * WorldRegion.worldSize) + x;
		float num2 = (float)(DataRegion.selectedCity.rZ * WorldRegion.chunkSize * WorldRegion.worldSize) + z;
		return Mathf.Clamp(Map(0f, maxHeight * 2, 0f, 1f, fBM(num * resourceSmooth, num2 * resourceSmooth, 1.5f)) - 80f, 0f, 255f);
	}

	private static float Map(float newmin, float newmax, float origmin, float origmax, float value)
	{
		return Mathf.Lerp(newmin, newmax, Mathf.InverseLerp(origmin, origmax, value));
	}
}
