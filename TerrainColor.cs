using UnityEngine;

public static class TerrainColor
{
	private static Vector3 tropicalW = new Vector3(67f, 192f, 185f);

	private static Vector3 tempW = new Vector3(67f, 192f, 185f);

	private static Vector3 aridW = new Vector3(67f, 192f, 185f);

	private static Vector3 high = new Vector3(185f, 185f, 120f);

	private static Vector3 median = new Vector3(60f, 34f, 9f);

	private static Vector3 low = new Vector3(18f, 62f, 0f);

	private static float lowpoint = 30f;

	private static float medianpoint = 50f;

	private static float highpoint = 90f;

	private static Color32[] colorClass = new Color32[5]
	{
		new Color32(88, 85, 15, byte.MaxValue),
		new Color32(95, 94, 17, byte.MaxValue),
		new Color32(58, 100, 12, byte.MaxValue),
		new Color32(36, 121, 7, byte.MaxValue),
		new Color32(100, 65, 15, byte.MaxValue)
	};

	private static Color32 biomeDefault = new Color32(100, 65, 15, byte.MaxValue);

	private static float sandPoint = 27f;

	private static float red_diff = 0f;

	private static float green_diff = 0f;

	private static float blue_diff = 0f;

	private static float red_step = 0f;

	private static float green_step = 0f;

	private static float blue_step = 0f;

	private static float red_diff2 = 0f;

	private static float green_diff2 = 0f;

	private static float blue_diff2 = 0f;

	private static float red_step2 = 0f;

	private static float green_step2 = 0f;

	private static float blue_step2 = 0f;

	private static Vector3[] highArray = new Vector3[2]
	{
		new Vector3(185f, 185f, 120f),
		new Vector3(116f, 39f, 0f)
	};

	private static Vector3[] medianArray = new Vector3[2]
	{
		new Vector3(60f, 34f, 9f),
		new Vector3(176f, 86f, 27f)
	};

	private static Vector3[] lowArray = new Vector3[2]
	{
		new Vector3(18f, 62f, 0f),
		new Vector3(71f, 108f, 0f)
	};

	public static void ColorWizardry(float latitude)
	{
		latitude = Mathf.Clamp(latitude - 512f, 0f, 2560f) * 1.25f;
		float value = Mathf.Abs(latitude - 1280f) * 1f / 1280f;
		value = Mathf.Clamp(value, 0f, 1f);
		low = lowArray[1] * value + lowArray[0] * (1f - value);
		median = medianArray[1] * value + medianArray[0] * (1f - value);
		high = highArray[1] * value + highArray[0] * (1f - value);
		medianpoint = latitude * 45f / 2560f + 30f;
		red_diff = median.x - low.x;
		green_diff = median.y - low.y;
		blue_diff = median.z - low.z;
		red_step = red_diff / (medianpoint - lowpoint);
		green_step = green_diff / (medianpoint - lowpoint);
		blue_step = blue_diff / (medianpoint - lowpoint);
		red_diff2 = high.x - median.x;
		green_diff2 = high.y - median.y;
		blue_diff2 = high.z - median.z;
		red_step2 = red_diff2 / (highpoint - medianpoint);
		green_step2 = green_diff2 / (highpoint - medianpoint);
		blue_step2 = blue_diff2 / (highpoint - medianpoint);
	}

	public static Color32[] SandColor(float heightPosition, Mesh mesh)
	{
		Color32[] array = new Color32[mesh.vertices.Length];
		Color32 color = default;
		for (int i = 0; i < mesh.vertices.Length; i++)
		{
			float num = Mathf.Clamp((heightPosition - sandPoint) * 90f, 0f, 255f);
			array[i] = color;
			if (heightPosition >= 24f)
			{
				color = new Color32((byte)num, (byte)num, (byte)num, 100);
			}
			else if (heightPosition < 24f)
			{
				color = new Color32(0, 0, 0, 100);
			}
			array[i] = color;
		}
		return array;
	}

	public static Color32[] GrassColor(float heightPosition, Mesh mesh, int lotClass, float biome)
	{
		Color32[] array = new Color32[mesh.vertices.Length];
		Color32 color = default;
		for (int i = 0; i < mesh.vertices.Length; i++)
		{
			switch (lotClass)
			{
			case 2:
				heightPosition = Mathf.Clamp(heightPosition * 0.82f + 1f, 32f, 100f);
				break;
			case 3:
				heightPosition = Mathf.Clamp(heightPosition * 0.6f, 30f, 100f);
				break;
			}
			array[i] = color;
			if (heightPosition < lowpoint)
			{
				color = new Color32((byte)low.x, (byte)low.y, (byte)low.z, byte.MaxValue);
			}
			else if (heightPosition >= lowpoint && heightPosition < medianpoint)
			{
				color = new Color32((byte)(low.x + red_step * (heightPosition - lowpoint)), (byte)(low.y + green_step * (heightPosition - lowpoint)), (byte)(low.z + blue_step * (heightPosition - lowpoint)), byte.MaxValue);
			}
			else
			{
				color = ((!(heightPosition >= medianpoint) || !(heightPosition < highpoint)) ? new Color32((byte)high.x, (byte)high.y, (byte)high.z, byte.MaxValue) : new Color32((byte)(median.x + red_step2 * (heightPosition - medianpoint)), (byte)(median.y + green_step2 * (heightPosition - medianpoint)), (byte)(median.z + blue_step2 * (heightPosition - medianpoint)), byte.MaxValue));
			}
			float num = 0.3f;
			float num2 = 0.65f;
			float num3 = num2 - num;
			if (biome < num)
			{
				biome = num;
			}
			else if (biome > num2)
			{
				biome = num2;
			}
			float num4 = (biome - num) / num3;
			if (num4 > 0.99f)
			{
				num4 = 0.99f;
			}
			float w = 1f - num4;
			array[i] = CombineColors(w, num4, biomeDefault, color);
		}
		return array;
	}

	public static Color CombineColors(float w1, float w2, Color c1, Color c2)
	{
		Vector3 vector = new Vector3(c1.r, c1.g, c1.b);
		Vector3 vector2 = new Vector3(c2.r, c2.g, c2.b);
		Vector3 vector3 = (vector * w1 + vector2 * w2) / 2f;
		if (vector3.x > 1f)
		{
			vector3.x = 1f;
		}
		else if (vector3.x < 0f)
		{
			vector3.x = 0f;
		}
		if (vector3.y > 1f)
		{
			vector3.y = 1f;
		}
		else if (vector3.y < 0f)
		{
			vector3.y = 0f;
		}
		if (vector3.z > 1f)
		{
			vector3.z = 1f;
		}
		else if (vector3.z < 0f)
		{
			vector3.z = 0f;
		}
		return new Color(vector3.x, vector3.y, vector3.z, 1f);
	}
}
